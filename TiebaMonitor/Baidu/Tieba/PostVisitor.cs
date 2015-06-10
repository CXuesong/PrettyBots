using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrettyBots.Monitor.Baidu.Tieba
{
    public class PostVisitor : BaiduChildVisitor, ITextMessageVisitor
    {
        // pn >= 2
        public const string CommentUrlFormat = "http://tieba.baidu.com/p/comment?t={0}&tid={1}&pid={2}&pn={3}";

        private IEnumerable<PostComment> bufferedComments;
        private TopicVisitor _Topic;
        private long? _TopicId;

        public long Id { get; private set; }

        public TopicVisitor Topic
        {
            get
            {
                if (_Topic == null)
                {
                    Debug.Assert(_TopicId != null);
                    _Topic = new TopicVisitor(_TopicId.Value, Parent);
                    _Topic.Update();
                }
                return _Topic;
            }

        }

        public string AuthorName { get; private set; }

        public int Floor { get; private set; }

        public string Content { get; private set; }

        public int CommentsCount { get; private set; }

        public DateTime SubmissionTime { get; private set; }

        public IEnumerable<PostComment> Comments()
        {
            if (bufferedComments != null)
            {
                foreach (var c in bufferedComments)
                {
                    yield return c;
                }
            }
            //TODO:加载10条以后的楼中楼。
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public bool Reply(string contentCode)
        {
            return Topic.Reply(contentCode, Id);
        }

        internal PostVisitor(long id, string author, string content, DateTime submissionTime, long topicId,
            BaiduVisitor parent)
            : this(id , 0, author, content, submissionTime, 0, null, null, parent)
        {
            _TopicId = topicId;
        }

        internal PostVisitor(long id, int floor, string author, string content, DateTime submissionTime,
            int commentsCount, IEnumerable<PostComment> loadedComments, TopicVisitor topic, BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
            AuthorName = author;
            Floor = floor;
            Content = content;
            SubmissionTime = submissionTime;
            CommentsCount = commentsCount;
            bufferedComments = loadedComments;
            _TopicId = null;
            _Topic = topic;
        }
    }
}