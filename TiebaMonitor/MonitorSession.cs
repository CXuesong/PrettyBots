using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace TiebaMonitor.Kernel
{
    public sealed class MonitorSession : IDisposable
    {
        private string _ForumName;
        private ExtendedWebClient client = new ExtendedWebClient();
        private bool _IsDisposed = false;

        public string ForumName
        {
            get { return _ForumName; }
            set { _ForumName = value; }
        }

        public bool Login(string userName, string password)
        {
            //STEP 1
            //BAIDUID包含在返回的Cookie中。
            //var tbsString = client.DownloadString("http://tieba.baidu.com/dc/common/tbs");
            //tbsString
            //  {"tbs":"b70cfbe3e17eb6b81433580819","is_login":0}
            //Cookie
            //  $Version=1; BAIDUID=9ABCFA4624E7F5023F52F611EC207798:FG=1; $Path=/; $Domain=.baidu.com
            //Debug.Print(tbsString);
            client.CookieContainer.Add(new Cookie("BAIDUID", "9ABCFA4624E7F5023F52F611EC207798:FG=1", "/", ".baidu.com"));
            //client.CookieContainer.Add(new Cookie("FG", "1", "/", ".baidu.com"));
            Debug.Print(Utility.FormatCookies(client.CookieContainer, "http://baidu.com"));
            //STEP 2
            var apiString = client.DownloadString("https://passport.baidu.com/v2/api/?getapi&tpl=mn");
            /*
var bdPass=bdPass||{};
bdPass.api=bdPass.api||{};
bdPass.api.params=bdPass.api.params||{};
bdPass.api.params._token='b7d20178ba61804fe341bc8730f4c2fd';
	bdPass.api.params._tpl='mn';

		document.write('<script type="text/javascript" charset="UTF-8" src="https://passport.baidu.com/js/pass_api_.js?v=20131115"></script>');
             */
            //Debug.Print(apiString);
            //偶尔会出现
            // the fisrt two args should be string type:0,1!
            //可能和 Cookie 过期相关。
            var tokenMatcher = new Regex("bdPass\\.api\\.params\\._token\\s*=\\s*['\"](.*?)['\"]");
            var matchResult = tokenMatcher.Match(apiString);
            if (!matchResult.Success) throw new UnexpectedDataException();
            var token = matchResult.Groups[1].Value;
            Debug.Assert(!string.IsNullOrWhiteSpace(token));
            if (token.Any(char.IsWhiteSpace)) throw new UnexpectedDataException(token);
            Debug.Print("Token : {0}", token);
            //STEP3
            var loginResult = client.UploadString("https://passport.baidu.com/v2/api/?login",
                string.Format(
                    "charset=utf-8&mem_pass=on&token={0}&tpl=mn&username={1}&password={2}&codestring=&verifycode=",
                    token, userName, password));
            Debug.Print(loginResult);
            return true;
        }

        public MonitorSession()
        {
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
        }

        public void Dispose()
        {
            if (_IsDisposed) return;
            client.Dispose();
            _IsDisposed = true;
        }
    }
}
