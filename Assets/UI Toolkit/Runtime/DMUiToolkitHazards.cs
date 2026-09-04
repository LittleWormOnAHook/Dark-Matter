using System.Text;
using Project.Core;
using Project.Survival;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK temperature + hazards cluster, zone-entry banner, and exposure ticks.
    /// Matches VerticalHazardExposureGauge live layout (UITK hazards overlay).
    /// </summary>
    [DefaultExecutionOrder(-377)]
    [DisallowMultipleComponent]
    public class DMUiToolkitHazards : MonoBehaviour
    {
        private const int SegmentCount = 12;
        private const float FadeDuration = 0.35f;
        private const float ManualPeekDuration = 5f;
        private const float BannerHold = 3f;
        private const float BannerFadeIn = 0.25f;
        private const float BannerFadeOut = 0.45f;

        private static readonly Color ColdColor = ExposureHazardPresentation.ColdColor;
        private static readonly Color HeatColor = ExposureHazardPresentation.HeatColor;
        private static readonly Color RadColor = ExposureHazardPresentation.RadiationColor;
        private static readonly Color SulfurColor = ExposureHazardPresentation.SulfurColor;
        private static readonly Color VolcanoColor = ExposureHazardPresentation.VolcanoColor;
        private static readonly Color ShelterColor = ExposureHazardPresentation.ShelterColor;

        private static DMUiToolkitHazards instance;
        private static Sprite thermalGradientSprite;

        private UIDocument document;
        private VisualElement root;
        private VisualElement cluster;
        private VisualElement hazardPanel;
        private Label thermalStatus;
        private Label thermalValue;
        private VisualElement thermalTrack;
        private VisualElement thermalFill;
        private VisualElement thermalNeedle;
        private Label hazardTitle;
        private Label hazardSeverity;
        private Label hazardPercent;
        private VisualElement hazardSummaryFill;
        private Label radPct;
        private VisualElement radSegs;
        private Label coldPct;
        private VisualElement coldSegs;
        private Label heatPct;
        private VisualElement heatSegs;
        private Label sulfurPct;
        private VisualElement sulfurSegs;
        private Label volcanoPct;
        private VisualElement volcanoSegs;
        private Label shelterPct;
        private VisualElement shelterSegs;
        private Label ticksLabel;
        private VisualElement zoneBanner;
        private VisualElement zoneAccent;
        private Label zoneName;
        private bool bound;
        private bool hasActiveHazard;
        private float hazardAlpha;
        private float hazardAlphaTarget;
        private float manualPeekTimer;
        private ExposureReceiver boundReceiver;
        private float bannerElapsed;
        private int bannerPhase;
        private readonly StringBuilder tickBuilder = new StringBuilder(128);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitHazards EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.HazardsName,
                DMUiToolkitOverlayDocument.HazardsUxml,
                DMUiToolkitOverlayDocument.HazardsUss,
                DMUiToolkitOverlayDocument.HazardsSort);
            if (doc == null)
                return null;

            DMUiToolkitHazards host = doc.GetComponent<DMUiToolkitHazards>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitHazards>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
            // Hide legacy thermal / exposure uGUI before first frame paint.
            HideUgui();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            HideUgui();
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
            {
                service.OnSnapshotChanged -= HandleSnapshot;
                service.OnSnapshotChanged += HandleSnapshot;
            }

            BindZoneReceiver();
        }

        private void OnDisable()
        {
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged -= HandleSnapshot;

            UnbindZoneReceiver();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private bool uguiHidden;
        private int nextHideUguiFrame;
        private Label cachedPilotElev;
        private int nextElevResolveFrame;
        private float lastHazardPinLeft = float.NaN;
        private float lastHazardPinBottom = float.NaN;

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool want = DMUiToolkitOverlayDocument.GameplayHudWanted()
                && !GameplayHudVisibility.CinematicChromeHidden;
            DMUiToolkitOverlayDocument.SetShown(root, want);

            // Legacy uGUI cleanup uses FindAnyObjectByType - throttle once settled.
            if (!uguiHidden || Time.frameCount >= nextHideUguiFrame)
            {
                HideUgui();
                uguiHidden = DMUiToolkitConfig.IsEnabled && DMUiToolkitBootstrap.IsRootActive;
                nextHideUguiFrame = Time.frameCount + (uguiHidden ? 60 : 15);
            }

            if (!want)
            {
                manualPeekTimer = 0f;
                hazardAlpha = 0f;
                ApplyHazardAlpha();
                DMUiToolkitOverlayDocument.SetShown(zoneBanner, false);
                return;
            }

            if (hasActiveHazard)
                hazardAlphaTarget = 1f;

            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                manualPeekTimer = ManualPeekDuration;
                hazardAlphaTarget = 1f;
            }

            if (manualPeekTimer > 0f)
            {
                manualPeekTimer -= Time.unscaledDeltaTime;
                if (manualPeekTimer <= 0f && !hasActiveHazard)
                    hazardAlphaTarget = 0f;
            }

            hazardAlpha = Mathf.MoveTowards(hazardAlpha, hazardAlphaTarget, Time.unscaledDeltaTime / FadeDuration);
            ApplyHazardAlpha();
            TickBanner();
            PinAboveElev();
            if (boundReceiver == null)
                BindZoneReceiver();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("hazards-root") ?? tree;
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(root);
            cluster = tree.Q<VisualElement>("hazards-cluster");
            hazardPanel = tree.Q<VisualElement>("hazard-panel");
            VisualElement thermalPanel = tree.Q<VisualElement>("thermal-panel");
            if (thermalPanel != null)
                thermalPanel.style.display = DisplayStyle.None;
            HideUgui();
            ShowBuilderHost(tree.Q("hazard-title"));
            ShowBuilderHost(tree.Q("hazard-severity"));
            ShowBuilderHost(tree.Q("hazard-percent"));
            ShowBuilderHost(tree.Q("hazard-rows"));
            PinBesidePilot();
            if (document != null && document.sortingOrder != DMUiToolkitOverlayDocument.HazardsSort)
                document.sortingOrder = DMUiToolkitOverlayDocument.HazardsSort;
            thermalStatus = tree.Q<Label>("thermal-status");
            thermalValue = tree.Q<Label>("thermal-value");
            thermalTrack = tree.Q<VisualElement>("thermal-track");
            thermalFill = tree.Q<VisualElement>("thermal-fill");
            thermalNeedle = tree.Q<VisualElement>("thermal-needle");
            hazardTitle = tree.Q<Label>("hazard-title");
            hazardSeverity = tree.Q<Label>("hazard-severity");
            hazardPercent = tree.Q<Label>("hazard-percent");
            hazardSummaryFill = tree.Q<VisualElement>("hazard-summary-fill");
            radPct = tree.Q<Label>("hz-rad-pct");
            radSegs = tree.Q<VisualElement>("hz-rad-segs");
            coldPct = tree.Q<Label>("hz-cold-pct");
            coldSegs = tree.Q<VisualElement>("hz-cold-segs");
            heatPct = tree.Q<Label>("hz-heat-pct");
            heatSegs = tree.Q<VisualElement>("hz-heat-segs");
            sulfurPct = tree.Q<Label>("hz-sulfur-pct");
            sulfurSegs = tree.Q<VisualElement>("hz-sulfur-segs");
            volcanoPct = tree.Q<Label>("hz-volcano-pct");
            volcanoSegs = tree.Q<VisualElement>("hz-volcano-segs");
            shelterPct = tree.Q<Label>("hz-shelter-pct");
            shelterSegs = tree.Q<VisualElement>("hz-shelter-segs");
            ticksLabel = tree.Q<Label>("hazard-ticks-label");
            zoneBanner = tree.Q<VisualElement>("zone-banner");
            zoneAccent = tree.Q<VisualElement>("zone-banner-accent");
            zoneName = tree.Q<Label>("zone-banner-name");

            DMUiToolkitOverlayDocument.PopulateSegments(radSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(coldSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(heatSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(sulfurSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(volcanoSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(shelterSegs, SegmentCount);

            ApplyThermalGradientTrack();

            hazardAlpha = 0f;
            hazardAlphaTarget = 0f;
            ApplyHazardAlpha();
            DMUiToolkitOverlayDocument.SetShown(zoneBanner, false);
            DMUiToolkitOverlayDocument.SetShown(root, false);

            bound = root != null;
            HandleSnapshot(ExposureStatusService.Current);
        }

        private void HandleSnapshot(ExposureStatusSnapshot snapshot)
        {
            if (snapshot == null)
                snapshot = ExposureStatusSnapshot.Empty;

            if (thermalStatus != null)
                thermalStatus.text = string.IsNullOrEmpty(snapshot.ThermalStatusLabel) ? "EVA NOMINAL" : snapshot.ThermalStatusLabel;

            if (thermalValue != null)
            {
                string text = string.IsNullOrEmpty(snapshot.TemperatureText)
                    ? $"{Mathf.RoundToInt(snapshot.DisplayTemperatureF)}°F"
                    : snapshot.TemperatureText.Replace("?", "°").Replace(" F", "°F");
                thermalValue.text = text;
            }

            float tempN = Mathf.Clamp01(snapshot.TemperatureGaugeNormalized);
            // Legacy thermometer: full cold→hot gradient tube with needle only (no magenta fill).
            if (thermalFill != null)
                thermalFill.style.display = DisplayStyle.None;
            if (thermalNeedle != null)
                thermalNeedle.style.bottom = Length.Percent(tempN * 100f);

            float combined = Mathf.Clamp01(Mathf.Max(
                snapshot.CombinedExposureLevel,
                snapshot.DominantHazard.IsClear ? 0f : snapshot.DominantHazard.Severity));

            bool hazardNow = !snapshot.DominantHazard.IsClear
                || snapshot.RadiationHazardLevel > 0.01f
                || snapshot.ColdHazardLevel > 0.01f
                || snapshot.HeatHazardLevel > 0.01f
                || snapshot.SulfurHazardLevel > 0.01f
                || snapshot.VolcanoHazardLevel > 0.01f
                || snapshot.IsInShelter;
            if (hazardNow)
            {
                hasActiveHazard = true;
                hazardAlphaTarget = 1f;
            }
            else if (hasActiveHazard)
            {
                hasActiveHazard = false;
                if (manualPeekTimer <= 0f)
                    hazardAlphaTarget = 0f;
            }

            if (hazardTitle != null)
            {
                // Same source as pilot zone label: DominantHazard.DisplayName (CLEAR when none).
                string zoneTitle = string.IsNullOrEmpty(snapshot.DominantHazard.DisplayName)
                    ? "CLEAR"
                    : snapshot.DominantHazard.DisplayName.ToUpperInvariant();
                hazardTitle.text = zoneTitle;
            }

            if (hazardSeverity != null)
            {
                hazardSeverity.text = string.IsNullOrEmpty(snapshot.HazardSeverityLabel) ? "CLEAR" : snapshot.HazardSeverityLabel;
                hazardSeverity.style.color = combined >= 0.65f
                    ? DarkMatterGenesisUiPalette.DeepMagenta
                    : combined >= 0.35f
                        ? DarkMatterGenesisUiPalette.Gold
                        : DarkMatterGenesisUiPalette.WarmOffWhite;
            }

            if (hazardPercent != null)
                hazardPercent.text = $"{Mathf.RoundToInt(combined * 100f)}%";

            if (hazardSummaryFill != null)
            {
                // Vertical summary bar: fill grows upward via height %.
                hazardSummaryFill.style.width = Length.Percent(100f);
                hazardSummaryFill.style.height = Length.Percent(combined * 100f);
                Color fill = snapshot.DominantHazard.IsClear
                    ? ExposureHazardPresentation.ClearColor
                    : snapshot.DominantHazard.DisplayColor;
                hazardSummaryFill.style.backgroundColor = fill;
            }

            VisualElement summaryRow = hazardSummaryFill != null ? hazardSummaryFill.parent : null;
            if (summaryRow != null)
                summaryRow = summaryRow.parent; // hazard-summary-track -> hz-row-summary
            if (summaryRow != null)
                summaryRow.style.display = DisplayStyle.Flex;

            ApplyHazardRow(radSegs, radPct, snapshot.RadiationHazardLevel, RadColor);
            ApplyHazardRow(coldSegs, coldPct, snapshot.ColdHazardLevel, ColdColor);
            ApplyHazardRow(heatSegs, heatPct, snapshot.HeatHazardLevel, HeatColor);
            ApplyHazardRow(sulfurSegs, sulfurPct, snapshot.SulfurHazardLevel, SulfurColor);
            ApplyHazardRow(volcanoSegs, volcanoPct, snapshot.VolcanoHazardLevel, VolcanoColor);
            ApplyHazardRow(shelterSegs, shelterPct, snapshot.IsInShelter ? 1f : 0f, ShelterColor);

            PullTicks(snapshot);
            PullCompactExposure();
        }

        private static void ApplyHazardRow(VisualElement segs, Label pct, float level, Color color)
        {
            float n = Mathf.Clamp01(level);
            DMUiToolkitOverlayDocument.ApplySegments(segs, n, color);
            if (pct != null)
                pct.text = $"{Mathf.RoundToInt(n * 100f)}%";

            VisualElement row = segs != null ? segs.parent : null;
            if (row != null)
                row.style.display = n > 0.01f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyHazardAlpha()
        {
            if (hazardPanel == null)
                return;

            hazardPanel.style.opacity = hazardAlpha;
        }

        private void ApplyThermalGradientTrack()
        {
            if (thermalTrack == null)
                thermalTrack = thermalFill != null ? thermalFill.parent : root?.Q<VisualElement>("thermal-track");
            if (thermalTrack == null)
                return;

            if (thermalGradientSprite == null)
            {
                thermalGradientSprite = GaugeGradientTexture.BuildVertical(new[]
                {
                    new Color(0.10f, 0.35f, 0.85f, 1f),
                    new Color(0.12f, 0.65f, 0.78f, 1f),
                    new Color(0.30f, 0.78f, 0.32f, 1f),
                    new Color(0.95f, 0.72f, 0.15f, 1f),
                    new Color(0.92f, 0.20f, 0.14f, 1f),
                });
            }

            DMUiToolkitStyle.TrySetSpriteBackground(thermalTrack, thermalGradientSprite, ScaleMode.StretchToFill);
            if (thermalFill != null)
                thermalFill.style.display = DisplayStyle.None;
        }

        private void PullTicks(ExposureStatusSnapshot snapshot)
        {
            if (ticksLabel == null)
                return;

            tickBuilder.Clear();
            AppendTicks(snapshot.PlayerBuffTicks);
            AppendTicks(snapshot.PlayerDebuffTicks);
            string text = tickBuilder.ToString().Trim().TrimEnd('?', ' ');
            ticksLabel.text = text;
            DMUiToolkitOverlayDocument.SetShown(ticksLabel, !string.IsNullOrEmpty(text));
        }

        private void AppendTicks(ExposureModifierTick[] ticks)
        {
            if (ticks == null)
                return;

            for (int i = 0; i < ticks.Length; i++)
            {
                if (string.IsNullOrEmpty(ticks[i].Label))
                    continue;

                if (tickBuilder.Length > 0)
                    tickBuilder.Append("  ?  ");

                if (!string.IsNullOrEmpty(ticks[i].IconGlyph))
                {
                    tickBuilder.Append(ticks[i].IconGlyph);
                    tickBuilder.Append(' ');
                }

                tickBuilder.Append(ticks[i].Label);
            }
        }

        private static void PullCompactExposure()
        {
            SurvivalStats stats = FindAnyObjectByType<SurvivalStats>();
            if (stats == null)
                return;

            // Compact RAD/S/V readout is superseded by the hazard rows; keep uGUI hidden only.
        }

        private void BindZoneReceiver()
        {
            if (boundReceiver != null)
                return;

            ExposureStatusService service = ExposureStatusService.Instance;
            ExposureReceiver receiver = service != null
                ? service.GetComponent<ExposureReceiver>()
                : null;
            if (receiver == null)
                receiver = FindAnyObjectByType<ExposureReceiver>();
            if (receiver == null)
                return;

            boundReceiver = receiver;
            boundReceiver.ZoneEntered += HandleZoneEntered;
        }

        private void UnbindZoneReceiver()
        {
            if (boundReceiver != null)
                boundReceiver.ZoneEntered -= HandleZoneEntered;
            boundReceiver = null;
        }

        private void HandleZoneEntered(ExposureZoneVolume zone)
        {
            if (!GameSession.HasStarted || zone?.Profile == null)
                return;

            if (MainMenuController.BlocksGameplayHud)
                return;

            string name = zone.Profile.displayName;
            if (string.IsNullOrWhiteSpace(name))
                name = ExposureHazardPresentation.GetShortLabel(zone.Profile.zoneKind);

            if (zoneName != null)
                zoneName.text = name.ToUpperInvariant();
            if (zoneAccent != null)
                zoneAccent.style.backgroundColor = ExposureHazardPresentation.GetColor(zone.Profile.zoneKind);

            bannerPhase = 1;
            bannerElapsed = 0f;
            if (zoneBanner != null)
            {
                DMUiToolkitOverlayDocument.SetShown(zoneBanner, true);
                zoneBanner.style.opacity = 0f;
            }
        }

        private void TickBanner()
        {
            if (bannerPhase == 0 || zoneBanner == null)
                return;

            bannerElapsed += Time.unscaledDeltaTime;
            if (bannerPhase == 1)
            {
                float t = Mathf.Clamp01(bannerElapsed / BannerFadeIn);
                zoneBanner.style.opacity = t;
                if (t >= 1f)
                {
                    bannerPhase = 2;
                    bannerElapsed = 0f;
                }
            }
            else if (bannerPhase == 2)
            {
                zoneBanner.style.opacity = 1f;
                if (bannerElapsed >= BannerHold)
                {
                    bannerPhase = 3;
                    bannerElapsed = 0f;
                }
            }
            else if (bannerPhase == 3)
            {
                float t = Mathf.Clamp01(bannerElapsed / BannerFadeOut);
                zoneBanner.style.opacity = 1f - t;
                if (t >= 1f)
                {
                    bannerPhase = 0;
                    DMUiToolkitOverlayDocument.SetShown(zoneBanner, false);
                }
            }
        }


        private static void ShowBuilderHost(VisualElement element)
        {
            if (element == null)
                return;
            element.style.display = DisplayStyle.Flex;
        }

        // Matches PilotCluster.uss .pilot-cluster (left 4, bottom 4, width 248, height 290).
        // Pilot cluster + minimap geometry from PilotCluster.uss
        private const float PilotLeft = 4f;
        private const float PilotBottom = 4f;
        private const float PilotWidth = 248f;
        private const float PilotHeight = 290f;
        private const float MapStageLeft = 4f;
        private const float MapStageWidth = 200f;
        private const float HazardGapAboveElev = 6f;
        // Minimap center X in panel space: cluster left + stage left + half stage.
        private const float MinimapCenterX = PilotLeft + MapStageLeft + MapStageWidth * 0.5f; // 108

        private VisualElement cachedPilotMapRing;
        private VisualElement cachedPilotMapPlayer;
        private int nextMapRingResolveFrame;

        private void PinBesidePilot()
        {
            // Legacy name - pins just above ELEV, horizontally centered on the red player arrow.
            PinAboveElev();
        }

        private void PinAboveElev()
        {
            if (cluster == null)
                return;

            float left = MinimapCenterX;
            float bottom = PilotBottom + PilotHeight + HazardGapAboveElev;

            // Horizontal: center on the red player arrow (fallback: map ring).
            VisualElement arrow = ResolvePilotMapPlayer();
            if (arrow != null && arrow.panel != null)
            {
                Rect ab = arrow.worldBound;
                if (ab.width > 0.5f && ab.height > 0.5f)
                    left = ab.xMin + ab.width * 0.5f;
            }
            else
            {
                VisualElement ring = ResolvePilotMapRing();
                if (ring != null && ring.panel != null)
                {
                    Rect rb = ring.worldBound;
                    if (rb.width > 1f && rb.height > 1f)
                        left = rb.xMin + rb.width * 0.5f;
                }
            }

            // Vertical: sit just above ELEV.
            Label elev = ResolvePilotElevLabel();
            if (elev != null && elev.panel != null)
            {
                Rect wb = elev.worldBound;
                if (wb.width > 1f && wb.height > 1f)
                {
                    float panelH = elev.panel.visualTree.worldBound.height;
                    if (panelH > 1f)
                    {
                        float elevTopFromBottom = panelH - wb.yMin;
                        bottom = elevTopFromBottom + HazardGapAboveElev;
                    }
                }
            }

            if (!float.IsNaN(lastHazardPinLeft)
                && Mathf.Abs(left - lastHazardPinLeft) < 0.25f
                && Mathf.Abs(bottom - lastHazardPinBottom) < 0.25f)
                return;

            lastHazardPinLeft = left;
            lastHazardPinBottom = bottom;
            cluster.style.left = left;
            cluster.style.right = StyleKeyword.Auto;
            cluster.style.top = StyleKeyword.Auto;
            cluster.style.bottom = bottom;
            cluster.style.marginLeft = 0;
            cluster.style.translate = new Translate(new Length(-50f, LengthUnit.Percent), 0);
            cluster.style.alignItems = Align.Center;
        }

        private Label ResolvePilotElevLabel()
        {
            if (cachedPilotElev != null && cachedPilotElev.panel != null)
                return cachedPilotElev;

            if (Time.frameCount < nextElevResolveFrame)
                return cachedPilotElev;

            nextElevResolveFrame = Time.frameCount + 30;
            VisualElement root = ResolvePilotRoot();
            cachedPilotElev = root != null ? root.Q<Label>("pilot-elev") : null;
            return cachedPilotElev;
        }

        private VisualElement ResolvePilotMapRing()
        {
            if (cachedPilotMapRing != null && cachedPilotMapRing.panel != null)
                return cachedPilotMapRing;

            if (Time.frameCount < nextMapRingResolveFrame)
                return cachedPilotMapRing;

            nextMapRingResolveFrame = Time.frameCount + 30;
            VisualElement root = ResolvePilotRoot();
            cachedPilotMapRing = root != null ? root.Q<VisualElement>("pilot-map-ring") : null;
            return cachedPilotMapRing;
        }

        private VisualElement ResolvePilotMapPlayer()
        {
            if (cachedPilotMapPlayer != null && cachedPilotMapPlayer.panel != null)
                return cachedPilotMapPlayer;

            if (Time.frameCount < nextMapRingResolveFrame)
                return cachedPilotMapPlayer;

            VisualElement root = ResolvePilotRoot();
            cachedPilotMapPlayer = root != null ? root.Q<VisualElement>("pilot-map-player") : null;
            return cachedPilotMapPlayer;
        }

        private static VisualElement ResolvePilotRoot()
        {
            DMUiToolkitPilotCluster pilot = DMUiToolkitPilotCluster.EnsureHost();
            if (pilot == null)
                return null;
            UIDocument doc = pilot.GetComponent<UIDocument>();
            return doc != null ? doc.rootVisualElement : null;
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            // Destroy leftover retired uGUI hosts if still present in a scene/prefab.
            Transform env = DMUiToolkitOverlayDocument.FindNamed("EnvironmentStatusHud")?.transform;
            if (env != null)
                Object.Destroy(env.gameObject);

            ExposureZoneEntryBannerUI banner = FindAnyObjectByType<ExposureZoneEntryBannerUI>(FindObjectsInactive.Include);
            banner?.DismissImmediate();
        }
    }
}
