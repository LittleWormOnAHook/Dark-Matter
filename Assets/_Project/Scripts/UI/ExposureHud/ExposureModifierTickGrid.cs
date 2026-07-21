using System.Collections.Generic;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Wrap layout for exposure buff/debuff chips.
    /// </summary>
    public class ExposureModifierTickGrid : MonoBehaviour
    {
        private readonly List<ExposureModifierTickView> activeViews = new List<ExposureModifierTickView>(8);
        private RectTransform contentRect;
        private TextMeshProUGUI emptyLabel;

        public void SetTicks(IReadOnlyList<ExposureModifierTick> ticks, string emptyMessage = "No active modifiers")
        {
            EnsureBuilt();

            int count = ticks != null ? ticks.Count : 0;
            EnsureViewCount(count);

            for (int i = 0; i < count; i++)
                activeViews[i].Bind(ticks[i]);

            for (int i = count; i < activeViews.Count; i++)
                activeViews[i].gameObject.SetActive(false);

            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(count == 0);
                emptyLabel.text = emptyMessage;
            }
        }

        private void EnsureBuilt()
        {
            if (contentRect != null)
                return;

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(contentRect);

            HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject emptyObject = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyObject.transform.SetParent(transform, false);
            RectTransform emptyRect = emptyObject.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0f, 1f);
            emptyRect.anchorMax = new Vector2(1f, 1f);
            emptyRect.pivot = new Vector2(0f, 1f);
            emptyRect.sizeDelta = new Vector2(0f, 18f);

            emptyLabel = emptyObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(emptyLabel);
            emptyLabel.fontSize = 11f;
            emptyLabel.color = SurvivalPioneerUiPalette.MutedText;
            emptyLabel.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void EnsureViewCount(int count)
        {
            while (activeViews.Count < count)
            {
                GameObject viewObject = new GameObject($"ModifierTick_{activeViews.Count}", typeof(RectTransform), typeof(ExposureModifierTickView));
                viewObject.transform.SetParent(contentRect, false);
                activeViews.Add(viewObject.GetComponent<ExposureModifierTickView>());
            }

            for (int i = 0; i < count; i++)
                activeViews[i].gameObject.SetActive(true);
        }
    }
}
