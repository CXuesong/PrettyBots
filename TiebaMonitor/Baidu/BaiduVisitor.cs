using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Visitors.Baidu
{
    /// <summary>
    /// 用于登录百度平台。
    /// </summary>
    /// <remarks>可以使用同一个<see cref="WebSession"/>建立多个Visitor。</remarks>
    public class BaiduVisitor : Visitor
    {
        /// <summary>
        /// 管理当前用户的账户信息。
        /// </summary>
        public BaiduAccountInfo AccountInfo { get; private set; }

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages { get; private set; }

        private static void TraceCookies(ExtendedWebClient wc)
        {
            //Debug.Print("COOKIES: baidu.com");
            //Debug.Print(Utility.FormatCookies(wc.CookieContainer, "https://baidu.com"));
            //Debug.Print("COOKIES: passport.baidu.com");
            //Debug.Print(Utility.FormatCookies(wc.CookieContainer, "https://passport.baidu.com"));
        }

        /// <summary>
        /// 登录百度帐号。
        /// </summary>
        public override bool Login(string userName, string password)
        {
            using (var client = Session.CreateWebClient())
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
                TraceCookies(client);
                //STEP 2
                var apiString =
                    client.DownloadString("https://passport.baidu.com/v2/api/?getapi&tpl=pp&apiver=v3&class=login");
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
                var loginResultStr = client.Encoding.GetString(loginResultData);
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
                TraceCookies(client);
                switch (loginResultCode)
                {
                    case 0:
                        Session.OverrideCookies(client.CookieContainer);
                        AccountInfo.Update();
                        Debug.Assert(AccountInfo.IsLoggedIn);
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
                            var codeImagePath = string.Format("https://passport.baidu.com/cgi-bin/genimage?{0}",
                                codestring);
                            verifycode = Session.RequestVerificationCode(codeImagePath);
                            if (verifycode == null) return false;
                            goto LOGIN_ATTEMPT;
                        }
                        throw new LoginException(string.Format(Prompts.LoginException_ErrorCode, loginResultCode));
                }
                //Debug.Print(client.DownloadString(redirect));
            }
        }

        private TiebaVisitor _Tieba;

        /// <summary>
        /// 访问百度贴吧。
        /// </summary>
        public TiebaVisitor Tieba
        {
            get
            {
                if (_Tieba == null) _Tieba = new TiebaVisitor(this);
                return _Tieba;
            }
        }

        /// <summary>
        /// 注销当前用户。
        /// </summary>
        public override void Logout()
        {
            var request = WebRequest.CreateHttp("https://passport.baidu.com/?logout&u=http://www.baidu.com");
            Session.SetupCookies(request);
            request.GetResponse();
            Session.OverrideCookies(request.CookieContainer);
            AccountInfo.Update();
        }

        public BaiduVisitor()
        {
            AccountInfo = new BaiduAccountInfo(this);
            Messages = new MessagesVisitor(this);
        }
    }
}
