using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Monitor.Baidu;
using PrettyBots.Monitor.Baidu.Tieba;

namespace PrettyBots.Monitor.Strategies
{
    /// <summary>
    /// 用于贴吧迎新。
    /// </summary>
    public class TiebaWelcomePoster : Strategy<BaiduVisitor>
    {
        public IEnumerable<TopicVisitor> CheckForum(string forumName)
        {
            throw new NotImplementedException();
        }

        public TiebaWelcomePoster(BaiduVisitor visitor)
            : base(visitor)
        {

        }
    }
}
