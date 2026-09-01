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
    /// Matches HotbarExposureGaugeCluster / VerticalHazardExposureGauge live layout.
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
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
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

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool want = DMUiToolkitOverlayDocument.GameplayHudWanted();
            DMUiToolkitOverlayDocument.SetShown(root, want);

            if (!DMUiToolkitHud.IsDriving)
            {
                uguiHidden = false;
            }
            else if (!uguiHidden)
            {
                HideUgui();
                uguiHidden = true;
            }

            if (!want)
            {
                manualPeekTimer = 0f;
                hazardAlpha = 0f;
                hazardAlphaTarget = 0f;
                ApplyHazardAlpha();
                DMUiToolkitOverlayDocument.SetShown(zoneBanner, false);
                return;
            }

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
            cluster = tree.Q<VisualElement>("hazards-cluster");
            hazardPanel = tree.Q<VisualElement>("hazard-panel");
            thermalStatus = tree.Q<Label>("thermal-status");
            thermalValue = tree.Q<Label>("thermal-value");
            thermalTrack = tree.Q<VisualElement>("thermal-track");
            thermalFill = tree.Q<VisualElement>("thermal-fill");
            thermalNeedle = tree.Q<VisualElement>("thermal-needle");
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

            bool hazardNow = !snapshot.DominantHazard.IsClear;
            if (hazardNow && !hasActiveHazard)
            {
                hasActiveHazard = true;
                hazardAlphaTarget = 1f;
            }
            else if (!hazardNow && hasActiveHazard)
            {
                hasActiveHazard = false;
                if (manualPeekTimer <= 0f)
                    hazardAlphaTarget = 0f;
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

            DMUiToolkitOverlayDocument.SetFillPercent(hazardSummaryFill, combined);

            ApplyHazardRow(radSegs, radPct, snapshot.RadiationHazardLevel, RadColor);
            ApplyHazardRow(coldSegs, coldPct, snapshot.ColdHazardLevel, ColdColor);
            ApplyHazardRow(heatSegs, heatPct, snapshot.HeatHazardLevel, HeatColor);
            ApplyHazardRow(sulfurSegs, sulfurPct, snapshot.SulfurHazardLevel, SulfurColor);
            ApplyHazardRow(volcanoSegs, volcanoPct, snapshot.VolcanoHazardLevel, VolcanoColor);
            ApplyHazardRow(shelterSegs, shelterPct, snapshot.IsInShelter ? 1f : 0.08f, ShelterColor);

            PullTicks(snapshot);
            PullCompactExposure();
        }

        private static void ApplyHazardRow(VisualElement segs, Label pct, float level, Color color)
        {
            float n = Mathf.Clamp01(level);
            DMUiToolkitOverlayDocument.ApplySegments(segs, n, color);
            if (pct != null)
                pct.text = $"{Mathf.RoundToInt(n * 100f)}%";
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

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            HotbarExposureGaugeCluster clusterUi = FindAnyObjectByType<HotbarExposureGaugeCluster>(FindObjectsInactive.Include);
            clusterUi?.SetGameplayVisible(false);

            ExposureStatusHud compact = FindAnyObjectByType<ExposureStatusHud>(FindObjectsInactive.Include);
            DMUiToolkitOverlayDocument.HideGameObject(compact != null ? compact.gameObject : null);

            ExposureZoneEntryBannerUI banner = FindAnyObjectByType<ExposureZoneEntryBannerUI>(FindObjectsInactive.Include);
            banner?.DismissImmediate();

            Transform env = DMUiToolkitOverlayDocument.FindNamed("EnvironmentStatusHud")?.transform;
            DMUiToolkitOverlayDocument.HideGameObject(env != null ? env.gameObject : null);
        }
    }
}
