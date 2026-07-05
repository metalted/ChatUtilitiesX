using UnityEngine;

namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionStyle
    {
        public Color PanelColor { get; private set; }
        public Color RowColor { get; private set; }
        public Color SelectedRowColor { get; private set; }
        public Color ViewportColor { get; private set; }
        public Vector2 PanelAnchorMin { get; private set; }
        public Vector2 PanelAnchorMax { get; private set; }
        public float RowHeightMultiplier { get; private set; }

        public ChatSuggestionStyle(
            Color panelColor,
            Color rowColor,
            Color selectedRowColor,
            Color viewportColor,
            Vector2 panelAnchorMin,
            Vector2 panelAnchorMax,
            float rowHeightMultiplier)
        {
            PanelColor = panelColor;
            RowColor = rowColor;
            SelectedRowColor = selectedRowColor;
            ViewportColor = viewportColor;
            PanelAnchorMin = panelAnchorMin;
            PanelAnchorMax = panelAnchorMax;
            RowHeightMultiplier = Mathf.Max(0.25f, rowHeightMultiplier);
        }
    }
}
