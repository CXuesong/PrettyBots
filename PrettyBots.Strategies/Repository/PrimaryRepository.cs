
using System;
using System.Collections.Generic;
using System.Data;

namespace PrettyBots.Strategies.Repository
{
    /// <summary>
    /// 用于管理主要数据存储。
    /// </summary>
    public class PrimaryRepository : IDisposable
    {
        internal PrimaryDataContext DataContext { get; private set; }

        public AccountRepository Accounts { get; private set; }

        public LoggingRepository Loggings { get; private set; }

        public SessionRepository Sessions { get; private set; }

        private void Initialize()
        {
            Accounts = new AccountRepository(this);
            Loggings = new LoggingRepository(this);
            Sessions = new SessionRepository(this);
        }

        internal void SubmitChanges()
        {
            DataContext.SubmitChanges();
        }

        public PrimaryRepository()
        {
            DataContext = new PrimaryDataContext();
            Initialize();
        }

        public PrimaryRepository(string connection)
        {
            DataContext = new PrimaryDataContext(connection);
            Initialize();
        }

        public PrimaryRepository(IDbConnection connection)
        {
            DataContext = new PrimaryDataContext(connection);
            Initialize();
        }

        public void Dispose()
        {
            DataContext.Dispose();
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

        public IEnumerable<Session> Sessions { get { return Repository == null ? null : Repository.DataContext.Session; } }

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
