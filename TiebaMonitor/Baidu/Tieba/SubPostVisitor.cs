using System;
using System.Threading.Tasks;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class SubPostVisitor : PostVisitorBase
    {
        public override TopicVisitor Topic
        {
            get { return Post.Topic; }
        }

        public PostVisitor Post
        {
            get { return Parent.Parent; }
        }

        public new SubPostListView Parent {
            get { return (SubPostListView) base.Parent; }
        }

        /// <summary>
        /// 异步回复楼中楼。
        /// </summary>
        public override Task<bool> ReplyAsync(string contentCode)
        {
            return Post.ReplyAsync(string.Format(Prompts.SubPostReplyTemplate, Author.Name, contentCode));
        }

        public override void BlockAuthor(string reason)
        {
            Topic.BlockUser(new BlockUserParams(Author.Name, Id), reason);
        }

        internal SubPostVisitor(long id, TiebaUserStub author,
            string content, DateTime submissionTime, SubPostListView parent)
            : base(id, -1, author, content, submissionTime, parent)
        { }
    }
}
