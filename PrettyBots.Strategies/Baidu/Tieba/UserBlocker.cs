using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;
using PrettyBots.Strategies.Repository;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    public class TiebaUserBlocker : Strategy
    {
        public static readonly XName XNUser = "user";
        public static readonly XName XNName = "name";
        public static readonly XName XNForum = "forum";
        public static readonly XName XNTid = "tid";
        public static readonly XName XNPid = "pid";
        public static readonly XName XNLastBlocked = "lastBlocked";
        public static readonly XName XNEnabled = "enabled";

        private BaiduVisitor _Visitor;

        private BaiduVisitor Visitor
        {
            get
            {
                if (_Visitor != null) _Visitor = new BaiduVisitor(WebSession);
                return _Visitor;
            }
        }

        /// <summary>
        /// 两次封禁之间的最短时间间隔。
        /// </summary>
        public TimeSpan BlockInterval { get; set; }

        public bool RegisterUser(string forumName, string userName)
        {
            Logging.Enter(this, forumName);
            var f = Status.Elements(XNForum).FirstOrDefault(xf => (string)xf.Attribute(XNName) == forumName);
            if (f == null)
            {
                f = new XElement(XNForum, new XAttribute(XNName, forumName));
                Status.Add(f);
            }
            if (f.Elements(XNUser).Any(xu => (string)xu.Attribute(XNName) == userName)) return Logging.Exit(this, false);
            f.Add(new XElement(XNUser, new XAttribute(XNName, userName), new XAttribute(XNEnabled, true)));
            return Logging.Exit(this, true);
        }

        /// <summary>
        /// 寻找一个可以用作封禁的用户发帖。
        /// </summary>
        private Tuple<long, long> GetBlockReferer(string forumName, string userName)
        {
            var p = Visitor.Tieba.Search(null, forumName, userName).Result.FirstOrDefault();
            return p == null ? null : Tuple.Create(p.TopicId, p.PostId);
        }

        private static Tuple<string, string, long, long, DateTime> ParseUserInfo(XElement xUser)
        {
            Debug.Assert(xUser.Parent != null);
            return Tuple.Create((string) xUser.Parent.Attribute(XNName), (string) xUser.Attribute(XNName),
                (long?) xUser.Attribute(XNTid) ?? 0, (long?) xUser.Attribute(XNPid) ?? 0,
                (DateTime?) xUser.Attribute(XNLastBlocked) ?? DateTime.MinValue);
        }

        private static void SetUserInfo(XElement xUser, long tid, long pid, DateTime time)
        {
            xUser.SetAttributeValue(XNTid, tid);
            xUser.SetAttributeValue(XNPid, pid);
            xUser.SetAttributeValue(XNLastBlocked, time);
        }

        public override void EntryPoint()
        {
            foreach (var ui in Status.Elements(XNForum).Elements(XNUser).Where(e => (bool?)e.Attribute(XNEnabled)??true)
                .Select(e => Tuple.Create(e, ParseUserInfo(e))))
            {
                var forumName = ui.Item2.Item1;
                var userName = ui.Item2.Item2;
                var tid = ui.Item2.Item3;
                var pid = ui.Item2.Item4;
                var time = ui.Item2.Item5;
                if ((DateTime.Now - time) > BlockInterval)
                {
                    //验证基准帖。
                    var post = tid == 0 ? null : Visitor.Tieba.GetPost(tid, pid);
                    if (post == null)
                    {
                        //需要更新基准帖。
                        var newReferer = GetBlockReferer(forumName, userName);
                        if (newReferer == null) return;
                        tid = newReferer.Item1;
                        pid = newReferer.Item2;
                        post = Visitor.Tieba.GetPost(tid, pid);
                        if (post == null) continue;
                    }
                    //开始封禁。
                    post.BlockAuthor();
                    time = DateTime.Now;
                    //保存设置。
                    SetUserInfo(ui.Item1, pid, tid, time);
                    SubmitStatus();
                }
            }
        }

        public TiebaUserBlocker(Session session)
            : base(session)
        {
            BlockInterval = TimeSpan.FromHours(20);
        }
    }
}
