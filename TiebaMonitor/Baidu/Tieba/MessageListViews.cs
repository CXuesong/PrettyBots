using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    internal enum ReplicationwMode
    {
        RepliedMe = 0,
        ReferedMe
    }

    /// <summary>
    /// 用于枚举回复或提到当前账户的帖子。
    /// </summary>
    public class ReplicationMessageListView : VisitorPageListView<MessagesVisitor, PostStub>
    {
        private readonly ReplicationwMode _Mode;
        private static Regex postUrlMatcher = new Regex(@"/p/(?<T>\d*)?.*pid=(?<P>\d*)(&cid=(?<C>\d*))?");

        protected override async Task OnRefreshPageAsync()
        {
            const string pagerIndexText = "首页";
            const string pagerPrevText = "&lt;上一页";
            const string pagerNextText = "下一页&gt;";
            var modeName = _Mode == ReplicationwMode.RepliedMe ? "replyme" : "atme";
            var doc = new HtmlDocument();
            using (var client = Parent.Session.CreateWebClient())
                doc.LoadHtml(await client.DownloadStringTaskAsync(PageUrl));
            var feedNode = doc.GetElementbyId("feed");
            if (feedNode == null)
            {
                ClaimExistence(false);
                return;
            }
            // TODO .?
            foreach (var eachNode in feedNode.SelectSingleNode("./ul").SelectNodes("./li"))
            {
                /*
<li class="feed_item clearfix feed_replyme j_feed_replyme"><div class="feed_left j_del_ctrl clearfix" data-tuid="18661895">
	<div class="replyme_text clearfix j_replyme">
		<div class="replyme_user"><a href="/p/2036306685?pid=27092548016&amp;cid=71216847397#71216847397" target="_blank">as465620690：</a></div>
		<div class="replyme_content"><a href="/p/2036306685?pid=27092548016&amp;cid=71216847397#71216847397" target="_blank">正解</a></div>
	</div>
	<div class="feed_rich"><!----></div>
	<div class="feed_from">
		回复我的主题：“<a title="求高手解答~~椭圆运动为什么角动量守恒= =" class="itb_thread" href="/p/2036306685" target="_blank">求高手解答~~椭圆运动为什么角动量守恒= =</a>” &gt; <a class="itb_kw" href="/f?kw=%CE%EF%C0%ED" target="_blank">物理吧</a>
	</div>
</div><div class="feed_right j_del_ctrl" data-tuid="18661895">
	<div class="feed_time">07-09 17:16</div>
	<div class="reply icon_reply"><a class="reply_del" onclick="return false;" href="#" data-param='{"is_finf": "1","fid": "7616","pid": "71216847397","tid": "2036306685","tbs":"8c25d957638c532d1436842083","ie":"utf-8","kw":"物理","is_vipdel":1}'>删除</a><a href="/p/2036306685?pid=27092548016&amp;cid=71216847397#71216847397" target="_blank">回复</a></div>
</div></li>
                 */
                var node = eachNode.SelectSingleNode(".//div[@class='" + modeName + "_user']/a");
                if (node == null) throw new UnexpectedDataException();
                var matchResult = postUrlMatcher.Match(node.GetAttributeValue("href", ""));
                if (!matchResult.Success) continue;
                var tid = Convert.ToInt64(matchResult.Groups["T"].Value);
                var pid = Convert.ToInt64(matchResult.Groups["P"].Value);
                var cid = string.IsNullOrEmpty(matchResult.Groups["C"].Value)
                    ? 0
                    : Convert.ToInt64(matchResult.Groups["C"].Value);
                var author = HtmlEntity.DeEntitize(node.InnerText);
                if (author[author.Length - 1] == '：') author = author.Substring(0, author.Length - 1);
                node = eachNode.SelectSingleNode(".//div[@class='" + modeName + "_content']/a");
                var content = node == null ? null : HtmlEntity.DeEntitize(node.InnerText);
                node = eachNode.SelectSingleNode(".//div[@class='feed_time']");
                var time = node == null ? DateTime.MinValue : Utility.ParseDateTime(node.InnerText);
                node = eachNode.SelectSingleNode(".//a[@class='itb_thread']");
                if (node == null) throw new UnexpectedDataException();
                var title = HtmlEntity.DeEntitize(node.InnerText);
                node = eachNode.SelectSingleNode(".//a[@class='itb_kw']");
                if (node == null) throw new UnexpectedDataException();
                var forumName = node.InnerText;
                RegisterNewItem(new PostStub(tid, pid, cid, forumName,
                    title, author, content, time));
            }
            ClaimExistence(true);
            //解析当前页面页码。
            var pagerData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.pager");
            PageIndex = (int) pagerData["cur_page"] - 1;    //以 0 为下标。
            //解析其它页面地址。
            var pagerNode = doc.GetElementbyId("pager");
            if (pagerNode == null)
            {
                PageIndex = 0;
                return;
            }
            PageCount = UnknownPageCount;
            var linkNodes = pagerNode.SelectNodes("./a[@href]");
            bool canMoveNext = false;
            if (linkNodes != null)
            {
                foreach (var linkNode in linkNodes)
                {
                    var href = linkNode.GetAttributeValue("href", "");
                    var thisUrl = TiebaVisitor.TiebaIndexUrl + href;
                    switch (linkNode.InnerText)
                    {
                        case pagerIndexText:
                            RegisterNavigationLocation(PageRelativeLocation.First, thisUrl);
                            break;
                        case pagerPrevText:
                            RegisterNavigationLocation(PageRelativeLocation.Previous, thisUrl);
                            break;
                        case pagerNextText:
                            RegisterNavigationLocation(PageRelativeLocation.Next, thisUrl);
                            canMoveNext = true;
                            break;
                    }
                }
            }
            // 没有“下一页”按钮，表明已经到达最后。
            if (!canMoveNext) PageCount = PageIndex + 1;
        }

        protected override VisitorPageListView<PostStub> PageFactory(string url)
        {
            return new ReplicationMessageListView(Parent, url, _Mode);
        }

        internal ReplicationMessageListView(MessagesVisitor parent, string pageUrl, ReplicationwMode mode)
            : base(parent, pageUrl)
        {
            _Mode = mode;
        }
    }
}
