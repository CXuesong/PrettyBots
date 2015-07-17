using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies
{
    internal static class Utility
    {
        /// <summary>
        /// 移除字符串中的符号和空白等内容，并使得大小写一致，便于比对。
        /// </summary>
        public static string NormalizeString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            var builder = new StringBuilder();
            foreach (var c in str)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) ||
                    char.IsSymbol(c) || char.IsControl(c) ||
                    char.IsSeparator(c))
                    continue;
                builder.Append(char.ToUpper(c));
            }
            return builder.ToString();
        }

        /// <summary>
        /// 筛选指定作者的帖子。
        /// </summary>
        public static IEnumerable<T> OfAuthor<T>(this IEnumerable<T> source, string authorName) where T : PostVisitorBase
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Where(p => p.Author.Name == authorName);
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

        public static long ForumNameToId(string forumName)
        {
            //TODO 缓存查询结果。
            var visitor = new BaiduVisitor();
            var f = visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new ArgumentException(string.Format(Prompts.ForumNotExists, forumName));
            return f.Id;
        }
    }
}
