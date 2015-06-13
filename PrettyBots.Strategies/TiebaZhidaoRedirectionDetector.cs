using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies
{
    /// <summary>
    /// 用于检查由百度知道重定向而来的贴吧主题。
    /// </summary>
    public class TiebaZhidaoRedirectionDetector : Strategy<BaiduVisitor>
    {
        private IList<string> _WeakMatcher;
        private List<string> internalWeakMatcher = new List<string>();
        private List<string> internalStrongMatcher = new List<string>();
        private IList<string> _StrongMatcher;

        public IList<string> StrongMatcher
        {
            get { return _StrongMatcher; }
            set
            {
                if (_StrongMatcher != value)
                {
                    _StrongMatcher = value;
                    internalStrongMatcher.Clear();
                    if (_StrongMatcher != null)
                        internalStrongMatcher.AddRange(_StrongMatcher
                            .Select(m => m.Trim())
                            .Where(s => !string.IsNullOrEmpty(s)));
                }
            }
        }

        public IList<string> WeakMatcher
        {
            get { return _WeakMatcher; }
            set
            {
                if (_WeakMatcher != value)
                {
                    _WeakMatcher = value;
                    internalWeakMatcher.Clear();
                    if (_WeakMatcher != null)
                        internalWeakMatcher.AddRange(_WeakMatcher
                            .Select(Utility.NormalizeString)
                            .Where(s => !string.IsNullOrEmpty(s)));
                }
            }
        }

        /// <summary>如果在整个主题中检测到了指定的文本，则忽略此帖。</summary>
        public string AntiRecursionMagicString { get; set; }

        /// <summary>如果主题的回复数量超过此数目，则忽略此帖。</summary>
        public int RepliesCountLimit { get; set; }

        /// <summary>如果发贴人的等级超过此数目，则忽略此帖。</summary>
        public int AuthorLevelLimit { get; set; }

        private static bool InString(string test, string match)
        {
            if (string.IsNullOrEmpty(test)) return string.IsNullOrEmpty(match);
            return test.IndexOf(match, StringComparison.CurrentCulture) >= 0;
        }

        public IEnumerable<TopicVisitor> CheckForum(string forumName, int checkCount = 20)
        {
            if (StrongMatcher == null || StrongMatcher.Count == 0) yield break;
            var f = Visitor.Tieba.Forum(forumName);
            if (!f.IsExists) throw new InvalidOperationException();
            var normalizedFn = Utility.NormalizeString(f.Name);
            foreach (var t in f.Topics().Take(checkCount))
            {
                if (t.IsTop || t.IsGood) continue;
                if (string.IsNullOrWhiteSpace(t.PreviewText)) continue;
                if (t.RepliesCount > RepliesCountLimit) continue;
                var title = Utility.NormalizeString(t.Title);
                var preview = Utility.NormalizeString(t.PreviewText);
                //含有论坛名称，白名单。
                if (InString(title, normalizedFn) || InString(preview, normalizedFn)) continue;
                //遵循帖子标题格式，白名单。
                if (f.TopicPrefix.Count > 0 && f.IsTopicPrefixMatch(t.Title)) continue;
                //判断标题与内容是否重复。
                if (t.PreviewText.Length >= t.Title.Length)
                {
                    var contentScrap = t.PreviewText.Substring(0, t.Title.Length);
                    if (InString(preview, title))
                    {
                        //貌似重复了……
                        //检查帖子内容。
                        if (internalWeakMatcher.Any(m => InString(preview, m)))
                        {
                            Debug.Print("Weak Matcher: {0}", t);
                            goto CHECKTOPIC;
                        }
                    }
                }
                if (internalStrongMatcher.
                    Any(m => string.Compare(m, t.PreviewText, StringComparison.CurrentCultureIgnoreCase) == 0))
                {
                    Debug.Print("Strong Matcher: {0}", t);
                    goto CHECKTOPIC;
                }
                continue;
            CHECKTOPIC:
                var posts = t.Posts().ToList();
                var authorPost = posts.FirstOrDefault(p => string.Compare(p.Author.Name, t.AuthorName, StringComparison.OrdinalIgnoreCase) == 0);
                if (authorPost == null) continue;
                if (authorPost.Author.Level == null || authorPost.Author.Level > AuthorLevelLimit) continue;
                if (posts.Any(p => InString(p.Content, AntiRecursionMagicString))) continue;
                Debug.Print("\tMatched: {0}", t.Id);
                yield return t;
            }
        }

        public TiebaZhidaoRedirectionDetector(BaiduVisitor visitor)
            : base(visitor)
        {
            RepliesCountLimit = 20;
            AuthorLevelLimit = 4;
        }
    }
}
