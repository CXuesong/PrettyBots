using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    /// <summary>
    /// 用于访问百度贴吧。
    /// </summary>
    public class TiebaVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string TiebaIndexUrl = "http://tieba.baidu.com";

        public const string OneKeySignInUrl = "http://tieba.baidu.com/tbmall/onekeySignin1";

        public const int ForumCacheCapacity = 30;

        private OrderedDictionary forumCache = new OrderedDictionary(ForumCacheCapacity,
            StringComparer.OrdinalIgnoreCase);

        private List<FavoriteForum> _FavoriteForums = new List<FavoriteForum>();

        private string _TiebaPageCache;

        private string PageData_Tbs;
        private MessagesVisitor _Messages;

        // 调用方： BaiduAccountInfo
        internal void SetTiebaPageCache(string html)
        {
            _TiebaPageCache = html;
        }

        protected override async Task OnFetchDataAsync()
        {
            if (_TiebaPageCache == null)
                using (var client = Session.CreateWebClient(true))
                    _TiebaPageCache = await client.DownloadStringTaskAsync(TiebaIndexUrl);
            //使用 HtmlDocument 以进行必要的字符转换。
            var doc = new HtmlDocument();
            doc.LoadHtml(_TiebaPageCache);
            _TiebaPageCache = null;
            FavoriteForums.Clear();
            PageData_Tbs = Utility.FindStringAssignment(doc.DocumentNode.OuterHtml, "PageData.tbs");
            if (Root.AccountInfo.IsLoggedIn)
            {
                var node = doc.GetElementbyId("onekey_sign");
                if (node == null) throw new UnexpectedDataException();
                node = node.SelectSingleNode("./a[1]");
                if (node == null) throw new UnexpectedDataException();
                var className = node.GetAttributeValue("class", "");
                if (!className.Contains("onekey_btn")) throw new UnexpectedDataException();
                HasOneKeySignedIn = className.Contains("signed_btn");
            }
            else
            {
                HasOneKeySignedIn = false;
                OneKeySignedInSignificant = false;
            }
            //暂时必须要使用 JSON 辅助提取 forums
            var fd = Utility.Find_ModuleUse(doc.DocumentNode.OuterHtml, "spage/widget/forumDirectory");
            var forums = fd["forums"];
            /*
{
    "forums": [
...
        {
            "user_id": 13724678,
            "forum_id": 25439,
            "forum_name": "西安交通大学",
            "is_black": 0,
            "is_top": 0,
            "in_time": 1391863680,
            "level_id": 9,
            "cur_score": 1864,
            "score_left": 136,
            "level_name": "六级杀手",
            "is_sign": 0
        },
        {
            "user_id": 13724678,
            "forum_id": 69368,
            "forum_name": "高等数学",
            "is_black": 0,
            "is_top": 0,
            "in_time": 1376017112,
            "level_id": 9,
            "cur_score": 1567,
            "score_left": 433,
            "level_name": "曲线积分",
            "is_sign": 0
        },
...
    ],
    "directory": {
        "entertainment": {
            "directory_group": [
                {
                    "name": "娱乐明星",
                    "type": 1,
                    "id": 0,
                    "second_class": [
                        {
                            "name": "港台东南亚明星",
                            "id": 0,
                            "type": 1
                        },
            ...
             */
            foreach (var f in forums)
            {
                var ff = new FavoriteForum();
                ff.LoadData((long) f["forum_id"], (string) f["forum_name"],
                    Utility.FromUnixDateTime((long) f["in_time"]*1000), (int) f["level_id"],
                    (int) f["is_sign"] != 0);
                FavoriteForums.Add(ff);
            }
            OneKeySignedInSignificant = _FavoriteForums.Any(f => f.Level >= 7 && !f.HasSignedIn);
        }

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages
        {
            get
            {
                _Messages.Update();
                return _Messages;
            }
        }

        /// <summary>
        /// 获取当前账户已经关注的贴吧。
        /// </summary>
        public IList<FavoriteForum> FavoriteForums
        {
            get { return _FavoriteForums; }
        }

        /// <summary>
        /// 是否已经成功执行过一键签到。
        /// </summary>
        public bool HasOneKeySignedIn { get; private set; }

        /// <summary>
        /// 获取一个值，指示一键签到是否有意义。
        /// </summary>
        public bool OneKeySignedInSignificant { get; private set; }

        /// <summary>
        /// 访问具有指定名称的贴吧。
        /// </summary>
        public ForumVisitor Forum(string name)
        {
            var f = (ForumVisitor)forumCache[name];
            if (f == null)
            {
                //prune
                if (forumCache.Count >= ForumCacheCapacity)
                {
                    while (forumCache.Count > ForumCacheCapacity / 2)
                        forumCache.RemoveAt(0);
                }
                f = new ForumVisitor(name, Root);
                forumCache[name] = f;
            }
            f.Update();
            return f;
        }

        /// <summary>
        /// 直接访问指定 Id 的主题。
        /// </summary>
        public TopicVisitor GetTopic(long topicId)
        {
            var v = new TopicVisitor(topicId, Root);
            v.Update();
            return v.IsExists ? v : null;
        }

        /// <summary>
        /// 直接访问指定 Id 的主题，并定位到包含指定Id帖子的页面上。
        /// </summary>
        public TopicVisitor GetTopic(long topicId, long anchorPostId)
        {
            var v = new TopicVisitor(topicId, anchorPostId, Root);
            v.Update();
            return v.IsExists ? v : null;
        }

        /// <summary>
        /// 直接访问指定 Id 的帖子或楼中楼所在的主题，并返回主题中包含指定 Id 的帖子（主题帖或楼中楼）。
        /// </summary>
        /// <remarks>
        /// 注意现在百度有将LZL（comment）转换为子帖（SubPost）的趋势，目前看来，LZL和主贴的ID是不重复的。
        /// 目前仅支持每层至多前十层楼中楼的检查。
        /// </remarks>
        public PostVisitorBase GetPost(long topicId, long postId)
        {
            var t = GetTopic(topicId, postId);
            if (!t.IsExists) return null;
            return (PostVisitorBase)t.Posts.FirstOrDefault(p => p.Id == postId) ??
                   t.Posts.SelectMany(p => p.SubPosts).FirstOrDefault(sp => sp.Id == postId);
        }

        /// <summary>
        /// 对贴吧执行搜索。
        /// </summary>
        public SearchVisitor Search(string keyword = null, string forumName = null,
            string userName = null, bool reversedOrder = false)
        {
            return new SearchVisitor(Root, keyword, forumName, userName, reversedOrder);
        }

#region 操作
        /// <summary>
        /// 一键签到。
        /// </summary>
        public int OneKeySignIn()
        {
            Logging.Enter(this);
            var siParams = new NameValueCollection
            {
                {"ie", "utf-8"},
                {"tbs", PageData_Tbs}
            };
            using (var client = Session.CreateWebClient())
            {
                var resultStr = client.UploadValuesAndDecode(OneKeySignInUrl, siParams);
                var result = JObject.Parse(resultStr);
                /*
{
    "no": 0,
    "error": "success",
    "data": {
        "signedForumAmount": 16,
        "signedForumAmountFail": 0,
        "unsignedForumAmount": 9,
        "vipExtraSignedForumAmount": 9,
        "forum_list": [
            {
                "forum_id": 5234418,
                "forum_name": "绝境狼王",
                "is_sign_in": 1,
                "level_id": 11,
                "cont_sign_num": 1,
                "loyalty_score": {
                    "normal_score": 2,
                    "high_score": 14
                }
            },
            {
                "forum_id": 2195006,
                "forum_name": "mark5ds",
                "is_sign_in": 1,
                "level_id": 11,
                "cont_sign_num": 1,
                "loyalty_score": {
                    "normal_score": 2,
                    "high_score": 14
                }
            },
            ...
        ],
        "gradeNoVip": 32,
        "gradeVip": 350
    }
}
                 */
                switch ((int)result["no"])
                {
                    case 0:
                        //更新数据。
                        foreach (var f in result["data"]["forum_list"])
                        {
                            var ff = _FavoriteForums.FirstOrDefault(_f => _f.Id == (long) f["forum_id"]);
                            if (ff != null)
                            {
                                ff.LoadData((int) f["level_id"], (int) f["is_sign_in"] != 0);
                            }
                            else
                            {
                                ff = new FavoriteForum();
                                _FavoriteForums.Add(ff);
                                ff.LoadData((long) f["forum_id"], (string) f["forum_name"], DateTime.MinValue,
                                    (int) f["level_id"], (int) f["is_sign_in"] != 0);
                            }
                        }
                        HasOneKeySignedIn = true;
                        return Logging.Exit(this, (int)result["data"]["signedForumAmount"]);
                    case 2500113:   //所有符合签到要求的贴吧均已签到完毕。
                        HasOneKeySignedIn = true;
                        return Logging.Exit(this, 0);
                    default:
                        throw new OperationFailedException((int) result["no"], (string) result["error"]);
                }
            }
        }
#endregion

        internal TiebaVisitor(BaiduVisitor root)
            : base(root)
        {
            _Messages = new MessagesVisitor(Root);
        }
    }
}
