namespace ChatUtilities.Data
{
    public class ChatCommandDefinition
    {
        public string Command { get; private set; }
        public string Description { get; private set; }

        public ChatCommandDefinition(string command, string description)
        {
            Command = command ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
