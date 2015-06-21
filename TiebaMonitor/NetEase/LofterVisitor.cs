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
        // 0 : blogDomainName
        public const string NewTextUrl = "http://www.lofter.com/blog/{0}/new/text/";

        // 0 : sfx
        public const string EditUrl = "http://www.lofter.com/edit/{0}";

        /// <summary>
        /// 管理当前用户的账户信息。
        /// </summary>
        public new LofterAccountInfo AccountInfo { get { return (LofterAccountInfo)base.AccountInfo; } }

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

        public LofterVisitor(WebSession session)
            : base(session)
        {
            base.AccountInfo = new LofterAccountInfo(this);
        }

        public LofterVisitor()
            : base(null)
        { }
    }
}
