using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.NetEase
{
    [AccountInfo(AccountDomains.NetEase)]
    public class LofterAccountInfo : ChildVisitor<LofterVisitor>, IAccountInfo, IUpdatable
    {
        public const string LoginUrl = "https://reg.163.com/logins.jsp";

        public const string LogoutUrl = "http://www.lofter.com/logout";

        public const string DashboardUrl = "http://www.lofter.com/dashboard/";

        public bool IsLoggedIn { get; private set; }

        public long BlogId { get; private set; }

        public string BlogDomainName { get; private set; }

        public string UserName { get; private set; }

        public string SelfIntroduction { get; private set; }

        protected override async Task OnFetchDataAsync()
        {
            using (var c = Session.CreateWebClient())
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(await c.DownloadStringTaskAsync(DashboardUrl));
                Update(doc);
            }
        }

        internal void Update(HtmlDocument doc)
        {
            /*
{
    editReblogFromPersonalPage: false,
    goPublishData: “null“,
    allowUploadDIYMusic: false,
    v: '',
    isAdvancedBrowser: true,
    targetBlogInfo: {
        “blogId“: 490865246,
        “blogName“: “la-mobile“,
        “blogNickName“: “LaMobileBot“,
        “bigAvaImg“: “http: //imglf0.ph.126.net/w49nTvhf49_6VXQcVVPtIg==/6630873152141898564.jpg“,
        “keyTag“: ““,
        “selfIntro“: “Youknowthetruth.“,
        “postAddTime“: 1433933715843,
        “commentRank“: 10,
        “imageProtected“: false,
        “imageStamp“: false,
        “imageDigitStamp“: false,
        “ip“: null,
        “novisible“: false,
        “star“: null,
        “blogStat“: null
    },
    lastCCType: 0,
    postOverNum: false,
    blogList: [
        {
            “id“: 492863248,
            “userId“: 490865246,
            “blogId“: 490865246,
            “joinTime“: 1433929882105,
            “role“: 10,
            “newNoticeCount“: 0,
            “newRecommendNoticeCount“: 0,
            “newActivityTagNoticeCount“: 0,
            “newArtNoticeCount“: 0,
            “newResponseNoticeCount“: 0,
            “newFriendCount“: 0,
            “newFollowingUAppCount“: 0,
            “noticeCountUpdateTime“: 1434016394876,
            “blogInfo“: {
                “blogId“: 490865246,
                “blogName“: “la-mobile“,
                “blogNickName“: “LaMobileBot“,
                “bigAvaImg“: “http: //imglf0.ph.126.net/w49nTvhf49_6VXQcVVPtIg==/6630873152141898564.jpg“,
                “keyTag“: ““,
                “selfIntro“: “Youknowthetruth.“,
                “postAddTime“: 1433933715843,
                “commentRank“: 10,
                “imageProtected“: false,
                “imageStamp“: false,
                “imageDigitStamp“: false,
                “ip“: null,
                “novisible“: false,
                “star“: null,
                “blogStat“: null
            }
        }
    ],
    submiterBlogInfo: "null",
    ue_cfg_develop: false,
    ue_js_version: '20140722',
    mydomains: {
        
    },
    firstPermalink: '',
    tag: '',
    visitorId: 490865246,
    targetBlogId: 490865246,
    guide: 0,
    blogName: 'la-mobile',
    nickname: 'LaMobileBot',
    avaImg: 'http: //imglf0.ph.126.net/w49nTvhf49_6VXQcVVPtIg==/6630873152141898564.jpg',
    radarAdPercent: 60,
    radarActivityTagPercent: 30,
    noAppLogin: false,
    NewRegistUser: false
};
 */
            if (doc.GetElementbyId("logintab") != null
                || doc.GetElementbyId("newlogin") != null)
            {
                IsLoggedIn = false;
                return;
            }
            var jsTextArea = doc.DocumentNode.SelectSingleNode(".//textarea[@name='js']");
            if (jsTextArea == null) throw new UnexpectedDataException();
            var pData = Utility.FindJsonAssignment(HtmlEntity.DeEntitize(jsTextArea.InnerText), "this.p");
            var primaryBlog = pData["blogList"].FirstOrDefault();
            if (primaryBlog == null) throw new UnexpectedDataException("No primary blog.");
            BlogId = (long) primaryBlog["blogInfo"]["blogId"];
            BlogDomainName = (string) primaryBlog["blogInfo"]["blogName"];
            UserName = (string)primaryBlog["blogInfo"]["blogNickName"];
            SelfIntroduction = (string) primaryBlog["blogInfo"]["selfIntro"];
            IsLoggedIn = true;
        }


        public bool Login(string userName, string password)
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
                    throw new OperationFailedException(HtmlEntity.DeEntitize(errorHintNode.InnerText).Trim());
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
                        var enc = Encoding.Default;
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
                Update(doc);
                return true;
            }
        }

        public void Logout()
        {
            using (var c = Session.CreateWebClient())
            {
                var result = c.DownloadString(LogoutUrl);
            }
        }

        public override string ToString()
        {
            if (IsLoggedIn)
                return UserName + "[" + BlogDomainName + "]";
            else
                return Prompts.HasntLoggedIn;
        }

        internal LofterAccountInfo(LofterVisitor root)
            : base(root)
        { }
    }
}
