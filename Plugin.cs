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
        public const string PluginVersion = "2.4";

        public static Plugin Instance;

        public ConfigEntry<string> UserContentJson;
        public ConfigEntry<KeyCode> ApplySelectionKey;
        public ConfigEntry<KeyCode> ClearChatFieldKey;
        public ConfigEntry<KeyCode> PasteClipboardKey;
        public ConfigEntry<KeyCode> ApplyAndSendSelectionKey;
        public ConfigEntry<KeyCode> QuickUserContentKey;
        public ConfigEntry<KeyCode> PreviousHistoryKey;
        public ConfigEntry<KeyCode> NextHistoryKey;
        public ConfigEntry<string> EmoteTriggerCharacter;

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

        private OnlineChatUI onlineChatUi;
        private string preservedChatText = string.Empty;
        private bool preservedChatWasOpen;

        // Track when ChatUtilities consumes Enter key
        private static bool justConsumedEnterKey = false;
        private static float enterKeyConsumedTime = 0f;
        private const float ENTER_KEY_SUPPRESS_DURATION = 0.15f;

        public static bool ShouldSuppressEnterKey()
        {
            return justConsumedEnterKey && Time.unscaledTime - enterKeyConsumedTime < ENTER_KEY_SUPPRESS_DURATION;
        }

        public static void ConsumeEnterKey()
        {
            justConsumedEnterKey = true;
            enterKeyConsumedTime = Time.unscaledTime;
        }

        public static void ClearEnterKeyConsumption()
        {
            justConsumedEnterKey = false;
        }

        public ChatSuggestionController GetSuggestionController()
        {
            return suggestions;
        }

        private void Awake()
        {
            Instance = this;

            commands = new List<ChatCommandDefinition>();

            emotes = DefaultChatData.CreateEmotes();
            userContent = new List<UserContentDefinition>();

            BindConfig();

            history = new ChatHistoryController(
                PreviousHistoryKey,
                NextHistoryKey);

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
            CaptureChatSceneState();
            ClearSceneState();

            if ((UnityEngine.Object)onlineChatUi == null)
            {
                return;
            }

            this.onlineChatUi = onlineChatUi;

            chatInput = new ChatInputController();
            chatInput.Bind(onlineChatUi);

            if (!chatInput.IsAvailable)
            {
                ClearSceneState();
                return;
            }

            RestoreChatSceneState();

            if (history != null)
            {
                history.BindInput(chatInput);
            }

            suggestions = new ChatSuggestionController(chatInput, ApplySelectionKey, ApplyAndSendSelectionKey, QuickUserContentKey, MaxSuggestions, SendSuggestionMessage);
            suggestions.SetProviders(CreateSuggestionProviders());
            suggestions.InitializeView(chatInput.ChatRoot, chatInput.ChatTextTemplate);   
            suggestions.ResetInputState();
            suggestions.SetScrollRowsPerWheel(SuggestionScrollRowsPerWheel.Value);
        }

        private void CaptureChatSceneState()
        {
            preservedChatText = string.Empty;
            preservedChatWasOpen = false;

            if (chatInput == null || !chatInput.IsAvailable)
            {
                return;
            }

            preservedChatText = chatInput.GetText() ?? string.Empty;
            preservedChatWasOpen = OnlineChatUI.wasTyping;
        }

        private void RestoreChatSceneState()
        {
            if (chatInput == null || !chatInput.IsAvailable)
            {
                return;
            }

            if (!string.IsNullOrEmpty(preservedChatText))
            {
                chatInput.SetText(preservedChatText);
            }

            if (preservedChatWasOpen && (UnityEngine.Object)onlineChatUi != null)
            {
                OnlineChatUI.wasTyping = true;
                onlineChatUi.EnableSmallBox(false);
            }
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

            preservedChatText = string.Empty;
            preservedChatWasOpen = false;

            if (suggestions != null)
            {
                suggestions.ResetInputState();
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
                KeyCode.None,
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

            ApplyAndSendSelectionKey = Config.Bind(
                "1. Controls",
                "04. Apply And Send Selection",
                KeyCode.None,
                "Accept the selected chat suggestion and immediately send the resulting chat message.");

            QuickUserContentKey = Config.Bind(
                "1. Controls",
                "05. Quick User Content",
                KeyCode.None,
                "Hold this key to show user content suggestions without typing *.\nWhile held, press 1-9 to apply a user content entry.");

            PreviousHistoryKey = Config.Bind(
               "1. Controls",
               "06. Previous History",
               KeyCode.None,
               "Go to the previous sent chat message.");

            NextHistoryKey = Config.Bind(
                "Settings",
                "07. Next History",
                KeyCode.None,
                "Go to the next sent chat message.");

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

            EmoteTriggerCharacter = Config.Bind(
                "2. User Interface",
                "04. Emote Trigger Character",
                ":",
                "Character used to trigger emote suggestions.\nDefault is ':' but you can use ';' or '|' etc.\nThe actual emote codes remain unchanged."
            );

            SuggestionScrollRowsPerWheel = Config.Bind(
                "2. User Interface",
                "03. Scroll Rows Per Wheel Step",
                1f,
                "How many suggestion rows the mouse wheel should scroll per wheel step."
            );           
        }

        private IEnumerable<IZeepSettingsDrawer> BuildSettingsDrawers(ModSettingsDrawerBuildContext context)
        {
            yield return new ChatUtilitiesSettingsDrawer();
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

            onlineChatUi = null;

            if (history != null)
            {
                history.BindInput(null);
            }
        }

        private void SendSuggestionMessage(string message)
        {
            if ((UnityEngine.Object)onlineChatUi == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            onlineChatUi.SendChatMessage(message);

            preservedChatText = string.Empty;
            preservedChatWasOpen = false;

            OnlineChatUI.wasTyping = false;
            onlineChatUi.EnableSmallBox(true);

            if (suggestions != null)
            {
                suggestions.ResetInputState();
            }
        }

        private List<IChatSuggestionProvider> CreateSuggestionProviders()
        {
            return new List<IChatSuggestionProvider>
            {
                new CommandSuggestionProvider(commands),
                new UserContentSuggestionProvider(userContent),
                new EmoteSuggestionProvider(emotes, EmoteTriggerCharacter)
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
            shortcodeExpander = new ChatShortcodeExpander(commands, emotes, userContent, EmoteTriggerCharacter);
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
                Newtonsoft.Json.Linq.JObject root = Newtonsoft.Json.Linq.JObject.Parse(json);

                foreach (Newtonsoft.Json.Linq.JProperty property in root.Properties())
                {
                    string title = property.Name;
                    string content = string.Empty;
                    bool autoSend = false;

                    if (property.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        content = property.Value.ToString();
                        autoSend = false;
                    }
                    else if (property.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        Newtonsoft.Json.Linq.JObject entryObject = (Newtonsoft.Json.Linq.JObject)property.Value;

                        Newtonsoft.Json.Linq.JToken contentToken;

                        if (entryObject.TryGetValue("Content", System.StringComparison.OrdinalIgnoreCase, out contentToken))
                        {
                            content = contentToken.ToString();
                        }

                        Newtonsoft.Json.Linq.JToken autoSendToken;

                        if (entryObject.TryGetValue("AutoSend", System.StringComparison.OrdinalIgnoreCase, out autoSendToken))
                        {
                            if (autoSendToken.Type == Newtonsoft.Json.Linq.JTokenType.Boolean)
                            {
                                autoSend = autoSendToken.ToObject<bool>();
                            }
                            else
                            {
                                bool.TryParse(autoSendToken.ToString(), out autoSend);
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        continue;
                    }

                    result.Add(new UserContentDefinition(title, content, autoSend));
                }
            }
            catch (System.Exception exception)
            {
                Logger.LogWarning("Chat Utilities could not parse user content JSON: " + exception.Message);
            }

            return result;
        }
    }
}
