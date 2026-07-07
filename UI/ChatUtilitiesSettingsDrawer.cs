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
            gui.Text("Controls");
            DrawDefaultEntry(gui, context, Plugin.Instance.ApplySelectionKey, "Apply Selection");
            DrawDefaultEntry(gui, context, Plugin.Instance.ClearChatFieldKey, "Clear Chat Field");
            DrawDefaultEntry(gui, context, Plugin.Instance.PasteClipboardKey, "Paste Clipboard");
            gui.AddSpacing();

            gui.Separator();
            gui.AddSpacing();
            gui.Text("User Interface");
            DrawDefaultEntry(gui, context, Plugin.Instance.MaxSuggestions, "Max Suggestions");
            DrawDefaultEntry(gui, context, Plugin.Instance.RowTextScale, "Row Text Scale");
            DrawDefaultEntry(gui, context, Plugin.Instance.SuggestionScrollRowsPerWheel, "Rows Per Scroll");
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
