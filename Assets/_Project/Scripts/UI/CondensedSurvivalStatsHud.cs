using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Survival vitals as horizontal bars spanning the hotbar width, stacked above it.
    /// </summary>
    [DisallowMultipleComponent]
    public class CondensedSurvivalStatsHud : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private bool applyRuntimeLayout = true;

        private const float BarHeight = 16.8f * HudLayoutMetrics.HudScale;
        private const float SegmentGap = 4f * HudLayoutMetrics.HudScale;
        private const float IconGap = 3f * HudLayoutMetrics.HudScale;
        private const float HotbarVerticalPadding = 2f * HudLayoutMetrics.HudScale;

        private static readonly string[] SegmentOrder =
        {
            "HealthRow",
            "HungerRow",
            "ThirstRow",
            "EnergyRow"
        };

        private static readonly Color[] SegmentFillColors =
        {
            new Color(0.92f, 0.18f, 0.14f, 1f),
            new Color(0.91f, 0.63f, 0.27f, 1f),
            new Color(0.43f, 0.76f, 1f, 1f),
            new Color(0.71f, 0.88f, 0.40f, 1f)
        };

        private bool layoutApplied;

        public static bool IsActive => FindAnyObjectByType<CondensedSurvivalStatsHud>() != null;

        private static Sprite barFillSprite;

        public static void ApplyBarFill(Slider slider, float normalized)
        {
            if (slider == null)
                return;

            Transform fillTransform = slider.transform.Find("RingFill");
            if (fillTransform is not RectTransform fillRect)
                return;

            normalized = Mathf.Clamp01(normalized);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.SetAsLastSibling();
        }

        private void Start()
        {
            if (applyRuntimeLayout)
                RefreshLayout();
            else
                SyncSurvivalBarValues();
        }

        public void RefreshLayout()
        {
            if (!applyRuntimeLayout)
                return;

            layoutApplied = false;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (!applyRuntimeLayout)
                return;

            if (layoutApplied)
                return;

            layoutApplied = true;

            if (transform is not RectTransform panelRect)
                return;

            RemoveAutoLayoutComponents();
            ConfigurePanel(panelRect);
            ConfigureHorizontalSegments(panelRect);
            SyncSurvivalBarValues();
        }

        private static void SyncSurvivalBarValues()
        {
            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                ui.SyncSurvivalBars();
        }

        private void RemoveAutoLayoutComponents()
        {
            if (TryGetComponent<VerticalLayoutGroup>(out VerticalLayoutGroup panelLayout))
                Destroy(panelLayout);

            for (int i = 0; i < SegmentOrder.Length; i++)
            {
                Transform row = transform.Find(SegmentOrder[i]);
                if (row == null)
                    continue;

                if (row.TryGetComponent<HorizontalLayoutGroup>(out HorizontalLayoutGroup rowLayout))
                    Destroy(rowLayout);

                if (row.TryGetComponent<LayoutElement>(out LayoutElement layoutElement))
                    Destroy(layoutElement);
            }
        }

        private void ConfigurePanel(RectTransform panelRect)
        {
            RectTransform hotbarRect = ResolveHotbarRect();
            float hotbarWidth = hotbarRect != null ? hotbarRect.sizeDelta.x : HudLayoutMetrics.Scaled(960f);
            float hotbarHeight = hotbarRect != null ? hotbarRect.sizeDelta.y : HudLayoutMetrics.Scaled(82f);
            float anchoredY = hotbarRect != null ? hotbarRect.anchoredPosition.y : HudLayoutMetrics.BottomHudInset;
            float hotbarCenterX = hotbarRect != null ? hotbarRect.anchoredPosition.x : 0f;
            Vector2 hotbarAnchorMin = hotbarRect != null ? hotbarRect.anchorMin : new Vector2(0.5f, 0f);
            Vector2 hotbarAnchorMax = hotbarRect != null ? hotbarRect.anchorMax : new Vector2(0.5f, 0f);
            Vector2 hotbarPivot = hotbarRect != null ? hotbarRect.pivot : new Vector2(0.5f, 0f);

            panelRect.anchorMin = hotbarAnchorMin;
            panelRect.anchorMax = hotbarAnchorMax;
            panelRect.pivot = hotbarPivot;
            panelRect.sizeDelta = new Vector2(hotbarWidth, BarHeight);
            panelRect.anchoredPosition = new Vector2(
                hotbarCenterX,
                anchoredY + hotbarHeight + HotbarVerticalPadding);

            if (TryGetComponent<Image>(out Image panelImage))
            {
                panelImage.sprite = null;
                panelImage.color = new Color(0f, 0f, 0f, 0f);
                panelImage.raycastTarget = false;
            }
        }

        private void ConfigureHorizontalSegments(RectTransform panelRect)
        {
            int segmentCount = SegmentOrder.Length;
            float panelWidth = panelRect.sizeDelta.x;
            float totalGap = SegmentGap * (segmentCount - 1);
            float segmentWidth = (panelWidth - totalGap) / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                Transform row = transform.Find(SegmentOrder[i]);
                if (row == null)
                    continue;

                row.SetSiblingIndex(i);
                ConfigureSegment(row, i, segmentWidth, SegmentGap);
            }
        }

        private void ConfigureSegment(Transform row, int segmentIndex, float segmentWidth, float gap)
        {
            if (row is not RectTransform rowRect)
                return;

            rowRect.localScale = Vector3.one;
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(0f, 0f);
            rowRect.pivot = new Vector2(0f, 0f);
            rowRect.anchoredPosition = new Vector2(segmentIndex * (segmentWidth + gap), 0f);
            rowRect.sizeDelta = new Vector2(segmentWidth, BarHeight);

            Slider slider = row.GetComponentInChildren<Slider>(true);
            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>(true);
            Transform iconTransform = slider != null ? slider.transform.Find("Icon") : null;
            float iconWidth = BarHeight;
            float barWidth = Mathf.Max(8f, segmentWidth - iconWidth - IconGap);

            if (iconTransform != null)
            {
                iconTransform.SetParent(row, false);
                ConfigureSegmentIcon(iconTransform, iconWidth);
            }

            if (slider != null)
                ConfigureHorizontalBar(slider, SegmentFillColors[segmentIndex], iconWidth + IconGap, barWidth);

            if (label != null)
                label.gameObject.SetActive(false);
        }

        private static void ConfigureSegmentIcon(Transform iconTransform, float iconSize)
        {
            iconTransform.gameObject.SetActive(true);
            iconTransform.SetAsFirstSibling();

            if (iconTransform is not RectTransform iconRect)
                return;

            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            if (iconTransform.TryGetComponent<Image>(out Image iconImage))
            {
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
        }

        private void ConfigureHorizontalBar(Slider slider, Color fillColor, float barLeftInset, float barWidth)
        {
            RectTransform sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(0f, 0f);
            sliderRect.pivot = new Vector2(0f, 0f);
            sliderRect.anchoredPosition = new Vector2(barLeftInset, 0f);
            sliderRect.sizeDelta = new Vector2(barWidth, BarHeight);

            if (slider.TryGetComponent<CircularProgressBar>(out CircularProgressBar radialBar))
                DestroyImmediate(radialBar);

            ShiftUiTheme theme = ShiftUiTheme.Current;
            Sprite trackSprite = theme?.panelFrame;

            Transform trackTransform = slider.transform.Find("RingBackground");
            Transform fillTransform = slider.transform.Find("RingFill");

            Image trackImage = trackTransform != null ? trackTransform.GetComponent<Image>() : null;
            if (trackImage != null)
            {
                ApplyHorizontalTrackVisuals(trackImage, trackSprite, theme);
                StretchToParent(trackTransform as RectTransform);
            }

            Image fillImage = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            if (fillImage != null)
                ApplyHorizontalFillVisuals(fillImage, fillColor);

            float clamped = Mathf.Clamp01(slider.value);
            slider.SetValueWithoutNotify(clamped);
            ApplyBarFill(slider, clamped);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyHorizontalTrackVisuals(Image track, Sprite trackSprite, ShiftUiTheme theme)
        {
            if (trackSprite != null)
            {
                track.sprite = trackSprite;
                track.type = Image.Type.Sliced;
            }
            else
            {
                track.sprite = null;
                track.type = Image.Type.Simple;
            }

            track.color = theme != null
                ? new Color(theme.backgroundColor.r, theme.backgroundColor.g, theme.backgroundColor.b, 0.88f)
                : new Color(0.08f, 0.08f, 0.08f, 0.88f);
            track.raycastTarget = false;
            track.fillAmount = 1f;
        }

        private static void ApplyHorizontalFillVisuals(Image fill, Color fillColor)
        {
            fill.sprite = GetBarFillSprite();
            fill.type = Image.Type.Simple;
            fill.color = fillColor;
            fill.raycastTarget = false;
            fill.preserveAspect = false;
        }

        private static Sprite GetBarFillSprite()
        {
            if (barFillSprite != null)
                return barFillSprite;

            // Avoid Resources.GetBuiltinResource — it logs errors on many Unity versions when missing.
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            barFillSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            barFillSprite.name = "CondensedBarFillSprite";
            return barFillSprite;
        }

        private RectTransform ResolveHotbarRect()
        {
            InventoryUI inventoryUi = FindAnyObjectByType<InventoryUI>();
            if (inventoryUi != null && inventoryUi.hotbarParent is RectTransform hotbarFromInventory)
                return hotbarFromInventory;

            Transform hotbar = transform.parent != null ? transform.parent.Find("Hotbar") : null;
            return hotbar as RectTransform;
        }
    }
}
