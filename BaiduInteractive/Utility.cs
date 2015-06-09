using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Text;

namespace BaiduInterop.Interactive
{
    public class PrettyParseHtmlOptions
    {
        public static PrettyParseHtmlOptions Default = new PrettyParseHtmlOptions(false);

        public static PrettyParseHtmlOptions DefaultCompact = new PrettyParseHtmlOptions(true);

        public bool Compact { get; set; }

        public bool LoadImages { get; set; }

        public WebClient WebClient { get; set; }

        public int ImageWidth { get; set; }

        public PrettyParseHtmlOptions(bool compact, bool loadImages, WebClient webClient, int imageWidth)
        {
            Compact = compact;
            LoadImages = loadImages;
            WebClient = webClient;
            ImageWidth = imageWidth;
        }

        public PrettyParseHtmlOptions(bool compact)
            : this(compact, false, null, 0)
        { }

    }

    class Utility
    {
        private static string _ApplicationTitle;
        private static string _ProductName;
        private static Version _ProductVersion;

        public static string ApplicationTitle
        {
            get
            {
                if (_ApplicationTitle == null)
                {
                    var titleAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                    _ApplicationTitle = titleAttribute != null ? titleAttribute.Title : "";
                }
                return _ApplicationTitle;
            }
        }

        public static string ProductName
        {
            get
            {
                if (_ProductName == null)
                {
                    var productAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyProductAttribute>();
                    _ProductName = productAttribute != null ? productAttribute.Product : "";
                }
                return _ProductName;
            }
        }

        public static Version ProductVersion
        {
            get
            {
                if (_ProductVersion == null) _ProductVersion = typeof(Utility).Assembly.GetName().Version;
                return _ProductVersion;
            }
        }

        public static string StringEllipsis(string source, int length)
        {
            if (length < 3) throw new ArgumentOutOfRangeException("length");
            if (string.IsNullOrEmpty(source)) return string.Empty;
            if (source.Length <= length) return source;
            return source.Substring(0, length - 3) + "...";
        }

        public static string PadString(string s, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 回退指定的 Url。例如，将 http://abc.def/abc/def 回退为 http://abc.def/abc 。
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string FallbackUrl(string url)
        {
            throw new NotImplementedException();
        }

        public static string PrettyParseHtml(string html)
        {
            return PrettyParseHtml(html, PrettyParseHtmlOptions.Default);
        }

        public static string PrettyParseHtml(string html, PrettyParseHtmlOptions options)
        {
            if (options == null) options = PrettyParseHtmlOptions.Default;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return PrettyParseHtml(doc.DocumentNode, options);
        }

        private static string PrettyParseHtml(HtmlNode node, PrettyParseHtmlOptions opts)
        {
            if (node.NodeType == HtmlNodeType.Comment) return string.Empty;
            if (node.NodeType == HtmlNodeType.Text) return node.InnerText;
            string s = null, pre = null, post = null;
            switch (node.Name)
            {
                case "p":
                    post = "\n";
                    break;
                case "br":
                    return "\n";
                case "img":
                    var src = node.GetAttributeValue("src", "");
                    if (string.IsNullOrWhiteSpace(src)) return "[]";
                    if (opts.Compact) return "[图]";
                    s = "[" + node.GetAttributeValue("src", "") + "]";
                    if (opts.LoadImages)
                    {
                        if (opts.WebClient == null) throw new InvalidOperationException();
                        try
                        {
                            var data = opts.WebClient.DownloadData(src);
                            using (var ms = new MemoryStream(data, false))
                            using (var bmp = new Bitmap(ms))
                                s += "\n" + (AsciiArtGenerator.ConvertToAscii(bmp, opts.ImageWidth, true)) + "\n";
                        }
                        catch (WebException)
                        {
                        }
                        catch (NotSupportedException)
                        {
                        }
                    }
                    return s;
                case "a":
                    var href = node.GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(href))
                        return opts.Compact ?
                            "[" + node.InnerText + "]" 
                            : "[" + href + "|" + node.InnerText + "]";
                    break;
                case "strong":
                    pre = "''";
                    post = "''";
                    break;
            }
            var builder = new StringBuilder(pre);
            var isNewLine = false;
            foreach (var n in node.ChildNodes)
            {
                var ns = PrettyParseHtml(n, opts);
                if (opts.Compact)
                {
                    if (string.IsNullOrWhiteSpace(ns))
                    {
                        if (!isNewLine)
                        {
                            builder.AppendLine();
                            isNewLine = true;
                        }
                    }
                    else
                    {
                        isNewLine = false;
                        builder.Append(ns);
                    }
                }
                else
                {
                    builder.Append(ns);
                }
            }
            builder.Append(post);
            return builder.ToString();
        }
    }
}
