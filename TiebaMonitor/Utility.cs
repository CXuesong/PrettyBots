using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Monitor
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
            //TODO 检查字段内部是否可能出现 { 。
            var forumDataMatcher = new Regex(Regex.Escape(lhs) + @"\s*=\s*((?<lb>\{).*?(?<-lb>}))\s*;");
            var result = forumDataMatcher.Match(source);
            if (result.Success) return JObject.Parse(result.Groups[1].Value);
            if (noException) return null;
            throw new UnexpectedDataException();
        }

        /// <summary>
        /// 查找诸如 _.Module.use("common/widget/RichPoster", {...}); 这样的调用。
        /// </summary>
        public static JObject Find_ModuleUse(string source, string moduleName, string subProperty = null, bool noException = false)
        {
            //TODO 检查字段内部是否可能出现 { 。
            var matcher =
                new Regex(@"_.Module.use\(\s*" + "\"" + moduleName + "\"" + @"\s*,\s*((?<lb>\{).*?(?<-lb>\})).*?\);");
            var result = matcher.Match(source);
            if (!result.Success) goto ERR;
            if (!string.IsNullOrEmpty(subProperty))
            {
                //直接搜索子元素。
                matcher = new Regex(subProperty + "\"?" + @"\s*:\s*((?<lb>\{).*?(?<-lb>\}))");
                result = matcher.Match(result.Groups[1].Value);
                if (!result.Success) goto ERR;
            }
            return JObject.Parse(result.Groups[1].Value);
        ERR:
            if (noException) return null;
            throw new UnexpectedDataException();
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

        private static Regex ExtractMetaRedirectionUrl_EntityMatcher = new Regex(@"&#\d*(?!(\d|;))");
        /// <summary>
        /// 从指定的 META http-equiv="refresh" content="x; url=..." 的 content 中提取重定向 URL。
        /// </summary>
        public static string ExtractMetaRedirectionUrl(string contentExpression)
        {
            if (string.IsNullOrWhiteSpace(contentExpression)) return null;
            const string urlSeparator = "URL=";
            var separatorPos = contentExpression.IndexOf(urlSeparator, StringComparison.OrdinalIgnoreCase);
            if (separatorPos < 0) return null;
            //替换不标准的实体引用。
            contentExpression = contentExpression.Substring(separatorPos + urlSeparator.Length);
            contentExpression = ExtractMetaRedirectionUrl_EntityMatcher.Replace(contentExpression, "$0;");
            contentExpression = HtmlEntity.DeEntitize(contentExpression);
            return contentExpression;
        }

        /// <summary>
        /// 从指定的网页文档中查找 META 重定向指令，并返回重定向地址。
        /// </summary>
        /// <returns>解析后的重定向地址。如果没有重定向，则为<c>null</c>。</returns>
        public static string GetRedirectionUrl(HtmlDocument doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            var metaNodes = doc.DocumentNode.SelectNodes("//meta[@http-equiv]");
            if (metaNodes == null) return null;
            var redirectionNode = metaNodes.FirstOrDefault(n1 => string.Compare(n1.GetAttributeValue("http-equiv", ""), "refresh", StringComparison.OrdinalIgnoreCase) == 0);
            if (redirectionNode == null) return null;
            //<meta http-equiv="refresh" content="5; url=....." />
            return ExtractMetaRedirectionUrl(redirectionNode.GetAttributeValue("content", ""));
        }

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
    }
}
