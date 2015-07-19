using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Visitors.Baidu
{
    /// <summary>
    /// 表示当前登录账户的信息。
    /// </summary>
    [AccountInfo(Domains.Baidu)]
    public class BaiduAccountInfo : ChildVisitor<BaiduVisitor>, IAccountInfo, IUpdatable
    {
        public bool IsLoggedIn { get; private set; }

        public long UserId { get; set; }

        public string UserName { get; private set; }

        /// <summary>
        /// 应该是用于 Url 包含的用户名。
        /// </summary>
        public string UserNameUrl { get; private set; }

        /// <summary>
        /// 用于 消息提醒 的参数，全站保持一致。
        /// </summary>
        public string Portrait { get; private set; }

        internal void CheckPortrait()
        {
            if (string.IsNullOrEmpty(Portrait))
                throw new InvalidOperationException(Prompts.Exception_PortraitIsNull);
        }

        protected override async Task OnFetchDataAsync()
        {
            string pageHtml;
            using (var s = Root.Session.CreateWebClient())
            {
                //Workaround OetchData 之后再登录会失败。
                //不能先访问贴吧再访问 passport.baidu.com 获取 token
                //于是保证在访问 tieba 前先访问 passport.baidu.com
                var r = s.CreateHttpRequest("https://passport.baidu.com");
                (await r.GetResponseAsync()).Dispose();
                pageHtml = await s.DownloadStringTaskAsync(TiebaVisitor.TiebaIndexUrl);
            }
            Root.Tieba.SetTiebaPageCache(pageHtml);
            var userInfo = Utility.FindJsonAssignment(pageHtml, "PageData.user");
            /*
             {
    "id": "1733233632",
    "user_id": "1733233632",
    "name": "La_Mobile",
    "user_name": "La_Mobile",
    "name_url": "La_Mobile",
    "no_un": 0,
    "is_login": 1,
    "portrait": "e00b4c615f4d6f62696c654f67",
    "balv": {
        
    },
    /*Ban这个模块真够讨厌的-/
    "Parr_props": null,
    "Parr_scores": null,
    "mParr_props": null,
    "power": {
        
    }
}
             */
            IsLoggedIn = (int)userInfo["is_login"] != 0;
            if (IsLoggedIn)
            {
                UserId = (long)userInfo["user_id"];
                UserName = (string)userInfo["user_name"];
                UserNameUrl = (string)userInfo["name_url"];
                Portrait = (string)userInfo["portrait"];
            }
            else
            {
                UserName = UserNameUrl = Portrait = null;
            }
            await Root.Tieba.UpdateAsync();
        }

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
        public bool Login(string userName, string password)
        {
            Logging.Enter(this, UserName);
            try
            {
                using (var client = Session.CreateWebClient())
                {
                    //STEP 1
                    //BAIDUID 包含在返回的Cookie中。
                    //此处使用相同的域名，防止出现
                    // the fisrt two args should be string type:0,1!
                    //的提示。
                    {
                        var r = client.CreateHttpRequest("https://passport.baidu.com");
                        r.GetResponse().Dispose();
                    }
                    //Cookie
                    //  $Version=1; BAIDUID=9ABCFA4624E7F5023F52F611EC207798:FG=1; $Path=/; $Domain=.baidu.com
                    //Debug.Print(tbsString);
                    TraceCookies(client);
                    //STEP 2
                    //var apiString =
                    //    client.DownloadString("https://passport.baidu.com/v2/api/?getapi&tpl=pp&apiver=v3&class=login");
                    var apiString =
                        client.DownloadString(
                            string.Format(
                                "https://passport.baidu.com/v2/api/?getapi&tpl=pp&apiver=v3&tt={0}&class=login&logintype=basicLogin&callback=bd__cbs__8tl5km",
                                Utility.UnixNow()));
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
                    var loginResultStr = client.UploadValuesAndDecode("https://passport.baidu.com/v2/api/?login",
                        loginParams);
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
                    Logging.Trace(this, "Login Result = {0}", loginResultCode);
                    TraceCookies(client);
                    switch (loginResultCode)
                    {
                        case 0:
                            Session.OverrideCookies(client.CookieContainer);
                            Update(true);
                            Debug.Assert(IsLoggedIn);
                            return true;
                        case 1:
                        case 2:
                            throw new OperationFailedException(loginResultCode, Prompts.LoginException_UserName);
                        case 4:
                        case 9:
                            throw new OperationFailedException(loginResultCode, Prompts.LoginException_Password);
                        case 257:
                            if (string.IsNullOrWhiteSpace(userName))
                                throw new OperationFailedException(loginResultCode, Prompts.LoginException_UserName);
                            goto default;
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
                            throw new OperationFailedException(loginResultCode);
                    }
                    //Debug.Print(client.DownloadString(redirect));
                }
            }
            catch (Exception ex)
            {
                Logging.Exception(this, ex);
                throw;
            }
            finally
            {
                Logging.Exit(this);
            }
        }

        /// <summary>
        /// 注销当前用户。
        /// </summary>
        public void Logout()
        {
            var request = WebRequest.CreateHttp("https://passport.baidu.com/?logout&u=http://www.baidu.com");
            Session.SetupCookies(request);
            request.GetResponse().Dispose();
            Session.OverrideCookies(request.CookieContainer);
            Update(true);
        }

        public override string ToString()
        {
            if (IsLoggedIn)
                return "[" + UserId + "]" + UserName + ", portrait:" + Portrait;
            else
                return Prompts.HasntLoggedIn;
        }

        internal BaiduAccountInfo(BaiduVisitor root)
            : base(root)
        {
            IsLoggedIn = false;
            UserName = null;
        }
    }
}
