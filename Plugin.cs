using BepInEx;
using BepInEx.Configuration;
using ChatUtilities.Data;
using ChatUtilities.Suggestions;
using ChatUtilities.UI;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.Settings;
using ZeepSDK.Settings.Drawers;

namespace ChatUtilities
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("ZeepSDK", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.metalted.zeepkist.chatutilities";
        public const string PluginName = "Chat Utilities";
        public const string PluginVersion = "2.1";

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

            commands = new List<ChatCommandDefinition>();

            emotes = DefaultChatData.CreateEmotes();
            userContent = new List<UserContentDefinition>();
            history = new ChatHistoryController();

            BindConfig();
            SettingsApi.RegisterModSettingsDrawers(this, BuildSettingsDrawers);

            RebuildUserContent();

            Config.SettingChanged += OnConfigSettingChanged;

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Logger.LogInfo("Chat Utilities loaded.");
        }

        public void RefreshRegisteredCommands()
        {
            commands = CreateCombinedCommandList();

            RebuildShortcodeExpander();

            if (suggestions != null)
            {
                suggestions.SetProviders(CreateSuggestionProviders());
            }

            Logger.LogInfo("Chat Utilities loaded chat commands. Count: " + commands.Count);
        }

        private List<ChatCommandDefinition> CreateCombinedCommandList()
        {
            List<ChatCommandDefinition> result = new List<ChatCommandDefinition>();
            HashSet<string> usedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddCommands(result, usedCommands, DefaultChatData.CreateCommands());
            AddCommands(result, usedCommands, ZeepSdkChatCommandReader.CreateCommandsFromZeepSdk());

            return result;
        }

        private void AddCommands(
            List<ChatCommandDefinition> result,
            HashSet<string> usedCommands,
            List<ChatCommandDefinition> commandsToAdd)
        {
            if (commandsToAdd == null)
            {
                return;
            }

            foreach (ChatCommandDefinition command in commandsToAdd)
            {
                if (command == null)
                {
                    continue;
                }

                string commandText = command.Command ?? string.Empty;

                if (string.IsNullOrWhiteSpace(commandText))
                {
                    continue;
                }

                if (usedCommands.Contains(commandText))
                {
                    continue;
                }

                usedCommands.Add(commandText);
                result.Add(command);
            }
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
                "User Content",
                "{}",
                "[hide] JSON storage for custom user content entries.");

            ApplySelectionKey = Config.Bind(
                "1. Controls",
                "01. Apply Selection",
                KeyCode.RightArrow,
                "Accept the selected chat suggestion.");

            ClearChatFieldKey = Config.Bind(
                "1. Controls",
                "02. Clear Chat Field",
                KeyCode.None,
                "Remove all text from the chat field.");

            PasteClipboardKey = Config.Bind(
                "1. Controls",
                "03. Paste Clipboard",
                KeyCode.None,
                "Paste the clipboard contents into the chat field.");

            MaxSuggestions = Config.Bind(
                "2. User Interface",
                "01. Max Suggestions",
                40,
                "Maximum number of suggestions shown in the picker.");

            RowTextScale = Config.Bind(
                "2. User Interface",
                "02. Row Text Scale",
                18f,
                "Text scale inside the row.");

            SuggestionScrollRowsPerWheel = Config.Bind(
                "2. User Interface",
                "03. Scroll Rows Per Wheel Step",
                1f,
                "How many suggestion rows the mouse wheel should scroll per wheel step."
            );
        }

        private IEnumerable<IZeepSettingsDrawer> BuildSettingsDrawers(ModSettingsDrawerBuildContext context)
        {
            foreach(IZeepSettingsDrawer drawer in context.CreateDefaultDrawers())
            {
                yield return drawer;
            }

            yield return new ZeepSettingsHeaderDrawer("3. User Content");
            yield return new UserContentSettingsDrawer(UserContentJson);
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
