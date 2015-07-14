﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Web;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class ForumVisitor : ChildVisitor<BaiduVisitor>
    {
        private const string ForumUrlFormat = "http://tieba.baidu.com/f?ie=utf-8&kw={0}&fr=search";

        private TopicListView _Topics;

        public string QueryName { get; private set; }

        public bool IsExists { get; private set; }

        /// <summary>
        /// 请求贴吧发生错误时，返回的错误信息。
        /// </summary>
        public string QueryResult { get; private set; }

        public long Id { get; private set; }

        public string Name { get; private set; }

        /// <summary>
        /// 是否由于贴吧不存在而发生了重定向。
        /// </summary>
        public bool IsRedirected { get; private set; }

        public string MemberName { get; private set; }

        public int MembersCount { get; private set; }

        public int TopicsCount { get; private set; }

        public int PostsCount { get; private set; }

        private string PageData_Tbs { get; set; }

        /// <summary>
        /// 获取当前用户的签到位次。
        /// </summary>
        public int? SignInRank { get; set; }

        /// <summary>
        /// 获取一个值，指示用户是否已经签到。
        /// </summary>
        public bool HasSignedIn { get { return SignInRank != null; } }

        /// <summary>
        /// 在此论坛发帖时，建议的主题前缀。
        /// </summary>
        public IList<string> TopicPrefix { get; private set; }

        private string topicPrefixTime;

        private List<Regex> cachedTopicMatchers = new List<Regex>();

        private static IList<string> emptyStringList = new string[] { };

        #region 数据采集

        /// <summary>
        /// 枚举主题列表。
        /// </summary>
        public TopicListView Topics
        {
            get
            {
                _Topics.Refresh();
                return _Topics;
            }
        }

        /// <summary>
        /// 更新论坛的当前状态。
        /// </summary>
        protected override async Task OnFetchDataAsync()
        {
            var doc = new HtmlDocument();
            using (var s = Root.Session.CreateWebClient())
            {
                s.Headers[HttpRequestHeader.Referer] = TiebaVisitor.TiebaIndexUrl;
                var content = await s.DownloadStringTaskAsync(string.Format(ForumUrlFormat, QueryName));
                //顺带刷新主题列表。
                _Topics.SetPageHtmlCache(content);
                doc.LoadHtml(content);
            }
            var noResultTipNode =
                doc.GetElementbyId("forum_not_exist")
                ?? doc.DocumentNode.SelectSingleNode("//div[@class='search_noresult']")
                ?? doc.DocumentNode.SelectSingleNode("//div[@class='s_make_bar']");
            if (noResultTipNode != null)
            {
                //无结果。
                QueryResult = noResultTipNode.InnerText.Trim();
                IsExists = false;
                return;
            }
            QueryResult = string.Empty;
            var redirectTipNode = doc.DocumentNode.SelectSingleNode("//div[@class='polysemant-redirect-section']");
            //检查重定向
            IsRedirected = (redirectTipNode != null);
            var forumData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.forum");
            Id = (long)forumData["forum_id"];
            Name = (string)forumData["forum_name"];
            MemberName = (string)forumData["member_name"];
            MembersCount = (int)forumData["member_num"];
            TopicsCount = (int)forumData["thread_num"];
            PostsCount = (int)forumData["post_num"];
            var userSignInInfo = forumData["sign_in_info"]["user_info"];
            if ((int?) userSignInInfo["is_sign_in"] == null)
                SignInRank = null;  //一般表示用户尚未登录。
            else
                SignInRank = (int)userSignInInfo["is_sign_in"] != 0 ?
                    (int?)userSignInInfo["user_sign_rank"] : null;
            IsExists = true;
            PageData_Tbs = Utility.FindStringAssignment(doc.DocumentNode.OuterHtml, "PageData.tbs");
            //记录发帖前缀信息。
            var prefixSettings = (JObject)Utility.Find_ModuleUse(doc.DocumentNode.OuterHtml, @".*?/widget/RichPoster", "prefix");
            /*
杂谈北京{
    prefix: {
        "mode": 1,
        "text": "\u300e06.10\u300f",
        "type": [
            ""
        ],
        "time": "06.10",
        "sug_html": "<div class=\"pprefix-item\">\u300e06.10\u300f<\/div>",
        "value": "\u300e06.10\u300f",
        "need_sug": true
    },
    QinglangData: [
        
    ],
    redirectAfterPost: isGameTab?getNormalTabUrl(): false,
    isPaypost: 0,
    needPaypostAgree: !0
}
}
 */
            var prefixFormat = (string)prefixSettings["text"];
            if (string.IsNullOrWhiteSpace(prefixFormat))
                TopicPrefix = emptyStringList;
            else
            {
                prefixFormat = prefixFormat.Replace((string)prefixSettings["time"], "#time#");
                if (prefixSettings["type"].Type == JTokenType.Array)
                {
                    TopicPrefix =
                        prefixSettings["type"].Select(type => prefixFormat.Replace("#type#", (string)type)).ToArray();
                }
                else
                {
                    //type == ""
                    TopicPrefix = new [] { prefixFormat };
                }
                JToken jt;
                if (prefixSettings.TryGetValue("time", out jt))
                    topicPrefixTime = (string)jt;
                else
                    topicPrefixTime = null;
            }
            cachedTopicMatchers.Clear();
            //
            await _Topics.RefreshAsync();
        }
        #endregion

        /// <summary>
        /// 进行签到。
        /// </summary>
        public void SignIn()
        {
            //ie=utf-8&kw=%E7%8C%AB%E5%A4%B4%E9%B9%B0%E7%8E%8B%E5%9B%BD&tbs=2ac4c76dba9f5f9e1434877655
            var siParams = new NameValueCollection
            {
                {"ie", "utf-8"},
                {"kw", Name},           //注意这里会由 WebClient 自动编码。
                {"tbs", PageData_Tbs}
            };
            JObject result;
            /*
{
    "no": 0,
    "error": "",
    "data": {
        "errno": 0,
        "errmsg": "success",
        "sign_version": 2,
        "is_block": 0,
        "finfo": {
            "forum_info": {
                "forum_id": 656638,
                "forum_name": "猫头鹰王国"
            },
            "current_rank_info": {
                "sign_count": 115
            }
        },
        "uinfo": {
            "user_id": 13724678,
            "is_sign_in": 1,
            "user_sign_rank": 115,
            "sign_time": 1434877722,
            "cont_sign_num": 1,
            "total_sign_num": 534,
            "cout_total_sing_num": 534,
            "hun_sign_num": 6,
            "total_resign_num": 0,
            "is_org_name": 0
        }
    }
}             */
            using (var client = Session.CreateWebClient())
                result = JObject.Parse(client.UploadValuesAndDecode("http://tieba.baidu.com/sign/add", siParams));
            var num = (int) result["no"];
            switch (num)
            {
                case 0:
                    var resultData = result["data"];
                    SignInRank = (int)resultData["finfo"]["current_rank_info"]["sign_count"];
                    return;
                case 265:
                    throw new OperationFailedException(num, Prompts.NeedLogin);
                case 1102:
                    throw new OperationFailedException(num, Prompts.OperationsTooFrequentException);
                case 2150040:
                    //“/sign/getVcode”
                    //"/sign/checkVcode”
                    /*

{
  "no": 2150040,
  "error": "need vcode",
  "data": {
    "captcha_vcode_str": "captchaservice3037356454465a46596a63596e6b477456324c6b7a79386c304c74514b67436b776f5763522f6d4e4d2b664e5a4c46417565355a4861393064467470634d63357a67656553754a63734a2f6f434169677a4a6558337a33457045766b4845514d4357474a33344674386370507a58777a686d7471656770342b674d79506d722f5669482b444d4f39336d35516d7474706b2f4856416178485168626f6570554175502f4a3778314d342f6c454932504a304c5977415637393670344b4c776a6d6251394566746c417438476a716f6f466731652f6877595a5850713762626231664e33764559526d5677724667765449697275424d67723036767677664f4a574c6a6a4e65492f6535715a626d435042597849346c6a41747059726d54476e493769577756675868674b2f6730557634414e73",
    "captcha_code_type": 4,
    "str_reason": "请输入验证码完成操作"
  }
}
                     */
                    throw new OperationFailedException(num, Prompts.NeedVCode);
                default:
                    throw new OperationFailedException(num, (string) result["error"]);
            }
        }


        public string GetTopicPrefix(int index)
        {
            return TopicPrefix[index].Replace("#time#", topicPrefixTime);
        }

        public string MatchTopicPrefix(string topicTitle)
        {
            if (TopicPrefix.Count == 0) return string.Empty;
            if (cachedTopicMatchers.Count != TopicPrefix.Count)
            {
                foreach (var p in TopicPrefix)
                {
                    //宽松匹配。
                    var exp = Regex.Escape(p).Replace(@"\#time\#", ".*?");
                    cachedTopicMatchers.Add(new Regex(exp));
                }
            }
            for (var i = 0; i < cachedTopicMatchers.Count; i++)
                if (cachedTopicMatchers[i].IsMatch(topicTitle)) return TopicPrefix[i];
            return null;
        }

        /// <summary>
        /// 判断指定的主题名称是否符合当前论坛的前缀要求。
        /// </summary>
        public bool IsTopicPrefixMatch(string topicTitle)
        {
            return MatchTopicPrefix(topicTitle) != null;
        }

        public override string ToString()
        {
            return NeedRefetch ? QueryName : string.Format("[{0}]{1}", Id, Name);
        }

        internal ForumVisitor(string queryName, BaiduVisitor root)
            : base(root)
        {
            QueryName = queryName;
            _Topics = new TopicListView(this, string.Format(ForumUrlFormat, QueryName));
        }
    }

    public class TopicListView : VisitorPageListView<ForumVisitor, TopicVisitor>
    {
        //const string ForumUrlFormatPN = "http://tieba.baidu.com/f?ie=utf-8&kw={0}&fr=search&pn={1}";

        private string _PageHtmlCache;

        // 调用方：ForumVisitor
        internal void SetPageHtmlCache(string pageHtml)
        {
            _PageHtmlCache = pageHtml;
        }

        protected override async Task OnRefreshPageAsync()
        {
            var doc = new HtmlDocument();
            if (_PageHtmlCache == null)
                using (var client = Parent.Session.CreateWebClient())
                    _PageHtmlCache = await client.DownloadStringTaskAsync(PageUrl);
            doc.LoadHtml(_PageHtmlCache);
            _PageHtmlCache = null;
            var topicQuery = Enumerable.Empty<HtmlNode>();
            Action<string> collectTopics = id =>
            {
                var container = doc.GetElementbyId(id);
                if (container == null) return;
                //Debug.Print(container.InnerHtml);
                var nc = container.SelectNodes("./li[@data-field]");
                if (nc == null) return;
                topicQuery = topicQuery.Concat(nc);
            };
            collectTopics("thread_top_list");
            collectTopics("thread_list");
            string href;
            foreach (var eachLi in topicQuery)
            {
                var linkNode = eachLi.SelectSingleNode(".//a[@class='j_th_tit']");
                if (linkNode == null) continue;
                var title = HtmlEntity.DeEntitize(linkNode.GetAttributeValue("title", ""));
                href = linkNode.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                var threadTextNode = eachLi.SelectSingleNode(".//div[contains(@class,'threadlist_detail')]/div[contains(@class,'threadlist_text')]");
                var preview = threadTextNode == null ? null : Utility.StringCollapse(HtmlEntity.DeEntitize(threadTextNode.InnerText.Trim()));
                var threadDetailNode = eachLi.SelectSingleNode(".//div[contains(@class, 'threadlist_detail')]");
                string replyer = null;
                DateTime? replyTime = null;
                if (threadDetailNode != null)
                {
                    var replyerNode = threadDetailNode.SelectSingleNode(".//*[contains(@class,'j_replyer')]");
                    var replyTimeNode =
                        threadDetailNode.SelectSingleNode(".//*[contains(@class,'threadlist_reply_date')]");
                    if (replyerNode != null) replyer = replyerNode.InnerText.Trim();
                    if (replyTimeNode != null) replyTime = Utility.ParseDateTime(replyTimeNode.InnerText);
                }
                var dataFieldStr = HtmlEntity.DeEntitize(eachLi.GetAttributeValue("data-field", ""));
                //Debug.Print(dataFieldStr);
                //{"author_name":"Mark5ds","id":3540683824,"first_post_id":63285795913,
                //"reply_num":1,"is_bakan":0,"vid":"","is_good":0,"is_top":0,"is_protal":0}
                var jo = JObject.Parse(dataFieldStr);
                RegisterNewItem(new TopicVisitor((long) jo["id"], title,
                    (int) jo["is_good"] != 0, (int) jo["is_top"] != 0,
                    (string) jo["author_name"], preview, (int) jo["reply_num"],
                    replyer, replyTime, Parent));
            }
            ClaimExistence(true);
            //解析其它页面地址。
            var pagerNode = doc.GetElementbyId("frs_list_pager");
            if (pagerNode == null) {
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
                href = linkNode.GetAttributeValue("href", "");
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

        protected override VisitorPageListView<TopicVisitor> PageFactory(string url)
        {
            return new TopicListView(Parent, url);
        }

        internal TopicListView(ForumVisitor parent, string pageUrl)
            : base(parent, pageUrl)
        { }
    }

    /// <summary>
    /// 用于封禁用户时提供必要的参数。
    /// </summary>
    public struct BlockUserParams
    {
        /// <summary>
        /// 用户名。
        /// </summary>
        public string UserName { get; private set;}

        /// <summary>
        /// 帖子 Id。
        /// </summary>
        public long PostId { get; private set;}

        public BlockUserParams(string userName, long postId)
            : this()
        {
            UserName = userName;
            PostId = postId;
        }
    }
}
