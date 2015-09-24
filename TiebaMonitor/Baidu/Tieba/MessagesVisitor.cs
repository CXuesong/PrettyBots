using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class MessagesVisitor : ChildVisitor<BaiduVisitor>
    {
        //{0} -- UnixTime
        //private const string NotifierUrlFormat = "http://tbmsg.baidu.com/gmessage/get?mtype=1&_={0}";
        // 0 : Portrait
        private const string NotifierCounterUrlFormat = "http://message.tieba.baidu.com/i/msg/get_data?user={0}";

        // 0 : Type e.g. 4 ，与 initItiebaMessage 的顺序保持一致，以 1 为下标。
        // 1 : Portrait
        // 2 : Unix Time
        public const string ClearNotificationsUrlFormat = "http://message.tieba.baidu.com/i/msg/clear_data?type={0}&user={1}&stamp={2}";

        // 0 : Portrait
        // 1 : Type
        //      replyme
        //      atme
        //      friendapply
        //      fans
        public const string MessageListUrlFormat = "http://tieba.baidu.com/i/sys/jump?u={0}&type={1}";

        public const int MaxPeekedReplications = 100;

        /// <summary>
        /// 获取新消息的计数。
        /// </summary>
        public NewMessagesCounters Counters { get; set; }

        private static Regex countersMatcher = new Regex(@"\[.*\]");

        protected override async Task OnFetchDataAsync()
        {
            //initItiebaMessage([0,0,0,4,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
            using (var c = Session.CreateWebClient())
            {
                var pageText = await
                    c.DownloadStringTaskAsync(string.Format(NotifierCounterUrlFormat, Root.AccountInfo.Portrait));
                Debug.Print(pageText);
                var match = countersMatcher.Match(pageText);
                if (!match.Success) throw new UnexpectedDataException();
                Counters = new NewMessagesCounters(JArray.Parse(match.Value));
            }
        }

        /// <summary>
        /// 清除指定的通知。
        /// </summary>
        public void ClearNotifications(params MessageCounter[] counter)
        {
            Root.AccountInfo.CheckPortrait();
            using (var c = Session.CreateWebClient())
            {
                foreach (var ct in counter)
                {
                    var result = c.DownloadString(string.Format(ClearNotificationsUrlFormat,
                        (int) ct + 1, Root.AccountInfo.Portrait, Utility.ToUnixDateTime(DateTime.Now)));
                    Debug.Print("Clear counter:{0}, Result:{1}", ct, result);
                }
            }
        }

        /// <summary>
        /// 清除所有通知。
        /// </summary>
        public void ClearNotifications()
        {
            ClearNotifications(MessageCounter.FollowedMe, MessageCounter.RepliedMe,
                MessageCounter.ReferredMe,
                MessageCounter.PostsRecycled);
        }

#region 消息获取

        private ReplicationMessageListView _RepliedMe;

        public ReplicationMessageListView RepliedMe
        {
            get
            {
                return _RepliedMe;
            }
        }

        private ReplicationMessageListView _ReferredMe;
        public ReplicationMessageListView ReferredMe
        {
            get
            {
                return _ReferredMe;
            }
        }

        /// <summary>
        /// 异步获取回复或提到本账户的帖子。
        /// </summary>
        public async Task<IList<PostStub>> PeekReplicationsAsync(bool clearNotifications)
        {
            await UpdateAsync(true);
            var posts = new List<PostStub>();
            if (Counters.RepliedMe > 0)
            {
                if (clearNotifications) ClearNotifications(MessageCounter.RepliedMe);
                await _RepliedMe.RefreshAsync();
                posts.AddRange(_RepliedMe.EnumerateToEnd()
                    .Take(Math.Min(Counters.RepliedMe, MaxPeekedReplications)));
            }
            if (Counters.ReferredMe > 0)
            {
                if (clearNotifications) ClearNotifications(MessageCounter.ReferredMe);
                await _ReferredMe.RefreshAsync();
                posts.AddRange(_ReferredMe.EnumerateToEnd().Take(Math.Min(Counters.ReferredMe, MaxPeekedReplications)));
            }
            return posts.Distinct(PostEqualityComparer.Default).ToArray();
        }

        /// <summary>
        /// 异步获取回复或提到本账户的帖子。
        /// </summary>
        public IList<PostStub> PeekReplications(bool clearNotifications)
        {
            return Utility.WaitForResult(PeekReplicationsAsync(clearNotifications));
        }
#endregion


        internal MessagesVisitor(BaiduVisitor root)
            : base(root)
        {
            _RepliedMe = new ReplicationMessageListView(this,
                string.Format(MessageListUrlFormat, root.AccountInfo.Portrait, "replyme"), ReplicationwMode.RepliedMe);
            _ReferredMe = new ReplicationMessageListView(this,
                string.Format(MessageListUrlFormat, root.AccountInfo.Portrait, "atme"), ReplicationwMode.ReferedMe);
        }
    }

    /// <summary>
    /// 按照 initItiebaMessage 数组的顺序定义新消息类型。
    /// </summary>
    public enum MessageCounter
    {
        FollowedMe = 0,
        RepliedMe = 3,
        ReferredMe = 8,
        PostsRecycled = 9,
    }

    /// <summary>
    /// 用于解析和保存新消息的数目。
    /// </summary>
    public struct NewMessagesCounters
    {
        public int FollowedMe { get; private set; }

        public int RepliedMe { get; private set; }

        public int ReferredMe { get; private set; }

        public int PostsRecycled { get; private set; }

        /// <summary>
        /// 当前是否有新消息。
        /// </summary>
        public bool Any
        {
            get { return FollowedMe != 0 || ReferredMe != 0 || ReferredMe != 0; }
        }

        public override string ToString()
        {
            return string.Format("R{0},@{1},F{2}", RepliedMe, ReferredMe, FollowedMe);
        }

        internal NewMessagesCounters(JArray source)
            : this()
        {
            FollowedMe = (int)source[0];
            RepliedMe = (int)source[3];
            ReferredMe = (int)source[8];
            PostsRecycled = (int) source[9];
        }
    }
}
