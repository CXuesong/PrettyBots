namespace PrettyBots.Monitor
{
    public interface ITextMessageVisitor
    {
        void Update();

        string Content { get; }

        string AuthorName { get; }

        bool Reply(string content);
    }
}
