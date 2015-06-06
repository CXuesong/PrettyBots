using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TiebaMonitor.Kernel;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void LoginTest()
        {
            var session = new MonitorSession();
            session.Login("nvforest93@163.com", "haveatry");
        }
    }
}
