using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PrettyBots.Strategies.Repository;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public static class TiebaStatusManager
    {
        private static string BuildKey(long id, string typeName)
        {
            return PrimaryRepository.BuildStatusKey(Domains.BaiduTieba, typeName, id);
        }

        private static XElement GetStatus(PrimaryRepository repos, long id, string typeName)
        {
            var key = BuildKey(id, typeName);
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

        public static XElement GetForumStatus(PrimaryRepository repos, long fid)
        {
            return GetStatus(repos, fid, "Forums");
        }

        public static XElement GetTopicStatus(PrimaryRepository repos, long tid)
        {
            return GetStatus(repos, tid, "Topics");
        }

        public static XElement GetPostStatus(PrimaryRepository repos, long pid)
        {
            return GetStatus(repos, pid, "Posts");
        }
    }
}

