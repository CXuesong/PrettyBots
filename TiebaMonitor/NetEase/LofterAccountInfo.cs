using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.NetEase
{
    public class LofterAccountInfo : ChildVisitor<LofterVisitor>
    {
        public const string DashboardUrl = "http://www.lofter.com/dashboard/";

        public bool IsLoggedIn { get; private set; }

        public long BlogId { get; private set; }

        public string BlogDomainName { get; private set; }

        public string NickName { get; private set; }

        public string SelfIntroduction { get; private set; }

        public void Update()
        {
            using (var c = Session.CreateWebClient())
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(c.DownloadString(DashboardUrl));
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
            NickName = (string)primaryBlog["blogInfo"]["blogNickName"];
            SelfIntroduction = (string) primaryBlog["blogInfo"]["selfIntro"];
            IsLoggedIn = true;
        }

        internal LofterAccountInfo(LofterVisitor parent)
            : base(parent)
        { }
    }
}
