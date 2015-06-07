using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TiebaMonitor.Kernel.Tieba
{
    public class ForumVisitor : BaiduChildVisitor
    {
        public string Name { get; private set; }

        public IEnumerable<TopicVisitor> Topics()
        {
            const string ForumUrlFormat = "http://tieba.baidu.com/f?ie=utf-8&kw={0}&fr=search";
            const string TopicUrlFormat = "http://tieba.baidu.com/{0}";
            string forumPage;
            using (var s = Parent.Session.CreateWebClient())
                forumPage = s.DownloadString(string.Format(ForumUrlFormat, Name));
            var doc = new HtmlDocument();
            doc.LoadHtml(forumPage);
            var node = doc.GetElementbyId("thread_list");
            if (node == null) throw new UnexpectedDataException();
            //Debug.Print(node.InnerHtml);
            var nc = node.SelectNodes("./li[@data-field]");
            if (nc == null) yield break;
            foreach (var eachLi in nc)
            {
                node = eachLi.SelectSingleNode(".//a[@class='j_th_tit']");
                if (node == null) continue;
                var title = node.GetAttributeValue("title", "");
                var href = node.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                node = eachLi.SelectSingleNode(".//div[@class='threadlist_text']");
                var preview = node == null ? null : node.InnerText.Trim();
                var dataFieldStr = Utility.ParseHtmlEntities(eachLi.GetAttributeValue("data-field", ""));
                //Debug.Print(dataFieldStr);
                //{"author_name":"Mark5ds","id":3540683824,"first_post_id":63285795913,
                //"reply_num":1,"is_bakan":0,"vid":"","is_good":0,"is_top":0,"is_protal":0}
                var jo = JObject.Parse(dataFieldStr);
                yield return new TopicVisitor(title,
                    string.Format(TopicUrlFormat, href), (int)jo["is_good"] != 0, (int)jo["is_top"] != 0,
                    (string)jo["author_name"], preview, (int)jo["reply_num"], Parent);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        internal ForumVisitor(string name, BaiduVisitor parent)
            : base(parent)
        {
            Name = name;
        }
    }

    public class TopicVisitor :BaiduChildVisitor
    {
        public string Url { get; private set; }

        public string Name { get; private set; }

        public bool IsGood {get; private set; }

        public bool IsTop { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public int RepliesCount { get; private set; }

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
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Url, Name, AuthorName, RepliesCount, PreviewText);
        }

        internal TopicVisitor(string name, string url, bool isGood, bool isTop,
            string author, string preview, int repliesCount,
            BaiduVisitor parent)
            : base(parent)
        {
            Name = name;
            Url = url;
            IsGood = isGood;
            IsTop = isTop;
            AuthorName = author;
            PreviewText = preview;
            RepliesCount = repliesCount;
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
