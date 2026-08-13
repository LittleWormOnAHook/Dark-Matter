using System.Collections.Generic;
using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Skills tab — category hex trees wired to <see cref="PlayerSkillAllocator"/>.
    /// </summary>
    public class SkillsPanelUI : MonoBehaviour
    {
        private static readonly Color RankFilledBlue = new Color(0.55f, 0.78f, 0.95f, 1f);
        private static readonly Color RankEmptyGray = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.85f);
        private static readonly Color PathLocked = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.35f);
        private static readonly Color PathReady = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SoftBeigeGray, 0.55f);
        private static readonly Color PathOwned = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.72f);

        private const float HexSize = 92f;
        private const float ColSpacing = 118f;
        private const float RowSpacing = 104f;
        private const float TreePadX = 56f;
        private const float TreePadY = 48f;
        private const int TreeColumns = 3;

        private Transform embeddedParent;
        private GameObject panelRoot;
        private TextMeshProUGUI summaryLabel;
        private TextMeshProUGUI categoryTitleLabel;
        private Transform categoryTabsParent;
        private RectTransform treeContent;
        private RectTransform nodesLayer;
        private RectTransform pathsLayer;
        private RectTransform popupRoot;
        private TextMeshProUGUI popupTitle;
        private TextMeshProUGUI popupBody;
        private PlayerProgressionManager progression;
        private ShiftUiTheme theme;
        private SkillTreeCategory activeCategory = SkillTreeCategory.Player;
        private readonly Dictionary<string, HexNodeView> nodeViews = new Dictionary<string, HexNodeView>();
        private readonly List<Button> categoryButtons = new List<Button>();
        private string hoveredSkillId;
        private string selectedSkillId;

        private sealed class HexNodeView
        {
            public SkillDefinition Skill;
            public RectTransform Root;
            public Image Fill;
            public Image Outline;
            public Image Glow;
            public TextMeshProUGUI Label;
            public Image[] RankDots;
            public Vector2 AnchoredPos;
        }

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            progression = PlayerProgressionManager.EnsureExists();
            theme = ShiftUiTheme.Current;
            EnsureBuilt(parent);

            if (progression != null)
                progression.OnXpChanged += Refresh;

            Refresh();
        }

        public void Unembed()
        {
            if (progression != null)
                progression.OnXpChanged -= Refresh;

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            summaryLabel = null;
            categoryTitleLabel = null;
            categoryTabsParent = null;
            treeContent = null;
            nodesLayer = null;
            pathsLayer = null;
            popupRoot = null;
            popupTitle = null;
            popupBody = null;
            embeddedParent = null;
            nodeViews.Clear();
            categoryButtons.Clear();
            hoveredSkillId = null;
            selectedSkillId = null;
        }

        public void Refresh()
        {
            if (panelRoot == null)
                return;

            theme = ShiftUiTheme.Current;
            progression ??= PlayerProgressionManager.EnsureExists();
            int points = progression != null ? progression.UnspentSkillPoints : 0;
            int level = progression != null ? progression.Level : 1;
            summaryLabel.text =
                $"Level {JournalPanelLayout.FormatGoldValue(level.ToString())}  ·  " +
                $"Skill Points {JournalPanelLayout.FormatGoldValue(points.ToString())}";
            summaryLabel.color = points > 0
                ? SurvivalPioneerUiPalette.HighlightText
                : SurvivalPioneerUiPalette.BodyText;

            categoryTitleLabel.text = SkillDefinition.GetCategoryDisplayName(activeCategory);
            RefreshCategoryTabs();
            RebuildTree();
            RefreshPopupContent();
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("SkillsPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            JournalPanelLayout.StretchFill(panelRoot.GetComponent<RectTransform>());

            Image panelBg = panelRoot.GetComponent<Image>();
            JournalPanelLayout.StylePanelBackground(panelBg, theme);
            panelBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.96f);

            VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            JournalPanelLayout.ApplyRootVerticalLayout(rootLayout);
            rootLayout.padding = new RectOffset(10, 10, 8, 8);
            rootLayout.spacing = 6f;

            TextMeshProUGUI header = CreateLabel(panelRoot.transform, "SkillsHeader", JournalPanelLayout.HeaderFontSize);
            JournalPanelLayout.ApplyHeaderStyle(header);
            header.text = "Skill Trees";

            summaryLabel = CreateLabel(panelRoot.transform, "Summary", JournalPanelLayout.SummaryFontSize);
            summaryLabel.color = SurvivalPioneerUiPalette.BodyText;

            GameObject tabsObject = new GameObject("CategoryTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tabsObject.transform.SetParent(panelRoot.transform, false);
            LayoutElement tabsLayout = tabsObject.GetComponent<LayoutElement>();
            tabsLayout.minHeight = 34f;
            tabsLayout.preferredHeight = 36f;
            tabsLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup tabsGroup = tabsObject.GetComponent<HorizontalLayoutGroup>();
            tabsGroup.spacing = 6f;
            tabsGroup.childAlignment = TextAnchor.MiddleLeft;
            tabsGroup.childControlWidth = true;
            tabsGroup.childControlHeight = true;
            tabsGroup.childForceExpandWidth = false;
            tabsGroup.childForceExpandHeight = true;
            categoryTabsParent = tabsObject.transform;
            BuildCategoryTabs();

            categoryTitleLabel = CreateLabel(panelRoot.transform, "CategoryTitle", JournalPanelLayout.BodyFontSize);
            categoryTitleLabel.color = SurvivalPioneerUiPalette.Gold;
            categoryTitleLabel.fontStyle = FontStyles.Bold;

            GameObject bodySplit = new GameObject("BodySplit", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bodySplit.transform.SetParent(panelRoot.transform, false);
            bodySplit.GetComponent<LayoutElement>().flexibleHeight = 1f;
            HorizontalLayoutGroup split = bodySplit.GetComponent<HorizontalLayoutGroup>();
            split.spacing = 10f;
            split.childControlWidth = true;
            split.childControlHeight = true;
            split.childForceExpandWidth = true;
            split.childForceExpandHeight = true;

            GameObject treeHost = new GameObject("TreeHost", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            treeHost.transform.SetParent(bodySplit.transform, false);
            LayoutElement treeHostLayout = treeHost.GetComponent<LayoutElement>();
            treeHostLayout.flexibleWidth = 1.55f;
            treeHostLayout.minWidth = 320f;
            Image treeHostBg = treeHost.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(treeHostBg);
            treeHostBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.55f);

            GameObject scrollObject = new GameObject("TreeScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObject.transform.SetParent(treeHost.transform, false);
            RectTransform scrollRectTf = scrollObject.GetComponent<RectTransform>();
            scrollRectTf.anchorMin = Vector2.zero;
            scrollRectTf.anchorMax = Vector2.one;
            scrollRectTf.offsetMin = new Vector2(4f, 4f);
            scrollRectTf.offsetMax = new Vector2(-4f, -4f);
            Image scrollBg = scrollObject.GetComponent<Image>();
            scrollBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.72f);
            scrollBg.raycastTarget = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            JournalPanelLayout.StretchFill(viewportRect, 0f);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.15f);
            viewportImage.raycastTarget = true;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            treeContent = content.GetComponent<RectTransform>();
            treeContent.anchorMin = new Vector2(0f, 1f);
            treeContent.anchorMax = new Vector2(0f, 1f);
            treeContent.pivot = new Vector2(0f, 1f);
            treeContent.anchoredPosition = Vector2.zero;

            GameObject paths = new GameObject("Paths", typeof(RectTransform));
            paths.transform.SetParent(treeContent, false);
            pathsLayer = paths.GetComponent<RectTransform>();
            StretchLocal(pathsLayer);

            GameObject nodes = new GameObject("Nodes", typeof(RectTransform));
            nodes.transform.SetParent(treeContent, false);
            nodesLayer = nodes.GetComponent<RectTransform>();
            StretchLocal(nodesLayer);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = treeContent;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            GameObject detailColumn = new GameObject("DetailColumn", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            detailColumn.transform.SetParent(bodySplit.transform, false);
            LayoutElement detailLayout = detailColumn.GetComponent<LayoutElement>();
            detailLayout.flexibleWidth = 0.85f;
            detailLayout.minWidth = 220f;
            detailLayout.preferredWidth = 260f;
            Image detailBg = detailColumn.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(detailBg);
            detailBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
            VerticalLayoutGroup detailVertical = detailColumn.GetComponent<VerticalLayoutGroup>();
            detailVertical.padding = new RectOffset(12, 12, 12, 12);
            detailVertical.spacing = 8f;
            detailVertical.childControlWidth = true;
            detailVertical.childControlHeight = true;
            detailVertical.childForceExpandWidth = true;
            detailVertical.childForceExpandHeight = false;

            TextMeshProUGUI detailHeader = CreateLabel(detailColumn.transform, "DetailHeader", JournalPanelLayout.HeaderFontSize);
            JournalPanelLayout.ApplyHeaderStyle(detailHeader);
            detailHeader.text = "Skill Info";

            popupRoot = detailColumn.GetComponent<RectTransform>();
            popupTitle = CreateLabel(detailColumn.transform, "PopupTitle", JournalPanelLayout.SummaryFontSize);
            popupTitle.color = SurvivalPioneerUiPalette.Gold;
            popupTitle.fontStyle = FontStyles.Bold;
            popupTitle.text = "Hover a hex";

            popupBody = CreateLabel(detailColumn.transform, "PopupBody", JournalPanelLayout.BodyFontSize);
            popupBody.color = SurvivalPioneerUiPalette.BodyText;
            popupBody.textWrappingMode = TextWrappingModes.Normal;
            popupBody.overflowMode = TextOverflowModes.Overflow;
            LayoutElement bodyLayout = popupBody.GetComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.minHeight = 120f;
            popupBody.text = "Select a category, then hover or click a skill hexagon to view details and spend skill points.";
        }

        private void BuildCategoryTabs()
        {
            categoryButtons.Clear();
            for (int i = categoryTabsParent.childCount - 1; i >= 0; i--)
                Destroy(categoryTabsParent.GetChild(i).gameObject);

            foreach (SkillTreeCategory category in System.Enum.GetValues(typeof(SkillTreeCategory)))
            {
                SkillTreeCategory captured = category;
                GameObject tab = new GameObject(category.ToString(), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                tab.transform.SetParent(categoryTabsParent, false);
                LayoutElement layout = tab.GetComponent<LayoutElement>();
                layout.minWidth = 78f;
                layout.preferredWidth = 88f;
                layout.flexibleWidth = 0f;

                Image image = tab.GetComponent<Image>();
                MenuUiBuilder.ApplyUiSprite(image);
                Button button = tab.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    activeCategory = captured;
                    hoveredSkillId = null;
                    selectedSkillId = null;
                    Refresh();
                });

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(tab.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                ApplyThemeFont(label, semiBold: true);
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = JournalPanelLayout.CaptionFontSize;
                label.color = SurvivalPioneerUiPalette.WarmOffWhite;
                label.text = SkillDefinition.GetCategoryDisplayName(category);

                categoryButtons.Add(button);
            }
        }

        private void RefreshCategoryTabs()
        {
            if (categoryTabsParent == null)
                return;

            for (int i = 0; i < categoryTabsParent.childCount; i++)
            {
                Transform child = categoryTabsParent.GetChild(i);
                bool active = child.name == activeCategory.ToString();
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.color = active
                        ? SurvivalPioneerUiPalette.WithAlpha(GetCategoryAccent(activeCategory), 0.92f)
                        : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.75f);
                }

                TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = active ? SurvivalPioneerUiPalette.WarmOffWhite : SurvivalPioneerUiPalette.MutedText;
            }
        }

        private void RebuildTree()
        {
            nodeViews.Clear();
            for (int i = pathsLayer.childCount - 1; i >= 0; i--)
                Destroy(pathsLayer.GetChild(i).gameObject);
            for (int i = nodesLayer.childCount - 1; i >= 0; i--)
                Destroy(nodesLayer.GetChild(i).gameObject);

            List<SkillDefinition> skills = SkillRegistry.GetSkillsByCategory(activeCategory);
            if (skills.Count == 0)
            {
                treeContent.sizeDelta = new Vector2(420f, 280f);
                GameObject empty = new GameObject("Empty", typeof(RectTransform), typeof(TextMeshProUGUI));
                empty.transform.SetParent(nodesLayer, false);
                RectTransform emptyRect = empty.GetComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.sizeDelta = new Vector2(360f, 80f);
                TextMeshProUGUI emptyLabel = empty.GetComponent<TextMeshProUGUI>();
                ApplyThemeFont(emptyLabel);
                emptyLabel.alignment = TextAlignmentOptions.Center;
                emptyLabel.fontSize = JournalPanelLayout.BodyFontSize;
                emptyLabel.color = SurvivalPioneerUiPalette.MutedText;
                emptyLabel.text = "No skills in this tree yet.\nTools → Dark Matter Genesis → Content → Create Starter Skills";
                return;
            }

            int maxRow = 0;
            int maxCol = TreeColumns - 1;
            for (int i = 0; i < skills.Count; i++)
            {
                maxRow = Mathf.Max(maxRow, skills[i].treeRow);
                maxCol = Mathf.Max(maxCol, skills[i].treeColumn);
            }

            float width = TreePadX * 2f + (maxCol + 1) * ColSpacing;
            float height = TreePadY * 2f + (maxRow + 1) * RowSpacing + HexSize * 0.35f;
            treeContent.sizeDelta = new Vector2(Mathf.Max(width, 420f), Mathf.Max(height, 280f));

            Dictionary<string, HexNodeView> byId = new Dictionary<string, HexNodeView>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                HexNodeView view = CreateHexNode(skill);
                byId[skill.ResolvedId] = view;
                nodeViews[skill.ResolvedId] = view;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill.prerequisiteSkillIds == null)
                    continue;

                for (int p = 0; p < skill.prerequisiteSkillIds.Length; p++)
                {
                    string prereqId = skill.prerequisiteSkillIds[p];
                    if (string.IsNullOrEmpty(prereqId))
                        continue;
                    if (!byId.TryGetValue(prereqId, out HexNodeView from) || !byId.TryGetValue(skill.ResolvedId, out HexNodeView to))
                        continue;

                    CreatePath(from, to);
                }
            }
        }

        private HexNodeView CreateHexNode(SkillDefinition skill)
        {
            int rank = progression != null ? progression.GetSkillRank(skill.ResolvedId) : 0;
            int maxRank = skill.ClampedMaxRank;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, progression, out _);
            bool isMaxed = rank >= maxRank;
            bool unlocked = rank > 0 || canAllocate || ArePrerequisitesMet(skill);
            Color accent = GetCategoryAccent(activeCategory);

            Vector2 pos = GridToAnchored(skill.treeColumn, skill.treeRow);
            GameObject node = new GameObject(skill.ResolvedId, typeof(RectTransform), typeof(Image), typeof(Button));
            node.transform.SetParent(nodesLayer, false);
            RectTransform root = node.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(HexSize, HexSize);
            root.anchoredPosition = pos;

            Image hit = node.GetComponent<Image>();
            hit.sprite = DmHexUiSprites.FilledHex;
            hit.color = Color.clear;
            hit.raycastTarget = true;

            GameObject glowObject = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowObject.transform.SetParent(node.transform, false);
            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-10f, -10f);
            glowRect.offsetMax = new Vector2(10f, 10f);
            Image glow = glowObject.GetComponent<Image>();
            glow.sprite = DmHexUiSprites.SoftGlow;
            glow.raycastTarget = false;
            glow.color = Color.clear;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(node.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = DmHexUiSprites.FilledHex;
            fill.raycastTarget = false;
            fill.color = unlocked
                ? SurvivalPioneerUiPalette.WithAlpha(rank > 0 ? accent : SurvivalPioneerUiPalette.CharcoalGray, rank > 0 ? 0.88f : 0.82f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.55f);

            GameObject outlineObject = new GameObject("Outline", typeof(RectTransform), typeof(Image));
            outlineObject.transform.SetParent(node.transform, false);
            RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            Image outline = outlineObject.GetComponent<Image>();
            outline.sprite = DmHexUiSprites.OutlineHex;
            outline.raycastTarget = false;
            outline.color = isMaxed
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.95f)
                : canAllocate
                    ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.95f)
                    : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.9f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(node.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.12f, 0.28f);
            labelRect.anchorMax = new Vector2(0.88f, 0.78f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: true);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 11f;
            label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maxVisibleLines = 2;
            label.text = skill.displayName;
            label.raycastTarget = false;

            Image[] dots = new Image[SkillDefinition.DisplayMaxRank];
            GameObject dotsRow = new GameObject("RankDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotsRow.transform.SetParent(node.transform, false);
            RectTransform dotsRect = dotsRow.GetComponent<RectTransform>();
            dotsRect.anchorMin = new Vector2(0.5f, 0f);
            dotsRect.anchorMax = new Vector2(0.5f, 0f);
            dotsRect.pivot = new Vector2(0.5f, 0f);
            dotsRect.anchoredPosition = new Vector2(0f, 10f);
            dotsRect.sizeDelta = new Vector2(HexSize * 0.62f, 10f);
            HorizontalLayoutGroup dotsLayout = dotsRow.GetComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 3f;
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.childControlWidth = true;
            dotsLayout.childControlHeight = true;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;

            for (int d = 0; d < SkillDefinition.DisplayMaxRank; d++)
            {
                bool usedSlot = d < maxRank;
                GameObject dotObject = new GameObject($"Dot{d}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                dotObject.transform.SetParent(dotsRow.transform, false);
                LayoutElement dotLayout = dotObject.GetComponent<LayoutElement>();
                dotLayout.preferredWidth = 8f;
                dotLayout.preferredHeight = 8f;
                Image dot = dotObject.GetComponent<Image>();
                dot.sprite = DmHexUiSprites.RankDot;
                dot.raycastTarget = false;
                if (!usedSlot)
                    dot.color = Color.clear;
                else
                    dot.color = d < rank ? RankFilledBlue : RankEmptyGray;
                dots[d] = dot;
            }

            Button button = node.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.targetGraphic = hit;

            SkillDefinition captured = skill;
            button.onClick.AddListener(() => OnHexClicked(captured));

            EventTrigger trigger = node.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => OnHexHover(captured, true));
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => OnHexHover(captured, false));

            HexNodeView view = new HexNodeView
            {
                Skill = skill,
                Root = root,
                Fill = fill,
                Outline = outline,
                Glow = glow,
                Label = label,
                RankDots = dots,
                AnchoredPos = pos
            };

            ApplyHoverVisual(view, false);
            return view;
        }

        private void CreatePath(HexNodeView from, HexNodeView to)
        {
            GameObject line = new GameObject($"Path_{from.Skill.ResolvedId}_to_{to.Skill.ResolvedId}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(pathsLayer, false);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 a = from.AnchoredPos;
            Vector2 b = to.AnchoredPos;
            Vector2 delta = b - a;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rect.sizeDelta = new Vector2(Mathf.Max(8f, length - HexSize * 0.55f), 4f);
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image image = line.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.raycastTarget = false;

            int fromRank = progression != null ? progression.GetSkillRank(from.Skill.ResolvedId) : 0;
            int toRank = progression != null ? progression.GetSkillRank(to.Skill.ResolvedId) : 0;
            if (toRank > 0 && fromRank > 0)
                image.color = PathOwned;
            else if (fromRank > 0)
                image.color = PathReady;
            else
                image.color = PathLocked;
        }

        private void OnHexHover(SkillDefinition skill, bool entering)
        {
            if (skill == null)
                return;

            if (entering)
            {
                hoveredSkillId = skill.ResolvedId;
                selectedSkillId = skill.ResolvedId;
            }
            else if (hoveredSkillId == skill.ResolvedId)
            {
                hoveredSkillId = null;
            }

            foreach (KeyValuePair<string, HexNodeView> pair in nodeViews)
                ApplyHoverVisual(pair.Value, pair.Key == hoveredSkillId || pair.Key == selectedSkillId);

            RefreshPopupContent();
        }

        private void OnHexClicked(SkillDefinition skill)
        {
            if (skill == null)
                return;

            selectedSkillId = skill.ResolvedId;
            hoveredSkillId = skill.ResolvedId;

            if (PlayerSkillAllocator.TryAllocate(skill, out string error))
            {
                Refresh();
                return;
            }

            RefreshPopupContent();
            if (!string.IsNullOrEmpty(error) && error != "Max rank reached.")
                Debug.Log($"[Skills] {skill.displayName}: {error}");

            foreach (KeyValuePair<string, HexNodeView> pair in nodeViews)
                ApplyHoverVisual(pair.Value, pair.Key == selectedSkillId);
        }

        private void ApplyHoverVisual(HexNodeView view, bool highlighted)
        {
            if (view?.Glow == null || view.Skill == null)
                return;

            int rank = progression != null ? progression.GetSkillRank(view.Skill.ResolvedId) : 0;
            int maxRank = view.Skill.ClampedMaxRank;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(view.Skill, progression, out _);
            bool isMaxed = rank >= maxRank;

            if (highlighted)
            {
                bool selected = selectedSkillId == view.Skill.ResolvedId;
                view.Glow.color = SurvivalPioneerUiPalette.WithAlpha(
                    selected ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.RichFuchsia,
                    selected ? 0.5f : 0.55f);
                if (view.Outline != null)
                {
                    view.Outline.color = SurvivalPioneerUiPalette.WithAlpha(
                        selected ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.RichFuchsia,
                        1f);
                }
            }
            else
            {
                view.Glow.color = Color.clear;
                if (view.Outline != null)
                {
                    view.Outline.color = isMaxed
                        ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.95f)
                        : canAllocate
                            ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.95f)
                            : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.9f);
                }
            }
        }

        private void RefreshPopupContent()
        {
            if (popupTitle == null || popupBody == null)
                return;

            string id = !string.IsNullOrEmpty(hoveredSkillId) ? hoveredSkillId : selectedSkillId;
            if (string.IsNullOrEmpty(id) || !nodeViews.TryGetValue(id, out HexNodeView view) || view.Skill == null)
            {
                popupTitle.text = "Hover a hex";
                popupBody.text = "Hover a skill for details. Click to spend skill points and raise its rank.";
                return;
            }

            SkillDefinition skill = view.Skill;
            int rank = progression != null ? progression.GetSkillRank(skill.ResolvedId) : 0;
            int maxRank = skill.ClampedMaxRank;
            int nextCost = rank < maxRank ? skill.GetCostForNextRank(rank) : 0;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, progression, out string error);
            bool isMaxed = rank >= maxRank;

            popupTitle.text = skill.displayName;
            string prereqLine = FormatPrerequisites(skill);
            string status = isMaxed
                ? "MAX RANK"
                : canAllocate
                    ? $"Click to upgrade · Cost {nextCost} SP"
                    : error ?? "Locked";

            popupBody.text =
                $"{skill.description}\n\n" +
                $"Rank {rank}/{maxRank}\n" +
                $"Requires player level {skill.requiredPlayerLevel}\n" +
                (string.IsNullOrEmpty(prereqLine) ? string.Empty : $"{prereqLine}\n") +
                $"\n{status}";
            popupBody.color = canAllocate || isMaxed
                ? SurvivalPioneerUiPalette.BodyText
                : SurvivalPioneerUiPalette.MutedText;
        }

        private string FormatPrerequisites(SkillDefinition skill)
        {
            if (skill.prerequisiteSkillIds == null || skill.prerequisiteSkillIds.Length == 0)
                return string.Empty;

            List<string> names = new List<string>();
            for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
            {
                SkillDefinition prereq = SkillRegistry.Resolve(skill.prerequisiteSkillIds[i]);
                if (prereq != null)
                    names.Add(prereq.displayName);
            }

            return names.Count == 0 ? string.Empty : $"Requires: {string.Join(", ", names)}";
        }

        private bool ArePrerequisitesMet(SkillDefinition skill)
        {
            if (skill.prerequisiteSkillIds == null || skill.prerequisiteSkillIds.Length == 0)
                return true;
            if (progression == null)
                return false;

            for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
            {
                string id = skill.prerequisiteSkillIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (progression.GetSkillRank(id) <= 0)
                    return false;
            }

            return true;
        }

        private static Vector2 GridToAnchored(int column, int row)
        {
            float x = TreePadX + column * ColSpacing + HexSize * 0.5f;
            float y = -(TreePadY + row * RowSpacing + HexSize * 0.5f);
            // Offset odd columns slightly for a hex stagger.
            if ((column & 1) == 1)
                y -= RowSpacing * 0.18f;
            return new Vector2(x, y);
        }

        private static Color GetCategoryAccent(SkillTreeCategory category) =>
            category switch
            {
                SkillTreeCategory.Melee => SurvivalPioneerUiPalette.DeepMagenta,
                SkillTreeCategory.Pistols => SurvivalPioneerUiPalette.RichFuchsia,
                SkillTreeCategory.Rifles => SurvivalPioneerUiPalette.Gold,
                SkillTreeCategory.Survival => SurvivalPioneerUiPalette.SoftBeigeGray,
                SkillTreeCategory.Player => SurvivalPioneerUiPalette.CharcoalGray,
                _ => SurvivalPioneerUiPalette.SlateGray
            };

        private static void StretchLocal(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, float size)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: true);
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.GetComponent<LayoutElement>().preferredHeight = size + 6f;
            return label;
        }

        private void ApplyThemeFont(TextMeshProUGUI label, bool semiBold = false, bool bold = false)
        {
            if (theme != null)
                theme.ApplyFont(label, semiBold: semiBold, bold: bold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
        }
    }
}
