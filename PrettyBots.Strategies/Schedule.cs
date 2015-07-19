using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Strategies.Repository;
using ReposSchedule = PrettyBots.Strategies.Repository.Schedule;
using ReposTask = PrettyBots.Strategies.Repository.Task;
using System.Collections.ObjectModel;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 表示一次任务的计划。计划中可以有多个任务。
    /// </summary>
    public class Schedule
    {
        private PrimaryDataContext _DataContext;
        private ReposSchedule _DataSource;
        private TaskCollection _Tasks;

        /// <summary>
        /// 执行预订计划。
        /// </summary>
        public void ExecuteSchedule(TaskActionProvider actionProvider)
        {
            ExecuteSchedule(actionProvider, null);
        }

        public void ResetSchedule()
        {
            foreach (var task in _Tasks)
                task.LastExecuted = null;
            _DataContext.SubmitChanges();
        }

        /// <summary>
        /// 执行预订计划。
        /// </summary>
        public void ExecuteSchedule(TaskActionProvider actionProvider, object context)
        {
            if (actionProvider == null) throw new ArgumentNullException("actionProvider");
            try
            {
                foreach (var task in _Tasks
                    .Where(task => !string.IsNullOrEmpty(task.Action))
                    .Where(task => task.LastExecuted == null || DateTime.Now - task.LastExecuted > task.Interval))
                {
                    actionProvider.InvokeAction(task.Action, task, context);
                    task.LastExecuted = DateTime.Now;
                }
            }
            finally
            {
                _DataContext.SubmitChanges();
            }
        }

        internal Schedule(ReposSchedule dataSource, PrimaryDataContext dataContext)
        {
            Debug.Assert(dataSource != null && dataContext != null);
            _DataSource = dataSource;
            _DataContext = dataContext;
            _Tasks = new TaskCollection(this);
        }

        internal PrimaryDataContext DataContext
        {
            get { return _DataContext; }
        }

        internal ReposSchedule DataSource
        {
            get { return _DataSource; }
        }

        public TaskCollection Tasks
        {
            get { return _Tasks; }
        }

        public string Name
        {
            get { return _DataSource.Name; }
        }
    }

    public class TaskCollection : Collection<Task>
    {
        private Schedule parent;

        internal TaskCollection(Schedule parent)
        {
            this.parent = parent;
            foreach (var t in parent.DataContext.Task.Where(t => t.Schedule1 == parent.DataSource))
                Items.Add(new Task(t));
        }

        public Task Add(string action, TimeSpan interval)
        {
            var newInst = new Task() {Action = action, Interval = interval, Enabled = true};
            base.Add(newInst);
            return newInst;
        }

        protected override void InsertItem(int index, Task item)
        {
            if (item == null) throw new ArgumentNullException("item");
            item.Source.Schedule1 = parent.DataSource;
            if (!item.IsInRepository)
            {
                parent.DataContext.Task.InsertOnSubmit(item.Source);
                parent.DataContext.SubmitChanges();
                item.IsInRepository = true;
            }
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            if (item.IsInRepository)
            {
                parent.DataContext.Task.DeleteOnSubmit(item.Source);
                parent.DataContext.SubmitChanges();
                item.IsInRepository = false;
            }
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            parent.DataContext.Task.DeleteAllOnSubmit(parent.DataContext.Task.Where(t => t.Schedule1 == parent.DataSource));
            parent.DataContext.SubmitChanges();
            foreach (var item in Items) item.IsInRepository = false;
            base.ClearItems();
        }
    }

    public class Task
    {
        public string Action
        {
            get { return Source.Action; }
            set { Source.Action = value; }
        }

        public TimeSpan Interval
        {
            get { return TimeSpan.FromTicks(Source.Interval); }
            set { Source.Interval = value.Ticks; }
        }

        public bool Enabled
        {
            get { return Source.Enabled; }
            set { Source.Enabled = value; }
        }

        public DateTime? LastExecuted
        {
            get { return Source.LastExecuted; }
            set { Source.LastExecuted = value; }
        }

        public int? LastResult
        {
            get { return Source.LastResult; }
            set { Source.LastResult = value; }
        }

        internal ReposTask Source { get; private set; }

        internal bool IsInRepository { get; set; }

        internal Task(ReposTask source)
        {
            Debug.Assert(source != null);
            Source = source;
            IsInRepository = true;
        }

        internal Task()
        {
            Source = new ReposTask();
        }
    }
}
