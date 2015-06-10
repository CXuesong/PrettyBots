using System;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Monitor.NetEase;

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

        void LoginVisitor(LofterVisitor v, bool fromCredentials = false)
        {
            if (fromCredentials)
            {
                var cred = XDocument.Load("_credentials.xml");
                var login = cred.Root.Element("lofterLogin");
                v.Login((string)login.Attribute("username"), (string)login.Attribute("password"));
            }
            else
            {
                //v.Session.LoadCookies("../../../BaiduInteractive/bin/Debug/BDICookies.bin");
                //Trace.WriteLine(v.AccountInfo);
            }
        }

        [TestMethod]
        public void LoginTest()
        {
            var v = CreateVisitor();
            LoginVisitor(v, true);
        }
    }
}
