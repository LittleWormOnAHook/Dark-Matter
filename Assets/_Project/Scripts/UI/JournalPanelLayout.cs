using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Shared density / typography tokens for Journal fullscreen tabs.
    /// Inspired by Conceptual UI art (journal hub, echo roster): thin chrome, tight sectioning,
    /// Warm Off-White body / Soft Beige-Gray secondary / Gold key numbers.
    /// Section headers use Warm Off-White — never Deep Magenta or Rich Fuchsia for header text.
    /// </summary>
    public static class JournalPanelLayout
    {
        public const float PanelInset = 8f;
        public const float PanelPadding = 10f;
        public const float SectionSpacing = 8f;
        public const float ListSpacing = 4f;
        public const float RowPaddingH = 8f;
        public const float RowPaddingV = 4f;
        public const float RowMinHeight = 34f;
        public const float CardMinHeight = 48f;
        public const float ActionButtonWidth = 72f;
        public const float ActionButtonHeight = 26f;
        /// <summary>Square skill allocate control — equal width/height.</summary>
        public const float SkillAllocateButtonSize = 32f;
        public const float ScrollInset = 3f;
        public const float SectionDividerHeight = 2f;

        public const float HeaderFontSize = 16f;
        public const float SummaryFontSize = 15f;
        public const float BodyFontSize = 14f;
        public const float SecondaryFontSize = 13f;
        public const float ButtonFontSize = 12f;
        public const float CaptionFontSize = 12f;

        public static RectOffset PanelPaddingRect =>
            new RectOffset((int)PanelPadding, (int)PanelPadding, (int)PanelPadding, (int)PanelPadding);

        public static RectOffset RowPaddingRect =>
            new RectOffset((int)RowPaddingH, (int)RowPaddingH, (int)RowPaddingV, (int)RowPaddingV);

        public static RectOffset ContentPaddingRect =>
            new RectOffset(3, 3, 3, 3);

        public static void StretchFill(RectTransform rect, float inset = PanelInset)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static void ApplyRootVerticalLayout(VerticalLayoutGroup layout)
        {
            if (layout == null)
                return;

            layout.spacing = SectionSpacing;
            layout.padding = PanelPaddingRect;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
        }

        public static void ApplyRootHorizontalLayout(HorizontalLayoutGroup layout)
        {
            if (layout == null)
                return;

            layout.spacing = SectionSpacing;
            layout.padding = PanelPaddingRect;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.UpperLeft;
        }

        public static void ApplyListVerticalLayout(VerticalLayoutGroup layout)
        {
            if (layout == null)
                return;

            layout.spacing = ListSpacing;
            layout.padding = ContentPaddingRect;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
        }

        public static void StylePanelBackground(Image image, ShiftUiTheme theme = null)
        {
            if (image == null)
                return;

            if (theme != null)
                theme.ApplyPanelImage(image, large: true, alphaMultiplier: 0.98f);
            else
            {
                MenuUiBuilder.ApplyUiSprite(image);
                image.color = SurvivalPioneerUiPalette.PanelBackground;
            }
        }

        public static void StyleScrollBackground(Image image)
        {
            if (image == null)
                return;

            MenuUiBuilder.ApplyUiSprite(image);
            image.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
        }

        public static void StyleDenseCard(Image image, bool accentTrim = true)
        {
            if (image == null)
                return;

            MenuUiBuilder.ApplyUiSprite(image);
            image.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f);
            if (accentTrim && image.gameObject != null)
                SurvivalPioneerUiPalette.ApplyFuchsiaTrim(image.gameObject, new Vector2(1f, -1f));
        }

        /// <summary>Section headers — Warm Off-White, never magenta/fuchsia.</summary>
        public static void ApplyHeaderStyle(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = HeaderFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public static void ApplyBodyStyle(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = BodyFontSize;
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.alignment = TextAlignmentOptions.TopLeft;
        }

        public static void ApplyMutedStyle(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.fontSize = SecondaryFontSize;
            label.color = SurvivalPioneerUiPalette.MutedText;
            label.alignment = TextAlignmentOptions.TopLeft;
        }

        public static string FormatGoldValue(string value) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.Gold)}>{value}</color>";

        /// <summary>Title/name emphasis — Warm Off-White (not magenta).</summary>
        public static string FormatAccentTitle(string value) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.WarmOffWhite)}>{value}</color>";

        /// <summary>Selected link / interactive highlight only — Rich Fuchsia.</summary>
        public static string FormatLinkHighlight(string value) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>{value}</color>";

        public static string FormatMuted(string value) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.MutedText)}>{value}</color>";

        /// <summary>Secondary helper copy on dark panels — Gold for contrast (not Soft Beige / magenta).</summary>
        public static string FormatHelper(string value) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.Gold)}>{value}</color>";

        /// <summary>Thin horizontal rule between journal/craft sections.</summary>
        public static GameObject CreateSectionDivider(Transform parent)
        {
            if (parent == null)
                return null;

            GameObject divider = new GameObject("SectionDivider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            divider.transform.SetParent(parent, false);

            Image image = divider.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.85f);
            image.raycastTarget = false;

            LayoutElement layout = divider.GetComponent<LayoutElement>();
            layout.minHeight = SectionDividerHeight;
            layout.preferredHeight = SectionDividerHeight;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
            return divider;
        }

        /// <summary>Designed empty-state card for sparse journal/craft panels — non-overlapping hierarchy.</summary>
        public static void CreateEmptyStateCard(Transform parent, ShiftUiTheme theme, string title, string body, string tip = null)
        {
            if (parent == null)
                return;

            // Drop any prior empty card immediately so refresh cycles cannot stack overlapping copies.
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == "EmptyState")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            GameObject card = new GameObject("EmptyState", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(parent, false);

            Image bg = card.GetComponent<Image>();
            StyleDenseCard(bg, accentTrim: true);

            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.minHeight = 120f;
            cardLayout.flexibleWidth = 1f;
            cardLayout.flexibleHeight = 0f;

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = card.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI titleLabel = CreateInlineLabel(card.transform, theme, title, HeaderFontSize, FontStyles.Bold);
            titleLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;

            if (!string.IsNullOrWhiteSpace(body))
            {
                TextMeshProUGUI bodyLabel = CreateInlineLabel(card.transform, theme, body, BodyFontSize, FontStyles.Normal);
                bodyLabel.color = SurvivalPioneerUiPalette.Gold;
                bodyLabel.textWrappingMode = TextWrappingModes.Normal;
            }

            if (!string.IsNullOrWhiteSpace(tip))
            {
                TextMeshProUGUI tipLabel = CreateInlineLabel(card.transform, theme, tip, SecondaryFontSize, FontStyles.Italic);
                tipLabel.color = SurvivalPioneerUiPalette.Gold;
                tipLabel.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static TextMeshProUGUI CreateInlineLabel(Transform parent, ShiftUiTheme theme, string text, float size, FontStyles style)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(label, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;

            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minHeight = size + 4f;
            layout.preferredHeight = size + 10f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
            return label;
        }
    }
}
