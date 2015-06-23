using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

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

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages { get; private set; }

        /// <summary>
        /// 访问具有指定名称的贴吧。
        /// </summary>
        public ForumVisitor Forum(string name)
        {
            var f = (ForumVisitor)forumCache[name];
            if (f == null)
            {
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
