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
        private TextComposer _TextComposer;

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

        public StrategyStatus Status(string strategyName)
        {
            var sid = _DataSource.Id;
            var st = _Repository.DataContext.StrategyStatus.FirstOrDefault(
                s => s.Session == sid && s.Strategy == strategyName);
            if (st == null)
            {
                st = new StrategyStatus() { Session = sid, Strategy = strategyName };
                _Repository.DataContext.StrategyStatus.InsertOnSubmit(st);
                _Repository.SubmitChanges();
            }
            if (st.Status == null) st.Status = new XElement("root");
            return st;
        }

        public void SubmitSession()
        {
            Debug.Assert(_DataSource != null);
            using (var ms = new MemoryStream())
            {
                _WebSession.SaveCookies(ms);
                _DataSource.Cookies = new Binary(ms.ToArray());
            }
            _Repository.SubmitChanges();
        }

        #region 运行时状态

        public TextComposer TextComposer
        {
            get { return _TextComposer ?? TextComposer.Default; }
            set { _TextComposer = value; }
        }

        #endregion

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
