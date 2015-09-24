using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using HtmlAgilityPack;

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

        public const int SubPostMaxContentLength = 280;

        public static int EvalContentLength(string contentCode)
        {
            if (string.IsNullOrEmpty(contentCode)) return 0;
            var isInBrackets = false;
            string bracketText = null;
            var lengthCounter = 0;
            foreach (var c in contentCode)
            {
                switch (c)
                {
                    case '[':
                        if (isInBrackets) throw new ArgumentException();
                        bracketText = null;
                        isInBrackets = true;
                        break;
                    case ']':
                        if (!isInBrackets) throw new ArgumentException();
                        isInBrackets = false;
                        lengthCounter++;
                        break;
                    default:
                        if (isInBrackets)
                        {
                            bracketText += c;
                        }
                        else
                        {
                            if (!isInBrackets && bracketText == "img") continue;
                            var asc = Convert.ToUInt32(c);
                            lengthCounter += asc > 0xFF ? 2 : 1;
                        }
                        break;
                }
            }
            return lengthCounter;
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
            if (hasNewLine) temp = temp.Concat(new[] { "\n" });
            return temp;
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
