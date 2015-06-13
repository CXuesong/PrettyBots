using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class SearchVisitor : ChildVisitor<BaiduVisitor>
    {
        // 贴吧名 / 关键字 / 用户名
        public const string QueryFormat =
            "http://tieba.baidu.com/f/search/res?ie=utf-8&kw={0}&qw={1}&rn=30&un={2}&sm=1";

        public string ForumName { get; private set; }

        public string Keyword { get; private set; }

        public string UserName { get; private set; }

        public void Update()
        {

        }

        public IEnumerable<ITextMessageVisitor> Posts()
        {
            using (var c = Parent.Session.CreateWebClient())
            {
                c.Encoding = Encoding.GetEncoding("GBK");
                var currentUrl = string.Format(QueryFormat,
                    HttpUtility.UrlEncode(ForumName), HttpUtility.UrlEncode(Keyword),
                    HttpUtility.UrlEncode(UserName));
                var doc = new HtmlDocument();
                doc.LoadHtml(c.DownloadString(currentUrl));
                var resultNode = doc.DocumentNode.SelectSingleNode("//div[@class='s_post_list']");
                if (resultNode == null) yield break;
                foreach (var eachNode in resultNode.SelectNodes("./div[@class='s_post']"))
                {
                    //<span class="p_title">
                    //<a class="bluelink" href="/p/3817037245?pid=69633913577&amp;cid=0#69633913577" target="_blank"><em>测试</em>小尾巴</a>
                    //</span>
                    var node = eachNode.SelectSingleNode("./span[@class='p_title']/a[@href]");
                    if (node == null) break;
                    var postUrlMatcher = new Regex(@"/p/(\d*)?.*pid=(\d*)");
                    var matchResult = postUrlMatcher.Match(node.GetAttributeValue("href", ""));
                    if (!matchResult.Success) continue;
                    node = eachNode.SelectSingleNode("./a[last()]");
                    var author = node == null ? null : node.InnerText;
                    node = eachNode.SelectSingleNode("./div[@class='p_content']");
                    var content = node == null ? null : node.InnerText;
                    node = eachNode.SelectSingleNode("./*[contains(@class, 'p_date')]");
                    var date = node == null ? DateTime.MinValue : Convert.ToDateTime(node.InnerText);
                    var tid = Convert.ToInt64(matchResult.Groups[1].Value);
                    var pid = Convert.ToInt64(matchResult.Groups[2].Value);
                    yield return new PostVisitor(tid, new TiebaUserStub(author), content, date, tid, Parent);
                }
            }
        }

        internal SearchVisitor(BaiduVisitor parent, string keyword, string forumName, string userName)
            : base(parent)
        {
            ForumName = forumName;
            Keyword = keyword;
            UserName = UserName;
        }
    }
}
