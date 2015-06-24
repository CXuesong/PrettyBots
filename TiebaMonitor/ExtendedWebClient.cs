using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PrettyBots.Visitors
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

        #region 编码支持

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

        /// <summary>
        /// 使用指定的方法上载数据，并获取返回的字符串。
        /// </summary>
        public async Task<string> UploadValuesAndDecodeTaskAsync(string address, string method, NameValueCollection data)
        {
            if (data == null) throw new ArgumentNullException("data");
            var bytes = UploadValuesInternal(data);
            var result = await base.UploadDataTaskAsync(address, bytes);
            var resultStr = Encoding.GetString(result);
            return resultStr;
        }

        /// <summary>
        /// 使用 POST 方法上载数据，并获取返回的字符串。
        /// </summary>
        public Task<string> UploadValuesAndDecodeTaskAsync(string address, NameValueCollection data)
        {
            return UploadValuesAndDecodeTaskAsync(address, null, data);
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

        // 键	值
        //Content-Type	text/html; charset=GBK
        private string ExtractContentCharset(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;
            var entry = contentType.Split(';').Select(e => e.Trim())
                .FirstOrDefault(e => e.StartsWith("charset", StringComparison.OrdinalIgnoreCase));
            if (entry == null) return null;
            var subEntry = entry.Split('=');
            if (subEntry.Length < 2) return null;
            return subEntry[1];
        }

        public new Task<string> DownloadStringTaskAsync(string address)
        {
            return DownloadStringTaskAsync(new Uri(address, UriKind.Absolute));
        }

        public new async Task<string> DownloadStringTaskAsync(Uri address)
        {
            if (address == null) throw new ArgumentNullException("address");
            var request = GetWebRequest(address);
            Debug.Assert(request != null);
            using (var response = await request.GetResponseAsync())
            {
                Encoding enc;
                var rh = response.Headers;
                if (!string.IsNullOrEmpty(rh[HttpResponseHeader.ContentEncoding]))
                    enc = Encoding.GetEncoding(rh[HttpResponseHeader.ContentEncoding]);
                else
                {
                    var charset = ExtractContentCharset(rh[HttpResponseHeader.ContentType]);
                    //注意 ISO-8859-1
                    if (!string.IsNullOrEmpty(charset))
                        enc = Encoding.GetEncoding(charset);
                    else
                        enc = this.Encoding;
                }
                using (var s = response.GetResponseStream())
                using (var reader = new StreamReader(s, enc))
                {
                    return await reader.ReadToEndAsync();
                }
            }
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
            {
                webRequest.Timeout = 30 * 1000; //默认超时 30s。
                webRequest.CookieContainer = this.CookieContainer;
            }
            return request;
        }
    }
}
