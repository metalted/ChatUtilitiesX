using ChatUtilities.Data;
using ChatUtilities.Suggestions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChatUtilities
{
    public class ChatShortcodeExpander
    {
        private List<ChatCommandDefinition> commands;
        private List<EmoteDefinition> emotes;
        private List<UserContentDefinition> userContent;

        public ChatShortcodeExpander(
            IEnumerable<ChatCommandDefinition> commandDefinitions,
            IEnumerable<EmoteDefinition> emoteDefinitions,
            IEnumerable<UserContentDefinition> userContentDefinitions)
        {
            commands = new List<ChatCommandDefinition>();
            emotes = new List<EmoteDefinition>();
            userContent = new List<UserContentDefinition>();

            if (commandDefinitions != null)
            {
                commands.AddRange(commandDefinitions);
            }

            if (emoteDefinitions != null)
            {
                emotes.AddRange(emoteDefinitions);
            }

            if (userContentDefinitions != null)
            {
                userContent.AddRange(userContentDefinitions);
            }
        }

        public string Expand(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message ?? string.Empty;
            }

            string trimmedMessage = message.Trim();
            string expandedText;

            if (TryExpandCommandShortcode(trimmedMessage, out expandedText))
            {
                return expandedText;
            }

            if (TryExpandUserContentShortcode(trimmedMessage, out expandedText))
            {
                return expandedText;
            }

            return ExpandEmoteShortcodes(message);
        }

        private bool TryExpandCommandShortcode(string message, out string expandedText)
        {
            expandedText = string.Empty;
            int number;

            if (!TryGetNumberAfterPrefix(message, '/', out number))
            {
                return false;
            }

            int index = number - 1;

            if (index < 0 || index >= commands.Count)
            {
                return false;
            }

            ChatCommandDefinition command = commands[index];

            if (command == null)
            {
                return false;
            }

            expandedText = SuggestionTextUtility.RemovePlaceholders(command.Command);
            return true;
        }

        private bool TryExpandUserContentShortcode(string message, out string expandedText)
        {
            expandedText = string.Empty;
            int number;

            if (!TryGetNumberAfterPrefix(message, '*', out number))
            {
                return false;
            }

            int index = number - 1;

            if (index < 0 || index >= userContent.Count)
            {
                return false;
            }

            UserContentDefinition item = userContent[index];

            if (item == null)
            {
                return false;
            }

            expandedText = SuggestionTextUtility.RemovePlaceholders(item.Content);
            return true;
        }

        private string ExpandEmoteShortcodes(string message)
        {
            return Regex.Replace(message, @":(\d+)(?=\s|$)", ReplaceEmoteShortcode);
        }

        private string ReplaceEmoteShortcode(Match match)
        {
            if (match == null || match.Groups.Count < 2)
            {
                return string.Empty;
            }

            int number;

            if (!SuggestionTextUtility.TryParsePositiveInteger(match.Groups[1].Value, out number))
            {
                return match.Value;
            }

            int index = number - 1;

            if (index < 0 || index >= emotes.Count)
            {
                return match.Value;
            }

            EmoteDefinition emote = emotes[index];

            if (emote == null)
            {
                return match.Value;
            }

            return emote.Code;
        }

        private bool TryGetNumberAfterPrefix(string text, char prefix, out int number)
        {
            number = 0;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.Length < 2)
            {
                return false;
            }

            if (text[0] != prefix)
            {
                return false;
            }

            string numberText = text.Substring(1);
            return SuggestionTextUtility.TryParsePositiveInteger(numberText, out number);
        }
    }
}
