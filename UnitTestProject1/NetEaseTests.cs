using System;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Visitors.NetEase;

namespace UnitTestProject1
{
    [TestClass]
    public class NetEaseTests
    {
        LofterVisitor CreateVisitor()
        {
            var v = new LofterVisitor();
            return v;
        }

        void LoginVisitor(LofterVisitor v)
        {
            Utility.LoginAccount(v.AccountInfo);
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
        }

        //[TestMethod]
        public void LoginTest()
        {
            var v = CreateVisitor();
            LoginVisitor(v);
            v.AccountInfo.Logout();
        }

        [TestMethod]
        public void NewTextTest()
        {
            var v = CreateVisitor();
            LoginVisitor(v);
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
            var blogName = v.AccountInfo.BlogDomainName;
            Trace.WriteLine(v.NewText(blogName, new LofterTextEntry("Test Entry",
                "<p>Test Content</p><p>" + DateTime.Now.ToString("F") + "</p>",
                EntryPrivacy.Private, "Automation")));
            Trace.WriteLine(v.NewText(blogName, new LofterTextEntry("测试日志",
                "<strong>以下为测试内容</strong><br />" + DateTime.Now.ToString("F"),
                EntryPrivacy.Public, "自动化", "测试")));
            v.AccountInfo.Logout();
        }
    }
}
