using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class SkillsPanelUI : MonoBehaviour
    {
        private Transform embeddedParent;
        private GameObject panelRoot;
        private TextMeshProUGUI summaryLabel;
        private Transform listParent;
        private PlayerProgressionManager progression;
        private ShiftUiTheme theme;

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
            listParent = null;
            summaryLabel = null;
            embeddedParent = null;
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
                $"Unspent {JournalPanelLayout.FormatGoldValue(points.ToString())}";
            summaryLabel.color = points > 0
                ? SurvivalPioneerUiPalette.HighlightText
                : SurvivalPioneerUiPalette.BodyText;

            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);

            bool anySkill = false;
            foreach (SkillDefinition skill in SkillRegistry.GetAllSkills())
            {
                if (skill == null)
                    continue;

                anySkill = true;
                CreateSkillRow(skill);
            }

            if (!anySkill)
            {
                JournalPanelLayout.CreateEmptyStateCard(
                    listParent,
                    theme,
                    "No skills configured",
                    "Starter skill definitions have not been authored yet.",
                    "Tools → Dark Matter Genesis → Content → Create Starter Skills");
            }
        }

        private void CreateSkillRow(SkillDefinition skill)
        {
            GameObject row = new GameObject(skill.ResolvedId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(listParent, false);

            Image bg = row.GetComponent<Image>();
            JournalPanelLayout.StyleDenseCard(bg);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = JournalPanelLayout.RowMinHeight + 8f;
            rowLayout.preferredHeight = JournalPanelLayout.RowMinHeight + 14f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            int rank = progression != null ? progression.GetSkillRank(skill.ResolvedId) : 0;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, progression, out string error);
            bool isMaxed = rank >= skill.maxRank;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: true);
            label.fontSize = JournalPanelLayout.BodyFontSize;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maxVisibleLines = 2;
            label.text =
                $"{JournalPanelLayout.FormatAccentTitle(skill.displayName)}  " +
                $"{JournalPanelLayout.FormatGoldValue($"Rank {rank}/{skill.maxRank}")}  " +
                $"{JournalPanelLayout.FormatHelper($"Lv {skill.requiredPlayerLevel}+")}\n" +
                $"{JournalPanelLayout.FormatHelper(Truncate(skill.description, 90))}" +
                (!canAllocate && !isMaxed && !string.IsNullOrEmpty(error)
                    ? $"  ·  {JournalPanelLayout.FormatHelper(error)}"
                    : string.Empty);

            LayoutElement textLayout = textObject.GetComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.minHeight = JournalPanelLayout.RowMinHeight;

            float btnSize = JournalPanelLayout.SkillAllocateButtonSize;
            GameObject buttonObject = new GameObject("SpendButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(row.transform, false);
            LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = btnSize;
            buttonLayout.minWidth = btnSize;
            buttonLayout.minHeight = btnSize;
            buttonLayout.preferredHeight = btnSize;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.flexibleHeight = 0f;

            Image buttonImage = buttonObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(buttonImage);
            buttonImage.color = isMaxed
                ? SurvivalPioneerUiPalette.ButtonDisabled
                : canAllocate
                    ? SurvivalPioneerUiPalette.ButtonNormal
                    : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.75f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonImage.color;
            colors.highlightedColor = SurvivalPioneerUiPalette.ButtonHighlighted;
            colors.pressedColor = SurvivalPioneerUiPalette.ButtonPressed;
            colors.disabledColor = SurvivalPioneerUiPalette.ButtonDisabled;
            button.colors = colors;
            button.interactable = canAllocate && !isMaxed;

            GameObject buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            buttonLabelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI buttonLabel = buttonLabelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(buttonLabel, semiBold: true);
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.fontSize = JournalPanelLayout.ButtonFontSize;
            buttonLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            buttonLabel.text = isMaxed ? "MAX" : "+";

            SkillDefinition captured = skill;
            button.onClick.AddListener(() =>
            {
                if (PlayerSkillAllocator.TryAllocate(captured, out string allocateError))
                    Refresh();
                else if (!string.IsNullOrEmpty(allocateError))
                    Debug.Log(allocateError);
            });
        }

        private static string Truncate(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
                return value ?? string.Empty;
            return value.Substring(0, maxChars - 1) + "…";
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

            // Left half = allocation list; right half reserved for future skill detail.
            HorizontalLayoutGroup split = panelRoot.AddComponent<HorizontalLayoutGroup>();
            JournalPanelLayout.ApplyRootHorizontalLayout(split);
            split.spacing = JournalPanelLayout.SectionSpacing;
            split.padding = JournalPanelLayout.PanelPaddingRect;

            GameObject leftColumn = new GameObject("SkillsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            leftColumn.transform.SetParent(panelRoot.transform, false);
            LayoutElement leftLayout = leftColumn.GetComponent<LayoutElement>();
            leftLayout.flexibleWidth = 1f;
            leftLayout.preferredWidth = 0f;
            leftLayout.minWidth = 220f;
            VerticalLayoutGroup leftVertical = leftColumn.GetComponent<VerticalLayoutGroup>();
            leftVertical.spacing = JournalPanelLayout.SectionSpacing;
            leftVertical.padding = new RectOffset(0, 4, 0, 0);
            leftVertical.childControlWidth = true;
            leftVertical.childControlHeight = true;
            leftVertical.childForceExpandWidth = true;
            leftVertical.childForceExpandHeight = false;
            leftVertical.childAlignment = TextAnchor.UpperLeft;

            GameObject rightColumn = new GameObject("DetailColumn", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            rightColumn.transform.SetParent(panelRoot.transform, false);
            LayoutElement rightLayout = rightColumn.GetComponent<LayoutElement>();
            rightLayout.flexibleWidth = 1f;
            rightLayout.preferredWidth = 0f;
            rightLayout.minWidth = 120f;
            Image rightBg = rightColumn.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(rightBg);
            rightBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.35f);
            rightBg.raycastTarget = false;

            TextMeshProUGUI sectionHeader = CreateLabel(leftColumn.transform, JournalPanelLayout.HeaderFontSize);
            JournalPanelLayout.ApplyHeaderStyle(sectionHeader);
            sectionHeader.text = "Allocation";

            summaryLabel = CreateLabel(leftColumn.transform, JournalPanelLayout.SummaryFontSize);
            summaryLabel.color = SurvivalPioneerUiPalette.BodyText;

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(leftColumn.transform, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 200f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            JournalPanelLayout.StyleScrollBackground(scrollBg);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(JournalPanelLayout.ScrollInset, JournalPanelLayout.ScrollInset);
            viewportRect.offsetMax = new Vector2(-JournalPanelLayout.ScrollInset, -JournalPanelLayout.ScrollInset);
            viewport.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.28f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            JournalPanelLayout.ApplyListVerticalLayout(contentLayout);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            listParent = content.transform;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, float size)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
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
