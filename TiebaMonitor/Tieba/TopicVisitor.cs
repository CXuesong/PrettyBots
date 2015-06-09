using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

namespace TiebaMonitor.Kernel.Tieba
{
    public class TopicVisitor : BaiduChildVisitor
    {
        public const string TopicUrlFormat = "http://tieba.baidu.com/p/{0}?ie=utf-8";

        public const string TopicUrlFormatPN = "http://tieba.baidu.com/p/{0}?pn={1}&ie=utf-8";

        public const string CommentUrlFormat =
            "http://tieba.baidu.com/p/totalComment?t={0}&tid={1}&fid={2}&pn={3}&see_lz=0";

        public const string ReplyUrl = "http://tieba.baidu.com/f/commit/post/add";

        public const int MaxCommentLimit = 2000;

        public bool IsExists { get; private set; }

        public long ForumId { get; private set; }

        public long Id { get; private set; }

        public string Title { get; private set; }

        public string AuthorName { get; private set; }

        public string PreviewText { get; private set; }

        public bool IsGood { get; private set; }

        public bool IsTop { get; private set; }

        public int RepliesCount { get; private set; }

        public string LastReplyer { get; private set; }

        public string LastReplyTime { get; private set; }

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
            ForumId = (long) forumData["forum_id"];
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
                    JObject.Parse(s.DownloadString(string.Format(CommentUrlFormat, dateTimeBase,
                        Id, ForumId, pageNum)));
                if ((int)commentData["errno"] != 0)
                {
                    Debug.Print("Comment, Fid={0}, Page={1}, Error:{2}, {3}", ForumId, pageNum,
                        (string) commentData["errno"], (string) commentData["errmsg"]);
                    commentData = null;
                }
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
                    var content = (string) pdc["content"];
                    if (content == null)
                    {
                        //从HTML获取内容。
                        var contentNode = doc.GetElementbyId("post_content_" + pid);
                        if (contentNode == null) throw new UnexpectedDataException();
                        content = contentNode.InnerHtml.Trim();
                    }
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
                        var thisComments = commentData["data"]["comment_list"][pid.ToString(CultureInfo.InvariantCulture)];
                        if (thisComments != null)
                        {
                            comments = new List<PostComment>(Math.Min((int) thisComments["comment_num"], MaxCommentLimit));
                            foreach (var et in thisComments["comment_info"])
                            {
                                //TODO: 修复回帖时间不正确的问题。
                                comments.Add(new PostComment((long) et["comment_id"], (string) et["username"],
                                    (long) et["user_id"], Utility.FromUnixDateTime(dateTimeBase - (long) et["comment_id"]),
                                    (string) et["content"]));
                                if (comments.Count >= MaxCommentLimit) break;
                            }
                        }
                    }
                    yield return new PostVisitor(pid, (int) pdc["post_no"],
                        (string) pd["author"]["user_name"], content,
                        (int) pdc["comment_num"], comments, Parent);
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
            throw new NotImplementedException();
            using (var client = Parent.Session.CreateWebClient())
            {
                var replyParams = new NameValueCollection();
                var result = client.UploadValues(ReplyUrl, replyParams);
                var resultStr = client.Encoding.GetString(result);
                var resultObj = JObject.Parse(resultStr);
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}[A={2}][R={3}][{4}]", Id, Title, AuthorName, RepliesCount, PreviewText);
        }

        internal TopicVisitor(long id, string title, bool isGood, bool isTop,
            string author, string preview, int repliesCount, string lastReplyer, string lastReplyTime,
            long forumId, BaiduVisitor parent)
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
            ForumId = forumId;
            //默认表示帖子肯定是存在的。
            IsExists = true;
        }
    }
}