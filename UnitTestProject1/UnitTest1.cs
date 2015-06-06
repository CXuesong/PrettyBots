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
            session.RequestVerificationCode += session_RequestVerificationCode;
            session.Login("nvforest93@163.com", "haveatry");
        }

        void session_RequestVerificationCode(object sender, RequestVerificationCodeEventArgs e)
        {
            using (var ib = new VerificationCodeInputBox())
            {
                e.VerificationCode = ib.ShowDialog(e.ImageUrl);
            }
        }
    }
}
