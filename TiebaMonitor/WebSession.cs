using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 保存了一次网页会话的信息。
    /// </summary>
    public class WebSession
    {
        private TimeSpan _CacheDuration;

        /// <summary>
        /// 获取/设置验证码的识别器。
        /// </summary>
        public VerificationCodeRecognizer VerificationCodeRecognizer{get; set; }

        public CookieContainer CookieContainer { get; set; }

        public WebHeaderCollection Headers { get; set; }

        /// <summary>
        /// 指示本地页面缓存的有效期。
        /// </summary>
        public TimeSpan CacheDuration
        {
            get { return _CacheDuration; }
            set
            {
                _CacheDuration = value;
                cacheDurationMilliseconds = (int) value.TotalMilliseconds;
            }
        }
        private int cacheDurationMilliseconds;

        ///// <summary>
        ///// 创建一个可以异步更新的缓存。
        ///// </summary>
        //internal Cached<T> CreateAsyncCache<T>(Func<Task<T>> onRefreshCacheAsync)
        //{
        //    return new Cached<T>(onRefreshCacheAsync, cacheDurationMilliseconds);
        //}

        internal Cached<T> CreateCache<T>(T value)
        {
            return new Cached<T>(value, cacheDurationMilliseconds);
        }

        internal Cached<T> CreateCache<T>()
        {
            return new Cached<T>(cacheDurationMilliseconds);
        }

        /// <summary>
        /// 根据当前的会话，建立一个网络客户端。
        /// </summary>
        /// <returns>一个网络客户端，其引用了当前会话的 Cookie，并使用标头的副本。</returns>
        internal ExtendedWebClient CreateWebClient()
        {
            var c = new ExtendedWebClient();
            if (Headers != null) c.Headers.Add(Headers);
            //SetupCookie
            c.CookieContainer = CookieContainer;
            c.Encoding = Encoding.UTF8;
            return c;
        }

        #region Cookies 支持
        internal void SetupCookies(HttpWebRequest request)
        {
            request.CookieContainer = CookieContainer;
        }

        internal void OverrideCookies(CookieContainer container)
        {
            if (this.CookieContainer != container)
            {
                //TODO
            }
        }

        public void SaveCookies(Stream s)
        {
            if (s == null) throw new ArgumentNullException("s");
            var formatter = new BinaryFormatter();
            if (CookieContainer != null) formatter.Serialize(s, CookieContainer);
        }

        public void SaveCookies(string path)
        {
            using (var fs = File.OpenWrite(path))
                SaveCookies(fs);
        }

        public void LoadCookies(Stream s)
        {
            if (s == null) throw new ArgumentNullException("s");
            var formatter = new BinaryFormatter();
            CookieContainer = (CookieContainer)formatter.Deserialize(s);
        }

        public void LoadCookies(CookieContainer c)
        {
            if (c == null) throw new ArgumentNullException("c");
            CookieContainer = c;
        }

        public void LoadCookies(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                if (fs.Length == 0)
                    CookieContainer = new CookieContainer();
                else
                    LoadCookies(fs);
            }
        }

        public void ClearCookies()
        {
            LoadCookies(new CookieContainer());
        }

        #endregion

        /// <summary>
        /// 根据指定的图片地址，请求用户识别并输入验证码。
        /// </summary>
        public string RequestVerificationCode(string imageUrl)
        {
            if (VerificationCodeRecognizer == null) return null;
            return VerificationCodeRecognizer.Recognize(imageUrl, this);
        }

        public WebSession()
        {
            CookieContainer = new CookieContainer();
            Headers = new WebHeaderCollection
            {
                {HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; .NET4.0E; .NET4.0C; InfoPath.3; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729; Tablet PC 2.0; rv:11.0; PrettyBot/1.0; LMBot/1.0) like Gecko"}
            };
            CacheDuration = TimeSpan.FromSeconds(10);
        }
    }

    ///// <summary>
    ///// 表示一个支持在一段时期内缓存指定值的容器。
    ///// </summary>
    //public class Cached<T>
    //{
    //    /// <summary>
    //    /// 获取此缓存的有效时间（毫秒）。
    //    /// </summary>
    //    public long CacheDuration
    //    {
    //        get { return _CacheDurationMilliseconds; }
    //        set
    //        {
    //            if (value < 0) throw new ArgumentOutOfRangeException("value");
    //            _CacheDurationMilliseconds = value;
    //        }
    //    }

    //    private Func<T> _OnRefreshCache;
    //    private Func<Task<T>> _OnRefreshCacheAsync;
    //    private T _Value;
    //    private long _ValueTime;
    //    private long _CacheDurationMilliseconds;

    //    /// <summary>
    //    /// 获取一个值，指示此实例中包含的值是否已经过期。
    //    /// </summary>
    //    public bool IsExpired
    //    {
    //        get { return Environment.TickCount - _ValueTime > CacheDuration; }
    //    }

    //    /// <summary>
    //    /// 在必要时异步更新缓存，并获取缓存的值。
    //    /// </summary>
    //    public async Task<T> GetValueAsync()
    //    {
    //        if (IsExpired) await RefreshAsync();
    //        return _Value;
    //    }

    //    /// <summary>
    //    /// 在必要时更新缓存，并获取缓存的值。
    //    /// </summary>
    //    public T GetValue()
    //    {
    //        if (IsExpired) Refresh();
    //        return _Value;
    //    }

    //    /// <summary>
    //    /// 无条件异步更新缓存。
    //    /// </summary>
    //    public async Task RefreshAsync()
    //    {
    //        if (_OnRefreshCacheAsync != null)
    //            _Value = await _OnRefreshCacheAsync();
    //        else
    //            _Value = _OnRefreshCache();
    //        _ValueTime = Environment.TickCount;
    //    }

    //    /// <summary>
    //    /// 无条件更新缓存。
    //    /// </summary>
    //    public void Refresh()
    //    {
    //        if (_OnRefreshCache != null)
    //            _Value = _OnRefreshCache();
    //        else
    //        {
    //            var t = _OnRefreshCacheAsync();
    //            t.Wait();
    //            _Value = t.Result;
    //        }
    //        _ValueTime = Environment.TickCount;
    //    }

    //    public Cached(Func<T> onRefreshCache, long cacheDuration)
    //    {
    //        if (onRefreshCache == null) throw new ArgumentNullException("onRefreshCache");
    //        if (cacheDuration < 0) throw new ArgumentOutOfRangeException("cacheDuration");
    //        _OnRefreshCache = onRefreshCache;
    //        _ValueTime = int.MinValue;
    //    }

    //    public Cached(Func<Task<T>> onRefreshCacheAsync, long cacheDuration)
    //    {
    //        if (onRefreshCacheAsync == null) throw new ArgumentNullException("onRefreshCacheAsync");
    //        if (cacheDuration < 0) throw new ArgumentOutOfRangeException("cacheDuration");
    //        _OnRefreshCacheAsync = onRefreshCacheAsync;
    //        _ValueTime = int.MinValue;
    //    }
    //}

    /// <summary>
    /// 表示一个支持在一段时期内缓存指定值的容器。
    /// </summary>
    public class Cached<T>
    {
        /// <summary>
        /// 获取此缓存的有效时间（毫秒）。
        /// </summary>
        public int CacheDuration
        {
            get { return _CacheDurationMilliseconds; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");
                _CacheDurationMilliseconds = value;
            }
        }

        private T _Value;
        private int _ValueTime;
        private int _CacheDurationMilliseconds;

        /// <summary>
        /// 获取一个值，指示此实例中包含的值是否已经过期。
        /// </summary>
        public bool IsExpired
        {
            get
            {
                var now = Environment.TickCount;
                return Math.Sign(now) != Math.Sign(_ValueTime)
                    || now - _ValueTime > CacheDuration;
            }
        }

        public T Value
        {
            get { return IsExpired ? default(T) : _Value; }
            set
            {
                _Value = value;
                _ValueTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// 初始化一个当前包含指定值的缓存。
        /// </summary>
        public Cached(T value, int cacheDuration)
        {
            if (cacheDuration < 0) throw new ArgumentOutOfRangeException("cacheDuration");
            _CacheDurationMilliseconds = cacheDuration;
            this.Value = value;
        }

        /// <summary>
        /// 初始化一个已过期的缓存。
        /// </summary>
        public Cached(int cacheDuration)
        {
            if (cacheDuration < 0) throw new ArgumentOutOfRangeException("cacheDuration");
            _CacheDurationMilliseconds = cacheDuration;
            this._ValueTime = int.MinValue;
        }
    }
}
