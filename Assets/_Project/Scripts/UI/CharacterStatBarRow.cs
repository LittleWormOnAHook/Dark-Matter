using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Thin stat row: small icon, name/value header, and a slim progress bar (reference panel style).
    /// </summary>
    internal sealed class CharacterStatBarRow
    {
        private const float IconSize = 20f;
        private const float IconGap = 12f;
        private const float BarHeight = 7f;
        private const float RowSpacing = 14f;

        private readonly RectTransform fillRect;
        private readonly TextMeshProUGUI nameLabel;
        private readonly TextMeshProUGUI valueLabel;

        public static float PreferredRowHeight => Mathf.Max(IconSize, BarHeight + 18f) + RowSpacing * 0.35f;

        public CharacterStatBarRow(Transform parent, string iconGlyph, string statName, Color fillColor)
        {
            GameObject rowObject = new GameObject($"Stat_{statName}", typeof(RectTransform), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.minHeight = PreferredRowHeight;
            rowLayout.preferredHeight = PreferredRowHeight;
            rowLayout.flexibleHeight = 0f;
            rowLayout.flexibleWidth = 1f;
            rowLayout.minWidth = 0f;

            HorizontalLayoutGroup rowGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = IconGap;
            rowGroup.childAlignment = TextAnchor.UpperLeft;
            rowGroup.childControlWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;
            rowGroup.padding = new RectOffset(0, 0, 2, 2);

            CreateIcon(rowObject.transform, iconGlyph);
            CreateContent(rowObject.transform, statName, fillColor, out fillRect, out nameLabel, out valueLabel);
        }

        public void SetValues(float current, float max, string valueOverride = null)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            valueLabel.text = !string.IsNullOrEmpty(valueOverride)
                ? valueOverride
                : max > 0f
                    ? FormatStatValue(current)
                    : FormatStatValue(current);
        }

        public void SetUnavailable(string statName)
        {
            fillRect.anchorMax = new Vector2(0f, 1f);
            nameLabel.text = statName;
            valueLabel.text = "—";
        }

        private static string FormatStatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.#");
        }

        private static void CreateIcon(Transform parent, string glyph)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.minWidth = IconSize;
            iconLayout.preferredWidth = IconSize;
            iconLayout.minHeight = IconSize;
            iconLayout.preferredHeight = IconSize;

            Image iconBg = iconObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(iconBg);
            iconBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(iconObject, new Vector2(1f, -1f));

            GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform));
            glyphObject.transform.SetParent(iconObject.transform, false);
            RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(glyphRect);

            TextMeshProUGUI glyphLabel = glyphObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(glyphLabel);
            glyphLabel.text = glyph;
            glyphLabel.fontSize = 11f;
            glyphLabel.alignment = TextAlignmentOptions.Center;
            glyphLabel.color = SurvivalPioneerUiPalette.RichFuchsia;
            glyphLabel.raycastTarget = false;
        }

        private static void CreateContent(
            Transform parent,
            string statName,
            Color fillColor,
            out RectTransform fillRectOut,
            out TextMeshProUGUI nameLabelOut,
            out TextMeshProUGUI valueLabelOut)
        {
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            content.transform.SetParent(parent, false);

            LayoutElement contentLayout = content.GetComponent<LayoutElement>();
            contentLayout.flexibleWidth = 1f;
            contentLayout.minHeight = BarHeight + 18f;

            VerticalLayoutGroup contentGroup = content.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 4f;
            contentGroup.childControlWidth = true;
            contentGroup.childControlHeight = true;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childForceExpandHeight = false;

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(content.transform, false);
            LayoutElement headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 16f;
            headerLayout.minHeight = 16f;

            HorizontalLayoutGroup headerGroup = header.GetComponent<HorizontalLayoutGroup>();
            headerGroup.childAlignment = TextAnchor.MiddleLeft;
            headerGroup.childControlWidth = true;
            headerGroup.childForceExpandWidth = true;
            headerGroup.spacing = 8f;

            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObject.transform.SetParent(header.transform, false);
            LayoutElement nameLayout = nameObject.GetComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.minWidth = 120f;
            nameLabelOut = nameObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(nameLabelOut);
            nameLabelOut.fontSize = 13f;
            nameLabelOut.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabelOut.color = SurvivalPioneerUiPalette.BodyText;
            nameLabelOut.text = statName;
            nameLabelOut.overflowMode = TextOverflowModes.Overflow;
            nameLabelOut.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabelOut.raycastTarget = false;

            GameObject valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            valueObject.transform.SetParent(header.transform, false);
            LayoutElement valueLayout = valueObject.GetComponent<LayoutElement>();
            valueLayout.minWidth = 48f;
            valueLayout.preferredWidth = 64f;
            valueLabelOut = valueObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(valueLabelOut);
            valueLabelOut.fontSize = 13f;
            valueLabelOut.alignment = TextAlignmentOptions.MidlineRight;
            valueLabelOut.color = SurvivalPioneerUiPalette.BodyText;
            valueLabelOut.raycastTarget = false;

            GameObject track = new GameObject("Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            track.transform.SetParent(content.transform, false);
            LayoutElement trackLayout = track.GetComponent<LayoutElement>();
            trackLayout.preferredHeight = BarHeight;
            trackLayout.minHeight = BarHeight;

            Image trackImage = track.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(trackImage);
            trackImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.38f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);
            fillRectOut = fillObject.GetComponent<RectTransform>();
            fillRectOut.anchorMin = Vector2.zero;
            fillRectOut.anchorMax = Vector2.one;
            fillRectOut.offsetMin = Vector2.zero;
            fillRectOut.offsetMax = Vector2.zero;
            fillRectOut.pivot = new Vector2(0f, 0.5f);

            Image fillImage = fillObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
        }
    }
}
