using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

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
            return (d.Ticks - 621355968000000000L)/10000;
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
    }
}
