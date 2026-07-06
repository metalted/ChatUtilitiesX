namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionEntry
    {
        public int Number { get; private set; }
        public string InsertText { get; private set; }
        public string MatchText { get; private set; }
        public string PrimaryText { get; private set; }
        public string SecondaryText { get; private set; }

        public ChatSuggestionEntry(
            int number,
            string insertText,
            string matchText,
            string primaryText,
            string secondaryText)
        {
            Number = number;
            InsertText = insertText ?? string.Empty;
            MatchText = matchText ?? string.Empty;
            PrimaryText = primaryText ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
        }
    }
}
