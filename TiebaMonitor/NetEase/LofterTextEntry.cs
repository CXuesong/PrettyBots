using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors.NetEase
{
    /// <summary>
    /// 表示 LOFTER 博客的一个文本条目。
    /// </summary>
    public struct LofterTextEntry
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public EntryPrivacy Privacy { get; set; }

        public IEnumerable<string> Tags { get; set; }

        private static string NormalizeTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return string.Empty;
            tag = tag.Trim();
            if (tag.Length > 20) tag = tag.Substring(0, 20);
            tag = tag.Replace(',', '_');
            return tag;
        }

        internal string TagsExpression()
        {
            return Tags == null
                ? null
                : string.Join(",", from t in Tags
                    where !string.IsNullOrWhiteSpace(t)
                    select NormalizeTag(t));
        }

        internal int PrivacyExpression()
        {
            switch (Privacy)
            {
                case EntryPrivacy.Public:
                    return 0;
                case EntryPrivacy.Private:
                    return 100;
                default:
                    return 0;
            }
        }

        public LofterTextEntry(string title, string content, EntryPrivacy privacy)
            : this()
        {
            Title = title;
            Content = content;
            Privacy = privacy;
            Tags = null;
        }

        public LofterTextEntry(string title, string content, EntryPrivacy privacy, params string[] tags)
            : this(title, content, privacy)
        {
            Tags = tags;
        }
    }
}
