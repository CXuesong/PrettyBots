using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    public interface IVisitor
    {
        WebSession Session { get; }

        /// <summary>
        /// 获取账户信息。
        /// </summary>
        IAccountInfo AccountInfo { get; }
    }

    /// <summary>
    /// 表示此类型中的内容是可以更新的。
    /// </summary>
    public interface IUpdatable
    {
        void Update();
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

        public IAccountInfo AccountInfo { get; protected set; }

        protected Visitor(WebSession session)
        {
            _Session = session;
        }
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

        WebSession IVisitor.Session
        {
            get { throw new NotImplementedException(); }
        }

        IAccountInfo IVisitor.AccountInfo
        {
            get { return ((IVisitor) Parent).AccountInfo; }
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
