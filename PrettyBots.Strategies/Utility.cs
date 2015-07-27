using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;
using HtmlAgilityPack;

namespace PrettyBots.Strategies
{
    internal static class Utility
    {
        /// <summary>
        /// 移除字符串中的符号和空白等内容，并使得大小写一致，便于比对。
        /// </summary>
        public static string NormalizeString(string str)
        {
            return NormalizeString(str, false);
        }

        /// <summary>
        /// 移除字符串中的符号等内容，并使得大小写一致，便于比对。可以选择在分隔的位置插入空格。
        /// </summary>
        public static string NormalizeString(string str, bool keepSeparators)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            var builder = new StringBuilder();
            var lastIsWhitespace = false;
            foreach (var c in str)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) ||
                    char.IsSymbol(c) || char.IsControl(c) ||
                    char.IsSeparator(c))
                    if (keepSeparators)
                    {
                        if (!lastIsWhitespace)
                        {
                            builder.Append(' ');
                            lastIsWhitespace = true;
                        }
                        continue;
                    }
                builder.Append(char.ToUpper(c));
                lastIsWhitespace = false;
            }
            return keepSeparators ? builder.ToString().Trim() : builder.ToString();
        }

        /// <summary>
        /// 筛选指定作者的帖子。
        /// </summary>
        public static IEnumerable<T> OfAuthor<T>(this IEnumerable<T> source, string authorName) where T : PostVisitorBase
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Where(p => string.Compare(p.Author.Name, authorName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// 排除指定作者的帖子。
        /// </summary>
        public static IEnumerable<T> ExcludeAuthor<T>(this IEnumerable<T> source, string authorName) where T : PostVisitorBase
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Where(p => p.Author.Name != authorName);
        }

        /// <summary>
        /// 筛选指定作者的帖子。
        /// </summary>
        public static IEnumerable<T> OfAuthor<T>(this IEnumerable<T> source, UserStub author) where T : PostVisitorBase
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Where(p => p.Author == author);
        }

        /// <summary>
        /// 反向遍历一个主题下面的所有帖子。
        /// </summary>
        public static IEnumerable<PostVisitor> LatestPosts(this TopicVisitor source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var posts = source.Posts;
            var lastPage = posts.PageIndex < posts.PageCount - 1
                ? posts.Navigate(PageRelativeLocation.Last)
                : posts;
            return lastPage.EnumerateToBeginning();
        }

        /// <summary>
        /// 反向遍历一个主题下面指定数目的帖子。
        /// </summary>
        public static IEnumerable<PostVisitor> LatestPosts(this TopicVisitor source, int count)
        {
            return LatestPosts(source).Take(count);
        }

        /// <summary>
        /// 获取或创建指定名称的 XElement。
        /// </summary>
        public static XElement CElement(this XElement element, XName name)
        {
            if (element == null) throw new ArgumentNullException("element");
            var e = element.Element(name);
            if (e == null)
            {
                e = new XElement(name);
                element.Add(e);
            }
            return e;
        }

        private static Dictionary<string, long> fnCache = new Dictionary<string, long>();

        public static long ForumNameToId(string forumName)
        {
            long fid;
            if (fnCache.TryGetValue(forumName, out fid)) return fid;
            var visitor = new BaiduVisitor();
            var f = visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new ArgumentException(string.Format(Prompts.ForumNotExists, forumName));
            fnCache.Add(forumName, f.Id);
            return f.Id;
        }

        /// <summary>
        /// 判断两个用户名是否相等。
        /// </summary>
        public static bool UserIdentity(string name1, string name2)
        {
            return string.Compare(name1, name2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static Random r = new Random();
        //	Based on Java code from wikipedia:
        //	http://en.wikipedia.org/wiki/Fisher-Yates_shuffle
        public static void Shuffle<T>(IList<T> list)
        {
            for (var n = list.Count - 1; n >= 1; n += -1)
            {
                var k = r.Next(n + 1);
                var temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }

        /// <summary>
        /// 清理帖子的 Html 内容，使其便于分析。
        /// </summary>
        public static XElement ParsePostContent(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var root = new XElement("post");
            foreach (var node in doc.DocumentNode.ChildNodes)
                foreach (var xn in ParsePostContentCore(node)) root.Add(xn);
            return root;
        }

        private static IEnumerable ParsePostContentCore(HtmlNode node)
        {
            var hasNewLine = false;
            var e = new XElement("e_root");
            if (node.NodeType == HtmlNodeType.Comment) return Enumerable.Empty<object>();
            if (node.NodeType == HtmlNodeType.Text) return new[] { node.InnerText };
            //if (node.NodeType == HtmlNodeType.Document) goto BUILD_CONTENT;
            switch (node.Name)
            {
                case "p":
                    hasNewLine = true;
                    break;
                case "br":
                    return new[] { "\n" };
                case "img":
                    var src = node.GetAttributeValue("src", "");
                    if (string.IsNullOrWhiteSpace(src)) return Enumerable.Empty<object>();
                    return new[] { new XElement("img", new XAttribute("src", src)) };
                case "a":
                    var className = node.GetAttributeValue("class", "");
                    switch (className.Trim())
                    {
                        case "ps_cb":
                            //去掉根据关键词识别的链接
                            return new[] { node.InnerText };
                        case "at":
                            //@
                            var userName = node.InnerText;
                            if (userName[0] == '@') userName = userName.Substring(1);
                            return new[] { new XElement("at", new XAttribute("user", userName), node.InnerText) };
                        default:
                            if (className.Trim() != "")
                                Debug.Print("New a class : " + node.OuterHtml);
                            var href = node.GetAttributeValue("href", "");
                            Uri tempUri;
                            if (Uri.TryCreate(node.InnerText, UriKind.Absolute, out tempUri))
                                href = node.InnerText;
                            return new[] { new XElement("a", new XAttribute("href", href), node.InnerText) };
                    }
                case "strong":
                    e.Name = "b";
                    break;
            }
            foreach (var n in node.ChildNodes)
                foreach (var xn in ParsePostContentCore(n)) e.Add(xn);
            IEnumerable<object> temp = e.Name == "e_root" ? e.Nodes() : new[] { e };
            if (hasNewLine) temp = temp.Concat(new[] {"\n"});
            return temp;
        }
    }
}
