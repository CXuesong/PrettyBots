using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TiebaMonitor.Kernel.Tieba
{
    public class PostVisitor : BaiduChildVisitor
    {
        public long Id { get; private set; }

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

        private IEnumerable<PostComment> bufferedComments;

        internal PostVisitor(long id, int floor, string author, string content,
            int commentsCount, IEnumerable<PostComment> loadedComments, BaiduVisitor parent)
            : base(parent)
        {
            Id = id;
            AuthorName = author;
            Floor = floor;
            Content = content;
            CommentsCount = commentsCount;
            bufferedComments = loadedComments;
        }
    }
}