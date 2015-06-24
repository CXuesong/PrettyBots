using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class TopicVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string TopicUrlFormat = "http://tieba.baidu.com/p/{0}?ie=utf-8";

        public const string TopicUrlFormatPost = "http://tieba.baidu.com/p/{0}?pid={1}&ie=utf-8";

        public const string ReplyUrl = "http://tieba.baidu.com/f/commit/post/add";

        private ForumVisitor _Forum;
        private string _ForumName;
        private long? _ForumId;
        private PostListView posts;
        private string pageData_tbs = null;

        public bool IsExists { get; private set; }


        public string ForumName
        {
            get { return _ForumName ?? Forum.Name; }
        }

        public long ForumId
        {
            get { return _ForumId ?? Forum.Id; }
        }

        public ForumVisitor Forum
        {
            get
            {
                if (_Forum == null)
                {
                    //获取 Forum。
                    Debug.Assert(!NeedRefetch);
                    Debug.Assert(!string.IsNullOrWhiteSpace(_ForumName));
                    _Forum = Root.Tieba.Forum(_ForumName);
                }
                return _Forum;
            }
        }

        public long Id { get; private set; }

        /// <summary>
        /// 获取用于在当前页面定位的帖子。
        /// </summary>
        public long? AnchorPostId { get; private set; }

        public string Title { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public bool IsGood { get; private set; }

        public bool IsTop { get; private set; }

        public int RepliesCount { get; private set; }

        public string LastReplyer { get; private set; }

        public DateTime? LastReplyTime { get; private set; }
 
        protected override async Task OnFetchDataAsync()
        {
            var doc = new HtmlDocument();
            using (var s = Root.Session.CreateWebClient())
            {
                var pageHtml = await s.DownloadStringTaskAsync(posts.PageUrl);
                doc.LoadHtml(pageHtml);
                //下载页面时，将页面内容缓存至帖子列表视图中。
                posts.SetTopicPageCache(pageHtml);
            }
            var errorTextNode = doc.GetElementbyId("errorText");
            if (errorTextNode != null)
            {
                Debug.Print("pid={0}, {1}", Id, errorTextNode.InnerText);
                IsExists = false;
                return;
            }
            var forumData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.forum");
            _ForumId = (long)forumData["forum_id"];
            _ForumName = (string)forumData["forum_name"];
            var topicData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.thread");
            //PageData.thread = 
            //{ author: "来自草原的雪狼", thread_id: 3369574832, 
            //title: "【珈瑚传奇】猫头鹰王国之战火纷飞", reply_num: 506,
            //thread_type: "0",
            //topic: { is_topic: false, topic_type: false, is_live_post: false,
            //is_lpost: false, lpost_type: 0 }, /*null,*/ is_ad: 0, video_url: "" };
            AuthorName = (string)topicData["author"];
            Id = (long)topicData["thread_id"];
            Title = (string)topicData["title"];
            RepliesCount = (int)topicData["reply_num"];
            //状态信息
            var pageData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData");
            /*
{
    "tbs": "",
    "charset": "UTF-8",
    "product": "frs",
    "page": "new_version",
    "user": {
        "user_id": 1733233632,
        "user_name": "La_Mobile",
        "user_sex": 1,
        "user_status": 1,
        "card": "a:6:{s:8:&quot;post_num&quot;;i:46;s:8:&quot;good_num&quot;;i:0;s:12:&quot;manager_info&quot;;a:2:{s:7:&quot;manager&quot;;a:2:{s:10:&quot;forum_list&quot;;a:0:{}s:5:&quot;count&quot;;i:0;}s:6:&quot;assist&quot;;a:2:{s:10:&quot;forum_list&quot;;a:1:{i:0;s:10:&quot;守卫者传奇&quot;;}s:5:&quot;count&quot;;i:1;}}s:10:&quot;like_forum&quot;;a:0:{}s:9:&quot;is_novice&quot;;i:0;s:7:&quot;op_time&quot;;i:1435069193;}",
        "portrait_time": "1433862313",
        "tbscore_repeate_finish_time": "1433860103",
        "use_sig": 0,
        "notice_mask": {
            "2": 0,
            "3": 0,
            "5": 0,
            "6": 0,
            "9": 0,
            "1000": 0
        },
        "priv_sets": {
            "like": "2",
            "post": "2",
            "group": "2"
        },
        "user_type": 0,
        "meizhi_level": -1,
        "new_iconinfo": {
            "1": []
        },
        "global": {
            "tbmall_newprops": 0
        },
        "is_login": true,
        "email": "nvf***@163.com",
        "mobile": "",
        "no_un": 0,
        "is_new": 1,
        "start_time": 1435071754,
        "id": 1733233632,
        "name": "La_Mobile",
        "portrait": "e00b4c615f4d6f62696c654f67",
        "mobilephone": "",
        "name_show": "La_Mobile",
        "identity": {
            "is_forum_assist": true,
            "is_forum_member": true,
            "is_forum_bawu": true,
            "is_forum_pm": false,
            "manager": null,
            "member": true,
            "pm": false
        },
        "itieba_id": null,
        "like_forums": [],
        "bduid": "FA7B0721B3C254658A45CAA7D4B14881",
        "userhide": 0,
        "user_forum_list": {
            "info": [
                {
                    "forum_name": "天文",
                    "user_level": 2,
                    "user_exp": 7,
                    "id": 8322,
                    "is_like": true,
                    "favo_type": 1
                },
                {
                    "forum_name": "生物",
                    "user_level": 2,
                    "user_exp": 11,
                    "id": 997,
                    "is_like": true,
                    "favo_type": 1
                },
                {
                    "forum_name": "化学",
                    "user_level": 2,
                    "user_exp": 11,
                    "id": 9046,
                    "is_like": true,
                    "favo_type": 1
                },
                {
                    "forum_name": "mark5ds",
                    "user_level": 4,
                    "user_exp": 36,
                    "id": 2195006,
                    "is_like": true,
                    "favo_type": 1
                },
                {
                    "forum_name": "猫头鹰王国",
                    "user_level": 4,
                    "user_exp": 38,
                    "id": 656638,
                    "is_like": true,
                    "favo_type": 2
                },
               ......
            ]
        },
        "power": {
            "post": true,
            "bawu": true,
            "vote_creater": true,
            "bakan_edit": false,
            "idisk": false,
            "is_assist": true,
            "can_edit_gconforum": false
        },
        "balv": {
            "is_firstlike": 0,
            "has_award": 0,
            "award_code": 0,
            "award_inf1o": "",
            "award_value": 0,
            "is_lvup": 0,
            "rights": "",
            "has_trans_info": 0,
            "trans_info": "",
            "has_mall_expire_info": 0,
            "mall_expire_info": "",
            "has_liked": 1,
            "is_like": 0,
            "level_id": 3,
            "cur_score": 25,
            "score_left": 5,
            "levelup_score": 30,
            "level_name": "遭遇绑架",
            "picasso": false,
            "is_black": 0,
            "is_liked": 1,
            "is_by_pm": 0,
            "is_block": 1,
            "days_tofree": 1,
            "opgroup": "bawu",
            "block_reason": "This is a test block reason.",
            "like_forums": []
        },
        "rank": {
            "index": 407,
            "num": 0,
            "status": 0
        }
    },
    "fromPlatformUi": true
}
             */
            pageData_tbs = (string)pageData["tbs"];
            if (string.IsNullOrEmpty(pageData_tbs)) Debug.Print("TBS == null, Url:{0}", posts.PageUrl);
            IsExists = true;
            //顺带着更新一下。注意，要载入主题信息后，才能刷新列表内容。
            await posts.RefreshAsync();
        }

        /// <summary>
        /// 获取当前页面的帖子视图。
        /// </summary>
        public PostListView Posts
        {
            get
            {
                //更新自身的同时可以更新当前页面的帖子列表。
                Update();
                return posts;
            }
        }

        /// <summary>
        /// 回复主题。
        /// </summary>
        /// <param name="content">要回复的内容。</param>
        public bool Reply(string content)
        {
            var t = ReplyAsync(content);
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// 回复主题。
        /// </summary>
        /// <param name="content">要回复的内容。</param>
        public Task<bool> ReplyAsync(string content)
        {
            return ReplyAsync(content, null);
        }

        internal async Task<bool> ReplyAsync(string contentCode, long? pid)
        {
            //TIP
            //"@La_Mobile&nbsp;....[br][url]http://tieba.baidu.com/[/url]"
            if (string.IsNullOrWhiteSpace(contentCode)) return false;
            await UpdateAsync();
            if (!IsExists) throw new InvalidOperationException(Prompts.PageNotExists);
            Debug.Assert(_ForumId == null || Forum == null || Forum.Id == _ForumId);
            using (var client = Root.Session.CreateWebClient())
            {
                var baseTime = DateTime.Now;
                //var baseTimeStr = baseTime.ToString(CultureInfo.InvariantCulture);
                var replyParams = new NameValueCollection
                {
                    {"ie", "utf-8"},
                    {"kw", ForumName},
                    {"fid", Convert.ToString(ForumId)},
                    {"tid", Convert.ToString(Id)},
                    {"vcode_md5", ""},
                    /*{"floor_num", "5"},*/
                    {"rich_text", "1"},
                    {"tbs", pageData_tbs},
                    {"content", contentCode},
                    {"files", "[]"},
                    //{"sign_id", "4787987"},
                    {
                        "mouse_pwd",
                        BaiduUtility.GenerateMousePwd(baseTime)
                    },
                    {"mouse_pwd_t", Convert.ToString(Utility.ToUnixDateTime(baseTime))},
                    {"mouse_pwd_isclick", "0"},
                    {"__type__", "reply"}
                };
                if (pid != null)
                {
                    replyParams["quote_id"] = pid.ToString();
                    replyParams["repostid"] = pid.ToString();
                }
                var result = await client.UploadValuesAndDecodeTaskAsync(ReplyUrl, replyParams);
                var resultObj = JObject.Parse(result);
                /*
                 {
  "no": 40,
  "err_code": 40,
  "error": "",
  "data": {
    "autoMsg": "",
    "fid": 2195006,
    "fname": "mark5ds",
    "tid": 0,
    "is_login": 1,
    "content": "",
    "vcode": {
      "need_vcode": 1,
      "str_reason": "请点击验证码完成发贴",
      "captcha_vcode_str": "captchaservice3832303567744451634c64626552716953795a71306d576c715337424d6c2f55502f36614b6e5a674c337766766f53544a68693737615847356b6f2f50332b736d4337524a772b64697133332f792b5638663838704b433072324a6e746234396a35444454627868476b6637656861686b744841724c62733471786e6e764271597a6b58755044552b51334875724b324a38726e38437842516b6957554c54355945756b44385350535278475645543056433971347a6937786d776e5a4152556a764e4c354e5a6461796c355839526b52494350664f557562567a2f4972754579424a7433582b6876435431345232785239534233686752562b697166414d4271477457327069366e4b77525978684d356c696334415158742f324d6a7231616d5a6f6635526d574745696576577158684a6c69",
      "captcha_code_type": 4,
      "userstatevcode": 0
    }
  }
}
                 */
                //Debug.Print(resultObj.ToString());
                var errNumer = (int)resultObj["no"];
                switch (errNumer)
                {
                    case 0:
                        return true;
                    case 12:
                        throw new OperationFailedException(errNumer, Prompts.AccountHasBeenBlocked);
                    case 34:
                        throw new OperationFailedException(errNumer, Prompts.OperationsTooFrequentException);
                    case 40:
                        //需要验证码。
                        return false;
                    case 265:
                        throw new OperationFailedException(errNumer, Prompts.NeedLogin);
                    case 274:
                        throw new OperationFailedException(errNumer, "POST数据错误。");
                    case 2007:
                        throw new OperationFailedException(errNumer, Prompts.InvalidContentException);
                    default:
                        throw new OperationFailedException(errNumer, (string)resultObj["error"]);
                }
                return true;
            }
        }

        #region 权限

        /// <summary>
        /// 封禁此主题内某帖子的作者。
        /// </summary>
        public void BlockUser(BlockUserParams p)
        {
            BlockUser(p, null);
        }

        /// <summary>
        /// 封禁此主题内某帖子的作者。
        /// </summary>
        public void BlockUser(BlockUserParams p, string reason)
        {
            const string postUrl = "http://tieba.baidu.com/pmc/blockid";

            if (p.PostId <= 0 || string.IsNullOrWhiteSpace(p.UserName))
                throw new ArgumentException();
            if (string.IsNullOrWhiteSpace(reason)) reason = Prompts.BlockUserDefaultMessage;
            Update();
            var buParams = new NameValueCollection()
            {
                {"day", "1"},
                {"fid", Convert.ToString(ForumId)},
                {"tbs", pageData_tbs},
                {"ie", "utf-8"},
                {"user_name[]", p.UserName},
                {"pid[]", Convert.ToString(p.PostId)},
                {"reason", reason}
            };
            JObject result;
            using (var client = Session.CreateWebClient())
                result = JObject.Parse(client.UploadValuesAndDecode(postUrl, buParams));
            /*{"errno":0,"errmsg":"\u6210\u529f"}*/
            if ((int)result["errno"] == 0) return;
            throw new OperationFailedException((int)result["errno"], (string)result["errmsg"]);
        }
        #endregion

        public override string ToString()
        {
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Id, Title, AuthorName, RepliesCount, PreviewText);
        }

        // 由 Forum -> TopicListView 创建
        internal TopicVisitor(long id, string title, bool isGood, bool isTop,
            string author, string preview, int repliesCount, string lastReplyer, DateTime? lastReplyTime,
            ForumVisitor forum)
            : this(id, forum.Root)
        {
            Title = title;
            IsGood = isGood;
            IsTop = isTop;
            AuthorName = author;
            PreviewText = preview;
            RepliesCount = repliesCount;
            LastReplyer = lastReplyer;
            LastReplyTime = lastReplyTime;
            _Forum = forum;
            //默认表示帖子肯定是存在的。
            IsExists = true;
        }

        // 由 Tieba -> Topic 创建
        internal TopicVisitor(long id, BaiduVisitor root)
            : base(root)
        {
            Id = id;
            posts = new PostListView(this, string.Format(TopicUrlFormat, id));
        }

        // 由 Tieba -> Topic 创建
        // 由搜索结果创建
        internal TopicVisitor(long id, long anchorPid, BaiduVisitor root)
            : base(root)
        {
            Id = id;
            AnchorPostId = anchorPid;
            posts = new PostListView(this, string.Format(TopicUrlFormatPost, id, anchorPid));
        }
    }

    public class PostListView : VisitorPageListView<TopicVisitor, PostVisitor>
    {
        public const string TopicUrlFormatPN = "http://tieba.baidu.com/p/{0}?pn={1}&ie=utf-8";

        public const string OverallCommentUrlFormat =
            "http://tieba.baidu.com/p/totalComment?t={0}&tid={1}&fid={2}&pn={3}&see_lz=0";

        private string _TopicPageCache;

        // 调用方：TopicVisitor
        internal void SetTopicPageCache(string pageHtml)
        {
            _TopicPageCache = pageHtml;
        }

        protected new TopicVisitor Parent
        {
            get { return (TopicVisitor)base.Parent; }
        }

        private string GetPageUrl(int pageIndex)
        {
            return string.Format(TopicUrlFormatPN, ((TopicVisitor)Parent).Id, pageIndex);
        }

        protected async override Task<PageListView<PostVisitor>>
            OnNavigateAsync(PageRelativeLocation location)
        {
            var newIndex = -1;
            switch (location)
            {
                case PageRelativeLocation.First:
                    newIndex = 0;
                    break;
                case PageRelativeLocation.Previous:
                    newIndex = PageIndex - 1;
                    break;
                case PageRelativeLocation.Next:
                    newIndex = PageIndex + 1;
                    break;
                case PageRelativeLocation.Last:
                    newIndex = PageCount - 1;
                    break;
            }
            if (newIndex < 0 || newIndex >= PageCount) return null;
            var newPage = new PostListView(Parent, GetPageUrl(newIndex + 1));
            await newPage.RefreshAsync();
            return newPage;
        }

        protected override async Task OnRefreshPageAsync()
        {
            var doc = new HtmlDocument();
            JObject commentData;
            using (var client = Parent.Session.CreateWebClient())
            {
                if (_TopicPageCache == null) _TopicPageCache = await client.DownloadStringTaskAsync(PageUrl);
                doc.LoadHtml(_TopicPageCache);
                _TopicPageCache = null;
                //解析分页信息。
                var pagerData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.pager", true);
                if (pagerData != null)
                {
                    //PageData.pager = {"cur_page":1,"total_page":100};
                    PageIndex = (int)pagerData["cur_page"] - 1;
                    PageCount = (int)pagerData["total_page"];
                }
                //下载每层前10楼中楼
                //将在 SubPostListView 中解析。
                var dateTimeBase = Utility.ToUnixDateTime(DateTime.Now);
                commentData =
                    JObject.Parse(await client.DownloadStringTaskAsync(
                        string.Format(OverallCommentUrlFormat, dateTimeBase,
                            Parent.Id, Parent.ForumId, PageIndex + 1)));
                if ((int)commentData["errno"] != 0)
                {
                    Debug.Print("Comment, Fid={0}, Page={1}, Error:{2}, {3}", Parent.Forum.Id, PageIndex + 1,
                        (string)commentData["errno"], (string)commentData["errmsg"]);
                    commentData = null;
                }
            }
            //帖子列表。
            var postListNode = doc.GetElementbyId("j_p_postlist");
            if (postListNode == null)
            {
                ClaimExistence(false);
                return;
            }
            foreach (var eachNode in postListNode.SelectNodes("./div[@data-field]"))
            {
                //Normal
                //{"author":{"user_id":355004908,"user_name":"hellodrf","props":null},
                //"content":{"post_id":64616765755,"is_anonym":false,"forum_id":656638,
                //"thread_id":1747016486,"content":"\u5347\u7ea7\uff01\uff01",
                //"post_no":966,"type":"0","comment_num":2,"props":null,"post_index":1}}
                //IP党
                /*
{
    "author": {
        "user_name": "180.110.125.*"
    },
    "content": {
        "post_id": 10234026513,
        "is_anonym": 1,
        "open_id": "",
        "open_type": "",
        "date": "2010-11-12 21:59",
        "vote_crypt": "",
        "post_no": 2,
        "type": "0",
        "comment_num": 2,
        "ptype": "",
        "is_saveface": false,
        "props": null,
        "post_index": 1
    }
}
                 */
                var pd = JObject.Parse(HtmlEntity.DeEntitize(eachNode.GetAttributeValue("data-field", "")));
                var pdc = pd["content"];
                var pid = (long)pdc["post_id"];
                var submissionTime = (DateTime?)pdc["date"];
                if (submissionTime == null)
                {
                    var nc = eachNode.SelectNodes(".//div[@class='post-tail-wrap']//span[@class='tail-info']");
                    var n = nc == null ? null : nc.LastOrDefault();
                    submissionTime = n == null ? DateTime.MinValue : Convert.ToDateTime(n.InnerText);
                }
                var content = (string)pdc["content"];
                if (content == null)
                {
                    //从HTML获取内容。
                    var contentNode = doc.GetElementbyId("post_content_" + pid);
                    if (contentNode == null) throw new UnexpectedDataException();
                    content = contentNode.InnerHtml.Trim();
                }
                var badageNode = eachNode.SelectSingleNode(".//div[@class='d_badge_lv']");
                var badageValue = badageNode.InnerText;
                var author = new TiebaUserStub((long?) pd["author"]["user_id"],
                    (string) pd["author"]["user_name"],
                    string.IsNullOrWhiteSpace(badageValue)
                        ? null
                        : (int?) Convert.ToInt32(badageNode.InnerText));
                RegisterNewItem(new PostVisitor(pid, (int) pdc["post_no"],
                    author, content, submissionTime.Value,
                    (int) pdc["comment_num"], commentData, this));
            }
            ClaimExistence(true);
        }

        protected override VisitorPageListView<PostVisitor> PageFactory(string url)
        {
            throw new NotSupportedException();
        }

        internal PostListView(TopicVisitor parent, string pageUrl)
            : base(parent, pageUrl)
        { }
    }
}