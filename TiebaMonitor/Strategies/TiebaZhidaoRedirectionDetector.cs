using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiebaMonitor.Kernel.Tieba;

namespace TiebaMonitor.Kernel.Strategies
{
    /// <summary>
    /// 用于检查由百度知道重定向而来的贴吧主题。
    /// </summary>
    public class TiebaZhidaoRedirectionDetector
    {
        public BaiduVisitor Visitor { get; private set; }

        public IList<string> RedirectionMatcher { get; set; }

        public IEnumerable<TopicVisitor> CheckForum(string forumName, int checkCount = 20)
        {
            if (RedirectionMatcher == null || RedirectionMatcher.Count == 0) yield break;
            var f = Visitor.TiebaVisitor.Forum(forumName);
            if (!f.IsExists) throw new InvalidOperationException();
            foreach (var t in f.Topics().Take(checkCount))
            {
                if (string.IsNullOrWhiteSpace(t.PreviewText)) continue;
                var content = t.PreviewText.Trim();
                if (string.Compare(t.Title.Trim(), content, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    //帖子标题和内容相同
                    //先不管，不要扩大攻击范围。
                }
                if (RedirectionMatcher.Any(m => string.Compare(m.Trim(), content, StringComparison.CurrentCultureIgnoreCase) == 0))
                    yield return t;
            }
        }

        public TiebaZhidaoRedirectionDetector(BaiduVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
            Visitor = visitor;
        }
    }
}
