using System;
using System.Reflection;
using System.Xml.Linq;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public interface IStrategy
    {
        Session Session { get; }

        void EntryPoint();
    }

    public abstract class Strategy : IStrategy
    {
        public Session Session { get; private set; }

        public WebSession WebSession
        {
            get { return Session.WebSession; }
        }

        public PrimaryRepository Repository
        {
            get { return Session.Repository; }
        }

        private XElement _Status;
        protected XElement Status
        {
            get
            {
                if (_Status == null) _Status = Session.Status(StrategyName).Status;
                return _Status;
            }
        }

        protected void SubmitStatus()
        {
            //需要显式通知数据库，Status 发生了变化。
            var s = Session.Status(StrategyName);
            if (_Status != null) s.Status = new XElement(_Status);
            Repository.SubmitChanges();
        }

        public string StrategyName
        {
            get
            {
                var attr = GetType().GetCustomAttribute<StrategyAttribute>();
                if (attr != null && attr.Name != null) return attr.Name;
                return GetType().Name;
            }
        }

        public void EntryPoint()
        {
            Logging.Enter(this);
            EntryPointCore();
            Logging.Exit(this);
        }

        protected abstract void EntryPointCore();

        public Strategy(Session session)
        {
            if (session == null) throw new ArgumentNullException("session");
            Session = session;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class StrategyAttribute : Attribute
    {
        private string _Name;

        public string Name
        {
            get { return _Name; }
        }

        public StrategyAttribute(string name)
        {
            _Name = name;
        }
    }
}
