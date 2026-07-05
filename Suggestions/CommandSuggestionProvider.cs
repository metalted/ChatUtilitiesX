using ChatUtilities.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class CommandSuggestionProvider : IChatSuggestionProvider
    {
        private List<ChatCommandDefinition> commands;

        public string Name
        {
            get
            {
                return "Commands";
            }
        }

        public ChatSuggestionStyle Style { get; private set; }

        public CommandSuggestionProvider(IEnumerable<ChatCommandDefinition> commandDefinitions)
        {
            commands = new List<ChatCommandDefinition>();

            if (commandDefinitions != null)
            {
                commands.AddRange(commandDefinitions);
            }

            Style = new ChatSuggestionStyle(
                new Color(0.15f, 0.15f, 0.15f, 0.95f),
                new Color(1f, 1f, 1f, 0.05f),
                new Color(0.3f, 0.8f, 0.3f, 0.3f),
                new Color(0f, 0f, 0f, 0.25f),
                new Vector2(0f, 0.05f),
                new Vector2(1f, 0.75f),
                2f);
        }

        public bool TryGetQuery(string input, out string query)
        {
            query = string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (!input.StartsWith("/"))
            {
                return false;
            }

            query = input.Substring(1);
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

            string commandStart = "/" + safeQuery;

            for (int i = 0; i < commands.Count; i++)
            {
                ChatCommandDefinition command = commands[i];

                if (command == null)
                {
                    continue;
                }

                if (!command.Command.StartsWith(commandStart, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(CreateEntry(i, command));

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

            return SuggestionTextUtility.RemovePlaceholders(selected.InsertText);
        }

        private void AddNumberedEntry(List<ChatSuggestionEntry> result, int number)
        {
            int index = number - 1;

            if (index < 0 || index >= commands.Count)
            {
                return;
            }

            ChatCommandDefinition command = commands[index];

            if (command == null)
            {
                return;
            }

            result.Add(CreateEntry(index, command));
        }

        private ChatSuggestionEntry CreateEntry(int index, ChatCommandDefinition command)
        {
            int number = index + 1;
            string displayText = "<color=#69FF71>" + number + ". " + command.Description + "</color>\n" + command.Command;
            return new ChatSuggestionEntry(number, command.Command, command.Command, displayText);
        }
    }
}
