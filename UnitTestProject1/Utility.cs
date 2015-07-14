using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.NetEase;

namespace UnitTestProject1
{
    static partial class Utility
    {
        static partial void LoginBaidu(IAccountInfo account);
        static partial void LoginNetEase(IAccountInfo account);

        public static void LoginAccount(IAccountInfo account)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (account is BaiduAccountInfo)
            {
                LoginBaidu(account);
                return;
            }
            if (account is LofterAccountInfo)
            {
                LoginNetEase(account);
                return;
            }
            throw new NotSupportedException();
        }

        public static BaiduVisitor CreateBaiduVisitor()
        {
            var s = new BaiduVisitor();
            s.Session.VerificationCodeRecognizer = new InteractiveVCodeRecognizer();
            return s;
        }

        public static void LoginVisitor(BaiduVisitor v)
        {
            LoginAccount(v.AccountInfo);
            Assert.IsTrue(v.AccountInfo.IsLoggedIn);
        }

    }
}
