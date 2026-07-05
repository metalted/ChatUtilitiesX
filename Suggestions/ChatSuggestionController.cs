using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionController
    {
        private ChatInputController chatInput;
        private ConfigEntry<KeyCode> applySelectionKey;
        private ConfigEntry<int> maxSuggestions;
        private ChatSuggestionView view;
        private List<IChatSuggestionProvider> providers = new List<IChatSuggestionProvider>();
        private List<ChatSuggestionEntry> matches = new List<ChatSuggestionEntry>();
        private IChatSuggestionProvider activeProvider;
        private string previousInput = string.Empty;
        private int selectedIndex;

        public bool IsOpen
        {
            get
            {
                return view != null && view.IsOpen;
            }
        }

        public ChatSuggestionController(
            ChatInputController chatInputController,
            ConfigEntry<KeyCode> applySelectionKeyEntry,
            ConfigEntry<int> maxSuggestionsEntry)
        {
            chatInput = chatInputController;
            applySelectionKey = applySelectionKeyEntry;
            maxSuggestions = maxSuggestionsEntry;
            view = new ChatSuggestionView();
            view.RowClicked += OnRowClicked;
        }

        public void Destroy()
        {
            Hide();

            if (view != null)
            {
                view.RowClicked -= OnRowClicked;
                view.DestroyView();
                view = null;
            }

            providers.Clear();
            matches.Clear();
            activeProvider = null;
            previousInput = string.Empty;
            selectedIndex = 0;
            chatInput = null;
        }

        public void SetProviders(IEnumerable<IChatSuggestionProvider> newProviders)
        {
            providers.Clear();

            if (newProviders != null)
            {
                providers.AddRange(newProviders);
            }

            previousInput = string.Empty;
            Hide();
        }

        public void InitializeView(GameObject chatRoot, GameObject chatTextTemplate)
        {
            activeProvider = null;
            matches.Clear();
            selectedIndex = 0;
            previousInput = string.Empty;

            if (view != null)
            {
                view.Initialize(chatRoot, chatTextTemplate);
            }
        }

        public bool Update()
        {
            if (chatInput == null || !chatInput.IsAvailable)
            {
                return false;
            }

            string input = chatInput.GetText() ?? string.Empty;

            if (input != previousInput)
            {
                previousInput = input;
                RefreshForInput(input);
            }

            if (!IsOpen)
            {
                return false;
            }

            bool handledKey = HandleKeyboard();
            return handledKey;
        }

        public void Hide()
        {
            activeProvider = null;
            matches.Clear();
            selectedIndex = 0;

            if (view != null)
            {
                view.Hide();
            }
        }

        public void ResetInputState()
        {
            previousInput = string.Empty;
            Hide();
        }

        private void RefreshForInput(string input)
        {
            IChatSuggestionProvider provider;
            string query;

            if (!TryGetActiveProvider(input, out provider, out query))
            {
                Hide();
                return;
            }

            int maxResults = 40;

            if (maxSuggestions != null)
            {
                maxResults = Mathf.Clamp(maxSuggestions.Value, 1, 500);
            }

            List<ChatSuggestionEntry> newMatches = provider.GetMatches(query, maxResults);

            if (newMatches == null || newMatches.Count == 0)
            {
                Hide();
                return;
            }

            activeProvider = provider;
            matches.Clear();
            matches.AddRange(newMatches);
            selectedIndex = 0;

            if (view != null)
            {
                view.Show(matches, provider.Style);
                view.SetSelectedIndex(selectedIndex);
            }
        }

        private bool TryGetActiveProvider(string input, out IChatSuggestionProvider provider, out string query)
        {
            provider = null;
            query = string.Empty;

            for (int i = 0; i < providers.Count; i++)
            {
                IChatSuggestionProvider candidate = providers[i];

                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.TryGetQuery(input, out query))
                {
                    continue;
                }

                provider = candidate;
                return true;
            }

            return false;
        }

        private bool HandleKeyboard()
        {
            if (matches.Count == 0)
            {
                return false;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelection(1);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveSelection(-1);
                return true;
            }

            if (IsApplySelectionPressed())
            {
                ApplySelection();
                return true;
            }

            return false;
        }

        private bool IsApplySelectionPressed()
        {
            if (applySelectionKey == null)
            {
                return false;
            }

            if (applySelectionKey.Value == KeyCode.None)
            {
                return false;
            }

            return Input.GetKeyDown(applySelectionKey.Value);
        }

        private void MoveSelection(int direction)
        {
            if (matches.Count == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, matches.Count - 1);

            if (view != null)
            {
                view.SetSelectedIndex(selectedIndex);
            }
        }

        private void ApplySelection()
        {
            if (activeProvider == null)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= matches.Count)
            {
                return;
            }

            string oldInput = chatInput.GetText() ?? string.Empty;
            string newInput = activeProvider.Apply(oldInput, matches[selectedIndex]);

            chatInput.SetText(newInput);
            previousInput = newInput;
            Hide();
        }

        private void OnRowClicked(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= matches.Count)
            {
                return;
            }

            selectedIndex = rowIndex;
            ApplySelection();
        }

        public void SetScrollRowsPerWheel(float value)
        {
            if (view != null)
            {
                view.SetScrollRowsPerWheel(value);
            }
        }
    }
}
