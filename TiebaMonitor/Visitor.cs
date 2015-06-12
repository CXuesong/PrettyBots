using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Monitor
{
    public interface IVisitor
    {
        WebSession Session { get; }
    }

    public abstract class Visitor : IVisitor
    {
        private WebSession _Session;

        public virtual WebSession Session
        {
            get
            {
                if (_Session == null) _Session = new WebSession();
                return _Session;
            }
            set { _Session = value; }
        }

        /// <summary>
        /// 登录帐号。
        /// </summary>
        public abstract bool Login(string userName, string password);

        /// <summary>
        /// 注销当前用户。
        /// </summary>
        public abstract void Logout();

        protected Visitor(WebSession session)
        {
            _Session = session;
        }

        protected Visitor() : this(null)
        { }
    }

    public abstract class ChildVisitorBase : IVisitor
    {
        public Visitor Parent { get; private set; }

        public WebSession Session
        {
            get { return Parent.Session; }
        }

        protected ChildVisitorBase(Visitor parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            Parent = parent;
        }
    }

    public abstract class ChildVisitor<T> : ChildVisitorBase where T:Visitor
    {
        public new T Parent { get { return (T) base.Parent; } }

        protected ChildVisitor(T parent)
            : base(parent)
        { }
    }
}
