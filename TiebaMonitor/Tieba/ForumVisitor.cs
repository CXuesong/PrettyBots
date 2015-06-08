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
                        replyer, replyTime, Parent);
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

    public class TopicVisitor : BaiduChildVisitor
    {
        const string TopicUrlFormat = "http://tieba.baidu.com/p/{0}?ie=utf-8";

        public bool IsExists { get; private set; }

        public long Id { get; private set; }

        public string Title { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public bool IsGood { get; private set; }

        public bool IsTop { get; private set; }

        public int RepliesCount { get; private set; }

        public string LastReplyer { get; private set; }

        public string LastReplyTime { get; private set; }

        public void Update()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(TopicUrlFormat, Id)));
            var errorTextNode = doc.GetElementbyId("errorText");
            if (errorTextNode != null)
            {
                Debug.Print("pid={0}, {1}", Id, errorTextNode.InnerText);
                IsExists = false;
                return;
            }
            var topicData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.thread");
            //PageData.thread = 
            //{ author: "来自草原的雪狼", thread_id: 3369574832, 
            //title: "【珈瑚传奇】猫头鹰王国之战火纷飞", reply_num: 506,
            //thread_type: "0",
            //topic: { is_topic: false, topic_type: false, is_live_post: false,
            //is_lpost: false, lpost_type: 0 }, /*null,*/ is_ad: 0, video_url: "" };
            AuthorName = (string)topicData["author"];
            Id = (long)topicData["thread_id"];
            Title = (string)topicData["title"];
            RepliesCount = (int)topicData["reply_num"];
        }

        public IEnumerable<PostVisitor> Posts()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(TopicUrlFormat, Id)));
            var postListNode = doc.GetElementbyId("j_p_postlist");
            if (postListNode == null) yield break;
            //{"author":{"user_id":355004908,"user_name":"hellodrf","props":null},
            //"content":{"post_id":64616765755,"is_anonym":false,"forum_id":656638,
            //"thread_id":1747016486,"content":"\u5347\u7ea7\uff01\uff01",
            //"post_no":966,"type":"0","comment_num":2,"props":null,"post_index":1}}
            foreach (var eachNode in postListNode.SelectNodes("./div[@data-field]"))
            {
                var pd = JObject.Parse(HtmlEntity.DeEntitize(eachNode.GetAttributeValue("data-field", "")));
                var pdc = pd["content"];
                var id = (long) pdc["post_id"];
                var content = (string) pdc["content"];
                if (content == null)
                {
                    //从HTML获取内容。
                    var contentNode = doc.GetElementbyId("post_content_" + id);
                    if (contentNode == null) throw new UnexpectedDataException();
                    content = contentNode.InnerHtml.Trim();
                }
                yield return new PostVisitor(id, (int) pdc["post_no"],
                    (string) pd["author"]["user_name"], content,
                    (int) pdc["comment_num"], Parent);
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Id, Title, AuthorName, RepliesCount, PreviewText);
        }

        internal TopicVisitor(long id, string title, bool isGood, bool isTop,
            string author, string preview, int repliesCount, string lastReplyer, string lastReplyTime,
            BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
            Title = title;
            IsGood = isGood;
            IsTop = isTop;
            AuthorName = author;
            PreviewText = preview;
            RepliesCount = repliesCount;
            LastReplyer = lastReplyer;
            LastReplyTime = lastReplyTime;
            //默认表示帖子肯定是存在的。
            IsExists = true;
        }
    }

    public class PostVisitor : BaiduChildVisitor
    {

        public long Id { get; private set; }

        public string AuthorName { get; private set; }

        public int Floor { get; private set; }

        public string Content { get; private set; }

        public int CommentsCount { get; private set; }

        public DateTime SubmissionTime { get; private set; }

        internal PostVisitor(long id, int floor, string author, string content, int commentsCount, BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
            AuthorName = author;
            Floor = floor;
            Content = content;
            CommentsCount = commentsCount;
        }
    }
}
