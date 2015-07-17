using System;
using System.Collections.Generic;
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

        public void BlockUser(string forumName, string userName)
        {
            var fid = Utility.ForumNameToId(forumName);
            var userStatus = GetUserStatus(fid, userName) ?? Tuple.Create(0L, 0L, DateTime.MinValue);
            var tid = userStatus.Item1;
            var pid = userStatus.Item2;
            var time = userStatus.Item3;
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
                }
                //开始封禁。
                post.BlockAuthor();
                time = DateTime.Now;
                //保存设置。
                SetUserStatus(fid, userName, pid, tid, time);
            }
        }

        public static readonly XName XNUserBlocker = "userBlocker";
        public static readonly XName XNForum = "forum";
        public static readonly XName XNTid = "tid";
        public static readonly XName XNPid = "pid";
        public static readonly XName XNTime = "time";

        /// <summary>
        /// 寻找一个可以用作封禁的用户发帖。
        /// </summary>
        private Tuple<long, long> GetBlockReferer(string forumName, string userName)
        {
            var p = Visitor.Tieba.Search(null, forumName, userName).Result.FirstOrDefault();
            return p == null ? null : Tuple.Create(p.TopicId, p.PostId);
        }

        private Tuple<long,long,DateTime> GetUserStatus(long fid, string userName)
        {
            var entry = BaiduUserManager.GetUserStatus(Repository, userName);
            var status = entry.Elements(XNUserBlocker).Elements(XNForum).FirstOrDefault(e => (long) e == fid);
            if (status == null) return null;
            return Tuple.Create((long) status.Attribute(XNTid), (long) status.Attribute(XNPid),
                (DateTime) status.Attribute(XNTime));
        }

        private void SetUserStatus(long fid, string userName, long pid, long tid, DateTime time)
        {
            var entry = BaiduUserManager.GetUserStatus(Repository, userName);
            var xForum = entry.CElement(XNUserBlocker);
            var status = xForum.Elements(XNForum).FirstOrDefault(e => (long)e == fid);
            if (status == null)
            {
                status = new XElement(XNForum, fid);
                xForum.Add(status);
            }
            status.SetAttributeValue(XNPid, pid);
            status.SetAttributeValue(XNTid, tid);
            status.SetAttributeValue(XNTime, time);
            Repository.SubmitChanges();
        }

        public TiebaUserBlocker(Session session)
            : base(session)
        {
            BlockInterval = TimeSpan.FromHours(20);
        }
    }
}
