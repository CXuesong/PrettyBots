using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 为策略提供执行上下文。
    /// </summary>
    public class StrategyContext
    {
        public string SessionName { get; set; }

        public WebSession Session { get; private set; }

        public PrimaryRepository Repository { get; private set; }

        /// <summary>
        /// 将当前的 Cookies 信息存入 <see cref="SessionName"/> 对应的 Session 中。
        /// </summary>
        public void SaveSession()
        {
            if (string.IsNullOrEmpty(SessionName)) throw new InvalidOperationException();
            Repository.Sessions.SaveSession(SessionName, Session);
        }

        public StrategyContext(WebSession session, PrimaryRepository repository)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (repository == null) throw new ArgumentNullException("repository");
            Session = session;
            Repository = repository;
        }

        /// <summary>
        /// 使用默认参数构造一个包含了 WebSession 和 PrimaryRepository 的策略上下文，
        /// 并根据 sessionName 载入 Cookies 信息。
        /// </summary>
        public StrategyContext(string sessionName)
        {
            Repository = new PrimaryRepository();
            Session = Repository.Sessions.LoadSession(sessionName);
            SessionName = sessionName;
        }
    }
}
