using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Monitor.Baidu.Tieba
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
        /// 在此论坛发帖时，建议的主题前缀。
        /// </summary>
        public IList<string> TopicPrefix { get; private set; }

        private string topicPrefixTime;

        private List<Regex> cachedTopicMatchers = new List<Regex>();

        private static IList<string> emptyStringList = new string[] {};

        #region 数据采集
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
                    var threadTextNode = eachLi.SelectSingleNode(".//div[contains(@class,'threadlist_detail')]/div[contains(@class,'threadlist_text')]");
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
                    yield return new TopicVisitor((long)jo["id"], title,
                        (int)jo["is_good"] != 0, (int)jo["is_top"] != 0,
                        (string)jo["author_name"], preview, (int)jo["reply_num"],
                        replyer, replyTime, this, Parent);
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
            //记录发帖信息。
            var prefixSettings = Utility.Find_ModuleUse(doc.DocumentNode.OuterHtml, @".*?/widget/RichPoster", "prefix");
            /*
杂谈北京{
    prefix: {
        "mode": 1,
        "text": "\u300e06.10\u300f",
        "type": [
            ""
        ],
        "time": "06.10",
        "sug_html": "<div class=\"pprefix-item\">\u300e06.10\u300f<\/div>",
        "value": "\u300e06.10\u300f",
        "need_sug": true
    },
    QinglangData: [
        
    ],
    redirectAfterPost: isGameTab?getNormalTabUrl(): false,
    isPaypost: 0,
    needPaypostAgree: !0
}
}
 */
            var prefixFormat = (string)prefixSettings["text"];
            if (string.IsNullOrWhiteSpace(prefixFormat))
                TopicPrefix = emptyStringList;
            else
            {
                prefixFormat = prefixFormat.Replace((string)prefixSettings["time"], "#time#");
                if (prefixSettings["type"].Type == JTokenType.Array)
                {
                    TopicPrefix =
                        prefixSettings["type"].Select(type => prefixFormat.Replace("#type#", (string) type)).ToArray();
                }
                else
                {
                    //type == ""
                    TopicPrefix = new string[] { prefixFormat };
                }
                JToken jt;
                if (prefixSettings.TryGetValue("time", out jt))
                    topicPrefixTime = (string) jt;
                else
                    topicPrefixTime = null;
            }
            cachedTopicMatchers.Clear();
        }
        #endregion


        public string GetTopicPrefix(int index)
        {
            return TopicPrefix[index].Replace("#time#", topicPrefixTime);
        }

        public string MatchTopicPrefix(string topicTitle)
        {
            if (TopicPrefix.Count == 0) return string.Empty;
            if (cachedTopicMatchers.Count != TopicPrefix.Count)
            {
                foreach (var p in TopicPrefix)
                {
                    //宽松匹配。
                    var exp = Regex.Escape(p).Replace(@"\#time\#", ".*?");
                    cachedTopicMatchers.Add(new Regex(exp));
                }
            }
            for(var i = 0;i < cachedTopicMatchers.Count;i++)
                if (cachedTopicMatchers[i].IsMatch(topicTitle)) return TopicPrefix[i];
            return null;
        }

        /// <summary>
        /// 判断指定的主题名称是否符合当前论坛的前缀要求。
        /// </summary>
        public bool IsTopicPrefixMatch(string topicTitle)
        {
            return MatchTopicPrefix(topicTitle) != null;
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
