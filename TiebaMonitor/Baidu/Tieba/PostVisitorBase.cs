using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public abstract class PostVisitorBase : 
        ChildVisitor<BaiduVisitor>, ITextMessageVisitor
    {
        public long Id { get; private set; }

        /// <summary>
        /// 获取此帖子所在的主题。
        /// </summary>
        public abstract TopicVisitor Topic { get; }

        /// <summary>
        /// 获取此帖子所在的论坛。
        /// </summary>
        public ForumVisitor Forum
        {
            get { return Topic.Forum; }
        }

        public UserStub Author { get; private set; }

        public string Content { get; private set; }

        public DateTime SubmissionTime { get; private set; }

        string ITextMessageVisitor.Title
        {
            get { return null; }
        }

        string ITextMessageVisitor.AuthorName { get { return Author.Name; } }

        public int Floor { get; private set; }

        public bool Reply(string contentCode)
        {
            var t = ReplyAsync(contentCode);
            t.Wait();
            return t.Result;
        }

        public abstract Task<bool> ReplyAsync(string contentCode);

        /// <summary>
        /// 封禁帖子的作者。
        /// </summary>
        public void BlockAuthor()
        {
            BlockAuthor(null);
        }

        /// <summary>
        /// 封禁帖子的作者。
        /// </summary>
        public abstract void BlockAuthor(string reason);

        public override string ToString()
        {
            return string.Format("[{0}][{1}]{2}", Id, Author, Utility.StringElipsis(Content, 50));
        }

        protected PostVisitorBase(long id, int floor, UserStub author,
            string content, DateTime submissionTime, IVisitor parent)
            : base(parent)
        {
            Id = id;
            Floor = floor;
            Author = author;
            Content = content;
            SubmissionTime = submissionTime;
        }
    }
}
