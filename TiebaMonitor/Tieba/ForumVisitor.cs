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
        const string TopicUrlFormat = "http://tieba.baidu.com/{0}";

        public string QueryName { get; private set; }

        public bool IsExists { get; private set; }

        public long Id { get; private set; }

        public string Name { get; private set; }

        public bool IsRedirected { get; private set; }

        public string MemberName { get; private set; }

        public int MembersCount { get; private set; }

        public int TopicsCount { get; private set; }

        public int PostsCount { get; private set; }

        public IEnumerable<TopicVisitor> Topics()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(ForumUrlFormat, QueryName)));
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
            foreach (var eachLi in topicQuery)
            {
                var linkNode = eachLi.SelectSingleNode(".//a[@class='j_th_tit']");
                if (linkNode == null) continue;
                var title = linkNode.GetAttributeValue("title", "");
                var href = linkNode.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                var threadTextNode = eachLi.SelectSingleNode(".//div[@class='threadlist_text']");
                var preview = threadTextNode == null ? null : threadTextNode.InnerText.Trim();
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
                var dataFieldStr = Utility.ParseHtmlEntities(eachLi.GetAttributeValue("data-field", ""));
                //Debug.Print(dataFieldStr);
                //{"author_name":"Mark5ds","id":3540683824,"first_post_id":63285795913,
                //"reply_num":1,"is_bakan":0,"vid":"","is_good":0,"is_top":0,"is_protal":0}
                var jo = JObject.Parse(dataFieldStr);
                yield return new TopicVisitor(title,
                    string.Format(TopicUrlFormat, href), (int)jo["is_good"] != 0, (int)jo["is_top"] != 0,
                    (string)jo["author_name"], preview, (int)jo["reply_num"], replyer, replyTime, Parent);
            }
        }

        public void Update()
        {
            //polysemant-redirect-section
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(ForumUrlFormat, QueryName)));
            var noResultTipNode = doc.DocumentNode.SelectSingleNode("//div[@class='search_noresult']");
            if (noResultTipNode != null)
            {
                //无结果。
                IsExists = false;
            }
            var redirectTipNode = doc.DocumentNode.SelectSingleNode("//div[@class='polysemant-redirect-section']");
            //检查重定向
            IsRedirected = (redirectTipNode != null);
            //TODO 检查字段内部是否可能出现分号。
            var forumDataMatcher = new Regex(@"PageData.forum\s=\s(\{.*?\});");
            var result = forumDataMatcher.Match(doc.DocumentNode.OuterHtml);
            if (!result.Success) throw new UnexpectedDataException();
            var forumData = JObject.Parse(result.Groups[1].Value);
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
        public string Url { get; private set; }

        public string Title { get; private set; }

        public bool IsGood { get; private set; }

        public bool IsTop { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public int RepliesCount { get; private set; }

        public string LastReplyer { get; private set; }

        public string LastReplyTime { get; private set; }

        public IEnumerable<PostVisitor> Posts()
        {
            throw new NotImplementedException();
            string forumPage;
            using (var s = Parent.Session.CreateWebClient())
            {

            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Url, Title, AuthorName, RepliesCount, PreviewText);
        }

        internal TopicVisitor(string name, string url, bool isGood, bool isTop,
            string author, string preview, int repliesCount, string lastReplyer, string lastReplyTime,
            BaiduVisitor parent)
            : base(parent)
        {
            Title = name;
            Url = url;
            IsGood = isGood;
            IsTop = isTop;
            AuthorName = author;
            PreviewText = preview;
            RepliesCount = repliesCount;
            LastReplyer = lastReplyer;
            LastReplyTime = lastReplyTime;
        }
    }

    public class PostVisitor : BaiduChildVisitor
    {
        internal PostVisitor(string id, string content, BaiduVisitor parent)
            : base(parent)
        {
        }
    }
}
