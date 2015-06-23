using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies;
using PrettyBots.Strategies.Baidu.Tieba;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

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
        public void BlockUserTest()
        {
            var visitor = CreateVisitor();
            LoginVisitor(visitor);
            var post = visitor.Tieba.GetPost(932580115, 52671043153);
            post.BlockAuthor();
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void ForumVisitTest()
        {
            var visitor = CreateVisitor();
            //LoginVisitor(visitor);
            var f = visitor.Tieba.Forum("化学");
            foreach (var t in f.Topics.Take(50))
                Trace.WriteLine(t);
            //visitor.AccountInfo.Logout();
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
            var s = visitor.Tieba.Search(userName: "狐の笑");
            Trace.WriteLine(s.Result.PageUrl);
            VisitorPageListView<SearchResultEntry> r = s.Result;
            var loopCount = 0;
            var r1 = r.FirstOrDefault();
            if (r1 != null)
            {
                var p = r1.GetPost();
                Trace.WriteLine(p);
            }
            while (r != null)
            {
                Trace.WriteLine(string.Format("Page {0}/{1}", r.PageIndex, r.PageCount));
                foreach (var p in r)
                {
                    Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                        p.SubmissionTime, p.ForumName,
                        p.Title, p.AuthorName, p.Content));
                }
                r = r.Navigate(PageRelativeLocation.Next);
                loopCount += 1;
                if (loopCount >= 12) break;
            }
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
