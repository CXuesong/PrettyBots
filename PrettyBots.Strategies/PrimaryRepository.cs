using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using PrettyBots.Strategies.Repository;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 用于管理主要数据存储。
    /// </summary>
    public class PrimaryRepository : IDisposable
    {
        internal PrimaryDataContext DataContext { get; private set; }

        public Strategies.Session CreateSession()
        {
            var rs = new Repository.Session();
            DataContext.Session.InsertOnSubmit(rs);
            DataContext.SubmitChanges();
            return new Strategies.Session(rs, this);
        }

        public Strategies.Session GetSession(string name)
        {
            var rs = DataContext.Session.FirstOrDefault(s => s.Name == name);
            return new Strategies.Session(rs, this);
        }

        public Strategies.Schedule CreateSchedule()
        {
            var rs = new Repository.Schedule();
            DataContext.Schedule.InsertOnSubmit(rs);
            DataContext.SubmitChanges();
            return new Strategies.Schedule(rs, DataContext);
        }

        public Strategies.Schedule GetSchedule(string name)
        {
            var rs = DataContext.Schedule.FirstOrDefault(s => s.Name == name);
            return new Strategies.Schedule(rs, DataContext);
        }

        /// <summary>
        /// 提交对数据库的所有修改。
        /// </summary>
        public void SubmitChanges()
        {
            DataContext.SubmitChanges();
        }

        public PrimaryRepository()
        {
            DataContext = new PrimaryDataContext();
        }

        public PrimaryRepository(string connection)
        {
            DataContext = new PrimaryDataContext(connection);
        }

        public PrimaryRepository(IDbConnection connection)
        {
            DataContext = new PrimaryDataContext(connection);
        }

        public void Remove(Strategies.Session s)
        {
            DataContext.Session.DeleteOnSubmit(s.DataSource);
            DataContext.SubmitChanges();
        }

        public void Remove(Strategies.Schedule s)
        {
            DataContext.Schedule.DeleteOnSubmit(s.DataSource);
            DataContext.SubmitChanges();
        }

        public void Dispose()
        {
            DataContext.Dispose();
        }

        /// <summary>
        /// 转义指定的字符串，使其适合于作为 dbo.Status.Key 使用。
        /// </summary>
        public static string StatusKeyEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var builder = new StringBuilder();
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\':
                        builder.Append(@"\\");
                        break;
                    case '|':
                        builder.Append(@"\|");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        public static string BuildStatusKey(string domain, string key, params object[] param)
        {
            return domain + "." + key + string.Join(null, from p in param select "|" + StatusKeyEscape(p.ToString()));
        }
    }

    public abstract class ChildRepository
    {
        public PrimaryRepository Parent { get; private set; }

        internal PrimaryDataContext DataContext { get { return Parent.DataContext; } }

        internal ChildRepository(PrimaryRepository parent)
        {
            if (parent == null) throw new ArgumentNullException();
            Parent = parent;
        }
    }

    public class PrimaryRepositoryDbAdapter
    {
        public PrimaryRepository Repository { get; private set; }

        public IEnumerable<Account> Accounts { get { return Repository == null ? null : Repository.DataContext.Account; } }

        public IEnumerable<LogEntry> Loggings { get { return Repository == null ? null : Repository.DataContext.LogEntry; } }

        public IEnumerable<Repository.Session> Sessions { get { return Repository == null ? null : Repository.DataContext.Session; } }

        public bool DatabaseExists { get { return Repository.DataContext.DatabaseExists(); } }

        public void SubmitChanges()
        {
            Repository.SubmitChanges();
        }

        public PrimaryRepositoryDbAdapter(PrimaryRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            Repository = repository;
        }

        public PrimaryRepositoryDbAdapter()
        {
            Repository = null;
        }
    }
}
