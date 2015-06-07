using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace TiebaMonitor.Kernel
{
    /// <summary>
    /// 保存了一次网页会话的信息。
    /// </summary>
    public class WebSession
    {
        public event EventHandler<RequestingVerificationCodeEventArgs> RequestingVerificationCode;

        public CookieContainer CookieContainer { get; set; }

        public NameValueCollection Headers { get; set; }

        internal ExtendedWebClient CreateWebClient()
        {
            var c = new ExtendedWebClient();
            if (Headers != null) c.Headers.Add(Headers);
            //SetupCookie
            c.CookieContainer = CookieContainer;
            c.Encoding = Encoding.UTF8;
            return c;
        }

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

        /// <summary>
        /// 根据指定的图片地址，请求用户识别并输入验证码。
        /// </summary>
        public string RequestVerificationCode(string imageUrl)
        {
            var e = new RequestingVerificationCodeEventArgs(imageUrl);
            OnRequestingVerificationCode(e);
            return e.VerificationCode;
        }

        protected virtual void OnRequestingVerificationCode(RequestingVerificationCodeEventArgs e)
        {
            if (RequestingVerificationCode == null) return;
            RequestingVerificationCode(this, e);
        }

        public WebSession()
        {
            CookieContainer = new CookieContainer();
            Headers = new NameValueCollection
            {
                {"user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)"}
            };
        }
    }

    public class RequestingVerificationCodeEventArgs : EventArgs
    {
        private string _ImageUrl;
        private string _VerificationCode;

        /// <summary>
        /// 验证码的图片地址。
        /// </summary>
        public string ImageUrl
        {
            get { return _ImageUrl; }
        }

        /// <summary>
        /// 用于保存用户输入的验证码。如果用户未输入验证码，则为 <c>null</c>。
        /// </summary>
        public string VerificationCode
        {
            get { return _VerificationCode; }
            set { _VerificationCode = value; }
        }

        public RequestingVerificationCodeEventArgs(string imageUrl)
        {
            _ImageUrl = imageUrl;
        }
    }
}
