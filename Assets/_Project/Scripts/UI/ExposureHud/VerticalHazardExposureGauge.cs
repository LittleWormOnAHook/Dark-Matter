using System.Collections;
using Project.Core;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Hazard dashboard: summary severity + per-zone list (hotbar) or compact icon grid (journal).
    /// In hotbar/HUD mode (enableAutoHide: true) the panel stays hidden until a real hazard is
    /// active, fades out again on exit, and can be peeked at any time with the J key for a few
    /// seconds. Journal usage is unaffected (enableAutoHide defaults false: always visible).
    /// </summary>
    public class VerticalHazardExposureGauge : MonoBehaviour
    {
        /// <summary>Hotbar HUD hazard panel scale (matches temp gauge).</summary>
        public const float HotbarPanelScale = 1.85f;

        private const float BasePanelWidth = 172f;
        private const float BaseCompactWidth = 88f;
        private const float BaseCompactHeight = 132f;
        private const float FadeDuration = 0.35f;
        private const float ManualPeekDuration = 5f;

        private static readonly HazardListDefinition[] HotbarHazardRows =
        {
            new HazardListDefinition(ExposureZoneKind.RadiationFlat, snap => snap.RadiationHazardLevel, icons => icons.GetRadiation()),
            new HazardListDefinition(ExposureZoneKind.ThermalCold, snap => snap.ColdHazardLevel, icons => icons.GetCold()),
            new HazardListDefinition(ExposureZoneKind.ThermalHeat, snap => snap.HeatHazardLevel, icons => icons.GetHeat()),
            new HazardListDefinition(ExposureZoneKind.SulfurField, snap => snap.SulfurHazardLevel, icons => icons.GetBio()),
            new HazardListDefinition(ExposureZoneKind.VolcanoCaldera, snap => snap.VolcanoHazardLevel, icons => icons.GetVolcano()),
            new HazardListDefinition(ExposureZoneKind.ShelterSafe, ResolveShelterLevel, icons => icons.GetShelter())
        };

        private RectTransform summaryFillRect;
        private Image summaryFillImage;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI severityLabel;
        private TextMeshProUGUI percentLabel;
        private TextMeshProUGUI dominantLabel;
        private readonly HazardIconSlot[] iconSlots = new HazardIconSlot[4];
        private readonly VehicleStatSegmentBar[] listRows = new VehicleStatSegmentBar[HotbarHazardRows.Length];
        private HazardHudIconSet iconSet;
        private bool compactMode;
        private bool suppressOwnPanelChrome;
        private bool enableAutoHide;
        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;
        private bool hasActiveHazard;
        private float manualPeekTimer;

        private static Sprite severityGradientSprite;

        private static Sprite GetSeverityGradientSprite()
        {
            if (severityGradientSprite != null)
                return severityGradientSprite;

            severityGradientSprite = GaugeGradientTexture.BuildHorizontal(new[]
            {
                SurvivalPioneerUiPalette.PositiveGreen,
                SurvivalPioneerUiPalette.Gold,
                SurvivalPioneerUiPalette.DeepMagenta,
            });
            return severityGradientSprite;
        }

        private readonly struct HazardListDefinition
        {
            public readonly ExposureZoneKind Kind;
            public readonly System.Func<ExposureStatusSnapshot, float> ResolveLevel;
            public readonly System.Func<HazardHudIconSet, HazardHudIconEntry> ResolveIcon;

            public HazardListDefinition(
                ExposureZoneKind kind,
                System.Func<ExposureStatusSnapshot, float> resolveLevel,
                System.Func<HazardHudIconSet, HazardHudIconEntry> resolveIcon)
            {
                Kind = kind;
                ResolveLevel = resolveLevel;
                ResolveIcon = resolveIcon;
            }
        }

        private sealed class HazardIconSlot
        {
            public TextMeshProUGUI IconLabel;
            public Image IconSprite;
            public RectTransform BarFill;
            public Image IconBackground;
        }

        public void Configure(bool compact, HazardHudIconSet icons = null, bool suppressOwnPanelChrome = false, bool enableAutoHide = false)
        {
            compactMode = compact;
            iconSet = icons ?? (compact ? null : HazardHudIconSet.LoadDefault());
            this.suppressOwnPanelChrome = suppressOwnPanelChrome;
            this.enableAutoHide = enableAutoHide;
            EnsureBuilt();
        }

        private void Update()
        {
            if (!enableAutoHide || canvasGroup == null)
                return;

            if (GameSession.HasStarted && !MainMenuController.BlocksGameplayHud
                && Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            {
                manualPeekTimer = ManualPeekDuration;
                FadeTo(1f);
            }

            if (manualPeekTimer > 0f)
            {
                manualPeekTimer -= Time.deltaTime;
                if (manualPeekTimer <= 0f && !hasActiveHazard)
                    FadeTo(0f);
            }
        }

        private void FadeTo(float target)
        {
            if (canvasGroup == null || (Mathf.Approximately(canvasGroup.alpha, target) && fadeRoutine == null))
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            float start = canvasGroup.alpha;
            float t = 0f;

            if (target > 0.01f)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / FadeDuration));
                yield return null;
            }

            canvasGroup.alpha = target;

            if (target <= 0.01f)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            fadeRoutine = null;
        }

        public void Refresh(in ExposureStatusSnapshot snapshot)
        {
            EnsureBuilt();

            HazardHudIconSet icons = iconSet ?? HazardHudIconSet.LoadDefault();

            float combined = Mathf.Clamp01(Mathf.Max(
                snapshot.CombinedExposureLevel,
                snapshot.DominantHazard.IsClear ? 0f : snapshot.DominantHazard.Severity));

            if (enableAutoHide)
            {
                bool hazardNow = !snapshot.DominantHazard.IsClear;
                if (hazardNow && !hasActiveHazard)
                {
                    hasActiveHazard = true;
                    FadeTo(1f);
                }
                else if (!hazardNow && hasActiveHazard)
                {
                    hasActiveHazard = false;
                    // A manual J-key peek in progress takes priority — let it finish its own
                    // countdown (Update()) instead of yanking the panel away immediately.
                    if (manualPeekTimer <= 0f)
                        FadeTo(0f);
                }
            }

            if (summaryFillImage != null)
                summaryFillImage.fillAmount = combined;

            if (severityLabel != null)
            {
                severityLabel.text = snapshot.HazardSeverityLabel;
                severityLabel.color = combined >= 0.65f
                    ? SurvivalPioneerUiPalette.DeepMagenta
                    : combined >= 0.35f
                        ? SurvivalPioneerUiPalette.Gold
                        : SurvivalPioneerUiPalette.WarmOffWhite;
            }

            if (percentLabel != null)
            {
                percentLabel.text = $"{Mathf.RoundToInt(combined * 100f)}%";
                percentLabel.color = SurvivalPioneerUiPalette.MutedText;
            }

            if (dominantLabel != null)
            {
                if (snapshot.ActiveZoneNames != null && snapshot.ActiveZoneNames.Length > 0)
                    dominantLabel.text = string.Join(" · ", snapshot.ActiveZoneNames);
                else if (!snapshot.DominantHazard.IsClear)
                    dominantLabel.text = snapshot.DominantHazard.DisplayName;
                else
                    dominantLabel.text = "EVA NOMINAL";

                dominantLabel.color = snapshot.DominantHazard.IsClear
                    ? SurvivalPioneerUiPalette.MutedText
                    : snapshot.DominantHazard.DisplayColor;
            }

            if (compactMode)
            {
                ApplyIconSlot(iconSlots[0], icons.GetCold(), snapshot.ColdHazardLevel, ExposureHazardPresentation.ColdColor);
                ApplyIconSlot(iconSlots[1], icons.GetHeat(), snapshot.HeatHazardLevel, ExposureHazardPresentation.HeatColor);
                ApplyIconSlot(iconSlots[2], icons.GetRadiation(), snapshot.RadiationHazardLevel, ExposureHazardPresentation.RadiationColor);
                float bioLevel = Mathf.Max(snapshot.SulfurHazardLevel, snapshot.VolcanoHazardLevel);
                ApplyIconSlot(iconSlots[3], icons.GetBio(), bioLevel, ExposureHazardPresentation.SulfurColor);
                return;
            }

            for (int i = 0; i < HotbarHazardRows.Length; i++)
            {
                HazardListDefinition definition = HotbarHazardRows[i];
                float level = Mathf.Clamp01(definition.ResolveLevel(snapshot));
                ApplyListRow(listRows[i], definition.Kind, level, snapshot.IsInShelter);
            }
        }

        private static float ResolveShelterLevel(ExposureStatusSnapshot snapshot)
        {
            return snapshot.IsInShelter ? 1f : 0f;
        }

        /// <summary>
        /// Hotbar-mode row update: hazard rows now share the exact Shield/Hull/Fuel row style
        /// (icon badge + name + percentage + segmented bar) via VehicleStatSegmentBar. Load-flash is
        /// disabled here since a rising hazard is bad news, not a "topped up" cue; the shelter row
        /// gets a fixed positive-green tint since being sheltered is always good.
        /// </summary>
        private static void ApplyListRow(VehicleStatSegmentBar row, ExposureZoneKind kind, float level, bool isInShelter)
        {
            if (row == null)
                return;

            bool isShelterRow = kind == ExposureZoneKind.ShelterSafe;
            float fill = isShelterRow ? (isInShelter ? 1f : 0.08f) : level;
            row.SetValues(fill, 1f);
        }

        private static void ApplyIconSlot(HazardIconSlot slot, in HazardHudIconEntry entry, float level, Color tint)
        {
            if (slot == null)
                return;

            level = Mathf.Clamp01(level);
            bool active = level > 0.05f;
            Color displayTint = active ? tint : SurvivalPioneerUiPalette.MutedText;

            if (slot.IconSprite != null)
            {
                slot.IconSprite.sprite = entry.Icon;
                slot.IconSprite.enabled = entry.Icon != null;
                slot.IconSprite.color = active ? Color.white : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.MutedText, 0.65f);
            }

            if (slot.IconLabel != null)
            {
                slot.IconLabel.gameObject.SetActive(entry.Icon == null);
                slot.IconLabel.text = entry.Glyph;
                slot.IconLabel.color = displayTint;
            }

            if (slot.IconBackground != null)
                slot.IconBackground.color = SurvivalPioneerUiPalette.WithAlpha(tint, active ? 0.35f : 0.12f);

            if (slot.BarFill != null)
            {
                slot.BarFill.anchorMax = new Vector2(level, 1f);
                slot.BarFill.offsetMin = Vector2.zero;
                slot.BarFill.offsetMax = Vector2.zero;
                Image fillImage = slot.BarFill.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = SurvivalPioneerUiPalette.WithAlpha(tint, 0.95f);
            }
        }

        private void EnsureBuilt()
        {
            if (titleLabel != null)
                return;

            float layoutScale = compactMode ? 1f : HotbarPanelScale;
            float panelWidth = HudLayoutMetrics.Scaled((compactMode ? BaseCompactWidth : BasePanelWidth) * layoutScale);
            float headerHeight = HudLayoutMetrics.Scaled((compactMode ? 58f : 62f) * layoutScale);
            float listHeight = compactMode
                ? HudLayoutMetrics.Scaled(92f * layoutScale)
                : VehicleStatSegmentBar.RowHeight * HotbarHazardRows.Length
                  + HudLayoutMetrics.Scaled(2f * layoutScale) * (HotbarHazardRows.Length - 1)
                  + HudLayoutMetrics.Scaled(4f * layoutScale);
            float panelHeight = headerHeight + listHeight + HudLayoutMetrics.Scaled(10f * layoutScale);
            // Flat inside-border gap, per explicit request (top kept smaller than the other sides).
            float padX = 20f;
            float padTop = 10f;
            float padBottom = 20f;
            float spacing = HudLayoutMetrics.Scaled(2f * layoutScale);

            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            root.sizeDelta = new Vector2(panelWidth, panelHeight);
            if (!suppressOwnPanelChrome)
                CreatePanelChrome(root);

            if (enableAutoHide)
            {
                canvasGroup = root.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

                // Starts hidden — Refresh() fades it in the moment a real hazard becomes active,
                // and the J key can peek it early (see Update()).
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            GameObject contentObject = CreateChild("Content", root);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padX, padBottom);
            contentRect.offsetMax = new Vector2(-padX, -padTop);

            VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = spacing;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            titleLabel = CreateLayoutLabel(
                contentObject.transform,
                "Title",
                "HAZARDS",
                12f * layoutScale,
                FontStyles.Bold,
                SurvivalPioneerUiPalette.WarmOffWhite,
                HudLayoutMetrics.Scaled(16f * layoutScale));
            severityLabel = CreateLayoutLabel(
                contentObject.transform,
                "Severity",
                "CLEAR",
                10f * layoutScale,
                FontStyles.Bold,
                SurvivalPioneerUiPalette.WarmOffWhite,
                HudLayoutMetrics.Scaled(14f * layoutScale));

            GameObject summaryTrack = CreateChild("SummaryTrack", contentObject.transform);
            LayoutElement summaryLayout = summaryTrack.AddComponent<LayoutElement>();
            summaryLayout.preferredHeight = HudLayoutMetrics.Scaled(10f * layoutScale);
            summaryLayout.minHeight = summaryLayout.preferredHeight;
            Image summaryTrackImage = summaryTrack.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(summaryTrackImage);
            summaryTrackImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.95f);

            GameObject summaryFill = CreateChild("SummaryFill", summaryTrack.transform);
            summaryFillRect = summaryFill.GetComponent<RectTransform>();
            summaryFillRect.anchorMin = Vector2.zero;
            summaryFillRect.anchorMax = Vector2.one;
            summaryFillRect.offsetMin = new Vector2(1f, 1f);
            summaryFillRect.offsetMax = new Vector2(-1f, -1f);
            summaryFillImage = summaryFill.AddComponent<Image>();
            summaryFillImage.sprite = GetSeverityGradientSprite();
            summaryFillImage.type = Image.Type.Filled;
            summaryFillImage.fillMethod = Image.FillMethod.Horizontal;
            summaryFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            summaryFillImage.fillAmount = 0f;
            summaryFillImage.color = Color.white;

            if (compactMode)
            {
                dominantLabel = CreateLayoutLabel(
                    contentObject.transform,
                    "Dominant",
                    "EVA NOMINAL",
                    9f * layoutScale,
                    FontStyles.Normal,
                    SurvivalPioneerUiPalette.MutedText,
                    HudLayoutMetrics.Scaled(14f * layoutScale));

                GameObject spacer = CreateChild("Spacer", contentObject.transform);
                LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
                spacerLayout.flexibleHeight = 1f;
                spacerLayout.minHeight = HudLayoutMetrics.Scaled(4f * layoutScale);

                BuildIconGrid(contentObject.transform, layoutScale);
                return;
            }

            percentLabel = CreateLayoutLabel(
                contentObject.transform,
                "Percent",
                "0%",
                9f * layoutScale,
                FontStyles.Bold,
                SurvivalPioneerUiPalette.MutedText,
                HudLayoutMetrics.Scaled(12f * layoutScale));

            BuildHazardList(contentObject.transform, layoutScale);
        }

        /// <summary>
        /// Each hazard row now shares the exact Shield/Hull/Fuel row style from the Hovercraft HUD
        /// (icon badge + name + percentage + segmented bar) via VehicleStatSegmentBar, instead of the
        /// old bespoke icon-circle + thin-bar row.
        /// </summary>
        private void BuildHazardList(Transform contentParent, float layoutScale)
        {
            GameObject listObject = CreateChild("HazardList", contentParent);
            LayoutElement listLayout = listObject.AddComponent<LayoutElement>();
            float rowSpacing = HudLayoutMetrics.Scaled(2f * layoutScale);
            listLayout.preferredHeight = VehicleStatSegmentBar.RowHeight * HotbarHazardRows.Length
                + rowSpacing * (HotbarHazardRows.Length - 1);
            listLayout.minHeight = listLayout.preferredHeight;

            VerticalLayoutGroup listGroup = listObject.AddComponent<VerticalLayoutGroup>();
            listGroup.spacing = rowSpacing;
            listGroup.childControlWidth = true;
            listGroup.childControlHeight = true;
            listGroup.childForceExpandWidth = true;
            listGroup.childForceExpandHeight = false;

            HazardHudIconSet icons = iconSet ?? HazardHudIconSet.LoadDefault();

            for (int i = 0; i < HotbarHazardRows.Length; i++)
            {
                HazardListDefinition definition = HotbarHazardRows[i];
                HazardHudIconEntry entry = definition.ResolveIcon(icons);
                Color tint = definition.Kind == ExposureZoneKind.ShelterSafe
                    ? SurvivalPioneerUiPalette.PositiveGreen
                    : ExposureHazardPresentation.GetColor(definition.Kind);
                string label = ExposureHazardPresentation.GetHudDisplayName(definition.Kind);

                // Load-flash (white pop on increase) is disabled for hazards — a rising hazard level
                // is bad news, not a "topped up" cue — and there's no low-value "critical" state here.
                listRows[i] = new VehicleStatSegmentBar(
                    listObject.transform,
                    entry.Icon,
                    label,
                    tint,
                    this,
                    showCriticalPill: false,
                    enableLoadFlash: false);
            }
        }

        private void BuildIconGrid(Transform contentParent, float layoutScale)
        {
            float cellHeight = HudLayoutMetrics.Scaled(28f * layoutScale);
            float barHeight = HudLayoutMetrics.Scaled(5f * layoutScale);
            float rowHeight = cellHeight + barHeight + HudLayoutMetrics.Scaled(2f * layoutScale);

            GameObject gridObject = CreateChild("IconGrid", contentParent);
            LayoutElement gridLayout = gridObject.AddComponent<LayoutElement>();
            gridLayout.preferredHeight = rowHeight * 2f + HudLayoutMetrics.Scaled(6f * layoutScale);
            gridLayout.minHeight = gridLayout.preferredHeight;

            VerticalLayoutGroup gridGroup = gridObject.AddComponent<VerticalLayoutGroup>();
            gridGroup.spacing = HudLayoutMetrics.Scaled(4f * layoutScale);
            gridGroup.childControlWidth = true;
            gridGroup.childControlHeight = true;
            gridGroup.childForceExpandWidth = true;
            gridGroup.childForceExpandHeight = false;

            GameObject topRow = CreateChild("TopRow", gridObject.transform);
            SetupIconRow(topRow, rowHeight);
            iconSlots[0] = CreateIconSlot(topRow.transform, "Cold", cellHeight, barHeight, layoutScale);
            iconSlots[1] = CreateIconSlot(topRow.transform, "Heat", cellHeight, barHeight, layoutScale);

            GameObject bottomRow = CreateChild("BottomRow", gridObject.transform);
            SetupIconRow(bottomRow, rowHeight);
            iconSlots[2] = CreateIconSlot(bottomRow.transform, "Radiation", cellHeight, barHeight, layoutScale);
            iconSlots[3] = CreateIconSlot(bottomRow.transform, "Sulfur", cellHeight, barHeight, layoutScale);
        }

        private static void SetupIconRow(GameObject rowObject, float rowHeight)
        {
            LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = rowHeight;
            rowLayout.minHeight = rowHeight;

            HorizontalLayoutGroup rowGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = HudLayoutMetrics.Scaled(6f);
            rowGroup.childAlignment = TextAnchor.LowerCenter;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = true;
            rowGroup.childForceExpandHeight = false;
        }

        private HazardIconSlot CreateIconSlot(
            Transform rowParent,
            string name,
            float iconHeight,
            float barHeight,
            float layoutScale)
        {
            float slotHeight = iconHeight + barHeight + HudLayoutMetrics.Scaled(2f * layoutScale);

            GameObject slotObject = CreateChild(name, rowParent);
            LayoutElement slotLayout = slotObject.AddComponent<LayoutElement>();
            slotLayout.flexibleWidth = 1f;
            slotLayout.preferredHeight = slotHeight;
            slotLayout.minHeight = slotHeight;

            VerticalLayoutGroup slotGroup = slotObject.AddComponent<VerticalLayoutGroup>();
            slotGroup.spacing = 1f;
            slotGroup.childAlignment = TextAnchor.LowerCenter;
            slotGroup.childControlWidth = true;
            slotGroup.childControlHeight = true;
            slotGroup.childForceExpandWidth = true;
            slotGroup.childForceExpandHeight = false;

            GameObject iconBg = CreateChild("Icon", slotObject.transform);
            LayoutElement iconLayout = iconBg.AddComponent<LayoutElement>();
            iconLayout.preferredHeight = iconHeight;
            iconLayout.minHeight = iconHeight;
            Image iconBackground = iconBg.AddComponent<Image>();
            Sprite gridCircleSprite = ShiftUiTheme.CircleFilled;
            if (gridCircleSprite != null)
            {
                iconBackground.sprite = gridCircleSprite;
                iconBackground.type = Image.Type.Simple;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(iconBackground);
            }
            iconBackground.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.35f);

            GameObject spriteObject = CreateChild("Sprite", iconBg.transform);
            RectTransform spriteRect = spriteObject.GetComponent<RectTransform>();
            spriteRect.anchorMin = Vector2.zero;
            spriteRect.anchorMax = Vector2.one;
            spriteRect.offsetMin = new Vector2(4f, 4f);
            spriteRect.offsetMax = new Vector2(-4f, -4f);
            Image iconSprite = spriteObject.AddComponent<Image>();
            iconSprite.preserveAspect = true;
            iconSprite.raycastTarget = false;
            iconSprite.enabled = false;

            GameObject labelObject = CreateChild("Glyph", iconBg.transform);
            RectTransform labelRectTransform = labelObject.GetComponent<RectTransform>();
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = Vector2.zero;
            labelRectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI iconLabel = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(iconLabel);
            iconLabel.text = "?";
            iconLabel.fontSize = HudLayoutMetrics.Scaled(12f * layoutScale);
            iconLabel.fontStyle = FontStyles.Bold;
            iconLabel.alignment = TextAlignmentOptions.Center;
            iconLabel.raycastTarget = false;

            GameObject barTrack = CreateChild("BarTrack", slotObject.transform);
            LayoutElement barLayout = barTrack.AddComponent<LayoutElement>();
            barLayout.preferredHeight = barHeight;
            barLayout.minHeight = barHeight;
            Image barTrackImage = barTrack.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(barTrackImage);
            barTrackImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.9f);

            GameObject barFill = CreateChild("BarFill", barTrack.transform);
            RectTransform barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = new Vector2(1f, 1f);
            barFillRect.offsetMax = new Vector2(-1f, -1f);
            Image barFillImage = barFill.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(barFillImage);
            barFillImage.color = SurvivalPioneerUiPalette.PositiveGreen;

            return new HazardIconSlot
            {
                IconLabel = iconLabel,
                IconSprite = iconSprite,
                BarFill = barFillRect,
                IconBackground = iconBackground
            };
        }

        private static TextMeshProUGUI CreateLayoutLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            float lineHeight)
        {
            GameObject labelObject = CreateChild(name, parent);
            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            layout.preferredHeight = lineHeight;
            layout.minHeight = lineHeight;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = HudLayoutMetrics.Scaled(fontSize);
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
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
            // Matches HovercraftStatusHudUI's panel exactly (same alpha + trim inset) so the two
            // HUDs read as one consistent style.
            panel.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.9f);
            panel.raycastTarget = false;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(backgroundObject, new Vector2(1.2f, -1.2f));
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
