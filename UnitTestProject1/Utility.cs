using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
