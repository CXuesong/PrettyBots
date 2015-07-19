using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies
{
    public delegate void TaskAction(Task task, object context);
    /// <summary>
    /// 用于将 Task 的 Action 与实际执行的委托相关联。
    /// </summary>
    public class TaskActionProvider
    {
        private Dictionary<string, TaskAction> dict = new Dictionary<string, TaskAction>();

        public void RegisterAction(string name, TaskAction action)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            dict.Add(name, action);
        }

        public TaskAction GetAction(string name)
        {
            TaskAction act;
            if (dict.TryGetValue(name, out act))
                return act;
            return null;
        }

        public void InvokeAction(string name, Task task)
        {
            InvokeAction(name, task, null);
        }

        public void InvokeAction(string name, Task task, object context)
        {
            var act = GetAction(name);
            if (act == null) throw new KeyNotFoundException(string.Format(Prompts.ActionNotRegistered, name));
            act(task, context);
        }
    }
}
