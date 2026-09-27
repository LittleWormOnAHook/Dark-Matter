using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class StubFullscreenWindow : FullscreenUiWindow
    {
        private string stubHeading;
        private string stubBody;
        private string[] featureBullets;

        public void Configure(string heading, string body, params string[] bullets)
        {
            stubHeading = heading;
            stubBody = body;
            featureBullets = bullets;
        }

        protected override void OnBuild()
        {
            if (contentArea == null)
                return;

            VerticalLayoutGroup layout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = JournalPanelLayout.SectionSpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            GameObject iconBlock = new GameObject("IconBlock", typeof(RectTransform), typeof(Image));
            iconBlock.transform.SetParent(contentArea, false);
            Image iconImage = iconBlock.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(iconImage);
            iconImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.55f);
            LayoutElement iconLayout = iconBlock.AddComponent<LayoutElement>();
            iconLayout.minHeight = 64f;
            iconLayout.preferredHeight = 64f;
            iconLayout.minWidth = 64f;
            iconLayout.preferredWidth = 64f;

            TextMeshProUGUI heading = CreateStubText(contentArea, stubHeading ?? "Coming Soon", 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, theme);
            heading.color = DarkMatterGenesisUiPalette.BodyText;

            TextMeshProUGUI body = CreateStubText(
                contentArea,
                stubBody ?? string.Empty,
                JournalPanelLayout.BodyFontSize + 2f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                theme);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.color = theme != null ? theme.secondaryTextColor : DarkMatterGenesisUiPalette.BodyText;

            if (featureBullets != null && featureBullets.Length > 0)
            {
                GameObject bulletList = new GameObject("FeatureBullets", typeof(RectTransform));
                bulletList.transform.SetParent(contentArea, false);
                VerticalLayoutGroup bulletLayout = bulletList.AddComponent<VerticalLayoutGroup>();
                bulletLayout.spacing = JournalPanelLayout.ListSpacing;
                bulletLayout.childAlignment = TextAnchor.UpperLeft;
                bulletLayout.childControlWidth = true;
                bulletLayout.childForceExpandWidth = true;
                bulletLayout.childForceExpandHeight = false;
                LayoutElement bulletListLayout = bulletList.AddComponent<LayoutElement>();
                bulletListLayout.flexibleHeight = 1f;

                for (int i = 0; i < featureBullets.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(featureBullets[i]))
                        continue;

                    TextMeshProUGUI bullet = CreateStubText(
                        bulletList.transform,
                        $"\u2022 {featureBullets[i]}",
                        JournalPanelLayout.BodyFontSize,
                        FontStyles.Normal,
                        TextAlignmentOptions.TopLeft,
                        theme);
                    bullet.color = theme != null ? theme.secondaryTextColor : DarkMatterGenesisUiPalette.MutedText;
                }
            }

            TextMeshProUGUI footer = CreateStubText(
                contentArea,
                "Coming in a future update",
                JournalPanelLayout.SecondaryFontSize,
                FontStyles.Italic,
                TextAlignmentOptions.TopLeft,
                theme);
            footer.color = DarkMatterGenesisUiPalette.MutedText;
        }

        private static TextMeshProUGUI CreateStubText(
            Transform parent,
            string value,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment,
            ShiftUiTheme theme)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            if (theme != null)
                theme.ApplyFont(text, semiBold: style == FontStyles.Bold);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }
    }
}
