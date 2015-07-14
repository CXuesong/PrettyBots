using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu
{
    /// <summary>
    /// 用于管理百度平台的消息。
    /// </summary>
    public class MessagesVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string MessageCounterUrl = "http://msg.baidu.com/msg/msg_dataGetmsgCount";

        public NewMessagesCounter Counter { get; private set; }

        private static Regex messageCounterMatcher = new Regex(@"\(\s*({.*})\s*\)");

        protected override async Task OnFetchDataAsync()
        {
            using (var c = Session.CreateWebClient())
            {
                //({'sysMsgNum':'0','actMsgNum':'0','mailMsgNum':'0','myFRDNum':'0','mySTRNum':'0'});
                var pageContent = await c.DownloadStringTaskAsync(MessageCounterUrl);
                var match = messageCounterMatcher.Match(pageContent);
                if (!match.Success) throw new UnexpectedDataException();
                var ct = JObject.Parse(match.Groups[1].Value);
                Counter = new NewMessagesCounter
                {
                    SystemMessages = (int) ct["sysMsgNum"],
                    AccountMessages = (int) ct["actMsgNum"],
                    PrivateMessages = (int) ct["mailMsgNum"],
                    FriendPrivateMessages = (int) ct["myFRDNum"],
                    StrangerPrivateMessages = (int) ct["mySTRNum"]
                };
                Debug.Assert(Counter.PrivateMessages == Counter.FriendPrivateMessages + Counter.StrangerPrivateMessages);
            }
        }

        internal MessagesVisitor(BaiduVisitor root)
            : base(root)
        { }
    }

    /// <summary>
    /// 用于解析和保存新消息的数目。
    /// </summary>
    public struct NewMessagesCounter
    {
        public int SystemMessages { get; internal set; }

        public int AccountMessages { get; internal set; }

        public int PrivateMessages { get; internal set; }

        public int FriendPrivateMessages { get; internal set; }

        public int StrangerPrivateMessages { get; internal set; }

        public bool Any
        {
            get
            {
                return SystemMessages != 0 || AccountMessages != 0
                       || PrivateMessages != 0 || FriendPrivateMessages != 0
                       || StrangerPrivateMessages != 0;
            }
        }

        public override string ToString()
        {
            return string.Format("Sys:{0},Acc:{1},Msg:{2}(Fri:{3},Str:{4})",
                SystemMessages, AccountMessages,
                PrivateMessages, FriendPrivateMessages, StrangerPrivateMessages);
        }
    }
}
