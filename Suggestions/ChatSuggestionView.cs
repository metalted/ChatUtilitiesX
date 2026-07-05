using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChatUtilities.Suggestions
{
    public class ChatSuggestionView
    {
        private GameObject panel;
        private RectTransform panelTransform;
        private RectTransform contentRoot;
        private ScrollRect scrollRect;
        private Image panelImage;
        private Image viewportImage;
        private GameObject chatTextTemplate;
        private TextMeshProUGUI chatTextTemplateText;
        private float baseRowHeight;
        private float baseFontSize;
        private ChatSuggestionStyle currentStyle;
        private float scrollRowsPerWheel = 1f;
        private List<ChatSuggestionRow> rows = new List<ChatSuggestionRow>();

        public event Action<int> RowClicked;

        public bool IsOpen
        {
            get
            {
                return (UnityEngine.Object)panel != null && panel.activeSelf;
            }
        }

        public void Initialize(GameObject chatRoot, GameObject textTemplate)
        {
            DestroyView();

            if ((UnityEngine.Object)chatRoot == null)
            {
                Debug.LogError("Chat Utilities: Cannot initialize suggestions without chat root.");
                return;
            }

            if ((UnityEngine.Object)textTemplate == null)
            {
                Debug.LogError("Chat Utilities: Cannot initialize suggestions without text template.");
                return;
            }

            chatTextTemplate = textTemplate;
            chatTextTemplateText = chatTextTemplate.GetComponent<TextMeshProUGUI>();
            CaptureTemplateSizing();

            currentStyle = CreateFallbackStyle();
            BuildPanel(chatRoot);
            Hide();
        }

        public void DestroyView()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                ChatSuggestionRow row = rows[i];

                if (row != null && (UnityEngine.Object)row.Root != null)
                {
                    UnityEngine.Object.Destroy(row.Root);
                }
            }

            rows.Clear();

            if ((UnityEngine.Object)panel != null)
            {
                UnityEngine.Object.Destroy(panel);
            }

            panel = null;
            panelTransform = null;
            contentRoot = null;
            scrollRect = null;
            panelImage = null;
            viewportImage = null;
            chatTextTemplate = null;
            chatTextTemplateText = null;
            baseRowHeight = 0f;
            baseFontSize = 0f;
            currentStyle = null;
        }

        public void Show(IList<ChatSuggestionEntry> entries, ChatSuggestionStyle style)
        {
            if ((UnityEngine.Object)panel == null || (UnityEngine.Object)contentRoot == null)
            {
                return;
            }

            if (entries == null || entries.Count == 0)
            {
                Hide();
                return;
            }

            currentStyle = style ?? CreateFallbackStyle();
            ApplyStyle(currentStyle);
            EnsureRowCount(entries.Count);
            ApplyCurrentStyleToRows();

            for (int i = 0; i < rows.Count; i++)
            {
                ChatSuggestionRow row = rows[i];

                if (row == null || (UnityEngine.Object)row.Root == null)
                {
                    continue;
                }

                if (i >= entries.Count)
                {
                    row.Root.SetActive(false);
                    continue;
                }

                row.Index = i;
                row.Root.SetActive(true);

                /*if ((UnityEngine.Object)row.Text != null)
                {
                    ApplyTextTemplateSettings(row.Text);
                    row.Text.text = entries[i].DisplayText;
                }*/

                string primaryText;
                string secondaryText;
                SplitDisplayText(entries[i].DisplayText, out primaryText, out secondaryText);

                bool hasSecondaryText = !string.IsNullOrEmpty(secondaryText);
                ConfigureRowTextLayout(row, hasSecondaryText);

                if ((UnityEngine.Object)row.PrimaryText != null)
                {
                    ApplyTextTemplateSettings(row.PrimaryText);
                    row.PrimaryText.text = primaryText;
                }

                if ((UnityEngine.Object)row.SecondaryText != null)
                {
                    ApplyTextTemplateSettings(row.SecondaryText);
                    row.SecondaryText.text = secondaryText;
                }

                SetRowSelected(row, false);
            }

            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        public void Hide()
        {
            if ((UnityEngine.Object)panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void SetSelectedIndex(int selectedIndex)
        {
            if ((UnityEngine.Object)panel == null)
            {
                return;
            }

            RectTransform selectedTransform = null;

            for (int i = 0; i < rows.Count; i++)
            {
                ChatSuggestionRow row = rows[i];

                if (row == null || (UnityEngine.Object)row.Root == null || !row.Root.activeSelf)
                {
                    continue;
                }

                bool selected = row.Index == selectedIndex;
                SetRowSelected(row, selected);

                if (selected)
                {
                    selectedTransform = row.Transform;
                }
            }

            if ((UnityEngine.Object)selectedTransform != null)
            {
                ScrollToRow(selectedTransform);
            }
        }

        /*private void CaptureTemplateSizing()
        {
            RectTransform templateTransform = null;

            if ((UnityEngine.Object)chatTextTemplate != null)
            {
                templateTransform = chatTextTemplate.GetComponent<RectTransform>();
            }

            if ((UnityEngine.Object)chatTextTemplateText != null)
            {
                baseFontSize = Mathf.Max(1f, chatTextTemplateText.fontSize);
            }
            else
            {
                baseFontSize = 18f;
            }

            if ((UnityEngine.Object)templateTransform != null && templateTransform.rect.height > 4f)
            {
                baseRowHeight = templateTransform.rect.height;
            }
            else
            {
                baseRowHeight = Mathf.Max(24f, baseFontSize * 1.4f);
            }
        }*/

        private void CaptureTemplateSizing()
        {
            RectTransform templateTransform = null;

            if ((UnityEngine.Object)chatTextTemplate != null)
            {
                templateTransform = chatTextTemplate.GetComponent<RectTransform>();
            }

            float configuredFontSize = Plugin.Instance.RowTextScale.Value;

            if (float.IsNaN(configuredFontSize) || float.IsInfinity(configuredFontSize))
            {
                configuredFontSize = 18f;
            }

            baseFontSize = Mathf.Max(4f, configuredFontSize);

            if ((UnityEngine.Object)templateTransform != null && templateTransform.rect.height > 4f)
            {
                baseRowHeight = templateTransform.rect.height;
            }
            else
            {
                baseRowHeight = Mathf.Max(24f, baseFontSize * 1.4f);
            }
        }

        public void SetScrollRowsPerWheel(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 1f;
            }

            scrollRowsPerWheel = Mathf.Clamp(value, 0.1f, 20f);
            ApplyScrollSensitivity();
        }

        private float GetCurrentRowHeight()
        {
            ChatSuggestionStyle style = currentStyle ?? CreateFallbackStyle();
            return Mathf.Max(1f, baseRowHeight * style.RowHeightMultiplier);
        }

        private void ApplyScrollSensitivity()
        {
            if ((UnityEngine.Object)scrollRect == null)
            {
                return;
            }

            float rowHeight = GetCurrentRowHeight();
            scrollRect.scrollSensitivity = rowHeight * scrollRowsPerWheel;
        }

        private void BuildPanel(GameObject chatRoot)
        {
            panel = new GameObject("Chat Utilities Suggestion Panel", new Type[] { typeof(RectTransform) });
            panel.transform.SetParent(chatRoot.transform.parent, false);
            panelTransform = panel.GetComponent<RectTransform>();
            panelImage = panel.AddComponent<Image>();

            GameObject scrollViewObject = new GameObject(
                "Scroll View",
                new Type[] { typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect) });
            scrollViewObject.transform.SetParent(panel.transform, false);

            RectTransform scrollViewTransform = scrollViewObject.GetComponent<RectTransform>();
            scrollViewTransform.anchorMin = Vector2.zero;
            scrollViewTransform.anchorMax = Vector2.one;
            scrollViewTransform.offsetMin = Vector2.zero;
            scrollViewTransform.offsetMax = Vector2.zero;

            viewportImage = scrollViewObject.GetComponent<Image>();
            Mask mask = scrollViewObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject(
                "Content",
                new Type[] { typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter) });
            contentObject.transform.SetParent(scrollViewObject.transform, false);

            contentRoot = contentObject.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = scrollViewObject.GetComponent<ScrollRect>();
            scrollRect.viewport = scrollViewTransform;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;

            ApplyStyle(currentStyle);
        }

        private void ApplyStyle(ChatSuggestionStyle style)
        {
            if (style == null)
            {
                return;
            }

            if ((UnityEngine.Object)panelTransform != null)
            {
                panelTransform.anchorMin = style.PanelAnchorMin;
                panelTransform.anchorMax = style.PanelAnchorMax;
                panelTransform.pivot = new Vector2(0.5f, 0.5f);
                panelTransform.anchoredPosition = Vector2.zero;
                panelTransform.sizeDelta = Vector2.zero;
            }

            if ((UnityEngine.Object)panelImage != null)
            {
                panelImage.color = style.PanelColor;
            }

            if ((UnityEngine.Object)viewportImage != null)
            {
                viewportImage.color = style.ViewportColor;
            }
        }

        private void EnsureRowCount(int count)
        {
            while (rows.Count < count)
            {
                rows.Add(CreateRow(rows.Count));
            }
        }

        /*private ChatSuggestionRow CreateRow(int index)
        {
            GameObject rowObject = new GameObject(
                "ChatSuggestionRow",
                new Type[] { typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement) });
            rowObject.transform.SetParent(contentRoot, false);

            RectTransform rowTransform = rowObject.GetComponent<RectTransform>();
            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            Image rowImage = rowObject.GetComponent<Image>();
            Button button = rowObject.GetComponent<Button>();
            ChatSuggestionRow row = new ChatSuggestionRow(index, rowObject, rowTransform, layoutElement, rowImage, null);

            GameObject textObject = UnityEngine.Object.Instantiate(chatTextTemplate, rowObject.transform);
            textObject.name = "SuggestionText";
            textObject.transform.localScale = Vector3.one;
            textObject.transform.localRotation = Quaternion.identity;

            RectTransform textTransform = textObject.GetComponent<RectTransform>();

            if ((UnityEngine.Object)textTransform != null)
            {
                textTransform.anchorMin = new Vector2(0.02f, 0.02f);
                textTransform.anchorMax = new Vector2(0.98f, 0.98f);
                textTransform.pivot = new Vector2(0f, 0.5f);
                textTransform.anchoredPosition = new Vector2(10f, 0f);
                textTransform.offsetMin = new Vector2(10f, 0f);
                textTransform.offsetMax = new Vector2(-10f, 0f);
            }

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();

            if ((UnityEngine.Object)text != null)
            {
                ApplyTextTemplateSettings(text);
                text.text = string.Empty;
            }

            row.Text = text;
            ApplyCurrentStyleToRow(row);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((UnityAction)delegate
            {
                if (RowClicked != null)
                {
                    RowClicked(row.Index);
                }
            });

            return row;
        }*/

        private ChatSuggestionRow CreateRow(int index)
        {
            GameObject rowObject = new GameObject(
                "ChatSuggestionRow",
                new Type[] { typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement) });
            rowObject.transform.SetParent(contentRoot, false);

            RectTransform rowTransform = rowObject.GetComponent<RectTransform>();
            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            Image rowImage = rowObject.GetComponent<Image>();
            Button button = rowObject.GetComponent<Button>();

            ChatSuggestionRow row = new ChatSuggestionRow(
                index,
                rowObject,
                rowTransform,
                layoutElement,
                rowImage,
                null,
                null);

            GameObject primaryTextObject = CreateTextObject(rowObject.transform, "PrimaryText", 0.50f, 0.98f);
            GameObject secondaryTextObject = CreateTextObject(rowObject.transform, "SecondaryText", 0.02f, 0.50f);

            TextMeshProUGUI primaryText = primaryTextObject.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI secondaryText = secondaryTextObject.GetComponent<TextMeshProUGUI>();

            if ((UnityEngine.Object)primaryText != null)
            {
                ApplyTextTemplateSettings(primaryText);
                primaryText.text = string.Empty;
            }

            if ((UnityEngine.Object)secondaryText != null)
            {
                ApplyTextTemplateSettings(secondaryText);
                secondaryText.text = string.Empty;
            }

            row.PrimaryText = primaryText;
            row.SecondaryText = secondaryText;

            ApplyCurrentStyleToRow(row);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((UnityAction)delegate
            {
                if (RowClicked != null)
                {
                    RowClicked(row.Index);
                }
            });

            return row;
        }

        private GameObject CreateTextObject(Transform parent, string name, float anchorMinY, float anchorMaxY)
        {
            GameObject textObject = UnityEngine.Object.Instantiate(chatTextTemplate, parent);
            textObject.name = name;
            textObject.transform.localScale = Vector3.one;
            textObject.transform.localRotation = Quaternion.identity;

            RectTransform textTransform = textObject.GetComponent<RectTransform>();

            if ((UnityEngine.Object)textTransform != null)
            {
                textTransform.anchorMin = new Vector2(0.02f, anchorMinY);
                textTransform.anchorMax = new Vector2(0.98f, anchorMaxY);
                textTransform.pivot = new Vector2(0f, 0.5f);
                textTransform.anchoredPosition = Vector2.zero;
                textTransform.offsetMin = new Vector2(10f, 0f);
                textTransform.offsetMax = new Vector2(-10f, 0f);
            }

            return textObject;
        }

        private void ApplyCurrentStyleToRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                ApplyCurrentStyleToRow(rows[i]);
            }
        }

        private void ApplyCurrentStyleToRow(ChatSuggestionRow row)
        {
            if (row == null)
            {
                return;
            }

            float rowHeight = GetCurrentRowHeight();

            if ((UnityEngine.Object)row.Transform != null)
            {
                row.Transform.sizeDelta = new Vector2(0f, rowHeight);
            }

            if (row.LayoutElement != null)
            {
                row.LayoutElement.minHeight = rowHeight;
                row.LayoutElement.preferredHeight = rowHeight;
                row.LayoutElement.flexibleHeight = 0f;
            }

            ApplyScrollSensitivity();
        }

        private void ApplyTextTemplateSettings(TextMeshProUGUI text)
        {
            if ((UnityEngine.Object)text == null)
            {
                return;
            }

            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.enableAutoSizing = false;
            text.fontSize = baseFontSize;
            text.richText = true;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if ((UnityEngine.Object)chatTextTemplateText != null)
            {
                text.font = chatTextTemplateText.font;
                text.fontSharedMaterial = chatTextTemplateText.fontSharedMaterial;
                text.spriteAsset = chatTextTemplateText.spriteAsset;
                text.color = chatTextTemplateText.color;
                text.characterSpacing = chatTextTemplateText.characterSpacing;
                text.wordSpacing = chatTextTemplateText.wordSpacing;
                text.lineSpacing = chatTextTemplateText.lineSpacing;
                text.paragraphSpacing = chatTextTemplateText.paragraphSpacing;
            }
        }
        private void SplitDisplayText(string displayText, out string primaryText, out string secondaryText)
        {
            primaryText = string.Empty;
            secondaryText = string.Empty;

            if (string.IsNullOrEmpty(displayText))
            {
                return;
            }

            int newlineIndex = displayText.IndexOf('\n');

            if (newlineIndex < 0)
            {
                primaryText = displayText;
                return;
            }

            primaryText = displayText.Substring(0, newlineIndex);
            secondaryText = displayText.Substring(newlineIndex + 1);
        }

        private void ConfigureRowTextLayout(ChatSuggestionRow row, bool hasSecondaryText)
        {
            if (row == null)
            {
                return;
            }

            if ((UnityEngine.Object)row.PrimaryText != null)
            {
                RectTransform primaryTransform = row.PrimaryText.GetComponent<RectTransform>();

                if ((UnityEngine.Object)primaryTransform != null)
                {
                    if (hasSecondaryText)
                    {
                        primaryTransform.anchorMin = new Vector2(0.02f, 0.50f);
                        primaryTransform.anchorMax = new Vector2(0.98f, 0.98f);
                    }
                    else
                    {
                        primaryTransform.anchorMin = new Vector2(0.02f, 0.02f);
                        primaryTransform.anchorMax = new Vector2(0.98f, 0.98f);
                    }

                    primaryTransform.pivot = new Vector2(0f, 0.5f);
                    primaryTransform.anchoredPosition = Vector2.zero;
                    primaryTransform.offsetMin = new Vector2(10f, 0f);
                    primaryTransform.offsetMax = new Vector2(-10f, 0f);
                }

                row.PrimaryText.alignment = TextAlignmentOptions.Left;
            }

            if ((UnityEngine.Object)row.SecondaryText != null)
            {
                row.SecondaryText.gameObject.SetActive(hasSecondaryText);

                RectTransform secondaryTransform = row.SecondaryText.GetComponent<RectTransform>();

                if ((UnityEngine.Object)secondaryTransform != null)
                {
                    secondaryTransform.anchorMin = new Vector2(0.02f, 0.02f);
                    secondaryTransform.anchorMax = new Vector2(0.98f, 0.50f);
                    secondaryTransform.pivot = new Vector2(0f, 0.5f);
                    secondaryTransform.anchoredPosition = Vector2.zero;
                    secondaryTransform.offsetMin = new Vector2(10f, 0f);
                    secondaryTransform.offsetMax = new Vector2(-10f, 0f);
                }
            }
        }

        private void SetRowSelected(ChatSuggestionRow row, bool selected)
        {
            if (row == null || (UnityEngine.Object)row.Image == null)
            {
                return;
            }

            ChatSuggestionStyle style = currentStyle ?? CreateFallbackStyle();

            if (selected)
            {
                row.Image.color = style.SelectedRowColor;
            }
            else
            {
                row.Image.color = style.RowColor;
            }
        }

        private void ScrollToRow(RectTransform target)
        {
            if ((UnityEngine.Object)scrollRect == null ||
                (UnityEngine.Object)scrollRect.content == null ||
                (UnityEngine.Object)scrollRect.viewport == null ||
                (UnityEngine.Object)target == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            RectTransform content = scrollRect.content;
            float contentHeight = content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            float scrollableHeight = contentHeight - viewportHeight;

            if (scrollableHeight <= 0f)
            {
                scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            Vector2 localPoint = content.InverseTransformPoint(target.position);
            float targetTop = Mathf.Abs(localPoint.y);
            float normalized = Mathf.Clamp01((targetTop - viewportHeight * 0.5f) / scrollableHeight);
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }

        private ChatSuggestionStyle CreateFallbackStyle()
        {
            return new ChatSuggestionStyle(
                new Color(0.15f, 0.15f, 0.15f, 0.95f),
                new Color(1f, 1f, 1f, 0.05f),
                new Color(0.3f, 0.5f, 0.8f, 0.3f),
                new Color(0f, 0f, 0f, 0.25f),
                new Vector2(0f, 0.05f),
                new Vector2(1f, 0.75f),
                1f);
        }

        public class ChatSuggestionRow
        {
            public int Index;
            public GameObject Root;
            public RectTransform Transform;
            public LayoutElement LayoutElement;
            public Image Image;
            public TextMeshProUGUI PrimaryText;
            public TextMeshProUGUI SecondaryText;

            public ChatSuggestionRow(
                int index,
                GameObject root,
                RectTransform transform,
                LayoutElement layoutElement,
                Image image,
                TextMeshProUGUI primaryText,
                TextMeshProUGUI secondaryText)
            {
                Index = index;
                Root = root;
                Transform = transform;
                LayoutElement = layoutElement;
                Image = image;
                PrimaryText = primaryText;
                SecondaryText = secondaryText;
            }
        }

        /*public class ChatSuggestionRow
        {
            public int Index;
            public GameObject Root;
            public RectTransform Transform;
            public LayoutElement LayoutElement;
            public Image Image;
            public TextMeshProUGUI Text;

            public ChatSuggestionRow(
                int index,
                GameObject root,
                RectTransform transform,
                LayoutElement layoutElement,
                Image image,
                TextMeshProUGUI text)
            {
                Index = index;
                Root = root;
                Transform = transform;
                LayoutElement = layoutElement;
                Image = image;
                Text = text;
            }
        }*/
    }
}
