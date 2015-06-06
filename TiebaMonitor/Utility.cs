using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
    }
}
