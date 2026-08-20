using System;
using System.Collections.Generic;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Persistent top tab bar for the fullscreen journal navigator (GDD Phase C shell).
    /// Tabs sit along the top edge; hover/select enlarges a tab and slides neighbors via layout.
    /// </summary>
    public class JournalTabRail : MonoBehaviour
    {
        /// <summary>Unscaled height of the top tab bar (room for 20% hover/select enlarge).</summary>
        public const float RailHeight = 84f;

        /// <summary>Deprecated left-rail width; maps to <see cref="RailHeight"/> for existing callers.</summary>
        public const float RailWidth = RailHeight;

        public const float TabMinHeight = 48f;
        public const float TabMinWidth = 88f;
        public const float BaseFontSize = 14f * 1.25f;
        public const float HighlightScale = 1.2f;
        private const float ScaleLerpSpeed = 14f;
        private const float HoverTickCooldownSeconds = 0.04f;

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

        private sealed class TabEntry
        {
            public JournalWindowId WindowId;
            public Image Background;
            public TextMeshProUGUI Label;
            public LayoutElement Layout;
            public float BaseWidth;
            public float BaseHeight;
            public float CurrentScale = 1f;
            public float TargetScale = 1f;
            public bool IsHovered;
            public bool IsSelected;
        }

        private static readonly TabDef[] Tabs =
        {
            new TabDef("Journal", JournalWindowId.JournalQuest),
            new TabDef("Inventory", JournalWindowId.Inventory),
            new TabDef("Map", JournalWindowId.Map),
            new TabDef("Pet", JournalWindowId.Pet),
            new TabDef("Companions", JournalWindowId.Pioneers),
            new TabDef("Character", JournalWindowId.Character),
            new TabDef("Blueprints", JournalWindowId.Recipes),
            new TabDef("Skills", JournalWindowId.Skills),
            new TabDef("Echoes", JournalWindowId.Echoes),
            new TabDef("Achievements", JournalWindowId.Achievements)
        };

        private static readonly Color ActiveTabColor = DarkMatterGenesisUiPalette.ActiveTabBackground;
        private static readonly Color HoverTabColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.22f);
        private static readonly Color InactiveTabColor = DarkMatterGenesisUiPalette.InactiveTabBackground;
        /// <summary>Hover / selected / highlighted tab labels — Warm Off-White.</summary>
        private static readonly Color ActiveLabelColor = DarkMatterGenesisUiPalette.WarmOffWhite;
        private static readonly Color HoverLabelColor = DarkMatterGenesisUiPalette.WarmOffWhite;
        /// <summary>Idle (unused) tab labels — Gold.</summary>
        private static readonly Color InactiveLabelColor = DarkMatterGenesisUiPalette.Gold;

        private readonly List<TabEntry> tabs = new List<TabEntry>(Tabs.Length);
        private readonly Dictionary<JournalWindowId, TabEntry> tabsById = new Dictionary<JournalWindowId, TabEntry>();
        private bool dirtyVisuals;
        private JournalWindowId? lastActiveWindowId;
        private float lastHoverTickUnscaledTime = -1f;

        public void Build(Transform parent, float uiScale, Action<JournalWindowId> onTabClicked)
        {
            transform.SetParent(parent, false);

            RectTransform railRect = GetComponent<RectTransform>();
            railRect.anchorMin = new Vector2(0f, 1f);
            railRect.anchorMax = new Vector2(1f, 1f);
            railRect.pivot = new Vector2(0.5f, 1f);
            railRect.sizeDelta = new Vector2(0f, Sc(RailHeight, uiScale));
            railRect.anchoredPosition = Vector2.zero;

            Image railBg = GetComponent<Image>();
            if (railBg == null)
                railBg = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(railBg);
            railBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.96f);
            railBg.raycastTarget = true;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(gameObject, new Vector2(2f, -2f));

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabRow.transform.SetParent(transform, false);
            RectTransform tabRowRect = tabRow.GetComponent<RectTransform>();
            tabRowRect.anchorMin = Vector2.zero;
            tabRowRect.anchorMax = Vector2.one;
            tabRowRect.offsetMin = Vector2.zero;
            tabRowRect.offsetMax = Vector2.zero;
            tabRowRect.pivot = new Vector2(0.5f, 0.5f);

            HorizontalLayoutGroup rowLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(Sc(8, uiScale), Sc(8, uiScale), Sc(6, uiScale), Sc(6, uiScale));
            rowLayout.spacing = Sc(4f, uiScale);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            for (int i = 0; i < Tabs.Length; i++)
            {
                TabDef tab = Tabs[i];
                CreateTabButton(tabRow.transform, tab, theme, uiScale, onTabClicked);
            }

            gameObject.SetActive(false);
        }

        public void SetActiveTab(JournalWindowId? windowId)
        {
            bool selectionChanged = windowId != lastActiveWindowId;
            lastActiveWindowId = windowId;

            for (int i = 0; i < tabs.Count; i++)
            {
                TabEntry entry = tabs[i];
                entry.IsSelected = windowId.HasValue && entry.WindowId == windowId.Value;
                RefreshTabTarget(entry);
            }

            dirtyVisuals = true;
            ApplyTabVisuals(force: true);

            // Keyboard / gamepad tab switches (and clicks) land here — play once on change.
            // Hover uses OnTabPointerEnter separately so scrubbing the rail still ticks.
            if (selectionChanged && windowId.HasValue && gameObject.activeInHierarchy)
                PlayTabTick();
        }

        private void Update()
        {
            if (tabs.Count == 0)
                return;

            bool anyAnimating = false;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < tabs.Count; i++)
            {
                TabEntry entry = tabs[i];
                if (Mathf.Abs(entry.CurrentScale - entry.TargetScale) < 0.001f)
                {
                    if (entry.CurrentScale != entry.TargetScale)
                    {
                        entry.CurrentScale = entry.TargetScale;
                        ApplyScaleToLayout(entry);
                        anyAnimating = true;
                    }

                    continue;
                }

                entry.CurrentScale = Mathf.Lerp(entry.CurrentScale, entry.TargetScale, 1f - Mathf.Exp(-ScaleLerpSpeed * dt));
                if (Mathf.Abs(entry.CurrentScale - entry.TargetScale) < 0.002f)
                    entry.CurrentScale = entry.TargetScale;

                ApplyScaleToLayout(entry);
                anyAnimating = true;
            }

            if (anyAnimating || dirtyVisuals)
            {
                ApplyTabVisuals(force: dirtyVisuals);
                dirtyVisuals = false;
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

            float baseWidth = Sc(TabMinWidth, uiScale);
            float baseHeight = Sc(TabMinHeight, uiScale);

            LayoutElement layout = tabObject.AddComponent<LayoutElement>();
            layout.minWidth = baseWidth;
            layout.preferredWidth = baseWidth;
            layout.flexibleWidth = 1f;
            layout.minHeight = baseHeight;
            layout.preferredHeight = baseHeight;

            Image bg = tabObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = InactiveTabColor;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(tabObject);

            Button button = tabObject.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            // Click uses the shared button SFX; hover/focus ticks come from the pointer bridge.
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
            label.fontSize = Sc(BaseFontSize, uiScale);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = InactiveLabelColor;
            label.raycastTarget = false;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(Sc(4f, uiScale), Sc(2f, uiScale));
            labelRect.offsetMax = new Vector2(Sc(-4f, uiScale), Sc(-2f, uiScale));

            TabEntry entry = new TabEntry
            {
                WindowId = tab.WindowId,
                Background = bg,
                Label = label,
                Layout = layout,
                BaseWidth = baseWidth,
                BaseHeight = baseHeight
            };
            tabs.Add(entry);
            tabsById[tab.WindowId] = entry;

            JournalTabPointerBridge bridge = tabObject.AddComponent<JournalTabPointerBridge>();
            bridge.Initialize(this, tab.WindowId);
        }

        private void OnTabPointerEnter(JournalWindowId windowId)
        {
            if (!tabsById.TryGetValue(windowId, out TabEntry entry))
                return;

            bool wasHovered = entry.IsHovered;
            entry.IsHovered = true;
            RefreshTabTarget(entry);
            dirtyVisuals = true;

            // Tick when scrubbing onto a new tab (not when already hovered).
            if (!wasHovered)
                PlayTabTick();
        }

        private void OnTabPointerExit(JournalWindowId windowId)
        {
            if (!tabsById.TryGetValue(windowId, out TabEntry entry))
                return;

            entry.IsHovered = false;
            RefreshTabTarget(entry);
            dirtyVisuals = true;
        }

        private void OnTabSelected(JournalWindowId windowId)
        {
            // Gamepad / keyboard EventSystem focus — highlight + tick without requiring a click.
            OnTabPointerEnter(windowId);
        }

        private void OnTabDeselected(JournalWindowId windowId)
        {
            OnTabPointerExit(windowId);
        }

        private void PlayTabTick()
        {
            float now = Time.unscaledTime;
            if (now - lastHoverTickUnscaledTime < HoverTickCooldownSeconds)
                return;

            lastHoverTickUnscaledTime = now;
            GameAudioManager.Instance?.PlayUiHoverTick();
        }

        private static void RefreshTabTarget(TabEntry entry)
        {
            entry.TargetScale = entry.IsSelected || entry.IsHovered ? HighlightScale : 1f;
        }

        private static void ApplyScaleToLayout(TabEntry entry)
        {
            if (entry.Layout == null)
                return;

            float scale = entry.CurrentScale;
            entry.Layout.preferredWidth = entry.BaseWidth * scale;
            entry.Layout.minWidth = entry.BaseWidth * scale;
            entry.Layout.preferredHeight = entry.BaseHeight * scale;
            entry.Layout.minHeight = entry.BaseHeight * scale;
            // Grow flexible weight so neighbors compress and slide over.
            entry.Layout.flexibleWidth = scale;
        }

        private void ApplyTabVisuals(bool force)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                TabEntry entry = tabs[i];
                if (entry.Background == null || entry.Label == null)
                    continue;

                if (entry.IsSelected)
                {
                    entry.Background.color = ActiveTabColor;
                    entry.Label.color = ActiveLabelColor;
                }
                else if (entry.IsHovered)
                {
                    entry.Background.color = HoverTabColor;
                    entry.Label.color = HoverLabelColor;
                }
                else
                {
                    entry.Background.color = InactiveTabColor;
                    entry.Label.color = InactiveLabelColor;
                }

                if (force)
                    ApplyScaleToLayout(entry);
            }
        }

        private static float Sc(float value, float uiScale) => value * uiScale;

        private static int Sc(int value, float uiScale) => Mathf.RoundToInt(value * uiScale);

        /// <summary>
        /// Lightweight EventSystem bridge so tab hover does not require Button color transitions.
        /// Also handles gamepad / keyboard focus via <see cref="ISelectHandler"/>.
        /// </summary>
        private sealed class JournalTabPointerBridge : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            ISelectHandler,
            IDeselectHandler
        {
            private JournalTabRail owner;
            private JournalWindowId windowId;

            public void Initialize(JournalTabRail rail, JournalWindowId id)
            {
                owner = rail;
                windowId = id;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                owner?.OnTabPointerEnter(windowId);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                owner?.OnTabPointerExit(windowId);
            }

            public void OnSelect(BaseEventData eventData)
            {
                owner?.OnTabSelected(windowId);
            }

            public void OnDeselect(BaseEventData eventData)
            {
                owner?.OnTabDeselected(windowId);
            }
        }
    }
}
