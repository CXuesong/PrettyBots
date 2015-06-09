using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace TiebaMonitor.Kernel.Tieba
{
    public class ForumVisitor : BaiduChildVisitor
    {
        const string ForumUrlFormat = "http://tieba.baidu.com/f?ie=utf-8&kw={0}&fr=search";
        const string ForumUrlFormatPN = "http://tieba.baidu.com/f?ie=utf-8&kw={0}&fr=search&pn={1}";

        public string QueryName { get; private set; }

        public bool IsExists { get; private set; }

        public string QueryResult { get; private set; }

        public long Id { get; private set; }

        public string Name { get; private set; }

        /// <summary>
        /// 是否由于贴吧不存在而发生了重定向。
        /// </summary>
        public bool IsRedirected { get; private set; }

        public string MemberName { get; private set; }

        public int MembersCount { get; private set; }

        public int TopicsCount { get; private set; }

        public int PostsCount { get; private set; }

        /// <summary>
        /// 枚举主题列表。
        /// </summary>
        public IEnumerable<TopicVisitor> Topics()
        {
            using (var s = Parent.Session.CreateWebClient())
            {
                var currentUrl = string.Format(ForumUrlFormat, QueryName);
                var doc = new HtmlDocument();
            PARSE_PAGE:
                doc.LoadHtml(s.DownloadString(currentUrl));
                var topicQuery = Enumerable.Empty<HtmlNode>();
                Action<string> collectTopics = id =>
                {
                    var container = doc.GetElementbyId(id);
                    if (container == null) return;
                    //Debug.Print(container.InnerHtml);
                    var nc = container.SelectNodes("./li[@data-field]");
                    if (nc == null) return;
                    topicQuery = topicQuery.Concat(nc);
                };
                collectTopics("thread_top_list");
                collectTopics("thread_list");
                string href;
                foreach (var eachLi in topicQuery)
                {
                    var linkNode = eachLi.SelectSingleNode(".//a[@class='j_th_tit']");
                    if (linkNode == null) continue;
                    var title = HtmlEntity.DeEntitize(linkNode.GetAttributeValue("title", ""));
                    href = linkNode.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;
                    var threadTextNode = eachLi.SelectSingleNode(".//div[@class='threadlist_text']");
                    var preview = threadTextNode == null ? null : Utility.StringCollapse(HtmlEntity.DeEntitize(threadTextNode.InnerText.Trim()));
                    var threadDetailNode = eachLi.SelectSingleNode(".//div[contains(@class, 'threadlist_detail')]");
                    string replyer = null, replyTime = null;
                    if (threadDetailNode != null)
                    {
                        var replyerNode = threadDetailNode.SelectSingleNode(".//*[contains(@class,'j_replyer')]");
                        var replyTimeNode =
                            threadDetailNode.SelectSingleNode(".//*[contains(@class,'threadlist_reply_date')]");
                        if (replyerNode != null) replyer = replyerNode.InnerText.Trim();
                        if (replyTimeNode != null) replyTime = replyTimeNode.InnerText.Trim();
                    }
                    var dataFieldStr = HtmlEntity.DeEntitize(eachLi.GetAttributeValue("data-field", ""));
                    //Debug.Print(dataFieldStr);
                    //{"author_name":"Mark5ds","id":3540683824,"first_post_id":63285795913,
                    //"reply_num":1,"is_bakan":0,"vid":"","is_good":0,"is_top":0,"is_protal":0}
                    var jo = JObject.Parse(dataFieldStr);
                    yield return new TopicVisitor((long) jo["id"], title,
                        (int) jo["is_good"] != 0, (int) jo["is_top"] != 0,
                        (string) jo["author_name"], preview, (int) jo["reply_num"],
                        replyer, replyTime, this.Id, Parent);
                }
                //解析下一页
                var pagerNode = doc.GetElementbyId("frs_list_pager");
                if (pagerNode == null) yield break;
                var nextPageNode = pagerNode.SelectSingleNode("./a[@class='next']");
                if (nextPageNode == null) yield break;
                href = nextPageNode.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href)) yield break;
                var pnMatcher = new Regex(@"pn=(\d*)");
                var matchResult = pnMatcher.Match(href);
                if (!matchResult.Success) throw new UnexpectedDataException();
                currentUrl = string.Format(ForumUrlFormatPN, QueryName, matchResult.Groups[1].Value);
                Debug.Print("Forum: {0}, Page: {1}", Name, matchResult.Groups[1].Value);
                goto PARSE_PAGE;
            }
        }

        /// <summary>
        /// 更新论坛的当前状态。
        /// </summary>
        public void Update()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(ForumUrlFormat, QueryName)));
            var noResultTipNode =
                doc.GetElementbyId("forum_not_exist")
                ?? doc.DocumentNode.SelectSingleNode("//div[@class='search_noresult']")
                ?? doc.DocumentNode.SelectSingleNode("//div[@class='s_make_bar']");
            if (noResultTipNode != null)
            {
                //无结果。
                QueryResult = noResultTipNode.InnerText.Trim();
                IsExists = false;
                return;
            }
            QueryResult = string.Empty;
            var redirectTipNode = doc.DocumentNode.SelectSingleNode("//div[@class='polysemant-redirect-section']");
            //检查重定向
            IsRedirected = (redirectTipNode != null);
            var forumData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.forum");
            Id = (long)forumData["forum_id"];
            Name = (string)forumData["forum_name"];
            MemberName = (string)forumData["member_name"];
            MembersCount = (int)forumData["member_num"];
            TopicsCount = (int)forumData["thread_num"];
            PostsCount = (int)forumData["post_num"];
            IsExists = true;
        }

        public override string ToString()
        {
            return QueryName;
        }

        internal ForumVisitor(string queryName, BaiduVisitor parent)
            : base(parent)
        {
            QueryName = queryName;
        }
    }
}
