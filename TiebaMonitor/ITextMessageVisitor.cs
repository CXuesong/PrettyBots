namespace PrettyBots.Visitors
{
    public interface ITextMessageVisitor
    {
        void Update();

        string Content { get; }
        
        string AuthorName { get; }

        bool Reply(string content);
    }
}
