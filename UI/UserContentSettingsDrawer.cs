using BepInEx.Configuration;
using Imui.Controls;
using Imui.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.Settings.Drawers;

namespace ChatUtilities.UI
{
    public class UserContentSettingsDrawer : IZeepSettingsDrawer
    {
        private readonly ConfigEntry<string> userContentJson;
        private readonly List<EditableUserContentEntry> entries = new List<EditableUserContentEntry>();

        private string loadedJson = string.Empty;
        private string statusText = string.Empty;
        private bool isDirty;

        public UserContentSettingsDrawer(ConfigEntry<string> userContentJson)
        {
            this.userContentJson = userContentJson;
            ReloadFromConfig();
        }

        public void Draw(ImGui gui, ZeepSettingsDrawContext context)
        {
            if (!isDirty && userContentJson != null && loadedJson != userContentJson.Value)
            {
                ReloadFromConfig();
            }

            using (gui.Indent())
            {
                gui.Text("Create custom chat suggestions here. Use * to search them in chat.");
                gui.AddSpacing();

                UserContentTableLayout layout = UserContentTableLayout.Create(gui);

                DrawColumnHeader(gui, layout);
                gui.Separator();

                DrawEntries(gui, layout);

                gui.AddSpacing();
                gui.Separator();
                gui.AddSpacing();

                DrawBottomButtons(gui, layout);

                if (isDirty)
                {
                    gui.AddSpacing();
                    gui.Text("Unsaved changes.");
                }

                if (!string.IsNullOrWhiteSpace(statusText))
                {
                    gui.AddSpacing();
                    gui.Text(statusText);
                }
            }

            gui.Separator();
        }

        private void DrawColumnHeader(ImGui gui, UserContentTableLayout layout)
        {
            gui.BeginHorizontal(layout.TotalWidth);

            DrawTextCell(gui, "Title", layout.TitleWidth, layout.HeaderHeight);
            DrawTextCell(gui, "Content", layout.ContentWidth, layout.HeaderHeight);
            DrawTextCell(gui, "", layout.UpButtonWidth, layout.HeaderHeight);
            DrawTextCell(gui, "", layout.DownButtonWidth, layout.HeaderHeight);
            DrawTextCell(gui, "", layout.DeleteButtonWidth, layout.HeaderHeight);

            gui.EndHorizontal();
        }

        private void DrawEntries(ImGui gui, UserContentTableLayout layout)
        {
            if (entries.Count == 0)
            {
                gui.AddSpacing();
                gui.Text("No user content entries yet.");
                return;
            }

            int moveUpIndex = -1;
            int moveDownIndex = -1;
            int removeIndex = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                DrawEntryRow(gui, layout, i, ref moveUpIndex, ref moveDownIndex, ref removeIndex);

                if (i < entries.Count - 1)
                {
                    DrawRowPadding(gui, layout.RowPadding);
                }
            }

            if (moveUpIndex >= 0)
            {
                MoveEntryUp(moveUpIndex);
            }

            if (moveDownIndex >= 0)
            {
                MoveEntryDown(moveDownIndex);
            }

            if (removeIndex >= 0)
            {
                RemoveEntry(removeIndex);
            }
        }

        private void DrawEntryRow(
            ImGui gui,
            UserContentTableLayout layout,
            int index,
            ref int moveUpIndex,
            ref int moveDownIndex,
            ref int removeIndex)
        {
            EditableUserContentEntry entry = entries[index];

            gui.BeginHorizontal(layout.TotalWidth);

            string title = gui.TextEdit(entry.Title, new ImSize(layout.TitleWidth, layout.RowHeight));

            if (title != entry.Title)
            {
                entry.Title = title;
                MarkDirty();
            }

            string content = gui.TextEdit(entry.Content, new ImSize(layout.ContentWidth, layout.RowHeight));

            if (content != entry.Content)
            {
                entry.Content = content;
                MarkDirty();
            }

            bool canMoveUp = index > 0;
            bool canMoveDown = index < entries.Count - 1;

            if (canMoveUp)
            {
                if (gui.Button("▲", new ImSize(layout.UpButtonWidth, layout.RowHeight)))
                {
                    moveUpIndex = index;
                }
            }
            else
            {
                DrawPlaceholderButton(gui, layout.UpButtonWidth, layout.RowHeight);
            }

            if (canMoveDown)
            {
                if (gui.Button("▼", new ImSize(layout.DownButtonWidth, layout.RowHeight)))
                {
                    moveDownIndex = index;
                }
            }
            else
            {
                DrawPlaceholderButton(gui, layout.DownButtonWidth, layout.RowHeight);
            }

            if (gui.Button("X", new ImSize(layout.DeleteButtonWidth, layout.RowHeight)))
            {
                removeIndex = index;
            }

            gui.EndHorizontal();
        }

        private void DrawBottomButtons(ImGui gui, UserContentTableLayout layout)
        {
            gui.BeginHorizontal(layout.TotalWidth);

            if (gui.Button("Add New Entry", new ImSize(160f, layout.RowHeight)))
            {
                entries.Add(new EditableUserContentEntry(string.Empty, string.Empty));
                MarkDirty();
            }

            if (gui.Button("Save", new ImSize(100f, layout.RowHeight)))
            {
                SaveToConfig();
            }

            if (gui.Button("Discard", new ImSize(100f, layout.RowHeight)))
            {
                ReloadFromConfig();
            }

            gui.EndHorizontal();
        }

        private void DrawTextCell(ImGui gui, string text, float width, float height)
        {
            ImRect rect = gui.Layout.GetRect(width, height);
            gui.Text(text, new ImTextSettings(18), rect);
            gui.Layout.AddRect(rect);
        }

        private void DrawEmptyCell(ImGui gui, float width, float height)
        {
            ImRect rect = gui.Layout.GetRect(width, height);
            gui.Layout.AddRect(rect);
        }

        private void DrawRowPadding(ImGui gui, float height)
        {
            ImRect rect = gui.Layout.GetRect(1f, height);
            gui.Layout.AddRect(rect);
        }

        private void DrawPlaceholderButton(ImGui gui, float width, float height)
        {
            gui.Button("•", new ImSize(width, height));
        }

        private void MoveEntryUp(int index)
        {
            if (index <= 0 || index >= entries.Count)
            {
                return;
            }

            EditableUserContentEntry entry = entries[index];
            entries.RemoveAt(index);
            entries.Insert(index - 1, entry);

            MarkDirty();
        }

        private void MoveEntryDown(int index)
        {
            if (index < 0 || index >= entries.Count - 1)
            {
                return;
            }

            EditableUserContentEntry entry = entries[index];
            entries.RemoveAt(index);
            entries.Insert(index + 1, entry);

            MarkDirty();
        }

        private void RemoveEntry(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                return;
            }

            entries.RemoveAt(index);
            MarkDirty();
        }

        private void ReloadFromConfig()
        {
            entries.Clear();

            loadedJson = "{}";

            if (userContentJson != null && !string.IsNullOrWhiteSpace(userContentJson.Value))
            {
                loadedJson = userContentJson.Value;
            }

            try
            {
                Dictionary<string, string> dictionary =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(loadedJson);

                if (dictionary != null)
                {
                    foreach (KeyValuePair<string, string> pair in dictionary)
                    {
                        entries.Add(new EditableUserContentEntry(pair.Key, pair.Value));
                    }
                }

                isDirty = false;
                statusText = string.Empty;
            }
            catch (Exception exception)
            {
                isDirty = false;
                statusText = "Could not load user content JSON: " + exception.Message;
            }
        }

        private void SaveToConfig()
        {
            if (userContentJson == null)
            {
                statusText = "Could not save: config entry is missing.";
                return;
            }

            Dictionary<string, string> dictionary;

            if (!TryBuildDictionary(out dictionary, out string error))
            {
                statusText = error;
                return;
            }

            string json = JsonConvert.SerializeObject(dictionary, Formatting.Indented);

            userContentJson.Value = json;
            loadedJson = json;
            isDirty = false;
            statusText = "Saved user content.";
        }

        private bool TryBuildDictionary(out Dictionary<string, string> dictionary, out string error)
        {
            dictionary = new Dictionary<string, string>();
            error = string.Empty;

            HashSet<string> usedTitles = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                EditableUserContentEntry entry = entries[i];

                string title = (entry.Title ?? string.Empty).Trim();
                string content = entry.Content ?? string.Empty;

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    error = "Entry " + (i + 1) + " has content but no title.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    error = "Entry '" + title + "' has no content.";
                    return false;
                }

                if (usedTitles.Contains(title))
                {
                    error = "Duplicate title: " + title;
                    return false;
                }

                usedTitles.Add(title);
                dictionary.Add(title, content);
            }

            return true;
        }

        private void MarkDirty()
        {
            isDirty = true;
            statusText = string.Empty;
        }
    }

    public class UserContentTableLayout
    {
        public float TotalWidth;
        public float TitleWidth;
        public float ContentWidth;
        public float UpButtonWidth;
        public float DownButtonWidth;
        public float DeleteButtonWidth;
        public float RowHeight;
        public float HeaderHeight;
        public float RowPadding;

        public static UserContentTableLayout Create(ImGui gui)
        {
            float totalWidth = gui.GetLayoutWidth();
            float spacing = gui.Style.Layout.Spacing;

            float rowHeight = Mathf.Max(30f, gui.GetRowHeight());
            float headerHeight = Mathf.Max(24f, gui.GetRowHeight());
            float rowPadding = Mathf.Max(3f, spacing * 0.5f);

            float upButtonWidth = 58f;
            float downButtonWidth = 68f;
            float deleteButtonWidth = 42f;

            float reservedWidth =
                upButtonWidth +
                downButtonWidth +
                deleteButtonWidth +
                spacing * 4f;

            float availableTextWidth = Mathf.Max(260f, totalWidth - reservedWidth);

            float titleWidth = Mathf.Clamp(
                availableTextWidth * 0.28f,
                130f,
                220f);

            float contentWidth = Mathf.Max(
                180f,
                availableTextWidth - titleWidth);

            return new UserContentTableLayout
            {
                TotalWidth = totalWidth,
                TitleWidth = titleWidth,
                ContentWidth = contentWidth,
                UpButtonWidth = upButtonWidth,
                DownButtonWidth = downButtonWidth,
                DeleteButtonWidth = deleteButtonWidth,
                RowHeight = rowHeight,
                HeaderHeight = headerHeight,
                RowPadding = rowPadding
            };
        }
    }

    public class EditableUserContentEntry
    {
        public string Title;
        public string Content;

        public EditableUserContentEntry(string title, string content)
        {
            Title = title ?? string.Empty;
            Content = content ?? string.Empty;
        }
    }
}