using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies;
using PrettyBots.Strategies.Baidu.Tieba;
using PrettyBots.Visitors.Baidu;

namespace UnitTestProject1
{
    [TestClass]
    public class BaiduTests
    {
        BaiduVisitor CreateVisitor()
        {
            var s = new BaiduVisitor();
            s.Session.VerificationCodeRecognizer = new InteractiveVCodeRecognizer();
            return s;
        }

        void LoginVisitor(BaiduVisitor v)
        {
            Utility.LoginAccount(v.AccountInfo);
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
        }

        void LoginVisitorFromFile(BaiduVisitor v)
        {
            v.Session.LoadCookies("../../../BaiduInteractive/bin/Debug/BDICookies.bin");
            v.AccountInfo.Update();
            Trace.WriteLine(v.AccountInfo);
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void LoginTest()
        {
            var visitor = CreateVisitor();
            //visitor.AccountInfo.Update();
            LoginVisitor(visitor);
            Assert.IsTrue(visitor.AccountInfo.IsLoggedIn);
            Trace.WriteLine(visitor.AccountInfo.UserName);
            visitor.AccountInfo.Logout();
            Assert.IsFalse(visitor.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void ForumVisitTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            var f = visitor.Tieba.Forum("化学");
            foreach (var t in f.GetTopics().Take(50))
                Trace.WriteLine(t);
            visitor.AccountInfo.Logout();
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
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void MessageNotifierCleanupTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            visitor.Tieba.Messages.ClearNotifications();
            visitor.Tieba.Messages.Update();
            Trace.WriteLine(visitor.Tieba.Messages.Counter);
            visitor.AccountInfo.Logout();
        }

        //http://tieba.baidu.com/i/sys/jump?u=066c666f726573743933d100&type=replyme
        [TestMethod]
        public void TiebaSearchTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            var s = visitor.Tieba.Search("滴定", "化学");
            foreach (var p in s.Posts())
            {
                Trace.WriteLine(p.Content);
            }
            visitor.AccountInfo.Logout();
        }
        
        [TestMethod]
        public void SignInTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            //由于每天每贴吧只能签到一次，因此需要指定一个贴吧列表。
            var destList = new[] {"mark5ds", "化学", "化学2", "物理", "生物", "汉服"};
            //抽取第一个没有签到的贴吧。
            var f = destList.Select(fn => visitor.Tieba.Forum(fn))
                .FirstOrDefault(f1 => !f1.HasSignedIn);
            if (f == null)
                Assert.Inconclusive();
            else
            {
                //进行签到。
                Trace.WriteLine("Sign in : " + f.Name);
                f.SignIn();
                Assert.IsTrue(f.HasSignedIn);
                Trace.WriteLine("Rank : " + f.SignInRank);
            }
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void CountdownTopicTest()
        {
            var context = new StrategyContext("primary");
            var cd = new TopicCounterDown(context);
            Trace.WriteLine(cd.GetCurrentCounter(3044971499L));
        }
    }
}
