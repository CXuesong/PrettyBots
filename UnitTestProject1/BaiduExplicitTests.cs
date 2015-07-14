using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class BaiduExplicitTests
    {

        [TestMethod]
        public void MessageNotifierCleanupTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
            visitor.Tieba.Messages.ClearNotifications();
            visitor.Tieba.Messages.Update();
            Trace.WriteLine(visitor.Tieba.Messages.Counters);
            visitor.AccountInfo.Logout();
        }


        [TestMethod]
        public void BlockUserTest()
        {
            var visitor = Utility.CreateBaiduVisitor();
            Utility.LoginVisitor(visitor);
            var post = visitor.Tieba.GetPost(932580115, 52671043153);
            post.BlockAuthor();
            visitor.AccountInfo.Logout();
        }
    }
}
