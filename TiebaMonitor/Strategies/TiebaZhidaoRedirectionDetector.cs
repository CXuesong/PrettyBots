using System;
using System.Collections.Generic;
using System.Linq;
using PrettyBots.Monitor.Baidu;
using PrettyBots.Monitor.Baidu.Tieba;

namespace PrettyBots.Monitor.Strategies
{
    /// <summary>
    /// 用于检查由百度知道重定向而来的贴吧主题。
    /// </summary>
    public class TiebaZhidaoRedirectionDetector
    {
        public BaiduVisitor Visitor { get; private set; }

        public IList<string> StrongMatcher { get; set; }

        public IList<string> WeakMatcher { get; set; }

        private static bool InString(string test, string match)
        {
            if (string.IsNullOrEmpty(test)) return string.IsNullOrEmpty(match);
            return test.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public IEnumerable<TopicVisitor> CheckForum(string forumName, int checkCount = 20)
        {
            if (StrongMatcher == null || StrongMatcher.Count == 0) yield break;
            var f = Visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new InvalidOperationException();
            foreach (var t in f.Topics().Take(checkCount))
            {
                if (string.IsNullOrWhiteSpace(t.PreviewText)) continue;
                //含有论坛名称，白名单。
                if (InString(t.Title, f.Name) || InString(t.PreviewText, f.Name)) continue;
                //遵循帖子标题格式，白名单。
                if (f.TopicPrefix.Count > 0 && f.IsTopicPrefixMatch(t.Title)) continue;
                //判断标题与内容是否重复。
                var content = t.PreviewText.Trim();
                if (t.PreviewText.Length >= t.Title.Length)
                {
                    var contentScrap = t.PreviewText.Substring(0, t.Title.Length);
                    if (string.Compare(t.Title, contentScrap, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        //貌似重复了……
                        //检查帖子内容。
                        if (WeakMatcher.Any(m => InString(content, m)))
                        {
                            yield return t;
                            continue;
                        }
                    }
                }
                if (StrongMatcher.Any(m => string.Compare(m.Trim(), content, StringComparison.CurrentCultureIgnoreCase) == 0))
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
