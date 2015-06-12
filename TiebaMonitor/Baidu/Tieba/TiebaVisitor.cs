namespace PrettyBots.Monitor.Baidu.Tieba
{
    /// <summary>
    /// 用于访问百度贴吧。
    /// </summary>
    public class TiebaVisitor : ChildVisitor<BaiduVisitor>
    {
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

        internal TiebaVisitor(BaiduVisitor parent)
            : base(parent)
        {
            Messages = new MessagesVisitor(Parent);
        }
    }
}
