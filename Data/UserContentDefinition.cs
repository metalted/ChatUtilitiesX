namespace ChatUtilities.Data
{
    public class UserContentDefinition
    {
        public string Title { get; private set; }
        public string Content { get; private set; }

        public UserContentDefinition(string title, string content)
        {
            Title = title ?? string.Empty;
            Content = content ?? string.Empty;
        }
    }
}
