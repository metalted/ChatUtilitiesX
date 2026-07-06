using ChatUtilities.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class EmoteSuggestionProvider : IChatSuggestionProvider
    {
        private List<EmoteDefinition> emotes;

        public string Name
        {
            get
            {
                return "Emotes";
            }
        }

        public ChatSuggestionStyle Style { get; private set; }

        public EmoteSuggestionProvider(IEnumerable<EmoteDefinition> emoteDefinitions)
        {
            emotes = new List<EmoteDefinition>();

            if (emoteDefinitions != null)
            {
                emotes.AddRange(emoteDefinitions);
            }

            Style = new ChatSuggestionStyle(
                new Color(0.1f, 0.1f, 0.1f, 0.95f),
                new Color(1f, 1f, 1f, 0.05f),
                new Color(0.3f, 0.5f, 0.8f, 0.3f),
                new Color(0f, 0f, 0f, 0.25f),
                new Vector2(0f, 0.05f),
                new Vector2(1f, 0.4f),
                1f);
        }

        public bool TryGetQuery(string input, out string query)
        {
            query = string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (input.StartsWith("/") || input.StartsWith("*"))
            {
                return false;
            }

            int colonIndex = input.LastIndexOf(':');

            if (colonIndex < 0)
            {
                return false;
            }

            query = input.Substring(colonIndex + 1);

            if (SuggestionTextUtility.ContainsWhiteSpace(query))
            {
                return false;
            }

            return true;
        }

        public List<ChatSuggestionEntry> GetMatches(string query, int maxResults)
        {
            List<ChatSuggestionEntry> result = new List<ChatSuggestionEntry>();
            string safeQuery = query ?? string.Empty;
            int number;

            if (SuggestionTextUtility.TryParsePositiveInteger(safeQuery, out number))
            {
                AddNumberedEntry(result, number);
                return result;
            }

            string codeStart = ":" + safeQuery;

            for (int i = 0; i < emotes.Count; i++)
            {
                EmoteDefinition emote = emotes[i];

                if (emote == null)
                {
                    continue;
                }

                if (!emote.Code.StartsWith(codeStart, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(CreateEntry(i, emote));

                if (result.Count >= maxResults)
                {
                    break;
                }
            }

            return result;
        }

        public string Apply(string input, ChatSuggestionEntry selected)
        {
            if (selected == null)
            {
                return input ?? string.Empty;
            }

            string safeInput = input ?? string.Empty;
            int colonIndex = safeInput.LastIndexOf(':');

            if (colonIndex < 0)
            {
                return safeInput + selected.InsertText + " ";
            }

            return safeInput.Substring(0, colonIndex) + selected.InsertText + " ";
        }

        private void AddNumberedEntry(List<ChatSuggestionEntry> result, int number)
        {
            int index = number - 1;

            if (index < 0 || index >= emotes.Count)
            {
                return;
            }

            EmoteDefinition emote = emotes[index];

            if (emote == null)
            {
                return;
            }

            result.Add(CreateEntry(index, emote));
        }

        private ChatSuggestionEntry CreateEntry(int index, EmoteDefinition emote)
        {
            int number = index + 1;
            string primaryText = "<color=#8AB4F8>" + number + ".</color> " + emote.SpriteTag + "   " + emote.Code;
            string secondaryText = string.Empty;

            return new ChatSuggestionEntry(
                number,
                emote.Code,
                emote.Code,
                primaryText,
                secondaryText);
        }
    }
}
