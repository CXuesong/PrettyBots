namespace PrettyBots.Visitors
{
    public interface ITextMessageVisitor
    {
        void Update();

        string Title { get; }

        string Content { get; }
        
        string AuthorName { get; }

        bool Reply(string content);
    }
}
