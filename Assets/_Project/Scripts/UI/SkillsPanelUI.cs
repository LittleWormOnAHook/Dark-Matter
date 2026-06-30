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
            summaryLabel.text = $"Player Level {level}  |  Unspent Skill Points: {points}";
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
                CreateInfoLabel("No skills configured. Run Tools → Survival Pioneer → Content → Create Starter Skills.");
        }

        private void CreateSkillRow(SkillDefinition skill)
        {
            GameObject row = new GameObject(skill.ResolvedId, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image), typeof(Outline));
            row.transform.SetParent(listParent, false);

            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f);

            Outline outline = row.GetComponent<Outline>();
            outline.effectColor = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            int rank = progression != null ? progression.GetSkillRank(skill.ResolvedId) : 0;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, progression, out string error);
            bool isMaxed = rank >= skill.maxRank;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: true);
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>{skill.displayName}</color>  " +
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.Gold)}>(Rank {rank}/{skill.maxRank}, Lv {skill.requiredPlayerLevel}+)</color>\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.MutedText)}>{skill.description}</color>" +
                (!canAllocate && !isMaxed && !string.IsNullOrEmpty(error)
                    ? $"\n<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.SoftBeigeGray)}>{error}</color>"
                    : string.Empty);

            GameObject buttonObject = new GameObject("SpendButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(row.transform, false);
            LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = 96f;
            buttonLayout.minHeight = 34f;

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
            buttonLabel.fontSize = 13f;
            buttonLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            buttonLabel.text = isMaxed ? "Max" : $"+1 ({skill.costPerRank}pt)";

            SkillDefinition captured = skill;
            button.onClick.AddListener(() =>
            {
                if (PlayerSkillAllocator.TryAllocate(captured, out string allocateError))
                    Refresh();
                else if (!string.IsNullOrEmpty(allocateError))
                    Debug.Log(allocateError);
            });
        }

        private void CreateInfoLabel(string message)
        {
            GameObject labelObject = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(listParent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label);
            label.fontSize = 14f;
            label.color = SurvivalPioneerUiPalette.MutedText;
            label.text = message;
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("SkillsPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(16f, 16f);
            rootRect.offsetMax = new Vector2(-16f, -16f);

            Image panelBg = panelRoot.GetComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true, alphaMultiplier: 0.98f);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = SurvivalPioneerUiPalette.PanelBackground;
            }

            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            summaryLabel = CreateLabel(panelRoot.transform, 20f);
            summaryLabel.color = SurvivalPioneerUiPalette.BodyText;

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(panelRoot.transform, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 240f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
            viewport.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.35f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
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
