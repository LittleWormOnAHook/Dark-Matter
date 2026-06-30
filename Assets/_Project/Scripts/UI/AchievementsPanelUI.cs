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
        private Transform embeddedParent;
        private GameObject panelRoot;
        private Transform listParent;
        private TextMeshProUGUI headerLabel;
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
            headerLabel = null;
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

            GameObject headerRow = new GameObject("Header", typeof(RectTransform));
            headerRow.transform.SetParent(panelRoot.transform, false);
            RectTransform headerRect = headerRow.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 36f);
            headerRect.anchoredPosition = Vector2.zero;

            headerLabel = headerRow.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(headerLabel);
            theme?.ApplyFont(headerLabel, semiBold: true);
            headerLabel.fontSize = 20f;
            headerLabel.color = SurvivalPioneerUiPalette.BodyText;
            headerLabel.text = "Achievements";
            headerLabel.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject tabRow = new GameObject("CategoryTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabRow.transform.SetParent(panelRoot.transform, false);
            RectTransform tabRect = tabRow.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(1f, 1f);
            tabRect.pivot = new Vector2(0.5f, 1f);
            tabRect.anchoredPosition = new Vector2(0f, -40f);
            tabRect.sizeDelta = new Vector2(0f, 34f);

            HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = false;
            tabLayout.childForceExpandWidth = false;
            categoryTabParent = tabRow.transform;

            GameObject scrollHost = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollHost.transform.SetParent(panelRoot.transform, false);
            RectTransform scrollRect = scrollHost.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8f, 8f);
            scrollRect.offsetMax = new Vector2(-8f, -82f);

            Image scrollBg = scrollHost.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.5f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup listLayout = content.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.padding = new RectOffset(8, 8, 8, 8);
            listLayout.childControlWidth = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

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
                CreateInfoRow("Achievement system unavailable.");
                return;
            }

            List<AchievementEntry> entries = BuildSortedEntries();
            if (entries.Count == 0)
            {
                CreateInfoRow("No achievements configured. Run Tools → Survival Pioneer → Content → Create Starter Achievements.");
                return;
            }

            for (int i = 0; i < entries.Count; i++)
                CreateAchievementRow(entries[i].Definition, entries[i].Progress);
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

        private void CreateAchievementRow(AchievementDefinition definition, AchievementProgress progress)
        {
            bool unlocked = progress.unlocked;
            bool hiddenLocked = definition.hidden && !unlocked;

            GameObject row = new GameObject(definition.ResolvedId, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup));
            row.transform.SetParent(listParent, false);

            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = unlocked
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.85f);

            Outline outline = row.GetComponent<Outline>();
            outline.effectColor = unlocked
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.55f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            string title = hiddenLocked ? "???" : definition.title;
            string description = hiddenLocked ? "Hidden achievement" : definition.description;
            CreateRowLabel(row.transform, title, 17f, unlocked ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.BodyText, bold: true);
            CreateRowLabel(row.transform, description, 14f, SurvivalPioneerUiPalette.MutedText, bold: false);

            if (!unlocked && definition.targetCount > 1)
            {
                float fill = definition.targetCount > 0
                    ? Mathf.Clamp01((float)progress.currentCount / definition.targetCount)
                    : 0f;
                CreateProgressBar(row.transform, fill, $"{progress.currentCount} / {definition.targetCount}");
            }
            else if (unlocked)
            {
                CreateRowLabel(row.transform, "Unlocked", 13f, SurvivalPioneerUiPalette.PositiveGreen, bold: true);
            }

            int xpPreview = definition.xpReward;
            if (definition.hidden)
                xpPreview = Mathf.RoundToInt(xpPreview * 1.5f);

            if (xpPreview > 0)
                CreateRowLabel(row.transform, $"+{xpPreview} XP", 13f, SurvivalPioneerUiPalette.Gold, bold: false);
        }

        private void CreateProgressBar(Transform parent, float fill, string labelText)
        {
            GameObject barHost = new GameObject("Progress", typeof(RectTransform));
            barHost.transform.SetParent(parent, false);
            LayoutElement barLayout = barHost.AddComponent<LayoutElement>();
            barLayout.minHeight = 22f;
            barLayout.preferredHeight = 22f;

            GameObject track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(barHost.transform, false);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            Image trackImage = track.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(trackImage);
            trackImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.9f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(track.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(fill, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObj.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = SurvivalPioneerUiPalette.RichFuchsia;

            CreateRowLabel(barHost.transform, labelText, 12f, SurvivalPioneerUiPalette.MutedText, bold: false);
        }

        private void CreateRowLabel(Transform parent, string text, float size, Color color, bool bold)
        {
            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(tmp);
            theme?.ApplyFont(tmp, semiBold: bold);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        private void CreateInfoRow(string message)
        {
            CreateRowLabel(listParent, message, 15f, SurvivalPioneerUiPalette.MutedText, bold: false);
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
