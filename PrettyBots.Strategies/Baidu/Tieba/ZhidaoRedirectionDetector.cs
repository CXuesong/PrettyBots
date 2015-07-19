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
    /// 给定主题，生成指定主题的回复。
    /// </summary>
    public delegate string TopicReplyGenerator(TopicVisitor topic);

    /// <summary>
    /// 用于检查由百度知道重定向而来的贴吧主题。
    /// 规则：
    /// 如果帖子不遵循前缀规范，
    /// 且内容为 StrongMatcher
    ///     或标题长度大于10，
    ///     且标题与内容前半部分完全重合，
    ///     且包含 WeakMatcher 中的内容
    /// 且回复数量小于 RepliesCountUpper
    /// 且LZ等级低于 AuthorLevelUpper
    /// 则标记为重定向帖。
    /// </summary>
    public class ZhidaoRedirectionDetector : Strategy
    {
        private static readonly XName XNName = "name";
        private static readonly XName XNForum = "forum";
        private static readonly XName XNTopic = "topic";
        private static readonly XName XNTid = "tid";
        private static readonly XName XNTitle = "title";
        private static readonly XName XNAuthor = "author";
        private static readonly XName XNTime = "time";
        private static readonly XName XNEnabled = "enabled";

        private IList<string> _WeakMatcher;
        private List<string> internalWeakMatcher = new List<string>();
        private List<string> internalStrongMatcher = new List<string>();
        private IList<string> _StrongMatcher;

        public IList<string> StrongMatcher
        {
            get { return _StrongMatcher; }
            set
            {
                if (_StrongMatcher != value)
                {
                    _StrongMatcher = value;
                    internalStrongMatcher.Clear();
                    if (_StrongMatcher != null)
                        internalStrongMatcher.AddRange(_StrongMatcher
                            .Select(m => m.Trim())
                            .Where(s => !string.IsNullOrEmpty(s)));
                }
            }
        }

        public IList<string> WeakMatcher
        {
            get { return _WeakMatcher; }
            set
            {
                if (_WeakMatcher != value)
                {
                    _WeakMatcher = value;
                    internalWeakMatcher.Clear();
                    if (_WeakMatcher != null)
                        internalWeakMatcher.AddRange(_WeakMatcher
                            .Select(Utility.NormalizeString)
                            .Where(s => !string.IsNullOrEmpty(s)));
                }
            }
        }

        /// <summary>如果主题的回复数量超过此数目，则忽略此帖。</summary>
        public int RepliesCountUpper { get; set; }

        public TimeSpan LastReplyTimeSpanUpper { get; set; }

        /// <summary>如果发贴人的等级超过此数目，则忽略此帖。</summary>
        public int AuthorLevelUpper { get; set; }

        public TimeSpan LastReplyTimeLower { get; set; }

        public TopicReplyGenerator ReplyGenerator { get; set; }

        private static bool InString(string test, string match, bool requireBeginWith = false)
        {
            if (string.IsNullOrEmpty(test)) return string.IsNullOrEmpty(match);
            var index = test.IndexOf(match, StringComparison.CurrentCulture);
            return requireBeginWith ? index == 0 : index >= 0;
        }

        public bool RegisterForum(string forumName)
        {
            Logging.Enter(this, forumName);
            if (Status.Elements(XNForum).Any(el => (string)el.Attribute(XNName) == forumName))
                return Logging.Exit(this, false);
            Status.Add(new XElement(XNForum,
                new XAttribute(XNName, forumName),
                new XAttribute(XNEnabled, true)));
            SubmitStatus();
            return Logging.Exit(this, true);
        }

        /// <summary>
        /// 注册指定的主题，将其记录为知道重定向主题。
        /// </summary>
        private void RegisterTopic(string forumName, TopicVisitor topic)
        {
            if (topic == null) throw new ArgumentNullException("topic");
            var f = Status.Elements(XNForum).First(xf => (string) xf.Attribute(XNName) == forumName);
            f.Add(new XElement(XNTopic, new XAttribute(XNTid, topic.Id),
                new XAttribute(XNTitle, topic.Title), new XAttribute(XNAuthor, topic.AuthorName),
                new XAttribute(XNTime, DateTime.Now)));
            SubmitStatus();
        }

        public bool HasRegistered(long topicId)
        {
            return Status.Elements(XNForum).Elements(XNTopic).Any(xt => (long) xt.Attribute(XNTid) == topicId);
        }

        public ZhidaoRedirectionDetector(Session session)
            : base(session)
        {
            RepliesCountUpper = 20;
            AuthorLevelUpper = 4;
        }

        private string DefaultReplyGenerator(TopicVisitor topic)
        {
            return Prompts.ZhidaoRedirectionDetectorDefaultReply;
        }

        private void ReplyAndRegisterSuspects(string forumName, IEnumerable<TopicVisitor> topics)
        {
            var rg = ReplyGenerator ?? DefaultReplyGenerator;
            foreach (var topic in topics)
            {
                if (topic.Reply(rg(topic)))
                    RegisterTopic(forumName, topic);
            }
        }

        private bool InspectTopic(TopicVisitor t, ForumVisitor f)
        {
            if (t.IsTop || t.IsGood
                || (t.LastReplyTime ?? DateTime.MinValue) - DateTime.Now > LastReplyTimeSpanUpper
                || string.IsNullOrWhiteSpace(t.PreviewText)
                || (t.RepliesCount > RepliesCountUpper)) return false;
            var title = Utility.NormalizeString(t.Title);
            var preview = Utility.NormalizeString(t.PreviewText);
            var normalizedFn = Utility.NormalizeString(f.Name);
            //含有论坛名称，白名单。
            if (InString(title, normalizedFn) || InString(preview, normalizedFn)) return false;
            //遵循帖子标题格式，白名单。
            if (f.TopicPrefix.Count > 0 && f.IsTopicPrefixMatch(t.Title)) return false;
            //判断标题与内容是否重复。
            if (t.Title.Length > 10 && t.PreviewText.Length >= t.Title.Length)
            {
                if (InString(preview, title))
                {
                    //貌似重复了……
                    //检查帖子内容。
                    if (internalWeakMatcher.Any(m => InString(preview, m, true)))
                    {
                        Debug.Print("Weak Matcher: {0}", t);
                        return true;
                    }
                }
            }
            if (internalStrongMatcher.
                Any(
                    m =>
                        string.Compare(m, t.PreviewText, StringComparison.CurrentCultureIgnoreCase) == 0))
            {
                Debug.Print("Strong Matcher: {0}", t);
                return true;
            }
            return false;
        }

        private bool InspectInTopic(TopicVisitor t)
        {
            var posts = t.Posts.ToList();
            var authorPost = posts.OfAuthor(t.AuthorName).FirstOrDefault();
            if (authorPost == null) return false;
            if (authorPost.Author.Level < AuthorLevelUpper)
            {
                Debug.Print("\tMatched: {0}", t.Id);
                return true;
            }
            return false;
        }

        public IEnumerable<TopicVisitor> InspectForum(string forumName, BaiduVisitor visitor)
        {
            var forum = visitor.Tieba.Forum(forumName);
            if (!forum.IsExists) return Enumerable.Empty<TopicVisitor>();
            //仅检查第一页。
            return forum.Topics
                .Where(t => InspectTopic(t, forum))
                .Where(t => !HasRegistered(t.Id))
                .Where(InspectInTopic);
        }

        public override void EntryPoint()
        {
            if (StrongMatcher == null || StrongMatcher.Count == 0) return;
            var visitor = new BaiduVisitor(Session.WebSession);
            foreach (var xf in Status.Elements(XNForum).Where(e => (bool?)e.Attribute(XNEnabled) ?? true))
            {
                ReplyAndRegisterSuspects((string) xf.Attribute(XNName),
                    InspectForum((string) xf.Attribute(XNName), visitor));
            }
        }
    }
}
