using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class SearchVisitor : ChildVisitor<BaiduVisitor>
    {
        // 贴吧名 / 关键字 / 用户名 / 时间倒序？(1)
        public const string QueryFormat =
            "http://tieba.baidu.com/f/search/res?ie=utf-8&kw={0}&qw={1}&rn=30&un={2}&sm={3}";
        public const string QueryFormat_UserOnly =
            "http://tieba.baidu.com/f/search/ures?ie=utf-8&kw={0}&qw={1}&rn=30&un={2}&sm={3}";

        private SearchResultListView result;

        public string ForumName { get; private set; }

        public string Keyword { get; private set; }

        public string UserName { get; private set; }

        /// <summary>
        /// 是否按时间顺序排列。默认按时间倒序排列。
        /// </summary>
        public bool ReversedOrder { get; private set; }

        /// <summary>
        /// 获取搜索结果集。
        /// </summary>
        public SearchResultListView Result {
            get
            {
                this.Update();
                return result;
            }
        }

        protected override async Task OnFetchDataAsync()
        {
            await result.RefreshAsync();
        }

        internal SearchVisitor(BaiduVisitor root, string keyword, string forumName,
            string userName, bool reversedOrder)
            : base(root)
        {
            ForumName = forumName;
            Keyword = keyword;
            UserName = userName;
            ReversedOrder = reversedOrder;
            //如果是按照关键字或贴吧搜索用户，则应用 ures 而非 res。
            var startupUrl = string.Format(!string.IsNullOrWhiteSpace(userName) &&
                                           (string.IsNullOrWhiteSpace(ForumName) ||
                                            string.IsNullOrWhiteSpace(keyword))
                ? QueryFormat_UserOnly
                : QueryFormat,
                HttpUtility.UrlEncode(ForumName), HttpUtility.UrlEncode(Keyword),
                HttpUtility.UrlEncode(UserName), ReversedOrder ? 0 : 1);
            result = new SearchResultListView(this, startupUrl);
        }
    }

    public class SearchResultListView : VisitorPageListView<SearchVisitor, PostStub>
    {

        private static Regex postUrlMatcher = new Regex(@"/p/(?<T>\d*)?.*pid=(?<P>\d*)(&cid=(?<C>\d*))?");

        protected override async Task OnRefreshPageAsync()
        {
            var doc = new HtmlDocument();
            using (var client = Parent.Session.CreateWebClient())
                doc.LoadHtml(await client.DownloadStringTaskAsync(PageUrl));
            var resultNode = doc.DocumentNode.SelectSingleNode("//div[@class='s_post_list']");
            if (resultNode == null)
            {
                ClaimExistence(false);
                return;
            }
            foreach (var eachNode in resultNode.SelectNodes("./div[@class='s_post']"))
            {
                /*
<div class="s_post">
    <span class="p_title">
        <a class="bluelink" href="/p/3844238408?pid=70283516591&amp;cid=#70283516591" target="_blank">今晚不断网</a>
    </span>
    <div class="p_content">哦耶！</div>
    贴吧：<a class="p_forum" href="/f?kw=%B9%E3%CE%F7%B9%A4%D1%A7%D4%BA%C2%B9%C9%BD%D1%A7%D4%BA" target="_blank">
        <font class="p_violet">广西工学院鹿山学院</font>
    </a>作者：<a href="/i/sys/jump?un=%BB%F0%D0%C7%B0%AE%B0%DF%D2%B6" target="_blank">
        <font class="p_violet">火星爱斑叶</font>
    </a>
    <font class="p_green p_date">2015-06-22 23:34</font>
</div>
                 */
                var titleNode = eachNode.SelectSingleNode("./span[@class='p_title']/a[@href]");
                if (titleNode == null) continue;
                var title = HtmlEntity.DeEntitize(titleNode.InnerText);
                var matchResult = postUrlMatcher.Match(titleNode.GetAttributeValue("href", ""));
                //有可能是推荐贴吧的链接。
                if (!matchResult.Success) continue;
                var node = eachNode.SelectSingleNode("./a[last()]");
                var author = node == null ? null : HtmlEntity.DeEntitize(node.InnerText);
                node = eachNode.SelectSingleNode("./a[last()-1]");
                var forumName = node == null ? null : node.InnerText;
                node = eachNode.SelectSingleNode("./div[@class='p_content']");
                var content = node == null ? null : HtmlEntity.DeEntitize(node.InnerText);
                node = eachNode.SelectSingleNode("./*[contains(@class, 'p_date')]");
                var time = node == null ? DateTime.MinValue : Convert.ToDateTime(node.InnerText);
                var tid = Convert.ToInt64(matchResult.Groups["T"].Value);
                var pid = Convert.ToInt64(matchResult.Groups["P"].Value);
                var cid = string.IsNullOrEmpty(matchResult.Groups["C"].Value)
                    ? 0
                    : Convert.ToInt64(matchResult.Groups["C"].Value);
                RegisterNewItem(new PostStub(tid, pid, cid, forumName,
                    title, author, content, time));
            }
            ClaimExistence(true);
            //解析其它页面地址。
            //不幸的是，根据经验，搜索结果分页对页数的预测是很不准确的。
            var pagerNode = doc.DocumentNode.SelectSingleNode(".//div[@class='pager pager-search']");
            if (pagerNode == null)
            {
                PageIndex = 0;
                return;
            }
            var n = pagerNode.SelectSingleNode("./span[@class='cur']");
            if (n == null)
            {
                PageIndex = 0;
                return;
            }
            PageIndex = Convert.ToInt32(n.InnerText) - 1;   //以 0 为下标。
            PageCount = -1;
            var linkNodes = pagerNode.SelectNodes("./a[@href]");
            string lastPageUrl = null;
            string lastNumNavigatorUrl = null;
            var lastNumNavigator = -1;
            foreach (var linkNode in linkNodes)
            {
                var href = linkNode.GetAttributeValue("href", "");
                var thisUrl = TiebaVisitor.TiebaIndexUrl + href;
                switch (linkNode.GetAttributeValue("class", ""))
                {
                    case "first":
                        RegisterNavigationLocation(PageRelativeLocation.First, thisUrl);
                        break;
                    case "pre":
                        RegisterNavigationLocation(PageRelativeLocation.Previous, thisUrl);
                        break;
                    case "next":
                        RegisterNavigationLocation(PageRelativeLocation.Next, thisUrl);
                        break;
                    case "last":
                        RegisterNavigationLocation(PageRelativeLocation.Last, thisUrl);
                        lastPageUrl = thisUrl;
                        break;
                    default:
                        lastNumNavigatorUrl = thisUrl;
                        lastNumNavigator = Convert.ToInt32(linkNode.InnerText) - 1;
                        RegisterNavigationLocation(lastNumNavigator, thisUrl);
                        break;
                }
            }
            // 没有“尾页”按钮，表明已经到达最后。
            if (lastPageUrl == null) PageCount = PageIndex + 1;
            // 接近最后了，最后一个数字和“尾页”按钮指向的 Url 相同。
            if (lastPageUrl == lastNumNavigatorUrl) PageCount = lastNumNavigator + 1;
        }

        protected override VisitorPageListView<PostStub> PageFactory(string url)
        {
            return new SearchResultListView(Parent, url);
        }

        internal SearchResultListView(SearchVisitor parent, string pageUrl)
            : base(parent, pageUrl)
        { }
    }
}
