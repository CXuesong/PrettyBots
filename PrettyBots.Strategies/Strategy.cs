using System;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public interface IStrategy
    {
        Session Session { get; }
    }

    public class Strategy : IStrategy
    {
        public Session Session { get; private set; }

        public WebSession WebSession {
            get { return Session.WebSession; }
        }

        public PrimaryRepository Repository
        {
            get { return Session.Repository; }
        }

        public Strategy(Session session)
        {
            if (session == null) throw new ArgumentNullException("session");
            Session = session;
        }
    }
}
