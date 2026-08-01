using System;
using System.Collections.Generic;
using Project.Achievements;
using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class AchievementsPanelUI : MonoBehaviour
    {
        private const float SlotSize = 168f;

        private Transform embeddedParent;
        private GameObject panelRoot;
        private Transform listParent;
        private GridLayoutGroup listGrid;
        private AchievementCategory? selectedCategory;
        private Transform categoryTabParent;
        private AchievementManager achievementManager;
        private ShiftUiTheme theme;

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            achievementManager = AchievementManager.EnsureExists();
            theme = ShiftUiTheme.Current;
            EnsureBuilt(parent);

            if (achievementManager != null)
            {
                achievementManager.OnProgressUpdated += HandleProgressUpdated;
                achievementManager.OnAchievementUnlocked += HandleAchievementUnlocked;
            }

            Refresh();
        }

        public void Unembed()
        {
            if (achievementManager != null)
            {
                achievementManager.OnProgressUpdated -= HandleProgressUpdated;
                achievementManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
            }

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            listParent = null;
            listGrid = null;
            categoryTabParent = null;
            embeddedParent = null;
        }

        public void Refresh()
        {
            if (panelRoot == null)
                return;

            theme = ShiftUiTheme.Current;
            achievementManager ??= AchievementManager.EnsureExists();
            RebuildCategoryTabs();
            RebuildList();
        }

        private void HandleProgressUpdated(AchievementProgress progress, AchievementDefinition definition) => Refresh();

        private void HandleAchievementUnlocked(AchievementProgress progress, AchievementDefinition definition) => Refresh();

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = new GameObject("AchievementsPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(parent, false);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // No internal section header — journal tab rail identifies Achievements; shell title is hidden.
            GameObject tabRow = new GameObject("CategoryTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabRow.transform.SetParent(panelRoot.transform, false);
            RectTransform tabRect = tabRow.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(1f, 1f);
            tabRect.pivot = new Vector2(0.5f, 1f);
            tabRect.anchoredPosition = new Vector2(0f, -6f);
            tabRect.sizeDelta = new Vector2(0f, 34f);

            HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;
            tabLayout.childForceExpandWidth = false;
            categoryTabParent = tabRow.transform;

            GameObject scrollHost = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollHost.transform.SetParent(panelRoot.transform, false);
            RectTransform scrollRect = scrollHost.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8f, 8f);
            scrollRect.offsetMax = new Vector2(-8f, -48f);

            Image scrollBg = scrollHost.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.5f);

            // RectMask2D (not Mask) — Mask stencils off its Image's rendered alpha, so a fully
            // transparent masking graphic ends up with zero alpha everywhere and clips away every
            // child regardless of content. RectMask2D just clips by rect bounds, no alpha dependency,
            // matching the pattern already used by PioneerRosterPanelUI and JournalPanelUI's quest list.
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            listGrid = content.GetComponent<GridLayoutGroup>();
            listGrid.cellSize = new Vector2(SlotSize, SlotSize);
            listGrid.spacing = new Vector2(10f, 10f);
            listGrid.padding = new RectOffset(10, 10, 10, 10);
            listGrid.constraint = GridLayoutGroup.Constraint.Flexible;
            listGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            listGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            listGrid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollHost.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            listParent = content.transform;
        }

        private void RebuildCategoryTabs()
        {
            if (categoryTabParent == null)
                return;

            for (int i = categoryTabParent.childCount - 1; i >= 0; i--)
                Destroy(categoryTabParent.GetChild(i).gameObject);

            CreateCategoryTab("All", null);
            foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
                CreateCategoryTab(category.ToString(), category);
        }

        private void CreateCategoryTab(string label, AchievementCategory? category)
        {
            GameObject tab = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            tab.transform.SetParent(categoryTabParent, false);

            Image bg = tab.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bool active = selectedCategory == category;
            bg.color = active
                ? SurvivalPioneerUiPalette.ActiveTabBackground
                : SurvivalPioneerUiPalette.InactiveTabBackground;

            LayoutElement layout = tab.AddComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredHeight = 28f;

            Button button = tab.GetComponent<Button>();
            button.targetGraphic = bg;
            AchievementCategory? captured = category;
            button.onClick.AddListener(() =>
            {
                selectedCategory = captured;
                Refresh();
            });

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(tab.transform, false);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(tmp);
            theme?.ApplyFont(tmp, semiBold: true);
            tmp.text = label;
            tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = active ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.BodyText;
            tmp.raycastTarget = false;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);
        }

        private void RebuildList()
        {
            if (listParent == null)
                return;

            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);

            achievementManager ??= AchievementManager.EnsureExists();
            if (achievementManager == null)
            {
                ShowInfoState("Achievement system unavailable.");
                return;
            }

            List<AchievementEntry> entries = BuildSortedEntries();
            if (entries.Count == 0)
            {
                ShowInfoState("No achievements configured. Run Tools → Dark Matter Genesis → Content → Create Starter Achievements.");
                return;
            }

            if (listGrid != null)
                listGrid.enabled = true;

            for (int i = 0; i < entries.Count; i++)
                CreateAchievementSlot(entries[i].Definition, entries[i].Progress);
        }

        private List<AchievementEntry> BuildSortedEntries()
        {
            List<AchievementEntry> entries = new List<AchievementEntry>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (AchievementDefinition definition in AchievementRegistry.GetAllAchievements())
            {
                if (definition == null || !seen.Add(definition.ResolvedId))
                    continue;

                if (selectedCategory.HasValue && definition.category != selectedCategory.Value)
                    continue;

                AchievementProgress progress = achievementManager.GetProgress(definition.ResolvedId)
                    ?? new AchievementProgress(definition.ResolvedId);
                entries.Add(new AchievementEntry(definition, progress));
            }

            entries.Sort((a, b) =>
            {
                int order = a.Definition.sortOrder.CompareTo(b.Definition.sortOrder);
                if (order != 0)
                    return order;
                return string.Compare(a.Definition.title, b.Definition.title, StringComparison.Ordinal);
            });

            return entries;
        }

        private void CreateAchievementSlot(AchievementDefinition definition, AchievementProgress progress)
        {
            bool unlocked = progress.unlocked;
            bool hiddenLocked = definition.hidden && !unlocked;

            GameObject slot = new GameObject(definition.ResolvedId, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup));
            slot.transform.SetParent(listParent, false);

            Image bg = slot.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = unlocked
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.85f);

            Outline outline = slot.GetComponent<Outline>();
            outline.effectColor = unlocked
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.65f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            VerticalLayoutGroup layout = slot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            // Icon: uses the assigned sprite when the designer set one; otherwise a category-tinted
            // placeholder swatch so the slot still reads as "this achievement's category" at a glance
            // instead of showing an empty box. Runtime-generated (dynamic/starter) achievements have no
            // asset to assign a sprite on, so they always fall back to the placeholder today.
            // IconHost is a plain full-width row (so the outer VerticalLayoutGroup's childControlWidth
            // doesn't distort it) containing a fixed-size square Image centered within it — that's what
            // keeps the icon itself square while title/description below can still stretch to wrap.
            float iconSize = SlotSize - 78f;
            GameObject iconHost = new GameObject("Icon", typeof(RectTransform));
            iconHost.transform.SetParent(slot.transform, false);
            LayoutElement iconHostLayout = iconHost.AddComponent<LayoutElement>();
            iconHostLayout.minHeight = iconSize;
            iconHostLayout.preferredHeight = iconSize;

            GameObject iconObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(iconHost.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = Vector2.zero;

            Image iconImage = iconObj.GetComponent<Image>();
            if (!hiddenLocked && definition.icon != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = unlocked ? Color.white : SurvivalPioneerUiPalette.WithAlpha(Color.white, 0.5f);
                iconImage.preserveAspect = true;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(iconImage);
                Color placeholder = hiddenLocked ? SurvivalPioneerUiPalette.SlateGray : GetCategoryColor(definition.category);
                iconImage.color = unlocked ? placeholder : SurvivalPioneerUiPalette.WithAlpha(placeholder, 0.45f);
            }

            string title = hiddenLocked ? "???" : definition.title;
            string description = hiddenLocked ? "Hidden achievement" : definition.description;

            CreateSlotLabel(slot.transform, title, 14f, unlocked ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.BodyText,
                bold: true, wrap: true, maxLines: 2, overflow: TextOverflowModes.Ellipsis);
            CreateSlotLabel(slot.transform, description, 11f, SurvivalPioneerUiPalette.MutedText,
                bold: false, wrap: true, maxLines: 3, overflow: TextOverflowModes.Ellipsis);

            string statusLine;
            Color statusColor;
            if (unlocked)
            {
                statusLine = "Unlocked";
                statusColor = SurvivalPioneerUiPalette.PositiveGreen;
            }
            else if (definition.targetCount > 1)
            {
                statusLine = $"{progress.currentCount} / {definition.targetCount}";
                statusColor = SurvivalPioneerUiPalette.MutedText;
            }
            else
            {
                statusLine = string.Empty;
                statusColor = SurvivalPioneerUiPalette.MutedText;
            }

            int xpPreview = definition.xpReward;
            if (definition.hidden)
                xpPreview = Mathf.RoundToInt(xpPreview * 1.5f);

            if (xpPreview > 0)
                statusLine = string.IsNullOrEmpty(statusLine) ? $"+{xpPreview} XP" : $"{statusLine}  ·  +{xpPreview} XP";

            if (!string.IsNullOrEmpty(statusLine))
                CreateSlotLabel(slot.transform, statusLine, 11f, statusColor, bold: true, wrap: false, maxLines: 1, overflow: TextOverflowModes.Overflow);
        }

        private static Color GetCategoryColor(AchievementCategory category)
        {
            return category switch
            {
                AchievementCategory.Exploration => SurvivalPioneerUiPalette.Gold,
                AchievementCategory.Combat => SurvivalPioneerUiPalette.DangerRed,
                AchievementCategory.Crafting => SurvivalPioneerUiPalette.PositiveGreen,
                AchievementCategory.Pets => SurvivalPioneerUiPalette.RichFuchsia,
                AchievementCategory.Pioneers => SurvivalPioneerUiPalette.SlateGray,
                AchievementCategory.General => SurvivalPioneerUiPalette.SoftBeigeGray,
                AchievementCategory.Dynamic => SurvivalPioneerUiPalette.ConnectedGreen,
                _ => SurvivalPioneerUiPalette.SlateGray
            };
        }

        private void CreateSlotLabel(Transform parent, string text, float size, Color color, bool bold, bool wrap, int maxLines, TextOverflowModes overflow)
        {
            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(tmp);
            theme?.ApplyFont(tmp, semiBold: bold);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = overflow;
            if (maxLines > 0)
                tmp.maxVisibleLines = maxLines;
        }

        /// <summary>
        /// Shows a single centered message in place of the grid (system unavailable / nothing registered
        /// yet). The GridLayoutGroup forces every child to a fixed square cell, which would squeeze a
        /// full sentence awkwardly, so this disables the grid and stretches the message across the
        /// content width instead. RebuildList() re-enables the grid before populating real slots.
        /// </summary>
        private void ShowInfoState(string message)
        {
            if (listGrid != null)
                listGrid.enabled = false;

            GameObject labelObj = new GameObject("Info", typeof(RectTransform));
            labelObj.transform.SetParent(listParent, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-24f, 60f);

            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(tmp);
            theme?.ApplyFont(tmp, semiBold: false);
            tmp.text = message;
            tmp.fontSize = 15f;
            tmp.color = SurvivalPioneerUiPalette.MutedText;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        private readonly struct AchievementEntry
        {
            public AchievementDefinition Definition { get; }
            public AchievementProgress Progress { get; }

            public AchievementEntry(AchievementDefinition definition, AchievementProgress progress)
            {
                Definition = definition;
                Progress = progress;
            }
        }
    }
}
