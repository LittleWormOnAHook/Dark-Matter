using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Thermometer-style EVA temperature panel (-190°F to 200°F, nominal 70°F).
    /// Hotbar layout matches reference: TEMPERATURE header, dual °F/°C scale, circular needle, °C readout.
    /// </summary>
    public class VerticalThermalNeedleGauge : MonoBehaviour
    {
        /// <summary>Hotbar HUD temp panel scale (matches hazard gauge).</summary>
        public const float HotbarPanelScale = 1.85f;

        /// <summary>Reserved top header height for hotbar layout (unscaled design units).</summary>
        public const float HotbarTitleBlock = 32f;

        /// <summary>Reserved top header height for journal/compact layout (unscaled design units).</summary>
        public const float CompactTitleBlock = 22f;

        private static readonly float[] ScaleFahrenheitMarks = { -40f, 0f, 70f, 120f };

        private RectTransform pointerRect;
        private RectTransform tubeRect;
        private TextMeshProUGUI valueLabel;
        private TextMeshProUGUI statusLabel;
        private float targetNormalized;
        private float displayedNormalized;
        private float targetFahrenheit;
        private float displayedFahrenheit;
        private int lastDisplayedFahrenheitText = int.MinValue;
        private float trackHeight;
        private float labelBlock;
        private float titleBlock;
        private bool compactMode;
        private bool suppressOwnPanelChrome;

        private static Sprite thermalGradientSprite;

        private static Sprite GetThermalGradientSprite()
        {
            if (thermalGradientSprite != null)
                return thermalGradientSprite;

            thermalGradientSprite = GaugeGradientTexture.BuildVertical(new[]
            {
                new Color(0.10f, 0.35f, 0.85f, 1f), // cold basin - blue
                new Color(0.12f, 0.65f, 0.78f, 1f), // cyan
                new Color(0.30f, 0.78f, 0.32f, 1f), // green/nominal
                new Color(0.95f, 0.72f, 0.15f, 1f), // amber/warm
                new Color(0.92f, 0.20f, 0.14f, 1f), // hot - red
            });
            return thermalGradientSprite;
        }

        public void Configure(bool compact, bool suppressOwnPanelChrome = false)
        {
            compactMode = compact;
            this.suppressOwnPanelChrome = suppressOwnPanelChrome;
            EnsureBuilt();
        }

        public void Refresh(in ExposureStatusSnapshot snapshot)
        {
            EnsureBuilt();
            targetNormalized = Mathf.Clamp01(snapshot.TemperatureGaugeNormalized);
            targetFahrenheit = snapshot.DisplayTemperatureF;

            if (statusLabel != null)
                statusLabel.text = snapshot.ThermalStatusLabel;
        }

        private void Update()
        {
            // UITK owns the gameplay thermal readout — no needle lerp or TMP write behind it.
            if (DMUiToolkitHud.IsDriving)
                return;

            if (tubeRect == null)
                return;

            bool settled = Mathf.Abs(displayedNormalized - targetNormalized) < 0.0005f
                && Mathf.Abs(displayedFahrenheit - targetFahrenheit) < 0.05f;
            if (settled)
            {
                displayedNormalized = targetNormalized;
                displayedFahrenheit = targetFahrenheit;
            }
            else
            {
                float speed = Time.deltaTime * 10f;
                displayedNormalized = Mathf.Lerp(displayedNormalized, targetNormalized, speed);
                displayedFahrenheit = Mathf.Lerp(displayedFahrenheit, targetFahrenheit, speed);
            }

            ApplyPointer(displayedNormalized);

            if (valueLabel == null)
                return;

            int rounded = Mathf.RoundToInt(displayedFahrenheit);
            if (rounded == lastDisplayedFahrenheitText)
                return;

            lastDisplayedFahrenheitText = rounded;
            // Fahrenheit is the primary EVA suit readout everywhere — compact (Journal) and full
            // (hotbar) layouts both lead with °F; the hotbar's non-compact scale still shows °C as a
            // secondary reference on the right-hand tick labels (see EnsureBuilt below).
            valueLabel.text = ExposureTemperatureDisplay.FormatFahrenheit(displayedFahrenheit);
        }

        private void ApplyPointer(float normalized)
        {
            if (pointerRect == null || trackHeight <= 0f)
                return;

            pointerRect.anchoredPosition = new Vector2(0f, normalized * trackHeight);
        }

        private void EnsureBuilt()
        {
            if (tubeRect != null)
                return;

            float layoutScale = compactMode ? 1f : HotbarPanelScale;
            float panelWidth = HudLayoutMetrics.Scaled((compactMode ? 56f : 100f) * layoutScale);
            trackHeight = HudLayoutMetrics.Scaled((compactMode ? 88f : 108f) * layoutScale);
            labelBlock = HudLayoutMetrics.Scaled((compactMode ? 52f : 54f) * layoutScale);
            titleBlock = HudLayoutMetrics.Scaled((compactMode ? CompactTitleBlock : HotbarTitleBlock) * layoutScale);
            float panelHeight = titleBlock + trackHeight + labelBlock;
            float titleTopPadding = HudLayoutMetrics.Scaled((compactMode ? 4f : 8f) * layoutScale);

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            root.sizeDelta = new Vector2(panelWidth, panelHeight);
            if (!suppressOwnPanelChrome)
                CreatePanelChrome(root);

            GameObject tubeObject = CreateChild("Tube", root);
            tubeRect = tubeObject.GetComponent<RectTransform>();
            tubeRect.anchorMin = new Vector2(0.5f, 0f);
            tubeRect.anchorMax = new Vector2(0.5f, 0f);
            tubeRect.pivot = new Vector2(0.5f, 0f);
            tubeRect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled((compactMode ? 18f : 20f) * layoutScale), trackHeight);
            tubeRect.anchoredPosition = new Vector2(0f, labelBlock);

            Image tubeBg = tubeObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(tubeBg);
            tubeBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.95f);

            GameObject gradientObject = CreateChild("Gradient", tubeObject.transform);
            RectTransform gradientRect = gradientObject.GetComponent<RectTransform>();
            gradientRect.anchorMin = new Vector2(0.1f, 0.01f);
            gradientRect.anchorMax = new Vector2(0.9f, 0.99f);
            gradientRect.offsetMin = Vector2.zero;
            gradientRect.offsetMax = Vector2.zero;
            Image gradientImage = gradientObject.AddComponent<Image>();
            gradientImage.sprite = GetThermalGradientSprite();
            gradientImage.type = Image.Type.Simple;
            gradientImage.color = Color.white;
            gradientImage.raycastTarget = false;

            float nominalY = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(ExposureTemperatureDisplay.NominalFahrenheit) * trackHeight;
            CreateTick(tubeObject.transform, nominalY, DarkMatterGenesisUiPalette.Gold, 3f);

            for (int i = 0; i < ScaleFahrenheitMarks.Length; i++)
            {
                float mark = ScaleFahrenheitMarks[i];
                float normalized = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(mark);
                CreateScaleLabel(root, $"{Mathf.RoundToInt(mark)}", normalized, leftSide: true, layoutScale);
                if (!compactMode)
                {
                    int celsius = Mathf.RoundToInt(ExposureTemperatureDisplay.FahrenheitToCelsius(mark));
                    CreateScaleLabel(root, $"{celsius}", normalized, leftSide: false, layoutScale);
                }
            }

            GameObject pointerObject = CreateChild("Pointer", tubeObject.transform);
            pointerRect = pointerObject.GetComponent<RectTransform>();
            pointerRect.anchorMin = new Vector2(0.5f, 0f);
            pointerRect.anchorMax = new Vector2(0.5f, 0f);
            pointerRect.pivot = new Vector2(0.5f, 0.5f);
            float pointerSize = HudLayoutMetrics.Scaled((compactMode ? 10f : 14f) * layoutScale);
            pointerRect.sizeDelta = new Vector2(pointerSize, pointerSize);

            Sprite circleSprite = ShiftUiTheme.CircleFilled;

            GameObject pointerRing = CreateChild("Ring", pointerObject.transform);
            RectTransform ringRect = pointerRing.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(-3f, -3f);
            ringRect.offsetMax = new Vector2(3f, 3f);
            Image ringImage = pointerRing.AddComponent<Image>();
            if (circleSprite != null)
            {
                ringImage.sprite = circleSprite;
                ringImage.type = Image.Type.Simple;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(ringImage);
            }
            ringImage.color = DarkMatterGenesisUiPalette.WithAlpha(Color.black, 0.85f);
            ringImage.raycastTarget = false;

            GameObject pointerFill = CreateChild("Fill", pointerObject.transform);
            RectTransform fillRect = pointerFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image pointerImage = pointerFill.AddComponent<Image>();
            if (circleSprite != null)
            {
                pointerImage.sprite = circleSprite;
                pointerImage.type = Image.Type.Simple;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(pointerImage);
            }
            pointerImage.color = DarkMatterGenesisUiPalette.Gold;
            pointerImage.raycastTarget = false;

            valueLabel = CreateText(
                root,
                "Value",
                "70°F",
                compactMode ? 16f : 24f * layoutScale,
                FontStyles.Bold,
                new Vector2(0f, HudLayoutMetrics.Scaled(compactMode ? 16f : 12f * layoutScale)));
            valueLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            if (compactMode)
            {
                statusLabel = CreateText(
                    root,
                    "Status",
                    "EVA NOMINAL",
                    9f * layoutScale,
                    FontStyles.Normal,
                    new Vector2(0f, HudLayoutMetrics.Scaled(2f)));
                statusLabel.color = DarkMatterGenesisUiPalette.MutedText;
            }

            TextMeshProUGUI titleLabel = CreateTopTitle(
                root,
                "Title",
                compactMode ? "TEMP" : "TEMPERATURE",
                10f * layoutScale,
                titleBlock,
                titleTopPadding);
            titleLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            titleLabel.transform.SetAsLastSibling();

            displayedNormalized = targetNormalized;
            displayedFahrenheit = targetFahrenheit;
            ApplyPointer(displayedNormalized);
        }

        private void CreatePanelChrome(RectTransform root)
        {
            if (root.GetComponent<RectMask2D>() == null)
                root.gameObject.AddComponent<RectMask2D>();

            GameObject backgroundObject = CreateChild("PanelBackground", root);
            backgroundObject.transform.SetAsFirstSibling();
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image panel = backgroundObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panel);
            // Matches HovercraftStatusHudUI's panel exactly (same alpha + trim inset, no extra top
            // accent strip) so the Temperature/Hazards panels read as one consistent style with it.
            panel.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.9f);
            panel.raycastTarget = false;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(backgroundObject, new Vector2(1.2f, -1.2f));
        }

        private void CreateTick(Transform parent, float y, Color color, float height)
        {
            GameObject tick = CreateChild("Tick", parent);
            RectTransform rect = tick.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, y);
            Image image = tick.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateScaleLabel(RectTransform root, string text, float normalized, bool leftSide, float layoutScale)
        {
            float y = labelBlock + normalized * trackHeight;
            float offset = HudLayoutMetrics.Scaled((compactMode ? 18f : 24f) * layoutScale);
            float x = leftSide ? -offset : offset;
            TextMeshProUGUI label = CreateText(
                root,
                leftSide ? $"ScaleL_{text}" : $"ScaleR_{text}",
                text,
                7.5f * (compactMode ? 1f : layoutScale),
                FontStyles.Normal,
                new Vector2(x, y));
            label.alignment = leftSide ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            label.color = DarkMatterGenesisUiPalette.MutedText;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static TextMeshProUGUI CreateTopTitle(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            float reservedTitleBlock,
            float topPadding)
        {
            GameObject labelObject = CreateChild(name, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            float labelHeight = Mathf.Max(HudLayoutMetrics.Scaled(14f), reservedTitleBlock - topPadding);
            rect.sizeDelta = new Vector2(0f, labelHeight);
            rect.anchoredPosition = new Vector2(0f, -topPadding);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = HudLayoutMetrics.Scaled(fontSize);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            Vector2 anchoredPosition)
        {
            GameObject labelObject = CreateChild(name, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(parent.sizeDelta.x, HudLayoutMetrics.Scaled(16f));
            rect.anchoredPosition = anchoredPosition;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = HudLayoutMetrics.Scaled(fontSize);
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }
    }
}
