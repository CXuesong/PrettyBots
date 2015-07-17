using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;
using ReposSession = PrettyBots.Strategies.Repository.Session;
using System.IO;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 表示一个会话。一个会话有独立的 Cookies。
    /// </summary>
    public class Session
    {
        private PrimaryRepository _Repository;
        private WebSession _WebSession;
        private ReposSession _DataSource;

        public WebSession WebSession
        {
            get { return _WebSession; }
        }

        public string Name
        {
            get { return _DataSource.Name; }
            set { _DataSource.Name = value; }
        }

        internal ReposSession DataSource
        {
            get { return _DataSource; }
        }

        public PrimaryRepository Repository
        {
            get { return _Repository; }
        }

        public XElement Status(string strategyName)
        {
            get
            {
                var key = "";
                var st = repos.DataContext.Status.FirstOrDefault(s => s.Key == key);
                if (st == null)
                {
                    st = new Status() { Key = key };
                    repos.DataContext.Status.InsertOnSubmit(st);
                    repos.SubmitChanges();
                }
                if (st.Value == null) st.Value = new XElement("root");
                return st.Value;
            }
        }

        internal void Save(ReposSession dest)
        {
            Debug.Assert(dest != null);
            using (var ms = new MemoryStream())
            {
                _WebSession.SaveCookies(ms);
                dest.Cookies = new Binary(ms.ToArray());
            }
        }

        internal Session(ReposSession source, PrimaryRepository repository)
        {
            Debug.Assert(source != null && repository != null);
            _DataSource = source;
            _Repository = repository;
            _WebSession = new WebSession();
            if (source.Cookies != null && source.Cookies.Length > 0)
            {
                using (var ms = new MemoryStream(source.Cookies.ToArray()))
                    _WebSession.LoadCookies(ms);
            }
        }
    }
}
