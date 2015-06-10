using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiebaMonitor.Kernel
{
    public interface ITextMessageVisitor
    {
        void Update();

        string Content { get; }

        string AuthorName { get; }

        bool Reply(string content);
    }
}
