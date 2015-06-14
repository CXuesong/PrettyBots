using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies.Repository
{
    public class SessionRepository : ChildRepository
    {
        public WebSession LoadSession(string sessionName)
        {
            if (string.IsNullOrEmpty(sessionName)) throw new ArgumentNullException("sessionName");
            var s = DataContext.Session.FirstOrDefault(sd => sd.Name == sessionName);
            if (s == null) return new WebSession();
            return s.LoadSession();
        }

        public void SaveSession(string sessionName, WebSession session)
        {
            if (string.IsNullOrEmpty(sessionName)) throw new ArgumentNullException("sessionName");
            if (session == null) throw new ArgumentNullException("session");
            var s = DataContext.Session.FirstOrDefault(sd => sd.Name == sessionName);
            if (s == null)
            {
                s = new Session();
                DataContext.Session.InsertOnSubmit(s);
            }
            s.SaveSession(session);
            Parent.SubmitChanges();
        }

        internal SessionRepository(PrimaryRepository parent)
            : base(parent)
        { }
    }
}
