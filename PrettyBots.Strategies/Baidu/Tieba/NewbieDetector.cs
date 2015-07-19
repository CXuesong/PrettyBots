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
    /// B 且作者的等级小于 AuthorLevelUpper
    /// C 且最后回复时间小于 LastReplyTimeSpanExplicitUpper
    /// 且满足
    ///     C 回帖数小于 RepliesCountUpper
    ///     D 或 最后回复时间小于 LastReplyTimeSpanUpper
    /// E 且最后10层回复中有超过一半的回复有迎新倾向。
    /// 则判断为新人帖。
    /// </remarks>
    public class NewbieDetector : Strategy
    {
        private static readonly XName XNForum = "forum";
        private static readonly XName XNUser = "user";
        private static readonly XName XNName = "name";
        private static readonly XName XNTid = "tid";
        private static readonly XName XNTime = "time";
        private static readonly XName XNEnabled = "enabled";

        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> TopicKeywords { get; set; }

        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> ReplyKeywords { get; set; }

        public int RepliesCountUpper { get; set; }

        public TimeSpan LastReplyTimeSpanExplicitUpper { get; set; }

        public TimeSpan LastReplyTimeSpanUpper { get; set; }

        public TopicReplyGenerator ReplyGenerator { get; set; }

        /// <summary>如果发贴人的等级超过此数目，则忽略此帖。</summary>
        public int AuthorLevelUpper { get; set; }

        private static bool InString(string test, string match)
        {
            if (string.IsNullOrEmpty(test) || string.IsNullOrEmpty(match))
                return string.IsNullOrEmpty(test) && string.IsNullOrEmpty(match);
            return test.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public bool RegisterForum(string forumName)
        {
            Logging.Enter(this, forumName);
            if (Status.Elements(XNForum).Any(el => (string) el.Attribute(XNName) == forumName))
                return Logging.Exit(this, false);
            Status.Add(new XElement(XNForum,
                new XAttribute(XNName, forumName),
                new XAttribute(XNEnabled, true)));
            SubmitStatus();
            return Logging.Exit(this, true);
        }

        /// <summary>
        /// 注册指定的主题，将其记录为新人主题。
        /// </summary>
        public void SetAsNewbie(TopicVisitor topic, string forumName)
        {
            if (topic == null) throw new ArgumentNullException("topic");
            var xf = Status.Elements(XNForum).First(e => (string) e.Attribute(XNName) == forumName);
            xf.Add(new XElement(XNUser, new XAttribute(XNName, topic.AuthorName),
                new XAttribute(XNTid, topic.Id), new XAttribute(XNTime, topic.LastReplyTime ?? DateTime.Now)));
            SubmitStatus();
        }

        public bool HasRegistered(string user, string forumName)
        {
            var xf = Status.Elements(XNForum).FirstOrDefault(e => (string)e.Attribute(XNName) == forumName);
            if (xf == null) return false;
            return xf.Elements(XNUser).Any(e => Utility.UserIdentity(user, (string) e.Attribute(XNName)));
        }

        public NewbieDetector(Session session)
            : base(session)
        {
            RepliesCountUpper = 10;
            LastReplyTimeSpanUpper = TimeSpan.FromDays(3);
            LastReplyTimeSpanExplicitUpper = TimeSpan.FromDays(45);
            AuthorLevelUpper = 7;
            TopicKeywords = new[]
            {
                "新人", "报道"
            };
            ReplyKeywords = new[]
            {
                "你好", "欢迎", "这里", "介里", "大家好", "求昵称"
            };
        }

        private bool InspectTopic(TopicVisitor t)
        {
            //Debug.Print("Inspect : {0}", t);
            if (t.IsTop || t.IsGood
                || (t.LastReplyTime ?? DateTime.MinValue) - DateTime.Now > LastReplyTimeSpanUpper
                || string.IsNullOrEmpty(t.AuthorName)
                || (t.RepliesCount > RepliesCountUpper)) return false;
            if (!TopicKeywords.Any(k => InString(t.Title, k))) return false;
            //检查是否有必要回帖。
            if (t.RepliesCount > RepliesCountUpper 
                && (DateTime.Now - t.LastReplyTime) > LastReplyTimeSpanExplicitUpper)
                return false;
            return InspectInTopic(t);
        }

        private bool InspectInTopic(TopicVisitor t)
        {
            //扫描回帖。
            //Debug.Print("Inspect In : {0}", t);
            var top = t.Posts.OfAuthor(t.AuthorName).FirstOrDefault();
            if (top == null) return false;
            if (top.Author.Level == null || top.Author.Level > AuthorLevelUpper) return false;
            var matchResult =
                t.LatestPosts().ExcludeAuthor(t.AuthorName).Average((Func<PostVisitor, float?>) MatchNewbieReply);
            return matchResult > 0.5;
        }

        private float? MatchNewbieReply(PostVisitor p)
        {
            return ReplyKeywords.Any(k => InString(p.Content, k)) ? 1.0f : 0;
        }

        private void ReplySuspects(IEnumerable<TopicVisitor> topics)
        {
            var rg = ReplyGenerator ?? DefaultReplyGenerator;
            foreach (var topic in topics) topic.Reply(rg(topic));
        }

        private string DefaultReplyGenerator(TopicVisitor topic)
        {
            return string.Format(Prompts.TiebaNewbieDetectorDefaultReply, topic.ForumName);
        }

        private IEnumerable<TopicVisitor> InspectForum(string forumName, BaiduVisitor visitor)
        {
            var forum = visitor.Tieba.Forum(forumName);
            if (!forum.IsExists) return Enumerable.Empty<TopicVisitor>();
            //仅检查第一页。
            return forum.Topics
                .Where(InspectTopic)
                .Where(t => !HasRegistered(t.AuthorName, forumName))
                .Where(InspectInTopic);
        }

        public IEnumerable<TopicVisitor> InspectForum(string forumName)
        {
            return InspectForum(forumName, new BaiduVisitor(WebSession));
        }

        public override void EntryPoint()
        {
            if (TopicKeywords == null && ReplyKeywords == null) return;
            var visitor = new BaiduVisitor(WebSession);
            foreach (var xf in Status.Elements(XNForum).Where(e => (bool?)e.Attribute(XNEnabled) ?? true))
            {
                ReplySuspects(InspectForum((string)xf.Attribute(XNName), visitor));
            }
        }
    }
}
