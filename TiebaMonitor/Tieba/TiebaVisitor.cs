using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiebaMonitor.Kernel.Tieba
{
    /// <summary>
    /// 用于访问百度贴吧。
    /// </summary>
    public class TiebaVisitor : BaiduChildVisitor
    {
        /// <summary>
        /// 访问具有指定名称的贴吧。
        /// </summary>
        public ForumVisitor Forum(string name)
        {
            var f = new ForumVisitor(name, Parent);
            f.Update();
            return f;
        }

        internal TiebaVisitor(BaiduVisitor parent)
            : base(parent)
        { }
    }
}
