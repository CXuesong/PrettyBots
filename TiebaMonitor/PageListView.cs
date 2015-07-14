using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    public enum PageRelativeLocation
    {
        First = 0,
        Previous,
        Next,
        Last
    }

    public interface IPageListView : IEnumerable
    {
        /// <summary>
        /// 获取结果集的总页数。
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 获取当前页面的位置，以 0 为下标。
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// 获取当前页面的项目。
        /// </summary>
        object this[int index] { get; }
    }

    /// <summary>
    /// 为可分页的列表提供了基础功能。
    /// </summary>
    public abstract class PageListView<T> : IPageListView, IEnumerable<T>
    {
        public const int UnknownPageCount = -1;

        /// <summary>
        /// 获取结果集的总页数。
        /// </summary>
        public int PageCount { get; protected set; }

        /// <summary>
        /// 获取当前页面的位置，以 0 为下标。
        /// </summary>
        public int PageIndex { get; protected set; }

        /// <summary>
        /// 获取当前页面的项目。
        /// </summary>
        protected abstract IList<T> Items { get; }

        /// <summary>
        /// 获取当前页面的项目总数。
        /// </summary>
        public int Count
        {
            get { return Items.Count; }
        }

        public T this[int index]
        {
            get { return Items[index]; }
        }

        object IPageListView.this[int index]
        {
            get { return Items[index]; }
        }

        /// <summary>
        /// 异步获取指定相对位置处的页面。
        /// </summary>
        public async Task<PageListView<T>> NavigateAsync(PageRelativeLocation relativeLocation)
        {
            if (!Enum.IsDefined(typeof(PageRelativeLocation), relativeLocation))
                throw new ArgumentException();
            var newPage = await OnNavigateAsync(relativeLocation);
            if (newPage != null)
            {
                switch (relativeLocation)
                {
                    case PageRelativeLocation.First:
                        Debug.Assert(newPage.PageIndex == 0);
                        break;
                    case PageRelativeLocation.Previous:
                        Debug.Assert(newPage.PageIndex == PageIndex - 1);
                        break;
                    case PageRelativeLocation.Next:
                        Debug.Assert(newPage.PageIndex == PageIndex + 1);
                        break;
                }
            }
            return newPage;
        }

        /// <summary>
        /// 获取指定相对位置处的页面。
        /// </summary>
        public PageListView<T> Navigate(PageRelativeLocation relativeLocation)
        {
            var t = NavigateAsync(relativeLocation);
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// 在派生类中重写时，定位到指定的相对位置处。
        /// </summary>
        /// <value>如果指定的位置没有页面，则返回<c>null</c>。</value>
        protected abstract Task<PageListView<T>> OnNavigateAsync(PageRelativeLocation type);

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// 获取一个 <see cref="IEnumerable&lt;T&gt;"/>，用于遍历当前页面及其之后的所有项目。
        /// </summary>
        public IEnumerable<T> EnumerateToEnd()
        {
            var currentPage = this;
            do
            {
                foreach (var i in currentPage) yield return i;
                currentPage = currentPage.Navigate(PageRelativeLocation.Next);
            } while (currentPage != null);
        }

        /// <summary>
        /// 获取一个 <see cref="IEnumerable&lt;T&gt;"/>，用于反向遍历当前页面及其以前的所有项目。
        /// </summary>
        public IEnumerable<T> EnumerateToBeginning()
        {
            var currentPage = this;
            do
            {
                for (var i = currentPage.Items.Count - 1; i > -1; i--)
                    yield return currentPage.Items[i];
                currentPage = currentPage.Navigate(PageRelativeLocation.Previous);
            } while (currentPage != null);
        }
    }

    /// <summary>
    /// 表示一个支持从网页抓取信息的分页列表视图。
    /// </summary>
    public abstract class VisitorPageListView<TChild> : PageListView<TChild>, IVisitor
    {
        private Dictionary<int, string> navigationUrlDict = new Dictionary<int, string>();
        private List<TChild> _Items = new List<TChild>();
        private bool _IsExists;
        public IVisitor Parent { get; private set; }

        IVisitor IVisitor.Parent {
            get { return this.Parent; }
        }

        /// <summary>
        /// 获取一个 List，包含了当前页面的项目。
        /// </summary>
        protected override IList<TChild> Items
        {
            get { return _Items; }
        }

        /// <summary>
        /// 获取一个值，指示了页面内容是否存在。
        /// </summary>
        public bool IsExists
        {
            get { return _IsExists; }
        }

        /// <summary>
        /// 当前页面的 Url。
        /// </summary>
        public string PageUrl { get; private set; }

        public WebSession Session
        {
            get { return Parent.Session; }
        }

        public IAccountInfo AccountInfo
        {
            get { return Parent.AccountInfo; }
        }

        protected VisitorPageListView(IVisitor parent, string pageUrl)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (string.IsNullOrEmpty(pageUrl)) throw new ArgumentNullException("pageUrl");
            Parent = parent;
            PageUrl = pageUrl;
        }

        /// <summary>
        /// 声明页面内容的存在性。
        /// </summary>
        protected void ClaimExistence(bool value)
        {
            _IsExists = value; 
        }

        protected void RegisterNavigationLocation(PageRelativeLocation location, string url)
        {
            var key = -1 - (int)location;      // -1 ~ -4
            if (key > -1 || key < -4) throw new ArgumentException();
            navigationUrlDict[key] = url;
        }

        protected void RegisterNavigationLocation(int pageIndex, string url)
        {
            if (pageIndex < 0) throw new ArgumentException();
            navigationUrlDict[pageIndex] = url;
        }

        protected void RegisterNewItem(TChild newItem)
        {
            _Items.Add(newItem);
        }
        protected void RegisterNewItem(IEnumerable<TChild> newItems)
        {
            _Items.AddRange(newItems);
        }

        /// <summary>
        /// 当需要根据 <see cref="PageUrl"/> 更新 Items 以及其它项目时引发。
        /// </summary>
        /// <returns>当无需异步操作时，此函数返回<c>null</c>。</returns>
        protected abstract Task OnRefreshPageAsync();

        public async Task RefreshAsync()
        {
            _IsExists = false;
            navigationUrlDict.Clear();
            _Items.Clear();
            var t = OnRefreshPageAsync();
            if (t != null) await t;
        }

        public void Refresh()
        {
            RefreshAsync().Wait();
        }

        /// <summary>
        /// 定位到指定的相对位置处。
        /// </summary>
        public new VisitorPageListView<TChild> Navigate(PageRelativeLocation relativeLocation)
        {
            return (VisitorPageListView<TChild>)base.Navigate(relativeLocation);
        }

        /// <summary>
        /// 异步定位到指定的相对位置处。
        /// </summary>
        public new async Task<VisitorPageListView<TChild>> NavigateAsync(PageRelativeLocation relativeLocation)
        {
            return (VisitorPageListView<TChild>) await base.NavigateAsync(relativeLocation);
        }

        protected async override Task<PageListView<TChild>> OnNavigateAsync(PageRelativeLocation location)
        {
            var key = -1 - (int)location;
            string url;
            navigationUrlDict.TryGetValue(key, out url);
            if (url == null) return null;
            var newPage = PageFactory(url);
            await newPage.RefreshAsync();
            //如果认为页面不存在，那么就不用返回了。
            return newPage._IsExists ? newPage : null;
        }

        protected abstract VisitorPageListView<TChild> PageFactory(string url);
    }

    /// <summary>
    /// 表示一个支持从网页抓取信息的分页列表视图。
    /// </summary>
    public abstract class VisitorPageListView<TParent, TChild> : VisitorPageListView<TChild>
        where TParent : IVisitor
    {
        public new TParent Parent {
            get { return (TParent) base.Parent; }
        }

        protected VisitorPageListView(TParent parent, string pageUrl)
            : base(parent,pageUrl)
        { }
    }
}
