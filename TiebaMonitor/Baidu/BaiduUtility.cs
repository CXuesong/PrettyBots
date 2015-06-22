using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace PrettyBots.Visitors.Baidu
{
    public static class BaiduUtility
    {
        public static string TiebaEscape(string sourceText)
        {
            if (string.IsNullOrEmpty(sourceText)) return string.Empty;
            sourceText = HttpUtility.HtmlEncode(sourceText);
            var builder = new StringBuilder();
            //ç»§ç»­æµè¯[lbk]ceshi[rbk]ã123[emotion+pic_type=1+width=30+height=30]http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803[/emotion]123
            foreach (var c in sourceText)
            {
                switch (c)
                {
                    case '[':
                        builder.Append("[lbk]");
                        break;
                    case ']':
                        builder.Append("[rbk]");
                        break;
                    case '\n':
                        builder.Append("[br]");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// 生成 mouse_pwd 参数。
        /// </summary>
        /// <param name="UTSTART">打开页面的时间。</param>
        /// <param name="now">发帖时间。</param>
        internal static string GenerateMousePwd(long UTSTART, long now)
        {
            // g == UTSTART
            /*
            function r() {
                var t = h.ma.length;
                if (t > 0) {
                    var e = h.ma[t - 1];
                    return e[e.length - 1];
                }
            }
            function s() {
                var t = "", e = l.length;
                return e > 10 && (l = l.slice(e - 10)), t = l.join(",");
            }
            function u() {
                if (m)
                    return c;
                var t = [r(), s(), (new Date).getTime() - this.UTSTART, [screen.width, screen.height].join(",")].join("	");
                return c.c = f(t) + "," + g + this.MOUSEPWD_CLICK, m = !0, c;
            }
            function f(t) {
                for (var e = [], n = {}, o = g % 100, i = 0, r = t.length; r > i; i++) {
                    var s = t.charCodeAt(i) ^ o;
                    e.push(s), n[s] || (n[s] = []), n[s].push(i);
                }
                return e;
            }
             */
            var t = string.Format("665,5625\t1,0,1,0,1,0,1,0,1,0\t{0}\t1366,768", now - UTSTART);
            Debug.WriteLine(t);
            var o = UTSTART%100;
            // is_click = 0
            return string.Join(",", t.Select(c => Convert.ToInt32(c) ^ o)) + "," + UTSTART + "0";
        }

        internal static string GenerateMousePwd(DateTime UTSTART, DateTime now)
        {
            return GenerateMousePwd(Utility.ToUnixDateTime(UTSTART), Utility.ToUnixDateTime(now));
        }

        internal static string GenerateMousePwd(DateTime now)
        {
            return GenerateMousePwd(now - TimeSpan.FromMilliseconds(now.Millisecond % 779 + now.Second * (now.Millisecond % 317)), now);
        }
    }
}
