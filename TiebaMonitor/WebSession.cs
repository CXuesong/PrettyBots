using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 保存了一次网页会话的信息。
    /// </summary>
    public class WebSession
    {
        /// <summary>
        /// 获取/设置验证码的识别器。
        /// </summary>
        public VerificationCodeRecognizer VerificationCodeRecognizer{get; set; }

        public CookieContainer CookieContainer { get; set; }

        public WebHeaderCollection Headers { get; set; }

        /// <summary>
        /// 获取/设置一个值，指示当前是否为演习模式。
        /// 在此模式下，所有的发布操作都应当被忽略。
        /// </summary>
        public bool IsDryRun { get; set; }

        internal bool CheckDryRun()
        {
            return Logging.Exit(this, IsDryRun);
        }

        /// <summary>
        /// 检查网络连通性。
        /// </summary>
        public bool CheckConnectivity()
        {
            Logging.Enter(this);
            return Logging.Exit(this, NetworkInterface.GetIsNetworkAvailable());
        }

        ///// <summary>
        ///// 创建一个可以异步更新的缓存。
        ///// </summary>
        //internal Cached<T> CreateAsyncCache<T>(Func<Task<T>> onRefreshCacheAsync)
        //{
        //    return new Cached<T>(onRefreshCacheAsync, cacheDurationMilliseconds);
        //}

        /// <summary>
        /// 根据当前的会话，建立一个网络客户端。
        /// </summary>
        /// <returns>一个网络客户端，其引用了当前会话的 Cookie，并使用标头的副本。</returns>
        internal ExtendedWebClient CreateWebClient(bool emitReferer = false)
        {
            var c = new ExtendedWebClient();
            if (Headers != null) c.Headers.Add(Headers);
            if (emitReferer) c.Headers[HttpRequestHeader.Referer] = "http://cxuesong.com/lmbot";
            //SetupCookie
            c.CookieContainer = CookieContainer;
            c.Encoding = Encoding.UTF8;
            return c;
        }

        #region Cookies 支持
        /// <summary>
        /// 为指定的 <see cref="HttpWebRequest"/> 设置 CookieContainer 。
        /// </summary>
        internal void SetupCookies(HttpWebRequest request)
        {
            request.CookieContainer = CookieContainer;
        }

        internal void OverrideCookies(CookieContainer container)
        {
            if (this.CookieContainer != container)
            {
                Debug.Assert(false); 
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

        private Dictionary<string, DateTime> intervalDict = new Dictionary<string, DateTime>();

        /// <summary>
        /// 检查指定操作的频率约束，并在有必要时进行延时。
        /// </summary>
        public void CheckIntervalConstraint(string key, TimeSpan minInterval)
        {
            DateTime lastOperation;
            if (intervalDict.TryGetValue(key, out lastOperation))
            {
                var delta = DateTime.Now - lastOperation;
                if (delta < minInterval) Thread.Sleep(minInterval - delta);
            }
            intervalDict[key] = DateTime.Now;
        }

        /// <summary>
        /// 检查指定操作的频率约束，并在有必要时进行延时。
        /// </summary>
        public async Task CheckIntervalConstraintAsync(string key, TimeSpan minInterval)
        {
            DateTime lastOperation;
            if (intervalDict.TryGetValue(key, out lastOperation))
            {
                var delta = DateTime.Now - lastOperation;
                if (delta < minInterval) await Task.Delay(minInterval - delta);
            }
            intervalDict[key] = DateTime.Now;
        }

        public WebSession()
        {
            CookieContainer = new CookieContainer();
            Headers = new WebHeaderCollection
            {
                {
                    HttpRequestHeader.UserAgent,
                    "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; .NET4.0E; .NET4.0C; InfoPath.3; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729; Tablet PC 2.0; rv:11.0; PrettyBot/1.0; LMBot/1.0) like Gecko"
                },
            };
            //CacheDuration = TimeSpan.FromSeconds(10);
        }
    }
}
