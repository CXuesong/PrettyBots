using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    /// <summary>
    /// 用于访问百度贴吧。
    /// </summary>
    public class TiebaVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string TiebaIndexUrl = "http://tieba.baidu.com";

        public const int ForumCacheCapacity = 30;

        private OrderedDictionary forumCache = new OrderedDictionary(ForumCacheCapacity,
            StringComparer.OrdinalIgnoreCase);

        private List<FavoriteForum> _FavoriteForums = new List<FavoriteForum>();

        private string _TiebaPageCache;
        // 调用方： BaiduAccountInfo
        internal void SetTiebaPageCache(string html)
        {
            _TiebaPageCache = html;
        }

        protected override async Task OnFetchDataAsync()
        {
            if (_TiebaPageCache == null)
                using (var client = Session.CreateWebClient())
                    _TiebaPageCache = await client.DownloadStringTaskAsync(TiebaIndexUrl);
            //使用 HtmlDocument 以进行必要的字符转换。
            var doc = new HtmlDocument();
            doc.LoadHtml(_TiebaPageCache);
            _TiebaPageCache = null;
            FavoriteForums.Clear();
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
        }

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages { get; private set; }

        /// <summary>
        /// 获取当前账户已经关注的贴吧。
        /// </summary>
        public IList<FavoriteForum> FavoriteForums
        {
            get { return _FavoriteForums; }
        }

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
            return v;
        }

        /// <summary>
        /// 直接访问指定 Id 的主题，并定位到包含指定Id帖子的页面上。
        /// </summary>
        public TopicVisitor GetTopic(long topicId, long anchorPostId)
        {
            var v = new TopicVisitor(topicId, anchorPostId, Root);
            v.Update();
            return v;
        }

        /// <summary>
        /// 直接访问指定 Id 的帖子或楼中楼所在的主题，并返回主题中包含指定 Id 的帖子（主题帖或楼中楼）。
        /// </summary>
        /// <remarks>
        /// 注意现在百度有将LZL（comment）转换为子帖（SubPost）的趋势，目前看来，LZL和主贴的ID是不重复的。
        /// 目前仅支持每层至多前十层楼中楼的检查。
        /// </remarks>
        public PostVisitorBase GetPost(long topicId, long anchorPostId)
        {
            var t = GetTopic(topicId, anchorPostId);
            if (!t.IsExists) return null;
            return (PostVisitorBase)t.Posts.FirstOrDefault(p => p.Id == anchorPostId) ??
                   t.Posts.SelectMany(p => p.SubPosts).FirstOrDefault(sp => sp.Id == anchorPostId);
        }

        /// <summary>
        /// 对贴吧执行搜索。
        /// </summary>
        public SearchVisitor Search(string keyword = null, string forumName = null,
            string userName = null, bool reversedOrder = false)
        {
            return new SearchVisitor(Root, keyword, forumName, userName, reversedOrder);
        }

        internal TiebaVisitor(BaiduVisitor root)
            : base(root)
        {
            Messages = new MessagesVisitor(Root);
        }
    }
}
