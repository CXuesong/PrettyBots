using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies;
using PrettyBots.Strategies.Baidu.Tieba;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace UnitTestProject1
{
    [TestClass]
    public class BaiduTests
    {
        [TestMethod]
        public void LoginTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            visitor.AccountInfo.Update();
            Utility.LoginVisitor(visitor);
            Assert.IsTrue(visitor.AccountInfo.IsLoggedIn);
            Trace.WriteLine(visitor.AccountInfo.UserName);
            visitor.AccountInfo.Logout();
            Assert.IsFalse(visitor.AccountInfo.IsLoggedIn);
        }

        [TestMethod]
        public void FavoriteForumTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Trace.WriteLine("登录前");
            foreach (var f in visitor.Tieba.FavoriteForums)
                Trace.WriteLine(f);
            Utility.LoginVisitor(visitor);
            Trace.WriteLine("登录后");
            foreach (var f in visitor.Tieba.FavoriteForums)
                Trace.WriteLine(f);
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void ForumVisitTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            //Utility.LoginVisitor(visitor);
            Action<string> visit = fn =>
            {
                var f = visitor.Tieba.Forum(fn);
                Trace.WriteLine(f);
                for (int i = 0; i < f.TopicPrefix.Count; i++)
                    Trace.WriteLine(f.GetTopicPrefix(i));
                foreach (var t in f.Topics.EnumerateToEnd().Skip(25).Take(50))
                    Trace.WriteLine(t);
            };
            visit("化学");
            visit("猫武士");
            //visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void PostVisitTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            var topic = visitor.Tieba.GetTopic(3295483216L);
            Trace.WriteLine(topic.ToString());
            Trace.WriteLine(topic.Forum.ToString());
        }

        [TestMethod]
        public void MessageNotifierTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
            visitor.Messages.Update();
            Trace.WriteLine(visitor.Messages.Counter);
            visitor.Tieba.Messages.Update();
            Trace.WriteLine(visitor.Tieba.Messages.Counters);
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void TiebaMessagesTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
            var msg = visitor.Tieba.Messages;
            Trace.WriteLine("Replies:");
            msg.RepliedMe.Refresh();
            foreach (var r in visitor.Tieba.Messages.RepliedMe.EnumerateToEnd().Take(50))
                Trace.WriteLine(r);
            Trace.WriteLine("References:");
            msg.ReferredMe.Refresh();
            foreach (var r in visitor.Tieba.Messages.ReferredMe.EnumerateToEnd().Take(50))
                Trace.WriteLine(r);
            visitor.AccountInfo.Logout();
        }

        [TestMethod]
        public void TiebaMessagePeekTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
            var msg = visitor.Tieba.Messages;
            foreach (var r in visitor.Tieba.Messages.PeekReplications(false))
                Trace.WriteLine(r);
            visitor.AccountInfo.Logout();
        }

        //http://tieba.baidu.com/i/sys/jump?u=066c666f726573743933d100&type=replyme
        [TestMethod]
        public void TiebaSearchTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            var s = visitor.Tieba.Search(userName: "狐の笑");
            Trace.WriteLine(s.Result.PageUrl);
            PageListView<PostStub> r = s.Result;
            var loopCount = 0;
            while (r != null)
            {
                Trace.WriteLine(string.Format("Page {0}/{1}", r.PageIndex, r.PageCount));
                foreach (var p in r)
                    Trace.WriteLine(p);
                r = r.Navigate(PageRelativeLocation.Next);
                loopCount += 1;
                if (loopCount >= 12) break;
            }
        }
        
        [TestMethod]
        public void SignInTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
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
            var context = new PrimaryRepository();
            var cd = new TopicCounterDown(context.GetSession("primary"));
            Trace.WriteLine(cd.NextCounter(3044971499L));
        }

        [TestMethod]
        public void NewbieDetectorTest()
        {
            var context = new PrimaryRepository();
            var nd = new NewbieDetector(context.GetSession("primary"));
            Trace.WriteLine(string.Join("\n", nd.InspectForum("绝境狼王")));
        }
    }
}
