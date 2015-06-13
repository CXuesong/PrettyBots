using System;
using System.Data.Linq;
using System.IO;
using PrettyBots.Visitors;
using System.Data.Linq.Mapping;

namespace PrettyBots.Strategies.Repository
{

    partial class Account
    {
        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Domain, UserName, Password.GetHashCode());
        }
    }

    partial class LogEntry
    {
        [Column(Name = "Type", Storage = "_Type", DbType = "TinyInt NOT NULL")]
        public LoggingType Type
        {
            get { return (LoggingType) TypeInternal; }
            set { TypeInternal = (byte) value; }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", Source, Type, Message);
        }
    }

    partial class Session
    {
        public WebSession LoadSession()
        {
            var s = new WebSession();
            LoadSession(s);
            return s;
        }

        public void LoadSession(WebSession session)
        {
            if (Cookies != null && Cookies.Length > 0)
            {
                using (var ms = new MemoryStream(Cookies.ToArray()))
                    session.LoadCookies(ms);
            }
        }

        public void SaveSession(WebSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            using (var ms = new MemoryStream())
            {
                session.SaveCookies(ms);
                Cookies = new Binary(ms.ToArray());
            }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},Cookies:{2}", Id, Name,Cookies == null ? null : (object)Cookies.Length);
        }
    }
}
