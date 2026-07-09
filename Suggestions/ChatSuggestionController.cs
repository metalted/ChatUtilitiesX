using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionController
    {
        private ChatInputController chatInput;
        private ConfigEntry<KeyCode> applySelectionKey;
        private ConfigEntry<KeyCode> applyAndSendSelectionKey;
        private ConfigEntry<KeyCode> quickUserContentKey;
        private ConfigEntry<int> maxSuggestions;
        private Action<string> sendMessage;

        private ChatSuggestionView view;
        private List<IChatSuggestionProvider> providers = new List<IChatSuggestionProvider>();
        private List<ChatSuggestionEntry> matches = new List<ChatSuggestionEntry>();
        private IChatSuggestionProvider activeProvider;
        private string previousInput = string.Empty;
        private int selectedIndex;

        private bool quickUserContentMode;

        private KeyCode heldSelectionKey = KeyCode.None;
        private float nextSelectionRepeatTime;
        private float selectionRepeatDelay = 0.35f;
        private float selectionRepeatInterval = 0.08f;

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
            ConfigEntry<KeyCode> applyAndSendSelectionKeyEntry,
            ConfigEntry<KeyCode> quickUserContentKeyEntry,
            ConfigEntry<int> maxSuggestionsEntry,
            Action<string> sendMessageAction)
        {
            chatInput = chatInputController;
            applySelectionKey = applySelectionKeyEntry;
            applyAndSendSelectionKey = applyAndSendSelectionKeyEntry;
            quickUserContentKey = quickUserContentKeyEntry;
            maxSuggestions = maxSuggestionsEntry;
            sendMessage = sendMessageAction;

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
            quickUserContentMode = false;
            heldSelectionKey = KeyCode.None;
            nextSelectionRepeatTime = 0f;
            chatInput = null;
            sendMessage = null;
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
            quickUserContentMode = false;

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

            if (HandleQuickUserContentMode(input))
            {
                return true;
            }

            if (quickUserContentMode)
            {
                quickUserContentMode = false;
                Hide();
            }

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
            quickUserContentMode = false;
            ClearSelectionRepeat();

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

        public void SetScrollRowsPerWheel(float value)
        {
            if (view != null)
            {
                view.SetScrollRowsPerWheel(value);
            }
        }

        private bool HandleQuickUserContentMode(string input)
        {
            if (!IsQuickUserContentKeyHeld())
            {
                return false;
            }

            if (!quickUserContentMode)
            {
                quickUserContentMode = true;
                RefreshQuickUserContent();
            }

            int number = GetPressedNumberKey();

            if (number > 0)
            {
                int index = number - 1;

                if (index >= 0 && index < matches.Count)
                {
                    selectedIndex = index;
                    ApplySelection(false, true);
                }

                return true;
            }

            return true;
        }

        private bool IsQuickUserContentKeyHeld()
        {
            if (quickUserContentKey == null)
            {
                return false;
            }

            if (quickUserContentKey.Value == KeyCode.None)
            {
                return false;
            }

            return Input.GetKey(quickUserContentKey.Value);
        }

        private int GetPressedNumberKey()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                return 1;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                return 2;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                return 3;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                return 4;
            }

            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                return 5;
            }

            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                return 6;
            }

            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
            {
                return 7;
            }

            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            {
                return 8;
            }

            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            {
                return 9;
            }

            return 0;
        }

        private void RefreshQuickUserContent()
        {
            IChatSuggestionProvider provider;

            if (!TryGetProviderByName("User Content", out provider))
            {
                Hide();
                return;
            }

            List<ChatSuggestionEntry> newMatches = provider.GetMatches(string.Empty, 9);

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

        private bool TryGetProviderByName(string name, out IChatSuggestionProvider provider)
        {
            provider = null;

            for (int i = 0; i < providers.Count; i++)
            {
                IChatSuggestionProvider candidate = providers[i];

                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Name != name)
                {
                    continue;
                }

                provider = candidate;
                return true;
            }

            return false;
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

            if (HandleSelectionNavigation())
            {
                return true;
            }

            if (IsApplyAndSendSelectionPressed())
            {
                ApplySelection(true, false);
                return true;
            }

            if (IsApplySelectionPressed())
            {
                ApplySelection(false, false);
                return true;
            }

            return false;
        }

        private bool HandleSelectionNavigation()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                StartSelectionRepeat(KeyCode.DownArrow);
                MoveSelection(1);
                return true;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                StartSelectionRepeat(KeyCode.UpArrow);
                MoveSelection(-1);
                return true;
            }

            if (heldSelectionKey == KeyCode.DownArrow)
            {
                if (!Input.GetKey(KeyCode.DownArrow))
                {
                    ClearSelectionRepeat();
                    return false;
                }

                if (Time.unscaledTime >= nextSelectionRepeatTime)
                {
                    nextSelectionRepeatTime = Time.unscaledTime + selectionRepeatInterval;
                    MoveSelection(1);
                    return true;
                }
            }

            if (heldSelectionKey == KeyCode.UpArrow)
            {
                if (!Input.GetKey(KeyCode.UpArrow))
                {
                    ClearSelectionRepeat();
                    return false;
                }

                if (Time.unscaledTime >= nextSelectionRepeatTime)
                {
                    nextSelectionRepeatTime = Time.unscaledTime + selectionRepeatInterval;
                    MoveSelection(-1);
                    return true;
                }
            }

            return false;
        }

        private void StartSelectionRepeat(KeyCode key)
        {
            heldSelectionKey = key;
            nextSelectionRepeatTime = Time.unscaledTime + selectionRepeatDelay;
        }

        private void ClearSelectionRepeat()
        {
            heldSelectionKey = KeyCode.None;
            nextSelectionRepeatTime = 0f;
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

        private bool IsApplyAndSendSelectionPressed()
        {
            if (applyAndSendSelectionKey == null)
            {
                return false;
            }

            if (applyAndSendSelectionKey.Value == KeyCode.None)
            {
                return false;
            }

            return Input.GetKeyDown(applyAndSendSelectionKey.Value);
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

        private void ApplySelection(bool forceSend, bool allowEntryAutoSend)
        {
            if (activeProvider == null)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= matches.Count)
            {
                return;
            }

            ChatSuggestionEntry selected = matches[selectedIndex];

            string oldInput = chatInput.GetText() ?? string.Empty;
            string newInput = activeProvider.Apply(oldInput, selected);

            chatInput.SetText(newInput);
            previousInput = newInput;

            bool shouldSend = forceSend;

            if (!shouldSend && allowEntryAutoSend && selected.AutoSend)
            {
                shouldSend = true;
            }

            Hide();

            if (shouldSend)
            {
                SendAppliedMessage(newInput);
            }
        }

        private void SendAppliedMessage(string message)
        {
            if (sendMessage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            sendMessage(message);
        }

        private void OnRowClicked(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= matches.Count)
            {
                return;
            }

            selectedIndex = rowIndex;
            ApplySelection(false, false);
        }
    }
}