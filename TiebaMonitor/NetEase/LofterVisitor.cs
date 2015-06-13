using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Specialized;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.NetEase
{
    public enum EntryPrivacy
    {
        Public = 0,
        Private = 1
    }

    public class LofterVisitor : Visitor
    {

        public const string LoginUrl = "https://reg.163.com/logins.jsp";

        public const string LogoutUrl = "http://www.lofter.com/logout";

        // 0 : blogDomainName
        public const string NewTextUrl = "http://www.lofter.com/blog/{0}/new/text/";

        // 0 : sfx
        public const string EditUrl = "http://www.lofter.com/edit/{0}";

        public LofterAccountInfo AccountInfo { get; private set; }

        public override bool Login(string userName, string password)
        {
            using (var c = Session.CreateWebClient())
            {
                /*
                 password=...
type=1
url=http%3A%2F%2Fwww.lofter.com%2Flogingate.do
product=lofter
savelogin=1
username=...
domains=www.lofter.com
                 */
                var loginParams = new NameValueCollection()
                {
                    {"password",password},
                    {"type", "1"},
                    {"url", "http://www.lofter.com/logingate.do"},
                    {"savelogin", "1"},
                    {"username", userName},
                    {"domains", "www.lofter.com"}
                };
                var result = c.UploadValuesAndDecode(LoginUrl, loginParams);
                /*
http://reg.www.lofter.com/crossdomain.jsp?username=....
domains=www.lofter.com
loginCookie=...
persistCookie=...
sInfoCookie=1433946573%7C0%7C3%2620%23%23%7Cnvforest93
pInfoCookie=nvforest93%40163.com%7C1433946573%7C1%7Clofter%7C00%2699%7Csxi%261433944172%26lofter%23sxi%26610100%2310%230%230%7C%260%7Cmail163%26lofter%7Cnvforest93%40163.com
antiCSRFCookie=...
checkCookieTime=1
url=http%3A%2F%2Fwww.lofter.com%2Flogingate.do%3Fusername%3Dnvforest93                 */
                var doc = new HtmlDocument();
                doc.LoadHtml(result);
                var errorHintNode = doc.GetElementbyId("eHint");
                if (errorHintNode != null)
                    throw new LoginException(HtmlEntity.DeEntitize(errorHintNode.InnerText).Trim());
                var redirectionUrl = Utility.GetRedirectionUrl(doc);
                if (string.IsNullOrEmpty(redirectionUrl)) throw new UnexpectedDataException();
                //http://reg.www.lofter.com/crossdomain.jsp?username=....
                doc.LoadHtml(c.DownloadString(redirectionUrl));
                redirectionUrl = Utility.GetRedirectionUrl(doc);
                if (string.IsNullOrEmpty(redirectionUrl)) throw new UnexpectedDataException();
                //http://www.lofter.com/logingate.do?username=....
                //c.DownloadString(redirectionUrl);     会出现卡死的情况。
                var request = c.CreateHttpRequest(redirectionUrl);
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var enc= Encoding.Default;
                        if (!string.IsNullOrEmpty(response.CharacterSet))
                        {
                            try
                            {
// ReSharper disable once AssignNullToNotNullAttribute
                                enc = Encoding.GetEncoding(response.CharacterSet);
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                        doc.Load(response.GetResponseStream(), enc);
                    }
                    else
                        throw new WebException(response.StatusCode.ToString(), WebExceptionStatus.ProtocolError);
                }
                //Debug.Print(Utility.FormatCookies(Session.CookieContainer, "http://www.lofter.com"));
                AccountInfo.Update(doc);
                return true;
            }
        }

        public override void Logout()
        {
            using (var c = Session.CreateWebClient())
            {
                var result = c.DownloadString(LogoutUrl);
            }
        }

        /// <summary>
        /// 向指定的博客发布文本。
        /// </summary>
        public string NewText(string blogDomain, LofterTextEntry entry)
        {
            using (var c = Session.CreateWebClient())
            {
                /*
blogId=490865246
blogName=la-mobile
content=<p>This is a test</p><p>&nbsp;</p><p><strong>bold test.</strong></p>
allowView=100
isPublished=true
cctype=0
tag=%E6%B5%8B%E8%AF%95,test
syncSites=
title=Test%20Text
photoInfo=[]
valCode=
                 */
                c.Headers[HttpRequestHeader.Referer] = "http://www.lofter.com/dashboard/#publish=text";
                var ntParams = new NameValueCollection()
                {
                    {"blogId", Convert.ToString(AccountInfo.BlogId)},
                    {"blogName", AccountInfo.BlogDomainName},
                    {"content", entry.Content},
                    {"allowView", Convert.ToString(entry.PrivacyExpression())},
                    {"isPublished", "true"},
                    {"cctype", "0"},
                    {"tag", entry.TagsExpression()},
                    {"syncSites", ""},
                    {"title", entry.Title},
                    {"photoInfo", "[]"},
                    {"valCode", ""}
                };
                var result = c.UploadValuesAndDecode(string.Format(NewTextUrl, blogDomain), ntParams);
                //{r:1,id:'121836752',sfx:'1d42025e_74314d0',postOverNum:false}
                var resultObj = JObject.Parse(result);
                return (string)resultObj["sfx"];
            }
        }

        public LofterVisitor()
        {
            AccountInfo = new LofterAccountInfo(this);
        }

    }
}
