using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Persistent vertical tab rail for the fullscreen journal navigator (GDD Phase C shell).
    /// </summary>
    public class JournalTabRail : MonoBehaviour
    {
        public const float RailWidth = 132f;
        public const float TabMinHeight = 58f;

        private readonly struct TabDef
        {
            public readonly string Label;
            public readonly JournalWindowId WindowId;

            public TabDef(string label, JournalWindowId windowId)
            {
                Label = label;
                WindowId = windowId;
            }
        }

        private static readonly TabDef[] Tabs =
        {
            new TabDef("Journal", JournalWindowId.JournalQuest),
            new TabDef("Inventory", JournalWindowId.Inventory),
            new TabDef("Map", JournalWindowId.Map),
            new TabDef("Pet", JournalWindowId.Pet),
            new TabDef("Pioneers", JournalWindowId.Pioneers),
            new TabDef("Character", JournalWindowId.Character),
            new TabDef("Craft", JournalWindowId.Craft),
            new TabDef("Recipes", JournalWindowId.Recipes),
            new TabDef("Skills", JournalWindowId.Skills),
            new TabDef("Echoes", JournalWindowId.Echoes),
            new TabDef("Achievements", JournalWindowId.Achievements)
        };

        private readonly Dictionary<JournalWindowId, Image> tabBackgrounds = new Dictionary<JournalWindowId, Image>();
        private readonly Dictionary<JournalWindowId, TextMeshProUGUI> tabLabels = new Dictionary<JournalWindowId, TextMeshProUGUI>();

        private static readonly Color ActiveTabColor = SurvivalPioneerUiPalette.ActiveTabBackground;
        private static readonly Color InactiveTabColor = SurvivalPioneerUiPalette.InactiveTabBackground;
        private static readonly Color ActiveLabelColor = SurvivalPioneerUiPalette.Gold;
        private static readonly Color InactiveLabelColor = SurvivalPioneerUiPalette.BodyText;

        public void Build(Transform parent, float uiScale, Action<JournalWindowId> onTabClicked)
        {
            transform.SetParent(parent, false);

            RectTransform railRect = GetComponent<RectTransform>();
            railRect.anchorMin = new Vector2(0f, 0f);
            railRect.anchorMax = new Vector2(0f, 1f);
            railRect.pivot = new Vector2(0f, 0.5f);
            railRect.sizeDelta = new Vector2(Sc(RailWidth, uiScale), 0f);
            railRect.anchoredPosition = Vector2.zero;

            Image railBg = GetComponent<Image>();
            if (railBg == null)
                railBg = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(railBg);
            railBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.96f);
            railBg.raycastTarget = true;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(gameObject, new Vector2(2f, -2f));

            GameObject tabColumn = new GameObject("TabColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            tabColumn.transform.SetParent(transform, false);
            RectTransform tabColumnRect = tabColumn.GetComponent<RectTransform>();
            tabColumnRect.anchorMin = new Vector2(0f, 0.5f);
            tabColumnRect.anchorMax = new Vector2(1f, 1f);
            tabColumnRect.offsetMin = Vector2.zero;
            tabColumnRect.offsetMax = Vector2.zero;
            tabColumnRect.pivot = new Vector2(0.5f, 1f);

            VerticalLayoutGroup columnLayout = tabColumn.GetComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(Sc(6, uiScale), Sc(6, uiScale), Sc(16, uiScale), Sc(8, uiScale));
            columnLayout.spacing = Sc(8f, uiScale);
            columnLayout.childAlignment = TextAnchor.UpperCenter;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            for (int i = 0; i < Tabs.Length; i++)
            {
                TabDef tab = Tabs[i];
                CreateTabButton(tabColumn.transform, tab, theme, uiScale, onTabClicked);
            }

            gameObject.SetActive(false);
        }

        public void SetActiveTab(JournalWindowId? windowId)
        {
            foreach (KeyValuePair<JournalWindowId, Image> pair in tabBackgrounds)
            {
                Image bg = pair.Value;
                if (bg == null)
                    continue;

                bool active = windowId.HasValue && pair.Key == windowId.Value;
                bg.color = active ? ActiveTabColor : InactiveTabColor;
            }

            foreach (KeyValuePair<JournalWindowId, TextMeshProUGUI> pair in tabLabels)
            {
                TextMeshProUGUI label = pair.Value;
                if (label == null)
                    continue;

                bool active = windowId.HasValue && pair.Key == windowId.Value;
                label.color = active ? ActiveLabelColor : InactiveLabelColor;
            }
        }

        private void CreateTabButton(
            Transform parent,
            TabDef tab,
            ShiftUiTheme theme,
            float uiScale,
            Action<JournalWindowId> onTabClicked)
        {
            GameObject tabObject = new GameObject(tab.Label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            tabObject.transform.SetParent(parent, false);

            LayoutElement layout = tabObject.AddComponent<LayoutElement>();
            layout.minHeight = Sc(TabMinHeight, uiScale);
            layout.preferredHeight = Sc(TabMinHeight, uiScale);

            Image bg = tabObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = InactiveTabColor;
            tabBackgrounds[tab.WindowId] = bg;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(tabObject);

            Button button = tabObject.GetComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = InactiveTabColor;
            colors.highlightedColor = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.18f);
            colors.pressedColor = ActiveTabColor;
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            UiSoundHelper.BindButton(button);

            JournalWindowId capturedId = tab.WindowId;
            button.onClick.AddListener(() => onTabClicked?.Invoke(capturedId));

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(tabObject.transform, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            if (theme != null)
                theme.ApplyFont(label, semiBold: true);
            label.text = JournalWindowShortcuts.FormatTabLabel(tab.Label, tab.WindowId);
            label.fontSize = Sc(14f, uiScale);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = InactiveLabelColor;
            label.raycastTarget = false;
            tabLabels[tab.WindowId] = label;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(Sc(6f, uiScale), Sc(4f, uiScale));
            labelRect.offsetMax = new Vector2(Sc(-6f, uiScale), Sc(-4f, uiScale));
        }

        private static float Sc(float value, float uiScale) => value * uiScale;

        private static int Sc(int value, float uiScale) => Mathf.RoundToInt(value * uiScale);
    }
}
