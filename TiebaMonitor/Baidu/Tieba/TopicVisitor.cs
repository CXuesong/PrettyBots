using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class TopicVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string TopicUrlFormat = "http://tieba.baidu.com/p/{0}?ie=utf-8";

        public const string TopicUrlFormatPN = "http://tieba.baidu.com/p/{0}?pn={1}&ie=utf-8";

        public const string OverallCommentUrlFormat =
            "http://tieba.baidu.com/p/totalComment?t={0}&tid={1}&fid={2}&pn={3}&see_lz=0";

        public const string ReplyUrl = "http://tieba.baidu.com/f/commit/post/add";

        public const int MaxCommentLimit = 2000;

        public bool IsExists { get; private set; }

        //public string ForumName { get; private set; }

        //public long ForumId { get; private set; }

        private long? _ForumId;

        public ForumVisitor Forum { get; private set; }

        public long Id { get; private set; }

        public string Title { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public bool IsGood { get; private set; }

        public bool IsTop { get; private set; }

        public int RepliesCount { get; private set; }

        public string LastReplyer { get; private set; }

        public string LastReplyTime { get; private set; }

        private string pageData_tbs = null;

        private bool internalUpdated = false;

        private void UpdateInternal(HtmlDocument doc)
        {
            var pageData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData");
            pageData_tbs = (string) pageData["tbs"];
            internalUpdated = true;
        }

        private void EnsureInternal(ExtendedWebClient client)
        {
            if (!internalUpdated)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(client.DownloadString(string.Format(TopicUrlFormat, Id)));
                UpdateInternal(doc);
            }
        }

        public void Update()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
                doc.LoadHtml(s.DownloadString(string.Format(TopicUrlFormat, Id)));
            var errorTextNode = doc.GetElementbyId("errorText");
            if (errorTextNode != null)
            {
                Debug.Print("pid={0}, {1}", Id, errorTextNode.InnerText);
                IsExists = false;
                return;
            }
            var forumData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.forum");
            _ForumId = (long)forumData["forum_id"];
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
        }

        public IEnumerable<PostVisitor> Posts()
        {
            var doc = new HtmlDocument();
            using (var s = Parent.Session.CreateWebClient())
            {
                var pageNum = 1;
                var pageUrl = string.Format(TopicUrlFormat, Id);
                PARSE_PAGE:
                doc.LoadHtml(s.DownloadString(pageUrl));
                //楼中楼
                var dateTimeBase = Utility.ToUnixDateTime(DateTime.Now);
                var commentData =
                    JObject.Parse(s.DownloadString(string.Format(OverallCommentUrlFormat, dateTimeBase,
                        Id, Forum.Id, pageNum)));
                if ((int)commentData["errno"] != 0)
                {
                    Debug.Print("Comment, Fid={0}, Page={1}, Error:{2}, {3}", Forum.Id, pageNum,
                        (string) commentData["errno"], (string) commentData["errmsg"]);
                    commentData = null;
                }
                //帖子列表。
                var postListNode = doc.GetElementbyId("j_p_postlist");
                if (postListNode == null) yield break;
                foreach (var eachNode in postListNode.SelectNodes("./div[@data-field]"))
                {
                    //{"author":{"user_id":355004908,"user_name":"hellodrf","props":null},
                    //"content":{"post_id":64616765755,"is_anonym":false,"forum_id":656638,
                    //"thread_id":1747016486,"content":"\u5347\u7ea7\uff01\uff01",
                    //"post_no":966,"type":"0","comment_num":2,"props":null,"post_index":1}}
                    var pd = JObject.Parse(HtmlEntity.DeEntitize(eachNode.GetAttributeValue("data-field", "")));
                    var pdc = pd["content"];
                    var pid = (long) pdc["post_id"];
                    var submissionTime = (DateTime?) pdc["date"];
                    if (submissionTime == null)
                    {
                        var nc = eachNode.SelectNodes(".//div[@class='post-tail-wrap']//span[@class='tail-info']");
                        var n = nc == null ? null : nc.LastOrDefault();
                        submissionTime = n == null ? DateTime.MinValue : Convert.ToDateTime(n.InnerText);
                    }
                    var content = (string) pdc["content"];
                    if (content == null)
                    {
                        //从HTML获取内容。
                        var contentNode = doc.GetElementbyId("post_content_" + pid);
                        if (contentNode == null) throw new UnexpectedDataException();
                        content = contentNode.InnerHtml.Trim();
                    }
                    var badageNode = eachNode.SelectSingleNode(".//div[@class='d_badge_lv']");
                    var author = new TiebaUserStub((long) pd["author"]["user_id"],
                        (string)pd["author"]["user_name"], Convert.ToInt32(badageNode.InnerText));
                    //楼中楼
                    /*
                     {
    "errno": 0,
    "errmsg": "success",
    "data": {
        "comment_list": {
            "65718232016": {
                "comment_num": 1,
                "comment_list_num": 1,
                "comment_info": [
                    {
                        "thread_id": "3636549985",
                        "post_id": "65718232016",
                        "comment_id": "65718247989",
                        "username": "双鱼满月",
                        "user_id": "1038711401",
                        "now_time": 1426567930,
                        "content": "p.s. Lz注意前缀<img class=\"BDE_Smiley\" pic_type=\"1\" width=\"30\" height=\"30\" src=\"http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803\" >",
                        "ptype": 0,
                        "during_time": 0
                    }
                ]
            }
        },
        "user_list": {
            "1038711401": {
                "user_id": 1038711401,
                "user_name": "双鱼满月",
                "user_sex": 2,
                "user_status": 0,
                "bg_id": "1012",
                "card": "a:6:{s:8:\"post_num\";i:4175;s:8:\"good_num\";i:3;s:12:\"manager_info\";a:2:{s:7:\"manager\";a:2:{s:10:\"forum_list\";a:0:{}s:5:\"count\";i:0;}s:6:\"assist\";a:2:{s:10:\"forum_list\";a:3:{i:0;s:10:\"猫头鹰王国\";i:1;s:8:\"夜煞无牙\";i:2;s:6:\"邱吴洪\";}s:5:\"count\";i:3;}}s:10:\"like_forum\";a:3:{i:10;a:2:{s:10:\"forum_list\";a:1:{i:0;s:10:\"猫头鹰王国\";}s:5:\"count\";i:1;}i:8;a:2:{s:10:\"forum_list\";a:2:{i:0;s:10:\"守卫者传奇\";i:1;s:9:\"aea工作室\";}s:5:\"count\";i:2;}i:7;a:2:{s:10:\"forum_list\";a:2:{i:0;s:8:\"夜煞无牙\";i:1;s:8:\"绝境狼王\";}s:5:\"count\";i:4;}}s:9:\"is_novice\";i:0;s:7:\"op_time\";i:1433760933;}",
                "portrait_time": "1393118701",
                "mParr_props": [],
                "tbscore_repeate_finish_time": "1433562554",
                "use_sig": 0,
                "notice_mask": {
                    "2": 0,
                    "3": 0,
                    "5": 0,
                    "6": 0,
                    "9": 0,
                    "1000": 0
                },
                "user_type": 0,
                "meizhi_level": 0,
                "new_iconinfo": {
                    "1": []
                },
                "portrait": "697ae58f8ce9b1bce6bba1e69c88e93d",
                "nickname": "双鱼满月"
            }
        }
    }
}
                     */
                    IList<PostComment> comments = null;
                    if (commentData != null)
                    {
                        var thisComments = commentData["data"]["comment_list"];
                        //可能没有回复可用。
                        thisComments = thisComments.SelectToken(pid.ToString(CultureInfo.InvariantCulture), false);
                        if (thisComments != null)
                        {
                            comments = new List<PostComment>(Math.Min((int) thisComments["comment_num"], MaxCommentLimit));
                            foreach (var et in thisComments["comment_info"])
                            {
                                comments.Add(new PostComment((long) et["comment_id"], (string) et["username"],
                                    (long)et["user_id"], Utility.FromUnixDateTime((long)et["now_time"] * 1000),
                                    (string) et["content"]));
                                if (comments.Count >= MaxCommentLimit) break;
                            }
                        }
                    }
                    yield return new PostVisitor(pid, (int) pdc["post_no"],
                        author, content, submissionTime.Value,
                        (int) pdc["comment_num"], comments, this, Parent);
                }
                //下一页
                var pagerData = Utility.FindJsonAssignment(doc.DocumentNode.OuterHtml, "PageData.pager", true);
                if (pagerData != null)
                {
                    //PageData.pager = {"cur_page":1,"total_page":100};
                    var curPage = (int) pagerData["cur_page"];
                    if (curPage < (int) pagerData["total_page"])
                    {
                        pageNum = curPage + 1;
                        pageUrl = string.Format(TopicUrlFormatPN, Id, curPage + 1);
                        goto PARSE_PAGE;
                    }
                }
            }
        }

        /// <summary>
        /// 回复主题。
        /// </summary>
        /// <param name="content">要回复的内容。</param>
        public bool Reply(string content)
        {
            return Reply(content, null);
        }

        internal bool Reply(string contentCode, long? pid)
        {
            //TIP
            //"@La_Mobile&nbsp;....[br][url]http://tieba.baidu.com/[/url]"
            if (string.IsNullOrWhiteSpace(contentCode)) return false;
            Debug.Assert(_ForumId == null || Forum.Id == _ForumId);
            using (var client = Parent.Session.CreateWebClient())
            {
                EnsureInternal(client);
                var baseTime = Utility.ToUnixDateTime(DateTime.Now);
                var baseTimeStr = baseTime.ToString(CultureInfo.InvariantCulture);
                var replyParams = new NameValueCollection
                {
                    {"ie", "utf-8"},
                    {"kw", Forum.Name},
                    {"fid", Forum.Id.ToString(CultureInfo.InvariantCulture)},
                    {"tid", Id.ToString(CultureInfo.InvariantCulture)},
                    {"vcode_md5", ""},
                    /*{"floor_num", "5"},*/
                    {"rich_text", "1"},
                    {"tbs", pageData_tbs},
                    {"content", contentCode},
                    {"files", "[]"},
                    //{"sign_id", "4787987"},
                    {
                        "mouse_pwd",
                        "46,44,32,52,41,45,41,47,33,17,41,52,40,52,41,52,40,52,41,52,40,17,43,42,46,47,40,17,41,43,46,46,52,47,46,32," +
                        baseTimeStr
                    },
                    {"mouse_pwd_t", baseTimeStr},
                    {"mouse_pwd_isclick", "0"},
                    {"__type__", "reply"}
                };
                if (pid != null)
                {
                    replyParams["quote_id"] = pid.ToString();
                    replyParams["repostid"] = pid.ToString();
                }
                var result = client.UploadValues(ReplyUrl, replyParams);
                var resultStr = client.Encoding.GetString(result);
                var resultObj = JObject.Parse(resultStr);
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
                switch ((int) resultObj["no"])
                {
                    case 0:
                        return true;
                    case 40:
                        //需要验证码。
                        return false;
                    case 265:
                        throw new InvalidOperationException(Prompts.NeedLogin);
                    default:
                        throw new InvalidOperationException(string.Format(Prompts.OperationFailedException_ErrorCode,
                            (int) resultObj["no"]));
                }
                return true;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Id, Title, AuthorName, RepliesCount, PreviewText);
        }

        internal TopicVisitor(long id, BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
        }

        internal TopicVisitor(long id, string title, bool isGood, bool isTop,
            string author, string preview, int repliesCount, string lastReplyer, string lastReplyTime,
            ForumVisitor forum, BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
            Title = title;
            IsGood = isGood;
            IsTop = isTop;
            AuthorName = author;
            PreviewText = preview;
            RepliesCount = repliesCount;
            LastReplyer = lastReplyer;
            LastReplyTime = lastReplyTime;
            Forum = forum;
            //默认表示帖子肯定是存在的。
            IsExists = true;
        }
    }
}