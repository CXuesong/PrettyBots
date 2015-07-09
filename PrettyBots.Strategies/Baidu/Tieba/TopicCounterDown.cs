using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    public class TopicCounterDown : Strategy
    {

        public struct CountdownTopicInfo
        {
            public TopicVisitor Topic { get; private set; }

            public int CurrentCounter { get; private set; }

            public CountdownTopicInfo(TopicVisitor topic, int currentCounter) 
                : this()
            {
                Topic = topic;
                CurrentCounter = currentCounter;
            }
        }

        public class CountdownTopicStatus
        {
            /// <summary>
            /// 上一次参与倒数时，倒数的数值。
            /// </summary>
            public int LastCounter { get; set; }

            /// <summary>
            /// 上一次参与倒数时的时间。
            /// </summary>
            public DateTime LastTime { get; set; }

            public CountdownTopicStatus(int lastCounter, DateTime lastTime)
            {
                LastCounter = lastCounter;
                LastTime = lastTime;
            }

            public CountdownTopicStatus()
            {
                
            }
        }

        /// <summary>
        /// 由最后一层扫描帖子时，允许的最大回溯楼层数。
        /// </summary>
        public int MaxTraceBackPostsCount { get; set; }

        private static Regex counterMatcher = new Regex(@"^\d+");

        private static int? ExtractCounter(string postContent)
        {
            var content = Utility.NormalizeString(postContent);
            var result = counterMatcher.Match(content);
            return result.Success ? (int?) Convert.ToInt32(result.Value) : null;
        }

        private static int? ExtractCounter(PostVisitor p)
        {
            var counter = ExtractCounter(p.Content);
            if (counter == null) return null;
            //允许作者修订倒数。
            var revCounter = p.SubPosts.Where(sp => sp.Author == p.Author)
                .Select(sp => ExtractCounter(sp.Content)).LastOrDefault();
            return revCounter ?? counter;
        }

        /// <summary>
        /// 使用线性拟合计算下一层应当倒数的值。
        /// </summary>
        public int? NextCounter(TopicVisitor t)
        {
            if (!t.IsExists) return null;
            var traceback = t.Posts.Navigate(PageRelativeLocation.Last)
                .EnumerateToBeginning().Take(MaxTraceBackPostsCount)
                .Select(p => new {floor = p.Floor, counter = ExtractCounter(p)})
                .Where(p => p.counter != null).ToArray();
            foreach (var tb in traceback)
                Debug.Print("{0}\t{1}", tb.floor, tb.counter);
            //TODO 拟合。
            return null;
        }

        public int? NextCounter(long topicId)
        {
            var visitor = new BaiduVisitor(Context.Session);
            return NextCounter(visitor.Tieba.GetTopic(topicId));
        }

        /// <summary>
        /// 获取指定主题当前的倒数值。
        /// </summary>
        private int? GetCurrentCounter(TopicVisitor t)
        {
            if (!t.IsExists) return null;
            return t.Posts.Navigate(PageRelativeLocation.Last)
                .EnumerateToBeginning()
                .Select(ExtractCounter).FirstOrDefault();
        }

        /// <summary>
        /// 获取指定主题当前的倒数值。
        /// </summary>
        public int? GetCurrentCounter(long topicId)
        {
            var visitor = new BaiduVisitor(Context.Session);
            return GetCurrentCounter(visitor.Tieba.GetTopic(topicId));
        }

        /// <summary>
        /// 根据已经注册的主题列表，扫描指定类型的主题。
        /// </summary>
        public IList<CountdownTopicInfo> CheckTopics()
        {
            var visitor = new BaiduVisitor(Context.Session);
            return Context.Repository.TiebaStatus.GetStatus(StatusKey)
                .Where(s => s.Topic != null)
                .Select(s => visitor.Tieba.GetTopic(s.Topic.Value))
                .Where(t => t.IsExists)
                .Select(t => new { Topic = t, Counter = GetCurrentCounter(t) })
                .Where(d => d.Counter != null)
                .Select(d => new CountdownTopicInfo(d.Topic, d.Counter.Value))
                .ToList();
        }

        public const string StatusKey = "CountdownTopic";
        public const string FinishedStatusKey = "CountdownTopic.Finished";

        /// <summary>
        /// 注册指定的主题，将其记录为倒数接龙主题。
        /// </summary>
        /// <param name="topic">要记录为倒数的主题</param>
        public void RegisterTopic(TopicVisitor topic, CountdownTopicStatus status = null)
        {
            if (topic == null) throw new ArgumentNullException("topic");
            Context.Repository.TiebaStatus.SetTopicStatus(topic.ForumId, topic.Id,
                StatusKey, JsonConvert.SerializeObject(status));
        }

        public CountdownTopicStatus GetTopicStatus(long topicId)
        {
            var s = Context.Repository.TiebaStatus.GetTopicStatus(topicId, StatusKey).FirstOrDefault();
            if (s == null || string.IsNullOrEmpty(s.Value)) return null;
            return JsonConvert.DeserializeObject<CountdownTopicStatus>(s.Value);
        }

        public bool MarkTopicFinished(long topicId)
        {
            var s = Context.Repository.TiebaStatus.GetTopicStatus(topicId, StatusKey).FirstOrDefault();
            if (s == null) return false;
            if (s.Key != FinishedStatusKey)
            {
                s.Key = FinishedStatusKey;
                Context.Repository.SubmitChanges();
            }
            return true;
        }

        public bool IsRegistered(long topicId)
        {
            return Context.Repository.TiebaStatus.GetTopicStatus(topicId, StatusKey).Any();
        }

        public TopicCounterDown(StrategyContext context)
            : base(context)
        {
            MaxTraceBackPostsCount = 20;
        }
    }
}
