using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies.Repository;
using System.Linq.Expressions;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public static class BaiduUserManager
    {
        private static string BuildKey(string userName)
        {
            return PrimaryRepository.BuildStatusKey(Domains.Baidu, "Users", userName);
        }

        public static XElement GetUserStatus(PrimaryRepository repos, string userName)
        {
            var key = BuildKey(userName);
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

        /// <summary>
        /// 移除指定的状态条目。
        /// </summary>
        public static void RemoveStatus(PrimaryRepository repos, string userName)
        {
            var key = BuildKey(userName);
            repos.DataContext.Status.DeleteAllOnSubmit(repos.DataContext.Status.Where(s => s.Key == key));
            repos.DataContext.SubmitChanges();
        }
    }
}

