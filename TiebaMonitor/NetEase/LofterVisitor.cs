using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Specialized;
using HtmlAgilityPack;

namespace PrettyBots.Monitor.NetEase
{
    public class LofterVisitor : VisitorBase
    {

        public const string LoginUrl = "https://reg.163.com/logins.jsp";

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
                var result = c.PostValues(LoginUrl, loginParams);
                /*
http://reg.www.lofter.com/crossdomain.jsp?username=nvforest93
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
                var nc = doc.DocumentNode.SelectNodes("//meta[@http-equiv]");
                if (nc == null) throw new UnexpectedDataException();
                var n = nc.FirstOrDefault(n1 => string.Compare(n1.GetAttributeValue("http-equiv", ""), "refresh", StringComparison.OrdinalIgnoreCase) == 0);
                if (n == null) throw new UnexpectedDataException();
                //<meta http-equiv="refresh" content="5; url=....." />
                //http://reg.www.lofter.com/crossdomain.jsp?username=....
                var redirectionUrl = Utility.ExtractMetaRedirectionUrl(n.GetAttributeValue("content", ""));
                if (string.IsNullOrEmpty(redirectionUrl)) throw new UnexpectedDataException();
                var crossDomainContent = c.DownloadString(redirectionUrl);
                doc.LoadHtml(crossDomainContent);
                return true;
            }
        }


        public override void Logout()
        {
            throw new NotImplementedException();
        }
        
        public LofterVisitor(WebSession session)
            : base(session)
        { }

        public LofterVisitor()
            : base()
        { }

    }
}
