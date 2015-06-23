using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 为检索器提供了基础功能。
    /// </summary>
    public interface IVisitor
    {
        WebSession Session { get; }

        /// <summary>
        /// 获取账户信息。
        /// </summary>
        IAccountInfo AccountInfo { get; }

        /// <summary>
        /// 此检索器的父级。
        /// </summary>
        IVisitor Parent { get; }
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

        IVisitor IVisitor.Parent
        {
            get { return null; }
        }
    }

    public abstract class ChildVisitorBase : IVisitor
    {
        private bool _NeedRefetch = true;
        private bool _IsRefreshing = false;
        private IVisitor _Parent;

        /// <summary>
        /// 获取根级的检索器。
        /// </summary>
        public Visitor Root { get; private set; }

        /// <summary>
        /// 获取父级的检索器。
        /// </summary>
        /// <value>如果不存在父级，则返回<c>null</c>。</value>
        public virtual IVisitor Parent
        {
            get { return _Parent; }
        }


        public WebSession Session
        {
            get { return Root.Session; }
        }

        #region 页面更新
        /// <summary>
        /// 获取一个值，指示当前页面的内容是否需要重新获取并进行分析。
        /// </summary>
        public bool NeedRefetch
        {
            get { return _NeedRefetch; }
        }

        /// <summary>
        /// 将 <see cref="NeedRefetch"/> 设置为 <c>true</c>。
        /// </summary>
        protected void SetNeedRefetch()
        {
            _NeedRefetch = true;
        }

        /// <summary>
        /// 根据需要更新页面内容。
        /// </summary>
        public void Update()
        {
            Update(false);
        }

        /// <summary>
        /// 根据需要更新页面内容。
        /// </summary>
        /// <param name="forceRefetch">是否强行更新页面内容，忽略<see cref="NeedRefetch" />的值。</param>
        public void Update(bool forceRefetch)
        {
            if (_IsRefreshing) return;
            var t = UpdateAsync(forceRefetch);
            t.Wait();
        }

        /// <summary>
        /// 根据需要，异步更新页面内容。
        /// </summary>
        public Task UpdateAsync()
        {
            return UpdateAsync(false);
        }

        /// <summary>
        /// 根据需要，异步更新页面内容。
        /// </summary>
        /// <param name="forceRefetch">是否强行更新页面内容，忽略<see cref="NeedRefetch" />的值。</param>
        public async Task UpdateAsync(bool forceRefetch)
        {
            //注意，此处仅考虑到了重入情况，但并未对多线程调用提供支持。
            if (_IsRefreshing) return;
            if (_NeedRefetch || forceRefetch)
            {
                try
                {
                    var t = OnFetchDataAsync();
                    if (t != null) await t;
                }
                finally
                {
                    _IsRefreshing = false;
                }
            }
            _NeedRefetch = false;
        }

        /// <summary>
        /// 异步更新页面内容。
        /// </summary>
        /// <returns>如果仅支持同步更新，则可在更新后返回<c>null</c>。</returns>
        /// <remarks>在派生类中无需调用基类的此函数。</remarks>
        protected virtual Task OnFetchDataAsync()
        {
            return null;
        }
        #endregion

        /// <summary>
        /// 使用指定的根检索器进行初始化。
        /// </summary>
        /// <param name="root">此检索器的根检索器，同时也是其父级。</param>
        protected ChildVisitorBase(Visitor root)
            : this((IVisitor)root)
        { }

        private static Visitor FindRoot(IVisitor visitor)
        {
            while (visitor != null && !(visitor is Visitor))
                visitor = visitor.Parent;
            return (Visitor)visitor;
        }

        /// <summary>
        /// 使用指定的父级进行初始化。
        /// </summary>
        /// <param name="parent">此检索器的父级。由此指定了根检索器。</param>
        protected ChildVisitorBase(IVisitor parent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            _Parent = parent;
            var r = FindRoot(parent);
            if (r == null) throw new ArgumentException("无法根据 Parent 确定 Root。");
            Root = r;
        }

        WebSession IVisitor.Session
        {
            get { return ((IVisitor)Root).Session; }
        }

        IAccountInfo IVisitor.AccountInfo
        {
            get { return ((IVisitor)Root).AccountInfo; }
        }
    }

    public abstract class ChildVisitor<TRoot> : ChildVisitorBase where TRoot : Visitor
    {
        public new TRoot Root { get { return (TRoot)base.Root; } }

        protected ChildVisitor(IVisitor parent)
            : base(parent)
        { }
    }

    public abstract class ChildVisitor<TRoot, TParent> : ChildVisitorBase
        where TRoot : Visitor
        where TParent : IVisitor
    {
        public new TRoot Root { get { return (TRoot)base.Root; } }

        public new TParent Parent { get { return (TParent)base.Parent; } }

        protected ChildVisitor(TParent parent)
            : base(parent)
        { }
    }
}
