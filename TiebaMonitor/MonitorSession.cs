using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace TiebaMonitor.Kernel
{
    public class RequestVerificationCodeEventArgs : EventArgs
    {
        private string _ImageUrl;
        private string _VerificationCode;

        public string ImageUrl
        {
            get { return _ImageUrl; }
        }

        public string VerificationCode
        {
            get { return _VerificationCode; }
            set { _VerificationCode = value; }
        }

        public RequestVerificationCodeEventArgs(string imageUrl)
        {
            _ImageUrl = imageUrl;
        }
    }

    public class MonitorSession : IDisposable
    {
        private string _ForumName;
        private ExtendedWebClient client = new ExtendedWebClient();
        private bool _IsDisposed = false;

        public event EventHandler<RequestVerificationCodeEventArgs> RequestVerificationCode;

        public string ForumName
        {
            get { return _ForumName; }
            set { _ForumName = value; }
        }

        private void TraceCookies()
        {
            Debug.Print("COOKIES: baidu.com");
            Debug.Print(Utility.FormatCookies(client.CookieContainer, "https://baidu.com"));
            Debug.Print("COOKIES: passport.baidu.com");
            Debug.Print(Utility.FormatCookies(client.CookieContainer, "https://passport.baidu.com"));
        }

        public bool Login(string userName, string password)
        {
            //STEP 1
            //BAIDUID 包含在返回的Cookie中。
            //此处使用相同的域名，防止出现
            // the fisrt two args should be string type:0,1!
            //的提示。
            var tbsString = client.DownloadString("https://passport.baidu.com/");
            //Cookie
            //  $Version=1; BAIDUID=9ABCFA4624E7F5023F52F611EC207798:FG=1; $Path=/; $Domain=.baidu.com
            //Debug.Print(tbsString);
            TraceCookies();
            //STEP 2
            var apiString = client.DownloadString("https://passport.baidu.com/v2/api/?getapi&tpl=pp&apiver=v3&class=login");
            /*
{"errInfo":{ "no": "0" }, "data": { "rememberedUserName" : "", "codeString" : "", "token" : "3567aa6023153e7bbf7caea2c2e33338", "cookie" : "1", "usernametype":"", "spLogin" : "rate", "disable":"", "loginrecord":{ 'email':[ ], 'phone':[ ] } }}
             */
            //Debug.Print(apiString);
            var tokenMatcher = new Regex("\"token\"\\s:\\s['\"](.*?)['\"]");
            var matchResult = tokenMatcher.Match(apiString);
            if (!matchResult.Success) throw new UnexpectedDataException();
            var token = matchResult.Groups[1].Value;
            Debug.Assert(!string.IsNullOrWhiteSpace(token));
            if (token.Any(char.IsWhiteSpace)) throw new UnexpectedDataException(token);
            Debug.Print("Token : {0}", token);
            //STEP3
            string codestring = null, verifycode = null;
            LOGIN_ATTEMPT:
            var loginParams = new NameValueCollection
            {
                {"charset", "utf-8"},
                {"token", token},
                {"isPhone", "false"},
                {"tpl", "pp"},
                {"u", "https://passport.baidu.com/"},
                {"staticpage", "https://passport.baidu.com/static/passpc-account/html/v3Jump.html"},
                {"username", userName},
                {"password", password},
                {"callback", "parent.bd__pcbs__ra48vi"},
                {"codestring", codestring},
                {"verifycode", verifycode}
            };
            var loginResultData = client.UploadValues("https://passport.baidu.com/v2/api/?login", loginParams);
            var loginResultStr = Encoding.UTF8.GetString(loginResultData);
            /*
<!DOCTYPE html>
<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
</head>
<body>


<script type="text/javascript">

var url = encodeURI('https://passport.baidu.com/v2Jump.html?callback=&index=0&codestring=&username=&phonenumber=&mail=&tpl=&u=https%3A%2F%2Fpassport.baidu.com%2F&needToModifyPassword=&gotourl=&auth=&error=257');
//parent.callback(url)
window.location.replace(url);

</script>
</body>
</html>
             */
            var redirectMatcher = new Regex("['\"](https://passport.baidu.com/.*?)['\"]");
            //Debug.Print(loginResult);
            matchResult = redirectMatcher.Match(loginResultStr);
            if (!matchResult.Success) throw new UnexpectedDataException();
            var redirect = new Uri(matchResult.Groups[1].Value);
            Debug.Print("Redirect : {0}", redirect);
            //验证码
            var loginResult = Utility.ParseUriQuery(redirect.Query);
            var loginResultCode = Convert.ToInt32(loginResult["error"]);
            Debug.Print("Login Attempt, Error = {0}", loginResultCode);
            TraceCookies();
            switch (loginResultCode)
            {
                case 0:
                    return true;
                case 1:
                case 2:
                    throw new LoginException(Prompts.LoginException_UserName);
                case 4:
                case 9:
                    throw new LoginException(Prompts.LoginException_Password);
                default:
                    if (!string.IsNullOrEmpty(loginResult["codestring"]))
                    {
                        codestring = loginResult["codestring"];
                        var codeImagePath = string.Format("https://passport.baidu.com/cgi-bin/genimage?{0}", codestring);
                        verifycode = OnRequestVerificationCode(codeImagePath);
                        if (verifycode == null) return false;
                        goto LOGIN_ATTEMPT;
                    }
                    throw new LoginException(string.Format(Prompts.LoginException_ErrorCode, loginResultCode));
            }
            //Debug.Print(client.DownloadString(redirect));
        }

        protected virtual string OnRequestVerificationCode(string imageUrl)
        {
            if (RequestVerificationCode == null) return null;
            var e = new RequestVerificationCodeEventArgs(imageUrl);
            RequestVerificationCode(this, e);
            return e.VerificationCode;
        }

        public MonitorSession()
        {
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            client.Encoding = Encoding.UTF8;
        }

        public void Dispose()
        {
            if (_IsDisposed) return;
            client.Dispose();
            _IsDisposed = true;
        }
    }
}
