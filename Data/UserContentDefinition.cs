namespace ChatUtilities.Data
{
    public class UserContentDefinition
    {
        public string Title { get; private set; }
        public string Content { get; private set; }
        public bool AutoSend { get; private set; }

        public UserContentDefinition(string title, string content)
            : this(title, content, false)
        {
        }

        public UserContentDefinition(string title, string content, bool autoSend)
        {
            Title = title ?? string.Empty;
            Content = content ?? string.Empty;
            AutoSend = autoSend;
        }
    }
}
