namespace PrettyBots.Visitors.Baidu.Tieba
{
    /// <summary>
    /// 用于访问百度贴吧。
    /// </summary>
    public class TiebaVisitor : ChildVisitor<BaiduVisitor>
    {
        public const string TiebaIndexUrl = "http://tieba.baidu.com";

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages { get; private set; }

        /// <summary>
        /// 访问具有指定名称的贴吧。
        /// </summary>
        public ForumVisitor Forum(string name)
        {
            var f = new ForumVisitor(name, Parent);
            f.Update();
            return f;
        }

        /// <summary>
        /// 直接访问指定 Id 的主题。
        /// </summary>
        public TopicVisitor Topic(long topicId)
        {
            var v = new TopicVisitor(topicId, Parent);
            v.Update();
            return v;
        }

        /// <summary>
        /// 对贴吧执行搜索。
        /// </summary>
        public SearchVisitor Search(string keyword = null, string forumName = null, string userName = null)
        {
            return new SearchVisitor(Parent, keyword, forumName, userName);
        }

        internal TiebaVisitor(BaiduVisitor parent)
            : base(parent)
        {
            Messages = new MessagesVisitor(Parent);
        }
    }
}
