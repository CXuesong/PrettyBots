using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 用于贴吧迎新。
    /// </summary>
    public class TiebaNewbieDetector : Strategy
    {
        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> TopicKeywords { get; set; }

        /// <summary>
        /// 用于匹配的关键字。
        /// </summary>
        public IList<string> ReplyKeywords { get; set; }

        /// <summary>[已废弃]如果在整个主题中检测到了指定的文本，则忽略此帖。</summary>
        public string AntiRecursionMagicString { get; set; }

        /// <summary>如果主题的回复数量超过此数目，则忽略此帖。</summary>
        public int RepliesCountLimit { get; set; }

        /// <summary>如果发贴人的等级超过此数目，则忽略此帖。</summary>
        public int AuthorLevelLimit { get; set; }

        private static bool InString(string test, string match)
        {
            if (string.IsNullOrEmpty(test) || string.IsNullOrEmpty(match))
                return string.IsNullOrEmpty(test) && string.IsNullOrEmpty(match);
            return test.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public IList<TopicVisitor> CheckForum(string forumName, int maxCount = 20)
        {
            var suspects = new List<TopicVisitor>();
            if (TopicKeywords == null && ReplyKeywords == null) return suspects;
            var visitor = new BaiduVisitor(Context.Session);
            var f = visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new InvalidOperationException();
            var thisUser = visitor.AccountInfo.UserName;
            foreach (var t in f.Topics().Take(maxCount))
            {
                if (t.IsTop || t.IsGood) continue;
                if (t.RepliesCount > RepliesCountLimit) continue;
                if (string.IsNullOrEmpty(t.AuthorName)) continue;
                if (TopicKeywords != null && TopicKeywords.Any(k => InString(t.Title, k)))
                {
                    var weight = 0.3;
                    var posts = t.Posts().ToList();
                    var top = posts.FirstOrDefault(p => p.Author.Name == t.AuthorName);
                    if (top == null) continue;
                    if (top.Author.Level == null || top.Author.Level > AuthorLevelLimit) continue;
                    if (IsRegistered(t.Id)) continue;
                    if (TopicKeywords != null)
                        weight += 0.2*TopicKeywords.Count(k => InString(t.Title, k));
                    if (ReplyKeywords != null)
                        weight += 0.2*
                                  posts.Count(
                                      p =>
                                          p.Author.Name != t.AuthorName &&
                                          ReplyKeywords.Any(k => InString(p.Content, k)));
                    Debug.Print("{0},{1}", weight, t);
                    if (weight > 1.0) suspects.Add(t);
                }
            }
            return suspects;
        }


        public const string StatusKey = "NewbieTopic";

        /// <summary>
        /// 注册指定的主题，将其记录为新人主题。
        /// </summary>
        public void RegisterTopic(TopicVisitor topic)
        {
            if (topic == null) throw new ArgumentNullException("topic");
            Context.Repository.TiebaStatus.SetTopicStatus(topic.ForumId, topic.Id, StatusKey);
        }

        public void RegisterTopics(IEnumerable<TopicVisitor> topics)
        {
            if (topics == null) return;
            foreach (var tp in topics.Where(t => t != null)) RegisterTopic(tp);
        }

        public bool IsRegistered(long topicId)
        {
            return Context.Repository.TiebaStatus.GetTopicStatus(topicId, StatusKey).Any();
        }

        public TiebaNewbieDetector(StrategyContext context)
            : base(context)
        {
            RepliesCountLimit = 40;
            AuthorLevelLimit = 7;
        }
    }
}
