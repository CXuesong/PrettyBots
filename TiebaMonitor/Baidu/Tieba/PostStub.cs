using System;
using System.Collections.Generic;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    /// <summary>
    /// 保存了百度贴吧帖子的基本信息。
    /// 稍后可以利用这些信息查找帖子。
    /// </summary>
    public class PostStub : ITextMessageVisitor
    {
        void ITextMessageVisitor.Update()
        {
            throw new NotSupportedException();
        }

        public string ForumName { get; private set; }

        public string Title { get; private set; }

        public string Content { get; private set; }

        public string AuthorName { get; private set; }

        public bool Reply(string content)
        {
            throw new NotImplementedException();
        }

        public DateTime SubmissionTime { get; private set; }

        public long TopicId { get; private set; }

        public long PostId { get; private set; }

        /// <summary>
        /// 获取搜索结果对应的帖子。
        /// </summary>
        /// <returns>如果帖子已经不存在，则返回<c>null</c></returns>
        public PostVisitorBase GetPost(TiebaVisitor tieba)
        {
            return tieba.GetPost(TopicId, PostId);
        }

        /// <summary>
        /// 获取搜索结果对应的帖子。
        /// </summary>
        /// <returns>如果帖子已经不存在，则返回<c>null</c></returns>
        public PostVisitorBase GetPost(BaiduVisitor visitor)
        {
            return GetPost(visitor.Tieba);
        }

        public override string ToString()
        {
            return string.Format("{0} [{1},{2}][{3}] {4}", SubmissionTime, TopicId,
                PostId, AuthorName,
                Utility.StringElipsis(Content, 50));
        }

        internal PostStub(long tid, long pid, long cid, string forum,
            string title, string author,
            string contentPreview, DateTime submissionTime)
        {
            TopicId = tid;
            PostId = cid > 0 ? cid : pid;
            ForumName = forum;
            SubmissionTime = submissionTime;
            Title = title;
            AuthorName = author;
            Content = contentPreview;
        }
    }

    public class PostEqualityComparer : IEqualityComparer<PostStub>
    {
        public static readonly PostEqualityComparer Default = new PostEqualityComparer();

        public bool Equals(PostStub x, PostStub y)
        {
            return x.TopicId == y.TopicId && x.PostId == y.PostId;
        }

        public int GetHashCode(PostStub obj)
        {
            return obj.PostId.GetHashCode() ^ obj.TopicId.GetHashCode();
        }
    }
}
