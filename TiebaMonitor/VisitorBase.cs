using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Monitor
{
    public abstract class VisitorBase
    {
        public WebSession Session { get; set; }

        /// <summary>
        /// 登录帐号。
        /// </summary>
        public abstract bool Login(string userName, string password);

        /// <summary>
        /// 注销当前用户。
        /// </summary>
        public abstract void Logout();

        protected VisitorBase(WebSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            this.Session = session;
        }

        protected VisitorBase()
            : this(new WebSession())
        { }
    }
}
