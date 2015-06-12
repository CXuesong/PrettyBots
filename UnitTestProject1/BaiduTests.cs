using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Monitor;
using PrettyBots.Monitor.Baidu;

namespace UnitTestProject1
{
    [TestClass]
    public class BaiduTests
    {
        BaiduVisitor CreateVisitor()
        {
            var s = new BaiduVisitor();
            s.Session.RequestingVerificationCode += (sender, e) =>
            {
                using (var ib = new VerificationCodeInputBox())
                {
                    e.VerificationCode = ib.ShowDialog(e.ImageUrl);
                }
            };
            return s;
        }

        void LoginVisitor(BaiduVisitor v, bool fromCredentials = false)
        {
            if (fromCredentials)
            {
                var cred = XDocument.Load("_credentials.xml");
                var login = cred.Root.Element("login");
                v.Login((string) login.Attribute("username"), (string) login.Attribute("password"));
            }
            else
            {
                v.Session.LoadCookies("../../../BaiduInteractive/bin/Debug/BDICookies.bin");
                v.AccountInfo.Update();
                Trace.WriteLine(v.AccountInfo);
            }
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void LoginTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor, true);
            Assert.IsTrue(visitor.AccountInfo.IsLoggedIn);
            Trace.WriteLine(visitor.AccountInfo.UserName);
            visitor.Logout();
            Assert.IsFalse(visitor.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void ForumVisitTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            var f = visitor.Tieba.Forum("化学");
            foreach (var t in f.Topics().Take(50))
                Trace.WriteLine(t);
            //visitor.Logout();
        }

        [TestMethod]
        public void MessageNotifierTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            visitor.Messages.Update();
            Trace.WriteLine(visitor.Messages.Counter);
            visitor.Tieba.Messages.Update();
            Trace.WriteLine(visitor.Tieba.Messages.Counter);
        }

        [TestMethod]
        public void MessageNotifierCleanupTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            visitor.Tieba.Messages.ClearNotifications();
            visitor.Tieba.Messages.Update();
            Trace.WriteLine(visitor.Tieba.Messages.Counter);
        }
    }
}
