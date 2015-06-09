using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace TiebaMonitor.Kernel
{
    static class Utility
    {
        public static string FormatCookies(CookieCollection cookies)
        {
            if (cookies == null) return null;
            return string.Join("\n", cookies.Cast<object>());
        }
        public static string FormatCookies(CookieContainer cookies, string domain)
        {
            if (cookies == null) return null;
            return FormatCookies(cookies.GetCookies(new Uri(domain)));
        }

        public static long ToUnixDateTime(DateTime d)
        {
            return d.Ticks/10000 - 62135596800000L;
        }

        public static DateTime FromUnixDateTime(long d)
        {
            return new DateTime((d + 62135596800000L)*10000);
        }

        /// <summary>
        /// 将 Uri 格式的 Query 转换为对应的键-值对集合。
        /// </summary>
        public static NameValueCollection ParseUriQuery(string queryString)
        {
            var c = new NameValueCollection();
            if (string.IsNullOrWhiteSpace(queryString)) return c;
            if (queryString[0] == '?') queryString = queryString.Substring(1);
            foreach (var s in queryString.Split('&'))
            {
                var pair = s.Split('=');
                // ReSharper disable once AssignNullToNotNullAttribute
                c.Add(pair[0], pair.Length >= 2 ? pair[1] : null);
            }
            return c;
        }

        ///// <summary>
        ///// 解析 HTML 实体表达式，并返回与之对应的原文本。
        ///// </summary>
        //public static string HtmlDecode(string source)
        //{
        //    if (string.IsNullOrEmpty(source)) return string.Empty;
        //    var builder = new StringBuilder(source);
        //    builder.Replace("&quot;", "\"");
        //    builder.Replace("&apos;", "'");
        //    builder.Replace("&lt;", "<");
        //    builder.Replace("&gt;", ">");
        //    builder.Replace("&amp;", "&");
        //    return builder.ToString();
        //}

        public static JObject FindJsonAssignment(string source, string lhs, bool noException = false)
        {
            //TODO 检查字段内部是否可能出现分号。
            var forumDataMatcher = new Regex(lhs + @"\s=\s(\{.*?\});");
            var result = forumDataMatcher.Match(source);
            if (!result.Success)
            {
                if (noException) return null;
                throw new UnexpectedDataException();
            }
            return JObject.Parse(result.Groups[1].Value);
        }

        public static string StringCollapse(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var builder = new StringBuilder();
            var lastIsWhiteSpace = false;
            foreach (var c in source)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (lastIsWhiteSpace) continue;
                    lastIsWhiteSpace = true;
                    builder.Append(" ");
                }
                else
                {
                    lastIsWhiteSpace = false;
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
