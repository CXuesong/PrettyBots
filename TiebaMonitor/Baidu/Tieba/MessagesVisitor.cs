using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Monitor.Baidu.Tieba
{
    public class MessagesVisitor : ChildVisitor<BaiduVisitor>
    {
        //{0} -- UnixTime
        //private const string NotifierUrlFormat = "http://tbmsg.baidu.com/gmessage/get?mtype=1&_={0}";
        // 0 : Portrait
        private const string NotifierCounterUrlFormat = "http://message.tieba.baidu.com/i/msg/get_data?user={0}";

        // 0 : Type e.g. 4 ，与 initItiebaMessage 的顺序保持一致，以 1 为下标。
        // 1 : Profile
        // 2 : Unix Time
        public const string ClearNotificationsUrlFormat = "http://message.tieba.baidu.com/i/msg/clear_data?type={0}&user={1}&stamp={2}";

        /// <summary>
        /// 获取新消息的计数。
        /// </summary>
        public NewMessagesCounter Counter { get; set; }

        private static Regex countersMatcher = new Regex(@"\[.*\]");

        public void Update()
        {
            //initItiebaMessage([0,0,0,4,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
            using (var c = Session.CreateWebClient())
            {
                var pageText =
                    c.DownloadString(string.Format(NotifierCounterUrlFormat, Parent.AccountInfo.Portrait));
                Debug.Print(pageText);
                var match = countersMatcher.Match(pageText);
                if (!match.Success) throw new UnexpectedDataException();
                Counter = new NewMessagesCounter(JArray.Parse(match.Value));
            }
        }

        public void ClearNotifications(params MessageCounter[] counter)
        {
            Parent.AccountInfo.CheckPortrait();
            using (var c = Session.CreateWebClient())
            {
                foreach (var ct in counter)
                {
                    var result = c.DownloadString(string.Format(ClearNotificationsUrlFormat,
                        (int)ct, Parent.AccountInfo.Portrait, Utility.ToUnixDateTime(DateTime.Now)));
                    Debug.Print("Counter:{0}, Result:{1}", ct, result);
                }
            }
        }

        /// <summary>
        /// 清除所有通知。
        /// </summary>
        public void ClearNotifications()
        {
            ClearNotifications(MessageCounter.FollowedMe, MessageCounter.ReferedMe,
                MessageCounter.ReferedMe,
                MessageCounter.PostsRecycled);
        }

        internal MessagesVisitor(BaiduVisitor parent)
            : base(parent)
        { }
    }

    /// <summary>
    /// 按照 initItiebaMessage 数组的顺序定义新消息类型。
    /// </summary>
    public enum MessageCounter
    {
        FollowedMe = 0,
        RepliedMe = 3,
        ReferedMe = 8,
        PostsRecycled = 9,
    }

    /// <summary>
    /// 用于解析和保存新消息的数目。
    /// </summary>
    public struct NewMessagesCounter
    {
        public int FollowedMe { get; private set; }

        public int RepliedMe { get; private set; }

        public int ReferedMe { get; private set; }

        public int PostsRecycled { get; private set; }

        public bool AnyNewMessage
        {
            get { return FollowedMe != 0 || ReferedMe != 0 || ReferedMe != 0; }
        }

        public override string ToString()
        {
            return string.Format("R{0},@{1},F{2}", RepliedMe, ReferedMe, FollowedMe);
        }

        internal NewMessagesCounter(JArray source)
            : this()
        {
            FollowedMe = (int)source[0];
            RepliedMe = (int)source[3];
            ReferedMe = (int)source[8];
            PostsRecycled = (int) source[9];
        }
    }
}
