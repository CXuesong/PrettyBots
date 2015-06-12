using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;

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

        #region POST支持

        /// <summary>
        /// 使用指定的方法上载数据，并获取返回的字符串。
        /// </summary>
        public string UploadValuesAndDecode(string address, string method, NameValueCollection data)
        {
            if (data == null) throw new ArgumentNullException("data");
            var bytes = UploadValuesInternal(data);
            var result = base.UploadData(address, bytes);
            var resultStr = Encoding.GetString(result);
            return resultStr;
        }

        /// <summary>
        /// 使用 POST 方法上载数据，并获取返回的字符串。
        /// </summary>
        public string UploadValuesAndDecode(string address, NameValueCollection data)
        {
            return UploadValuesAndDecode(address, null, data);
        }

        private byte[] UploadValuesInternal(NameValueCollection data)
        {
            if (Headers == null) Headers = new WebHeaderCollection();
            var contentType = Headers["Content-Type"];
            //修整了框架代码中不允许指定字符集的问题。
            if (contentType != null && contentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) < 0)
                throw new WebException("无效的ContentType。");
            Headers["Content-Type"] = "application/x-www-form-urlencoded; charset=utf-8";
            var content = string.Empty;
            var builder = new StringBuilder();
            var isFirst = true;
            foreach (var key in data.AllKeys)
            {
                if (isFirst) isFirst = false; else builder.Append("&");
                //默认使用 UTF-8 编码。
                builder.Append(HttpUtility.UrlEncode(key));
                builder.Append("=");
                builder.Append(HttpUtility.UrlEncode(data[key]));
            }
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        #endregion
        public HttpWebRequest CreateHttpRequest(string address)
        {
            var r = WebRequest.CreateHttp(address);
            r.CookieContainer = CookieContainer;
            return r;
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
