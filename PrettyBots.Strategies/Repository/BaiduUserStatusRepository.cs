using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies.Repository
{
    public class BaiduUserStatusRepository : ChildRepository
    {

        public IEnumerable<BaiduUserStatus> GetStatus()
        {
            return DataContext.BaiduUserStatus;
        }

        public IEnumerable<BaiduUserStatus> GetStatus(string key)
        {
            return DataContext.BaiduUserStatus.Where(s => s.Key == key);
        }

        public IEnumerable<BaiduUserStatus> GetUserStatus(string userName, string key = null)
        {
            var q = DataContext.BaiduUserStatus.Where(s => s.UserName == userName);
            if (key != null) q = q.Where(s => s.Key == key);
            return q;
        }

        public string GetStatusValue(string userName, string key = null)
        {
            var entry = GetUserStatus(userName, key).FirstOrDefault();
            if (entry == null) return null;
            return entry.Value;
        }

        public void SetStatusValue(string userName, string key, string value = null)
        {
            if (key == null) throw new ArgumentNullException("key");
            var s = GetUserStatus(userName, key).FirstOrDefault();
            if (s == null)
            {
                s = new BaiduUserStatus();
                DataContext.BaiduUserStatus.InsertOnSubmit(s);
            }
            s.UserName = userName;
            s.Key = key;
            s.Value = value;
            DataContext.SubmitChanges();
        }

        /// <summary>
        /// 移除指定的状态条目。
        /// </summary>
        public void RemoveStatus(BaiduUserStatus status)
        {
            DataContext.BaiduUserStatus.DeleteOnSubmit(status);
            DataContext.SubmitChanges();
        }

        /// <summary>
        /// 移除指定的所有状态条目。
        /// </summary>
        public void RemoveStatus(IEnumerable<BaiduUserStatus> status)
        {
            DataContext.BaiduUserStatus.DeleteAllOnSubmit(status);
            DataContext.SubmitChanges();
        }

        internal BaiduUserStatusRepository(PrimaryRepository parent)
            :base(parent)
        { }
    }
}
