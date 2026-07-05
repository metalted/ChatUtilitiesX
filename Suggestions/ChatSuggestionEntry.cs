namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionEntry
    {
        public int Number { get; private set; }
        public string MatchText { get; private set; }
        public string InsertText { get; private set; }
        public string DisplayText { get; private set; }

        public ChatSuggestionEntry(int number, string matchText, string insertText, string displayText)
        {
            Number = number;
            MatchText = matchText ?? string.Empty;
            InsertText = insertText ?? string.Empty;
            DisplayText = displayText ?? string.Empty;
        }
    }
}
