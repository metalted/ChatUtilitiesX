using BepInEx;
using BepInEx.Configuration;
using ChatUtilities.Data;
using ChatUtilities.Suggestions;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatUtilities
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.metalted.zeepkist.chatutilities";
        public const string PluginName = "Chat Utilities";
        public const string PluginVersion = "2.0";

        public static Plugin Instance;

        public ConfigEntry<string> UserContentJson;
        public ConfigEntry<KeyCode> ApplySelectionKey;
        public ConfigEntry<KeyCode> ClearChatFieldKey;
        public ConfigEntry<KeyCode> PasteClipboardKey;
        public ConfigEntry<int> MaxSuggestions;
        public ConfigEntry<float> RowTextScale;
        public ConfigEntry<float> SuggestionScrollRowsPerWheel;

        private Harmony harmony;
        private ChatInputController chatInput;
        private ChatHistoryController history;
        private ChatSuggestionController suggestions;
        private List<ChatCommandDefinition> commands;
        private List<EmoteDefinition> emotes;
        private List<UserContentDefinition> userContent;
        private ChatShortcodeExpander shortcodeExpander;

        private void Awake()
        {
            Instance = this;

            commands = DefaultChatData.CreateCommands();
            emotes = DefaultChatData.CreateEmotes();
            userContent = new List<UserContentDefinition>();
            history = new ChatHistoryController();

            BindConfig();
            RebuildUserContent();

            Config.SettingChanged += OnConfigSettingChanged;

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Logger.LogInfo("Chat Utilities loaded.");
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnConfigSettingChanged;
            ClearSceneState();

            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }

            Instance = null;
        }

        private void Update()
        {
            if (chatInput == null || !chatInput.IsAvailable || !chatInput.IsOpen)
            {
                return;
            }

            HandleGlobalInputShortcuts();

            bool suggestionHandledInput = false;

            if (suggestions != null)
            {
                suggestionHandledInput = suggestions.Update();
            }

            if (!suggestionHandledInput && history != null)
            {
                history.Update();
            }
        }

        public void SetOnlineChatUI(OnlineChatUI onlineChatUi)
        {
            ClearSceneState();

            if ((UnityEngine.Object)onlineChatUi == null)
            {
                return;
            }

            chatInput = new ChatInputController();
            chatInput.Bind(onlineChatUi);

            if (!chatInput.IsAvailable)
            {
                ClearSceneState();
                return;
            }

            chatInput.Clear();

            if (history != null)
            {
                history.BindInput(chatInput);
            }

            suggestions = new ChatSuggestionController(chatInput, ApplySelectionKey, MaxSuggestions);
            suggestions.SetProviders(CreateSuggestionProviders());
            suggestions.InitializeView(chatInput.ChatRoot, chatInput.ChatTextTemplate);
            suggestions.SetScrollRowsPerWheel(SuggestionScrollRowsPerWheel.Value);
            suggestions.ResetInputState();
        }

        public void ChatWasClosed()
        {
            if (suggestions != null)
            {
                suggestions.Hide();
            }
        }

        public void AddToHistory(string message)
        {
            if (history != null)
            {
                history.Add(message);
            }
        }

        public void MessageWasSent(string message)
        {
            AddToHistory(message);

            if (suggestions != null)
            {
                suggestions.ResetInputState();
            }

            if (chatInput != null && chatInput.IsAvailable)
            {
                chatInput.Clear();
            }
        }

        public string ExpandShortcodesForSend(string message)
        {
            if (shortcodeExpander == null)
            {
                RebuildShortcodeExpander();
            }

            if (shortcodeExpander == null)
            {
                return message ?? string.Empty;
            }

            return shortcodeExpander.Expand(message);
        }

        private void BindConfig()
        {
            UserContentJson = Config.Bind(
                "Settings",
                "01. User Content",
                "{}",
                "JSON key-value pairs. The key is the searchable title/description. The value is the text inserted into chat. Trigger with *. Shortcode example: *3.");

            ApplySelectionKey = Config.Bind(
                "Settings",
                "02. Apply Selection",
                KeyCode.RightArrow,
                "Accept the selected chat suggestion.");

            ClearChatFieldKey = Config.Bind(
                "Settings",
                "03. Clear Chat Field",
                KeyCode.None,
                "Remove all text from the chat field.");

            PasteClipboardKey = Config.Bind(
                "Settings",
                "04. Paste Clipboard",
                KeyCode.None,
                "Paste the clipboard contents into the chat field.");

            MaxSuggestions = Config.Bind(
                "Settings",
                "05. Max Suggestions",
                40,
                "Maximum number of suggestions shown in the picker.");

            RowTextScale = Config.Bind(
                "Settings",
                "Row Text Scale",
                18f,
                "Text scale inside the row.");

            SuggestionScrollRowsPerWheel = Config.Bind(
                "Settings",
                "06. Scroll Rows Per Wheel Step",
                1f,
                "How many suggestion rows the mouse wheel should scroll per wheel step."
            );

        }

        private void HandleGlobalInputShortcuts()
        {
            if (ClearChatFieldKey != null && ClearChatFieldKey.Value != KeyCode.None && Input.GetKeyDown(ClearChatFieldKey.Value))
            {
                chatInput.Clear();
            }

            if (PasteClipboardKey == null)
            {
                return;
            }

            if (PasteClipboardKey.Value == KeyCode.None)
            {
                return;
            }

            if (!Input.GetKeyDown(PasteClipboardKey.Value))
            {
                return;
            }

            string clipboardText = GUIUtility.systemCopyBuffer;

            if (!string.IsNullOrEmpty(clipboardText))
            {
                chatInput.SetText((chatInput.GetText() ?? string.Empty) + clipboardText);
            }
        }

        private void OnConfigSettingChanged(object sender, SettingChangedEventArgs eventArgs)
        {
            if (eventArgs == null || eventArgs.ChangedSetting == UserContentJson)
            {
                RebuildUserContent();

                if (suggestions != null)
                {
                    suggestions.SetProviders(CreateSuggestionProviders());
                }
            }

            if (suggestions != null)
            {
                suggestions.SetScrollRowsPerWheel(SuggestionScrollRowsPerWheel.Value);
            }
        }

        private void ClearSceneState()
        {
            if (suggestions != null)
            {
                suggestions.Destroy();
                suggestions = null;
            }

            if (chatInput != null)
            {
                chatInput.Unbind();
                chatInput = null;
            }

            if (history != null)
            {
                history.BindInput(null);
            }
        }

        private List<IChatSuggestionProvider> CreateSuggestionProviders()
        {
            return new List<IChatSuggestionProvider>
            {
                new CommandSuggestionProvider(commands),
                new UserContentSuggestionProvider(userContent),
                new EmoteSuggestionProvider(emotes)
            };
        }

        private void RebuildUserContent()
        {
            string json = "{}";

            if (UserContentJson != null)
            {
                json = UserContentJson.Value;
            }

            userContent = ParseUserContent(json);
            RebuildShortcodeExpander();
        }

        private void RebuildShortcodeExpander()
        {
            shortcodeExpander = new ChatShortcodeExpander(commands, emotes, userContent);
        }

        private List<UserContentDefinition> ParseUserContent(string json)
        {
            List<UserContentDefinition> result = new List<UserContentDefinition>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            try
            {
                Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (dictionary == null)
                {
                    return result;
                }

                foreach (KeyValuePair<string, string> pair in dictionary)
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                    {
                        continue;
                    }

                    result.Add(new UserContentDefinition(pair.Key, pair.Value));
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning("Chat Utilities could not parse user content JSON: " + exception.Message);
            }

            return result;
        }
    }
}
