using System.Collections.Generic;

namespace ChatUtilities.Suggestions
{
    public interface IChatSuggestionProvider
    {
        string Name { get; }
        ChatSuggestionStyle Style { get; }
        bool TryGetQuery(string input, out string query);
        List<ChatSuggestionEntry> GetMatches(string query, int maxResults);
        string Apply(string input, ChatSuggestionEntry selected);
    }
}
