using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Single buff/debuff modifier chip for exposure HUD.
    /// </summary>
    public class ExposureModifierTickView : MonoBehaviour
    {
        private TextMeshProUGUI glyphLabel;
        private TextMeshProUGUI bodyLabel;
        private Image background;

        public void Bind(in ExposureModifierTick tick)
        {
            EnsureBuilt();

            if (glyphLabel != null)
            {
                glyphLabel.text = string.IsNullOrWhiteSpace(tick.IconGlyph)
                    ? tick.Kind == ExposureModifierKind.Buff ? "+" : "−"
                    : tick.IconGlyph;
            }

            if (bodyLabel != null)
                bodyLabel.text = tick.Label;

            if (background != null)
            {
                Color baseColor = tick.Kind == ExposureModifierKind.Buff
                    ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.PositiveGreen, 0.28f)
                    : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DeepMagenta, 0.32f);
                background.color = Color.Lerp(baseColor, tick.Tint, 0.35f);
            }
        }

        private void EnsureBuilt()
        {
            if (background != null)
                return;

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            root.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(148f), HudLayoutMetrics.Scaled(22f));

            GameObject bgObject = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bgObject.transform.SetParent(transform, false);
            MenuUiBuilder.StretchRectToFill(bgObject.GetComponent<RectTransform>());

            background = bgObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.88f);

            HorizontalLayoutGroup layout = bgObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 8, 2, 2);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            LayoutElement layoutElement = bgObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = HudLayoutMetrics.Scaled(22f);
            layoutElement.preferredHeight = HudLayoutMetrics.Scaled(22f);

            GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            glyphObject.transform.SetParent(bgObject.transform, false);
            LayoutElement glyphLayout = glyphObject.GetComponent<LayoutElement>();
            glyphLayout.minWidth = HudLayoutMetrics.Scaled(14f);
            glyphLayout.preferredWidth = HudLayoutMetrics.Scaled(14f);

            glyphLabel = glyphObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(glyphLabel);
            glyphLabel.fontSize = 11f;
            glyphLabel.fontStyle = FontStyles.Bold;
            glyphLabel.alignment = TextAlignmentOptions.Center;
            glyphLabel.color = DarkMatterGenesisUiPalette.HighlightText;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            bodyObject.transform.SetParent(bgObject.transform, false);
            LayoutElement bodyLayout = bodyObject.GetComponent<LayoutElement>();
            bodyLayout.flexibleWidth = 1f;

            bodyLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyLabel);
            bodyLabel.fontSize = 10f;
            bodyLabel.alignment = TextAlignmentOptions.MidlineLeft;
            bodyLabel.color = DarkMatterGenesisUiPalette.BodyText;
            bodyLabel.overflowMode = TextOverflowModes.Ellipsis;
            bodyLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
