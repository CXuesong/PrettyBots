using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies.Repository
{
    public class LoggingRepository : ChildRepository
    {
        public IEnumerable<LogEntry> GetLogs()
        {
            return Parent.DataContext.LogEntry;
        }

        internal LoggingRepository(PrimaryRepository parent)
            :base(parent)
        { }
    }
}
