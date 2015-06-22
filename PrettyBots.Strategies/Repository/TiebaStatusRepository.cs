using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies.Repository
{
    public class TiebaStatusRepository : ChildRepository
    {

        public IEnumerable<TiebaStatus> GetStatus()
        {
            return DataContext.TiebaStatus;
        }

        public IEnumerable<TiebaStatus> GetStatus(string key)
        {
            return DataContext.TiebaStatus.Where(s => s.Key == key);
        }

        public IEnumerable<TiebaStatus> GetForumStatus(long forumId, string key = null, bool includeChildren = false)
        {
            var q = DataContext.TiebaStatus.Where(s => s.Forum == forumId);
            if (!includeChildren) q = q.Where(s => s.Topic == null);
            if (key != null) q = q.Where(s => s.Key == key);
            return q;
        }

        public IEnumerable<TiebaStatus> GetTopicStatus(long topicId, string key = null, bool includeChildren = false)
        {
            var q = DataContext.TiebaStatus.Where(s => s.Topic == topicId);
            if (!includeChildren) q = q.Where(s => s.Post == null);
            if (key != null) q = q.Where(s => s.Key == key);
            return q;
        }

        public IEnumerable<TiebaStatus> GetPostStatus(long postId, string key = null)
        {
            var q = DataContext.TiebaStatus.Where(s => s.Post == postId);
            if (key != null) q = q.Where(s => s.Key == key);
            return q;
        }

        private void SetTiebaStatus(IEnumerable<TiebaStatus> filter,
            long forumId, long? topicId, long? postId,
            string key, string value = null)
        {
            var s = filter.FirstOrDefault();
            if (s == null)
            {
                s = new TiebaStatus();
                DataContext.TiebaStatus.InsertOnSubmit(s);
            }
            s.Forum = forumId;
            s.Topic = topicId;
            s.Post = postId;
            s.Key = key;
            s.Value = value;
            DataContext.SubmitChanges();
        }

        public void SetPostStatus(long forumId, long topicId, long postId,
            string key, string value = null)
        {
            SetTiebaStatus(GetPostStatus(postId), forumId, topicId, postId, key, value);
        }

        public void SetTopicStatus(long forumId, long topicId,
            string key, string value = null)
        {
            SetTiebaStatus(GetTopicStatus(topicId), forumId, topicId, null, key, value);
        }

        public void SetForumStatus(long forumId, string key, string value = null)
        {
            SetTiebaStatus(GetForumStatus(forumId), forumId, null, null, key, value);
        }

        /// <summary>
        /// 移除指定的状态条目。
        /// </summary>
        public void RemoveStatus(TiebaStatus status)
        {
            DataContext.TiebaStatus.DeleteOnSubmit(status);
            DataContext.SubmitChanges();
        }

        /// <summary>
        /// 移除指定的所有状态条目。
        /// </summary>
        public void RemoveStatus(IEnumerable<TiebaStatus> status)
        {
            DataContext.TiebaStatus.DeleteAllOnSubmit(status);
            DataContext.SubmitChanges();
        }

        internal TiebaStatusRepository(PrimaryRepository parent)
            :base(parent)
        { }
    }
}
