using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    /// <summary>
    /// 用于贴吧迎新。
    /// </summary>
    /// <remarks>
    /// 规则：A 如果在标题中检索到指定的关键字，
    /// B 且作者的等级小于 AuthorLevelLimit
    /// 且满足
    ///     C 回帖数小于 RepliesCountLower
    ///     D 或 最后回复时间小于 LastReplyTimeLower
    /// E 且最后10层回复中有超过一半的回复有迎新倾向。
    /// 则判断为新人帖。
    /// </remarks>
    public class NewbieDetector : Strategy
    {
        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> TopicKeywords { get; set; }

        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> ReplyKeywords { get; set; }

        public int RepliesCountLower { get; set; }

        public TimeSpan LastReplyTimeLower { get; set; }

        /// <summary>如果发贴人的等级超过此数目，则忽略此帖。</summary>
        public int AuthorLevelLimit { get; set; }

        private static bool InString(string test, string match)
        {
            if (string.IsNullOrEmpty(test) || string.IsNullOrEmpty(match))
                return string.IsNullOrEmpty(test) && string.IsNullOrEmpty(match);
            return test.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static TopicVisitor[] emptyTopics = {};

        public IList<TopicVisitor> CheckForum(string forumName, int maxCount = 20)
        {
            if (TopicKeywords == null && ReplyKeywords == null) return emptyTopics;
            var visitor = new BaiduVisitor(WebSession);
            var f = visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new InvalidOperationException();
            var now = DateTime.Now;
            Func<PostVisitor, float?> MatchNewbieReply = p =>
            {
                return ReplyKeywords.Any(k => InString(p.Content, k)) ? 1.0f : 0;
            };
            Func<TopicVisitor, bool> MatchNewbieTopic = t =>
            {
                if (t.IsTop || t.IsGood) return false;
                if (t.LastReplyTime == null) return false;
                if (string.IsNullOrEmpty(t.AuthorName)) return false;
                if (!TopicKeywords.Any(k => InString(t.Title, k))) return false;
                //检查是否有必要回帖。
                if (t.RepliesCount > RepliesCountLower && (now - t.LastReplyTime) > LastReplyTimeLower)
                    return false;
                //检查之前是否迎新。
                if (IsNewbie(t.Id) || IsNewbie(t.AuthorName, f.Id))
                    return false;
                //扫描回帖。
                var top = t.Posts.OfAuthor(t.AuthorName).FirstOrDefault();
                if (top == null) return false;
                if (top.Author.Level == null || top.Author.Level > AuthorLevelLimit) return false;
                var matchResult = t.LatestPosts().ExcludeAuthor(t.AuthorName).Average(MatchNewbieReply);
                return matchResult > 0.5;
            };
            return f.Topics.EnumerateToEnd().Take(maxCount).Where(MatchNewbieTopic).ToArray();
        }


        public static readonly XName XNNewbie = "newbie";
        public static readonly XName XNTiebaNewbie = "tiebaNewbie";
        public static readonly XName XNForum = "forum";
        public static readonly XName XNTime = "time";

        /// <summary>
        /// 注册指定的主题，将其记录为新人主题。
        /// </summary>
        public void SetAsNewbie(TopicVisitor topic)
        {
            if (topic == null) throw new ArgumentNullException("topic");
            //记录帖子。
            var value = TiebaStatusManager.GetTopicStatus(Repository, topic.Id);
            value.SetAttributeValue(XNNewbie, true);
            //记录用户。
            value = BaiduUserManager.GetUserStatus(Repository, topic.AuthorName);
            var tiebaNewbieElemenet = value.Element(XNTiebaNewbie);
            if (tiebaNewbieElemenet == null)
            {
                tiebaNewbieElemenet = new XElement(XNTiebaNewbie);
                value.Add(tiebaNewbieElemenet);
            }
            if (tiebaNewbieElemenet.Elements(XNForum).All(e => (long) e != topic.Id))
            {
                tiebaNewbieElemenet.Add(new XElement(XNForum, topic.ForumId,
                    new XAttribute(XNTime, topic.LastReplyTime ?? DateTime.Now)));
            }
            Repository.SubmitChanges();
        }
        
        public bool IsNewbie(string user, long fid)
        {
            return BaiduUserManager.GetUserStatus(Repository, user)
                .Elements(XNTiebaNewbie)
                .Elements(XNForum)
                .Select(e => (long) e)
                .Any(i => i == fid);
        }

        public bool IsNewbie(long topicId)
        {
            return (bool?) TiebaStatusManager.GetTopicStatus(Repository, topicId).Attribute(XNNewbie) ?? false;
        }

        public NewbieDetector(Session session)
            : base(session)
        {
            RepliesCountLower = 10;
            LastReplyTimeLower = TimeSpan.FromDays(3);
            AuthorLevelLimit = 7;
        }
    }
}
