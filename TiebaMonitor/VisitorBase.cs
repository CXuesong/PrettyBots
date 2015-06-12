using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Monitor
{
    public abstract class VisitorBase
    {
        public abstract WebSession Session { get; set; }

        protected VisitorBase()
        { }
    }

    public abstract class Visitor : VisitorBase
    {
        private WebSession _Session;

        public override WebSession Session
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

    public abstract class ChildVisitorBase : VisitorBase
    {
        public Visitor Parent { get; private set; }

        public override WebSession Session
        {
            get { return Parent.Session; }
            set { throw new NotSupportedException(); }
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
