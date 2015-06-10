using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace PrettyBots.Monitor
{
    class ExtendedWebClient : WebClient
    {
        #region Cookies支持
        private Stack<CookieContainer> _CookieStack = new Stack<CookieContainer>();

        public CookieContainer CookieContainer { get; set; }

        public void PushNewCookieContainer()
        {
            PushCookieContainer(new CookieContainer());
        }

        public void PushCookieContainer(CookieContainer newContainer)
        {
            _CookieStack.Push(CookieContainer);
            CookieContainer = newContainer ?? new CookieContainer();
        }

        public void PopCookieContainer()
        {
            CookieContainer = _CookieStack.Pop();
        }
        #endregion

        /// <summary>
        /// 使用 POST 方法上载数据，并获取返回的字符串。
        /// </summary>
        public string PostValues(string address, NameValueCollection data)
        {
            var result = base.UploadValues(address, data);
            var resultStr = Encoding.GetString(result);
            return resultStr;
        }

        public ExtendedWebClient()
            : this(new CookieContainer())
        { }

        public ExtendedWebClient(CookieContainer c)
        {
            this.CookieContainer = c;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            var webRequest = request as HttpWebRequest;
            if (webRequest != null)
                webRequest.CookieContainer = this.CookieContainer;
            return request;
        }
    }
}
