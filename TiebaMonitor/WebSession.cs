using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

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

        public NameValueCollection Headers { get; set; }

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
            Headers = new NameValueCollection
            {
                {"user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; .NET4.0E; .NET4.0C; InfoPath.3; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729; Tablet PC 2.0; rv:11.0) like Gecko"}
            };
        }
    }
}
