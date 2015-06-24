using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    public class TiebaUserBlocker : Strategy
    {
        private BaiduVisitor visitor;

        private class UserForumStatusEntry
        {

            public long ReferTopic { get; set; }

            public long ReferPost { get; set; }

            public DateTime LastOperationTime { get; set; }

            [JsonIgnore]
            public bool HasReferer
            {
                get { return ReferTopic > 0 && ReferPost > 0; }
            }

            public UserForumStatusEntry(long referTopic, long referPost, DateTime lastOperationTime)
            {
                ReferTopic = referTopic;
                ReferPost = referPost;
                LastOperationTime = lastOperationTime;
            }

            public UserForumStatusEntry()
            {

            }
        }

        /// <summary>
        /// 两次封禁之间的最短时间间隔。
        /// </summary>
        public TimeSpan BlockInterval { get; set; }

        public bool BlockUser(string forumName, string userName)
        {
            var userStatus = GetUserStatus(userName);
            UserForumStatusEntry forumStatus;
            if (!userStatus.TryGetValue(forumName, out forumStatus) || forumStatus == null)
                userStatus[forumName] = forumStatus = new UserForumStatusEntry();
            if ((DateTime.Now - forumStatus.LastOperationTime) > BlockInterval)
            {
                try
                {
                    PostVisitorBase post;
                    //验证基准帖。
                    if (forumStatus.HasReferer)
                    {
                        post = visitor.Tieba.GetPost(forumStatus.ReferTopic, forumStatus.ReferPost);
                        if (post == null)
                        {
                            //需要更新基准帖。
                            forumStatus.ReferTopic = forumStatus.ReferPost = 0;
                            SetUserReferer(forumName, userName, forumStatus);
                            if (!forumStatus.HasReferer) return false;
                            post = visitor.Tieba.GetPost(forumStatus.ReferTopic, forumStatus.ReferPost);
                        }
                    }
                    else
                    {
                        //初次使用，寻找基准帖。
                        SetUserReferer(forumName, userName, forumStatus);
                        if (!forumStatus.HasReferer) return false;
                        post = visitor.Tieba.GetPost(forumStatus.ReferTopic, forumStatus.ReferPost);
                    }
                    //开始封禁。
                    post.BlockAuthor();
                    forumStatus.LastOperationTime = DateTime.Now;
                    return true;
                }
                finally
                {
                    //保存设置。
                    userStatus[forumName] = forumStatus;
                    SetUserStatus(userName, userStatus);
                }
            }
            return false;
        }

        public const string StatusKey = "TiebaBlockingInfo";

        /// <summary>
        /// 寻找一个可以用作封禁的用户发帖。
        /// </summary>
        private void SetUserReferer(string forumName, string userName, UserForumStatusEntry entry)
        {
            var p = visitor.Tieba.Search(null, forumName, userName).Result
                .Select(r => r.GetPost())
                .FirstOrDefault(p1 => p1 != null);
            if (p == null) return;
            entry.ReferTopic = p.Topic.Id;
            entry.ReferPost = p.Id;
        }

        private Dictionary<string, UserForumStatusEntry> GetUserStatus(string userName)
        {
            var entry = Context.Repository.BaiduUserStatus.GetStatusValue(userName, StatusKey);
            if (string.IsNullOrEmpty(entry)) return new Dictionary<string, UserForumStatusEntry>();
            return JsonConvert.DeserializeObject<Dictionary<string, UserForumStatusEntry>>(entry);
        }

        private void SetUserStatus(string userName, Dictionary<string, UserForumStatusEntry> status)
        {
            Context.Repository.BaiduUserStatus.SetStatusValue(userName, StatusKey,
                status == null ? null : JsonConvert.SerializeObject(status));
        }

        public TiebaUserBlocker(StrategyContext context)
            : base(context)
        {
            visitor = new BaiduVisitor(context.Session);
            BlockInterval = TimeSpan.FromHours(20);
        }
    }
}
