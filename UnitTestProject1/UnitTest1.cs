using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TiebaMonitor.Kernel;
using System.Xml.Linq;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
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

        void LoginVisitor(BaiduVisitor v)
        {
            var cred = XDocument.Load("_credentials.xml");
            var login = cred.Root.Element("login");
            v.Login((string) login.Attribute("username"), (string) login.Attribute("password"));
        }

        [TestMethod]
        public void LoginTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            Assert.IsTrue(visitor.AccountInfo.IsLoggedIn);
            Trace.WriteLine(visitor.AccountInfo.UserName);
            visitor.Logout();
            Assert.IsFalse(visitor.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void ForumVisitTest()
        {
            var visitor = CreateVisitor();
            //LoginVisitor(visitor);
            var f = visitor.TiebaVisitor.Forum("化学");
            foreach (var t in f.Topics())
            {
                Trace.WriteLine(t);
            }
            //visitor.Logout();
        }
    }
}
