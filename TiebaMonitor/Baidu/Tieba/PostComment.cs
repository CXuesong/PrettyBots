using System;

namespace PrettyBots.Monitor.Baidu.Tieba
{
    public class PostComment
    {
        public long Id { get; private set;}

        public string AuthorName { get; private set; }

        public long AuthorId { get; private set; }

        public DateTime SubmissionTime { get; private set; }

        public string Content { get; private set; }

        internal PostComment(long id, string authorName, long authorId, DateTime submissionTime, string content)
        {
            Id = id;
            AuthorName = authorName;
            AuthorId = authorId;
            SubmissionTime = submissionTime;
            Content = content;
        }
    }
}
