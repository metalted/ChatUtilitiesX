using BepInEx.Configuration;
using ChatUtilities.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class EmoteSuggestionProvider : IChatSuggestionProvider
    {
        private List<EmoteDefinition> emotes;
        private ConfigEntry<string> triggerCharacterConfig;

        public string Name
        {
            get
            {
                return "Emotes";
            }
        }

        public ChatSuggestionStyle Style { get; private set; }

        public EmoteSuggestionProvider(IEnumerable<EmoteDefinition> emoteDefinitions, ConfigEntry<string> triggerCharacter)
        {
            emotes = new List<EmoteDefinition>();

            if (emoteDefinitions != null)
            {
                emotes.AddRange(emoteDefinitions);
            }

            triggerCharacterConfig = triggerCharacter;

            Style = new ChatSuggestionStyle(
                new Color(0.1f, 0.1f, 0.1f, 0.95f),
                new Color(1f, 1f, 1f, 0.05f),
                new Color(0.3f, 0.5f, 0.8f, 0.3f),
                new Color(0f, 0f, 0f, 0.25f),
                new Vector2(0f, 0.05f),
                new Vector2(1f, 0.4f),
                1f);
        }

        private char GetTriggerCharacter()
        {
            if (triggerCharacterConfig == null || string.IsNullOrEmpty(triggerCharacterConfig.Value))
            {
                return ':';
            }

            return triggerCharacterConfig.Value[0];
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

            char triggerChar = GetTriggerCharacter();
            int triggerIndex = input.LastIndexOf(triggerChar);

            if (triggerIndex < 0)
            {
                return false;
            }

            // Fix #1: Check if trigger char is at start or preceded by whitespace
            // This prevents "1:03" from triggering emoji selector
            if (triggerIndex > 0)
            {
                char charBeforeTrigger = input[triggerIndex - 1];
                if (!char.IsWhiteSpace(charBeforeTrigger))
                {
                    // Fix #2: Check if we just completed a valid emoji
                    // Find the second-to-last trigger character
                    int previousTriggerIndex = input.LastIndexOf(triggerChar, triggerIndex - 1);
                    if (previousTriggerIndex >= 0)
                    {
                        // Extract potential emoji code between the two triggers
                        string potentialEmoji = input.Substring(previousTriggerIndex, triggerIndex - previousTriggerIndex + 1);

                        // Check if this is a complete emoji (using the custom trigger)
                        if (IsValidEmoteCodeWithTrigger(potentialEmoji, triggerChar))
                        {
                            return false;
                        }
                    }

                    // No valid emoji found and not after whitespace, so reject
                    return false;
                }
            }

            query = input.Substring(triggerIndex + 1);

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
            char triggerChar = GetTriggerCharacter();
            int triggerIndex = safeInput.LastIndexOf(triggerChar);

            if (triggerIndex < 0)
            {
                return safeInput + selected.InsertText + " ";
            }

            return safeInput.Substring(0, triggerIndex) + selected.InsertText + " ";
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

        private bool IsValidEmoteCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            for (int i = 0; i < emotes.Count; i++)
            {
                if (emotes[i] != null && string.Equals(emotes[i].Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidEmoteCodeWithTrigger(string textWithTrigger, char triggerChar)
        {
            if (string.IsNullOrEmpty(textWithTrigger) || textWithTrigger.Length < 3)
            {
                return false;
            }

            // Convert custom trigger to standard colon format for validation
            // e.g., ";smile;" -> ":smile:"
            string normalizedCode = textWithTrigger.Replace(triggerChar, ':');

            return IsValidEmoteCode(normalizedCode);
        }
    }
}
