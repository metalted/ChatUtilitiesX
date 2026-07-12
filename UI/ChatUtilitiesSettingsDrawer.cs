using BepInEx.Configuration;
using Imui.Controls;
using Imui.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeepSDK.Settings.Drawers;

namespace ChatUtilities.UI
{
    public class ChatUtilitiesSettingsDrawer : IZeepSettingsDrawer
    {
        public void Draw(ImGui gui, ZeepSettingsDrawContext context)
        {
            gui.Separator();
            gui.AddSpacing();
            gui.Text("How to use");
            gui.BeginIndent();
            gui.Text("Start a message with / to see available commands.", wrap:true);
            gui.Text("Use the emote trigger character anywhere in the message to see available emotes.", wrap: true);
            gui.Text("Start a message with * to see available user content.", wrap: true);
            gui.Text("Search for a suggestion by typing part of the name, or using the number of the suggestion.", wrap: true);
            gui.Text("Examples: '/skip', '/2', ':smile', ':10', '*gg', '*3'", wrap: true);
            gui.EndIndent();
            gui.AddSpacing();

            gui.Separator();
            gui.AddSpacing();
            gui.Text("Controls");
            DrawDefaultEntry(gui, context, Plugin.Instance.ApplySelectionKey, "Apply Selection");
            DrawDefaultEntry(gui, context, Plugin.Instance.ApplyAndSendSelectionKey, "Apply & Send Selection");
            DrawDefaultEntry(gui, context, Plugin.Instance.ClearChatFieldKey, "Clear Chat Field");
            DrawDefaultEntry(gui, context, Plugin.Instance.PasteClipboardKey, "Paste Clipboard");
            DrawDefaultEntry(gui, context, Plugin.Instance.PreviousHistoryKey, "Previous History");
            DrawDefaultEntry(gui, context, Plugin.Instance.NextHistoryKey, "Next History");
            DrawDefaultEntry(gui, context, Plugin.Instance.QuickUserContentKey, "Quick User Content");
            gui.AddSpacing();

            gui.Separator();
            gui.AddSpacing();
            gui.Text("User Interface");
            DrawDefaultEntry(gui, context, Plugin.Instance.MaxSuggestions, "Max Suggestions");
            DrawDefaultEntry(gui, context, Plugin.Instance.RowTextScale, "Row Text Scale");
            DrawDefaultEntry(gui, context, Plugin.Instance.SuggestionScrollRowsPerWheel, "Rows Per Scroll");
            DrawDefaultEntry(gui, context, Plugin.Instance.EmoteTriggerCharacter, "Emote Trigger Character");
            gui.AddSpacing();

            gui.Separator();
            gui.AddSpacing();
            gui.Text("User Content");
            gui.AddSpacing();
        }

        private void DrawDefaultEntry(ImGui gui, ZeepSettingsDrawContext context, ConfigEntryBase entry, string label)
        {
            if (entry == null)
            {
                return;
            }

            IZeepSettingsDrawer drawer = new ZeepSettingsEntryDrawer(entry, label);
            drawer.Draw(gui, context);
        }
    }
}
