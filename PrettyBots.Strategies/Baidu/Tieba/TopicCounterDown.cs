using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;
using Newtonsoft.Json.Linq;
using PrettyBots.Strategies.Repository;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    public class TopicCounterDown : Strategy
    {
        private static readonly XName XNPost = "post";
        private static readonly XName XNPid = "pid";
        private static readonly XName XNlastCounter = "lastCounter";
        private static readonly XName XNlastCounting = "lastCounting";
        private static readonly XName XNEnabled = "enabled";

        private static readonly Regex counterMatcher = new Regex(@"^\d+");

        /// <summary>
        /// 由最后一层扫描帖子时，允许的最大回溯楼层数。
        /// </summary>
        public int MaxTraceBackPostsCount { get; set; }

        public TimeSpan ReplyInterval{get;set;}

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
        /// 计算下一层应当倒数的值。
        /// </summary>
        public int? NextCounter(TopicVisitor t)
        {
            if (!t.IsExists) return null;
            if (!t.IsExists) return null;
            return t.Posts.Navigate(PageRelativeLocation.Last)
                .EnumerateToBeginning()
                .Select(ExtractCounter).FirstOrDefault() - 1;
            //TODO 线性拟合。
            //var traceback = t.Posts.Navigate(PageRelativeLocation.Last)
            //    .EnumerateToBeginning().Take(MaxTraceBackPostsCount)
            //    .Select(p => Tuple.Create(p.Floor, ExtractCounter(p)))
            //    .Where(p => p.Item2 != null).ToArray();
            //foreach (var tb in traceback) Debug.Print("{0}", tb);
            //return null;
        }

        public int? NextCounter(long topicId)
        {
            var visitor = new BaiduVisitor(WebSession);
            return NextCounter(visitor.Tieba.GetTopic(topicId));
        }

        /// <summary>
        /// 注册指定的主题，将其记录为倒数接龙主题。
        /// </summary>
        /// <param name="topicId">要记录为倒数的主题</param>
        public bool RegisterTopic(long topicId)
        {
            Logging.Enter(this, topicId);
            if (Status.Elements(XNPost).Any(el => (long) el.Attribute(XNPid) == topicId)) return Logging.Exit(this, false);
            Status.Add(new XElement(XNPost,
                new XAttribute(XNPid, topicId), 
                new XAttribute(XNEnabled, true)));
            SubmitStatus();
            return Logging.Exit(this, true);
        }

        protected override void EntryPointCore()
        {
            //根据已经注册的主题列表，扫描指定类型的主题。
            var visitor = new BaiduVisitor(WebSession);
            var topics = Status.Elements(XNPost)
                .Where(e => (bool) e.Attribute(XNEnabled))
                .Select(e => Tuple.Create(e, visitor.Tieba.GetTopic((long) e.Attribute(XNPid))))
                .ToArray();
            var now = DateTime.Now;
            foreach (var xt in topics)
            {
                var xtopic = xt.Item1;
                var topic = xt.Item2;
                if (topic == null)
                {
                    xtopic.SetAttributeValue(XNEnabled, false);
                    xtopic.Add(new XComment("帖子已经消失。"));
                    continue;
                }
                if (now - topic.LastReplyTime < ReplyInterval) continue;
                if (now - (DateTime?)xtopic.Attribute(XNlastCounting) < ReplyInterval) continue;
                var nextCounter = NextCounter(topic);
                if (nextCounter == null) continue;
                if (nextCounter < 10)
                {
                    xtopic.SetAttributeValue(XNEnabled, false);
                    xtopic.Add(new XComment("Counting Finished."));
                    continue;
                }
                if (topic.Reply(nextCounter.ToString()))
                    xtopic.SetAttributeValue(XNlastCounter, nextCounter);
                SubmitStatus();
            }
        }
        public TopicCounterDown(Session session)
            : base(session)
        {
            MaxTraceBackPostsCount = 20;
        }

    }
}
