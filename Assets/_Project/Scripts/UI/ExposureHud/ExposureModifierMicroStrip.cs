using System.Collections.Generic;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Compact glyph strip for expedition pioneer modifier chips.
    /// </summary>
    public class ExposureModifierMicroStrip : MonoBehaviour
    {
        private const int MaxVisibleTicks = 3;

        private readonly List<TextMeshProUGUI> glyphLabels = new List<TextMeshProUGUI>(MaxVisibleTicks);
        private RectTransform contentRect;

        public void SetTicks(IReadOnlyList<ExposureModifierTick> buffs, IReadOnlyList<ExposureModifierTick> debuffs)
        {
            EnsureBuilt();

            int buffCount = buffs?.Count ?? 0;
            int debuffCount = debuffs?.Count ?? 0;
            int total = buffCount + debuffCount;
            int visible = Mathf.Min(total, MaxVisibleTicks);
            EnsureGlyphCount(visible);

            int glyphIndex = 0;
            for (int i = 0; i < buffCount && glyphIndex < visible; i++, glyphIndex++)
                ApplyGlyph(glyphLabels[glyphIndex], buffs[i], true);

            for (int i = 0; i < debuffCount && glyphIndex < visible; i++, glyphIndex++)
                ApplyGlyph(glyphLabels[glyphIndex], debuffs[i], false);

            for (int i = glyphIndex; i < glyphLabels.Count; i++)
                glyphLabels[i].gameObject.SetActive(false);

            gameObject.SetActive(total > 0);
        }

        private static void ApplyGlyph(TextMeshProUGUI label, in ExposureModifierTick tick, bool isBuff)
        {
            if (label == null)
                return;

            label.gameObject.SetActive(true);
            label.text = string.IsNullOrWhiteSpace(tick.IconGlyph)
                ? isBuff ? "+" : "−"
                : tick.IconGlyph;
            label.color = isBuff
                ? SurvivalPioneerUiPalette.PositiveGreen
                : SurvivalPioneerUiPalette.DeepMagenta;
        }

        private void EnsureBuilt()
        {
            if (contentRect != null)
                return;

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            root.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(54f), HudLayoutMetrics.Scaled(14f));

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentObject.transform.SetParent(transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(contentRect);

            HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        private void EnsureGlyphCount(int count)
        {
            while (glyphLabels.Count < count)
            {
                GameObject glyphObject = new GameObject($"Glyph_{glyphLabels.Count}", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                glyphObject.transform.SetParent(contentRect, false);

                LayoutElement layout = glyphObject.GetComponent<LayoutElement>();
                layout.minWidth = HudLayoutMetrics.Scaled(14f);
                layout.preferredWidth = HudLayoutMetrics.Scaled(14f);
                layout.minHeight = HudLayoutMetrics.Scaled(14f);
                layout.preferredHeight = HudLayoutMetrics.Scaled(14f);

                TextMeshProUGUI label = glyphObject.GetComponent<TextMeshProUGUI>();
                TmpUiHelper.ApplyDefaultFont(label);
                label.fontSize = HudLayoutMetrics.ScaledInt(11f);
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                glyphLabels.Add(label);
            }
        }
    }
}
