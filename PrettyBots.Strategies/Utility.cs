﻿using System;
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
    }
}
