using System.Collections.Generic;
using Project.Core;
using Project.Inventory;
using Project.Map;
using Project.Survival;
using Project.Features.Jetpack;
using Project.Progression;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Lower-left combined minimap / stats cluster. Toggle from
    /// DMUiToolkitConfig.showPilotCluster. No panel background.
    /// Tilde (~) hides this with the rest of the gameplay HUD.
    /// </summary>
    [DefaultExecutionOrder(-360)]
    [DisallowMultipleComponent]
    public class DMUiToolkitPilotCluster : MonoBehaviour
    {
        private const int MaxPois = 32;
        private const float MinimapPoiSizePx = 12f;
        private const float MapRadiusPx = 84f;
        private const float MarkerRange = 250f;
        private const float CompassFov = 140f;
        private const float CompassTickStep = 15f;
        private const float MapPoiRefreshInterval = 0.25f;
        private const float CompassDotsRefreshInterval = 1f / 15f;
        private const float CompassDotsCombatRefreshInterval = 1f / 12f;

        private static readonly Color EnergyColor = DarkMatterGenesisUiPalette.PositiveGreen;
        private static readonly Color StaminaColor = DarkMatterGenesisUiPalette.Gold;
        private static readonly Color OxygenColor = new Color(0.86f, 0.90f, 0.94f, 1f);
        // Vivid crimson health dashes (#E83B3B).
        private static readonly Color HealthColor = new Color(0.910f, 0.231f, 0.231f, 1f);
        // Jetfuel tank dashes (cyan).
        private static readonly Color JetFuelColor = new Color(0.306f, 0.784f, 0.910f, 1f);

        private static Color LockedTint(Color statColor)
        {
            Color locked = Color.Lerp(statColor, Color.black, 0.55f);
            locked.a = 0.92f;
            return locked;
        }

        private const int LockedArcDashCount = 4;
        private const int MaxHealthBonusDashes = 10;

        private static DMUiToolkitPilotCluster instance;

        public static DMUiToolkitPilotCluster Instance => instance;
        public VisualElement ZoneIconsHost => zoneIconsHost;
        public Label ZoneEnteringLabel => zoneEnteringLabel;
        public Label ZoneNameLabel => zoneLabel;

        private UIDocument document;
        private VisualElement root;
        private VisualElement cluster;
        private VisualElement mapView;
        private VisualElement mapFog;
        private VisualElement mapPlayer;
        private VisualElement pilotMapRing;
        private Label pilotCardinalN;
        private Label pilotCardinalE;
        private Label pilotCardinalS;
        private Label pilotCardinalW;
        private VisualElement poiHost;
        private VisualElement healthFill;
        private VisualElement healthTrack;
        private HealthDashes healthDashes;
        private VisualElement jetfuelTrack;
        private VisualElement jetfuelFill;
        private FuelDashes fuelDashes;
        private DMJetpackController cachedJetpack;
        private VisualElement compassTicks;
        private VisualElement compassDots;
        private VisualElement arcsHost;
        private VisualElement tempTrack;
        private PerimeterArcs arcs;
        private ThermalStrip thermal;
        private Label energyValue;
        private Label staminaValue;
        private Label oxygenValue;
        private Label loadValue;
        private Label healthValue;
        private Label elevLabel;
        private Label tempLabel;
        private Label latLabel;
        private Label longLabel;
        private VisualElement zoneIconsHost;
        private Label zoneEnteringLabel;
        private Label zoneLabel;
        private Label compassPoi;
        private bool bound;
        private bool playerArrowBound;
        private MapUI cachedMapUi;
        private bool legacyStartSpanApplied;
        private InventorySystem cachedInventory;
        private SurvivalStats cachedStats;
        private ExposureController cachedExposure;
        private DMUiToolkitMinimap cachedMinimap;
        private JournalPanelUI cachedJournal;
        private int nextJournalResolveFrame;
        private string lastEnergyText;
        private string lastStaminaText;
        private string lastOxygenText;
        private string lastHealthText;
        private string lastLoadText;
        private string lastElevText;
        private string lastLatText;
        private string lastLongText;
        private readonly Dictionary<ExposureZoneKind, ZoneIconVisual> zoneIconVisuals =
            new Dictionary<ExposureZoneKind, ZoneIconVisual>(8);
        private readonly Dictionary<ExposureZoneKind, float> zoneIconWanted =
            new Dictionary<ExposureZoneKind, float>(8);
        private readonly List<ExposureZoneKind> zoneIconRemove = new List<ExposureZoneKind>(4);

        private sealed class ZoneIconVisual
        {
            public VisualElement Host;
            public VisualElement Glow;
            public VisualElement Glyph;
            public float Fade;
            public float Center01;
            public bool Active;
        }
        private string lastTempText;
        private string lastZoneText;
        private string lastCompassPoiText;
        private float lastMapYaw = float.NaN;
        private Vector3 lastMapEuler;
        private float nextMapPoiRefresh;
        private float nextCompassDotsRefresh;
        private int livePoiCount;
        private readonly List<VisualElement> poiPool = new List<VisualElement>(MaxPois);
        private float lastCompassHeading = float.NaN;
        private float lastCompassWidth;
        private float nextHeavyRefresh;
        private float cachedCompassStripWidth = 320f;
        private int nextCompassWidthFrame;
        private readonly List<CompassTick> ticks = new List<CompassTick>(24);
        private readonly Dictionary<MapMarker, VisualElement> compassDotLookup = new Dictionary<MapMarker, VisualElement>(16);
        private readonly HashSet<MapMarker> compassDotsSeen = new HashSet<MapMarker>();
        private bool mapOpacityApplied;

        private struct CompassTick
        {
            public float Angle;
            public VisualElement Root;
        }

        private sealed class CompassDotState
        {
            public float LastDelta;
            public float LastSize;
            public float LastX;
            public float LastOpacity;
            public Sprite LastSprite;
            public Color LastColor;
        }

        private sealed class PoiDotState
        {
            public MapMarker Marker;
            public float LastLeft;
            public float LastTop;
            public float LastOpacity = -1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            DMUiToolkitConfig config = DMUiToolkitConfig.Instance;
            if (config != null && !config.showPilotCluster)
                return;

            EnsureHost();
        }

        public static DMUiToolkitPilotCluster EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.PilotClusterName,
                DMUiToolkitOverlayDocument.PilotClusterUxml,
                DMUiToolkitOverlayDocument.PilotClusterUss,
                DMUiToolkitOverlayDocument.PilotClusterSort);
            if (doc == null)
                return null;

            DMUiToolkitPilotCluster host = doc.GetComponent<DMUiToolkitPilotCluster>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitPilotCluster>();

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
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool want = WantShown();
            DMUiToolkitOverlayDocument.SetShown(root, want);
            if (!want || cluster == null)
                return;

            RefreshPresentation();
        }

        private static bool WantShown()
        {
            DMUiToolkitConfig config = DMUiToolkitConfig.Instance;
            if (config != null && !config.showPilotCluster)
                return false;

            return DMUiToolkitOverlayDocument.GameplayHudWanted()
                && !GameplayHudVisibility.CinematicChromeHidden;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("pilot-root");
            cluster = tree.Q<VisualElement>("pilot-cluster");
            mapView = tree.Q<VisualElement>("pilot-map-view");
            mapFog = tree.Q<VisualElement>("pilot-map-fog");
            mapPlayer = tree.Q<VisualElement>("pilot-map-player");
            poiHost = tree.Q<VisualElement>("pilot-pois");
            pilotMapRing = tree.Q<VisualElement>("pilot-map-ring");
            pilotCardinalN = tree.Q<Label>("pilot-cardinal-n");
            pilotCardinalE = tree.Q<Label>("pilot-cardinal-e");
            pilotCardinalS = tree.Q<Label>("pilot-cardinal-s");
            pilotCardinalW = tree.Q<Label>("pilot-cardinal-w");
            EnsureMapCardinals();
            healthFill = tree.Q<VisualElement>("pilot-health-fill");
            healthTrack = tree.Q<VisualElement>("pilot-health-track");
            jetfuelTrack = tree.Q<VisualElement>("pilot-jetfuel-track");
            jetfuelFill = tree.Q<VisualElement>("pilot-jetfuel-fill");
            compassTicks = tree.Q<VisualElement>("pilot-compass-ticks");
            compassDots = tree.Q<VisualElement>("pilot-compass-dots");
            arcsHost = tree.Q<VisualElement>("pilot-arcs");
            tempTrack = tree.Q<VisualElement>("pilot-temp-track");
            energyValue = tree.Q<Label>("pilot-energy-value");
            staminaValue = tree.Q<Label>("pilot-stamina-value");
            oxygenValue = tree.Q<Label>("pilot-o2-value");
            loadValue = tree.Q<Label>("pilot-load-value");
            healthValue = tree.Q<Label>("pilot-health-value");
            elevLabel = tree.Q<Label>("pilot-elev");
            tempLabel = tree.Q<Label>("pilot-temp");
            latLabel = tree.Q<Label>("pilot-lat");
            longLabel = tree.Q<Label>("pilot-long");
            zoneIconsHost = tree.Q<VisualElement>("pilot-zone-icons");
            zoneEnteringLabel = tree.Q<Label>("pilot-zone-entering");
            zoneLabel = tree.Q<Label>("pilot-zone");
            compassPoi = tree.Q<Label>("pilot-compass-poi");
            if (zoneEnteringLabel != null)
            {
                zoneEnteringLabel.style.color = DarkMatterGenesisUiPalette.Gold;
                DMUiToolkitOverlayDocument.SetShown(zoneEnteringLabel, false);
            }
            if (zoneLabel != null)
                zoneLabel.style.color = DarkMatterGenesisUiPalette.Gold;

            EnsureArcs();
            EnsureHealthDashes();
            EnsureFuelDashes();
            EnsureThermal();
            HideBuilderOnly(tree.Q("pilot-energy-block"));
            HideBuilderOnly(tree.Q("pilot-stamina-block"));
            HideBuilderOnly(tree.Q("pilot-o2-block"));
            HideBuilderOnly(tree.Q("pilot-left-meta"));
            HideBuilderOnly(tree.Q("pilot-right-meta"));
            HideBuilderOnly(tree.Q("pilot-health-label"));
            HideBuilderOnly(healthFill);
            HideBuilderOnly(jetfuelFill);
            if (!playerArrowBound && mapPlayer != null)
            {
                DMUiToolkitStyle.TrySetSpriteBackground(mapPlayer, MapUiSprites.PlayerArrow, ScaleMode.ScaleToFit);
                mapPlayer.style.backgroundColor = Color.clear;
                mapPlayer.style.unityBackgroundImageTintColor = new Color(1f, 0.12f, 0.08f, 1f); // bright red player arrow
                playerArrowBound = true;
            }

            ApplyMinimapPlayerIconRotation();
            BuildCompassTicks();
            bound = root != null;
        }

        private void ApplyMinimapPlayerIconRotation()
        {
            if (mapPlayer == null)
                return;

            WorldMapProvider provider = WorldMapProvider.Instance;
            float iconBase = provider != null ? provider.MinimapPlayerIconBaseDegrees : 0f;
            float size = provider != null ? provider.MinimapPlayerIconSizePixels : 24f;
            DMUiToolkitMenus.ApplyPlayerMapIcon(mapPlayer, size, iconBase, centerWithNegativeMargin: true);
        }


        /// <summary>
        /// Map art displays as authored (north up on texture). Pin N/E/W/S to rotating map edges.
        /// </summary>
        private void EnsureMapCardinals()
        {
            if (mapView == null || pilotCardinalN == null)
                return;

            PlaceMapCardinal(pilotCardinalN, "N", MapCardinalEdge.Top);
            PlaceMapCardinal(pilotCardinalS, "S", MapCardinalEdge.Bottom);
            PlaceMapCardinal(pilotCardinalE, "E", MapCardinalEdge.Right);
            PlaceMapCardinal(pilotCardinalW, "W", MapCardinalEdge.Left);
        }

        private enum MapCardinalEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }

        private void PlaceMapCardinal(Label label, string text, MapCardinalEdge edge)
        {
            if (label == null || mapView == null)
                return;

            if (label.parent != mapView)
            {
                label.RemoveFromHierarchy();
                mapView.Add(label);
            }

            ApplyMapCardinalStyle(label, text, edge);
        }

        private static void ApplyMapCardinalStyle(Label label, string text, MapCardinalEdge edge)
        {
            if (label == null)
                return;

            label.text = text;
            label.style.position = Position.Absolute;
            label.style.color = new Color(1f, 0.12f, 0.08f, 1f);
            label.style.fontSize = 13f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.display = DisplayStyle.Flex;
            label.style.left = StyleKeyword.Auto;
            label.style.right = StyleKeyword.Auto;
            label.style.top = StyleKeyword.Auto;
            label.style.bottom = StyleKeyword.Auto;
            label.style.marginLeft = 0;
            label.style.marginTop = 0;

            switch (edge)
            {
                case MapCardinalEdge.Top:
                    label.style.left = new Length(50f, LengthUnit.Percent);
                    label.style.marginLeft = -6f;
                    label.style.top = -2f;
                    break;
                case MapCardinalEdge.Bottom:
                    label.style.left = new Length(50f, LengthUnit.Percent);
                    label.style.marginLeft = -6f;
                    label.style.bottom = -2f;
                    break;
                case MapCardinalEdge.Right:
                    label.style.right = -2f;
                    label.style.top = new Length(50f, LengthUnit.Percent);
                    label.style.marginTop = -8f;
                    break;
                case MapCardinalEdge.Left:
                    label.style.left = -2f;
                    label.style.top = new Length(50f, LengthUnit.Percent);
                    label.style.marginTop = -8f;
                    break;
            }

            label.BringToFront();
        }

private void EnsureArcs()
        {
            if (arcsHost == null)
                return;

            if (arcs != null && arcs.parent == arcsHost)
                return;

            arcsHost.Clear();
            arcs = new PerimeterArcs();
            arcsHost.Add(arcs);
        }

        private void EnsureHealthDashes()
        {
            if (healthTrack == null)
                return;

            if (healthDashes != null && healthDashes.parent == healthTrack)
                return;

            healthTrack.Clear();
            healthDashes = new HealthDashes();
            healthTrack.Add(healthDashes);
        }

        private void EnsureFuelDashes()
        {
            if (jetfuelTrack == null)
                return;

            if (fuelDashes != null && fuelDashes.parent == jetfuelTrack)
                return;

            fuelDashes = new FuelDashes();
            jetfuelTrack.Add(fuelDashes);
        }


        private void EnsureThermal()
        {
            if (tempTrack == null)
                return;

            if (thermal != null && thermal.parent == tempTrack)
                return;

            tempTrack.Clear();
            thermal = new ThermalStrip();
            tempTrack.Add(thermal);
        }

        private static void HideBuilderOnly(VisualElement element)
        {
            if (element == null)
                return;
            element.style.display = DisplayStyle.None;
        }

        private void BuildCompassTicks()
        {
            if (compassTicks == null)
                return;

            if (ticks.Count > 0 && compassTicks.childCount == ticks.Count)
                return;

            compassTicks.Clear();
            ticks.Clear();
            for (float angle = 0f; angle < 360f; angle += CompassTickStep)
            {
                bool cardinal = Mathf.Approximately(angle % 90f, 0f);
                var tickRoot = new VisualElement { pickingMode = PickingMode.Ignore };
                tickRoot.usageHints = UsageHints.DynamicTransform;
                tickRoot.AddToClassList("pilot-compass-tick");

                var label = new Label(cardinal ? CardinalFromYaw(angle) : Mathf.RoundToInt(angle).ToString())
                {
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList(cardinal ? "pilot-compass-tick-cardinal" : "pilot-compass-tick-label");
                tickRoot.Add(label);

                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList(cardinal ? "pilot-compass-tick-line-cardinal" : "pilot-compass-tick-line");
                tickRoot.Add(line);

                compassTicks.Add(tickRoot);
                ticks.Add(new CompassTick { Angle = angle, Root = tickRoot });
            }
        }

        private void RefreshPresentation()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            SurvivalStats stats = ResolveStats(player);
            InventorySystem inventory = ResolveInventory(player);
            MapUI mapUi = ResolveMapUi();

            if (Time.unscaledTime >= nextHeavyRefresh)
            {
                nextHeavyRefresh = Time.unscaledTime + 0.066f;
                RefreshBars(stats);
                RefreshLoad(inventory);
                RefreshWorld(mapUi, stats);
            }

            RefreshMap(mapUi);
            RefreshCompass(mapUi);
            TickMinimapZoom(mapUi);
        }

        private void RefreshBars(SurvivalStats stats)
        {
            float energy = Ratio(stats != null ? stats.CurrentEnergy : 0f, stats != null ? stats.maxEnergy : 1f);
            float stamina = Ratio(stats != null ? stats.CurrentStamina : 0f, stats != null ? stats.maxStamina : 1f);
            float oxygen = stats != null ? stats.GetOxygenNormalized() : 0f;
            float health = Ratio(stats != null ? stats.CurrentHealth : 0f, stats != null ? stats.maxHealth : 1f);

            SetPercent(energyValue, energy, ref lastEnergyText);
            SetPercent(staminaValue, stamina, ref lastStaminaText);
            SetPercent(oxygenValue, oxygen, ref lastOxygenText);
            if (healthValue != null)
            {
                string ht = Mathf.RoundToInt(health * 100f).ToString();
                if (!string.Equals(ht, lastHealthText, System.StringComparison.Ordinal))
                {
                    lastHealthText = ht;
                    healthValue.text = ht;
                }
            }

            int energyBonus = GetEnergyUpgradeDashes();
            int staminaBonus = GetStaminaUpgradeDashes();
            int oxygenBonus = GetOxygenUpgradeDashes();
            int healthBonus = GetHealthUpgradeDashes();

            if (arcs != null)
                arcs.SetFills(energy, stamina, oxygen, energyBonus, staminaBonus, oxygenBonus);

            if (healthDashes != null)
                healthDashes.SetFill(health, healthBonus);

            float jetFuel = ResolveJetpackFuel();
            if (fuelDashes != null)
                fuelDashes.SetFill(jetFuel);
        }

        private void RefreshLoad(InventorySystem inventory)
        {
            if (loadValue == null)
                return;

            if (inventory == null || inventory.slots == null)
            {
                SetLabelText(loadValue, "0%", ref lastLoadText);
                return;
            }

            int total = inventory.unlockedMainSlots;
            int occupied = 0;
            int limit = Mathf.Min(total, inventory.slots.Count);
            for (int i = 0; i < limit; i++)
            {
                if (!inventory.slots[i].IsEmpty)
                    occupied++;
            }

            float ratio = total > 0 ? occupied / (float)total : 0f;
            SetLabelText(loadValue, Mathf.RoundToInt(ratio * 100f) + "%", ref lastLoadText);
        }

        private void RefreshWorld(MapUI mapUi, SurvivalStats stats)
        {
            ApplyMinimapPlayerIconRotation();

            Vector3 pos = Vector3.zero;
            bool hasPos = mapUi != null && mapUi.HasMinimapPlayerPosition;
            if (hasPos)
                pos = mapUi.MinimapPlayerWorldPosition;

            ResolveExposure(stats);

            int elev = Mathf.RoundToInt(pos.y);
            if (elevLabel != null)
                SetLabelText(elevLabel, "Elevation " + FormatSignedCoord(elev) + " M", ref lastElevText);

            if (hasPos)
            {
                WorldMapProvider map = WorldMapProvider.Instance;
                Vector2 grid = map != null ? map.WorldToGridXz(pos) : new Vector2(pos.x, pos.z);
                // North is world +X, so grid.x is latitude and grid.y (world Z) is longitude.
                SetLabelText(latLabel, "Lat " + FormatSignedCoord(Mathf.RoundToInt(grid.x)), ref lastLatText);
                SetLabelText(longLabel, "Long " + FormatSignedCoord(Mathf.RoundToInt(grid.y)), ref lastLongText);
            }
            else
            {
                SetLabelText(latLabel, "Lat ----", ref lastLatText);
                SetLabelText(longLabel, "Long ----", ref lastLongText);
            }

            RefreshZoneIcons(hasPos ? pos : Vector3.zero);

            float fahrenheit = stats != null ? stats.GetDisplayTemperatureFahrenheit() : 70f;
            float thermal01 = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(fahrenheit);
            if (tempLabel != null)
                SetLabelText(tempLabel, Mathf.RoundToInt(fahrenheit) + "\u00B0F", ref lastTempText);
            if (thermal != null)
                thermal.SetNormalized(thermal01);

            if (zoneLabel != null)
            {
                string zone = "";
                ExposureStatusSnapshot snap = ExposureStatusService.Current;
                if (snap != null && !snap.DominantHazard.IsClear)
                {
                    zone = string.IsNullOrEmpty(snap.DominantHazard.DisplayName)
                        ? ""
                        : snap.DominantHazard.DisplayName.ToUpperInvariant();
                }
                SetLabelText(zoneLabel, zone, ref lastZoneText);
                zoneLabel.style.color = DarkMatterGenesisUiPalette.Gold;
            }
        }

        private void RefreshMap(MapUI mapUi)
        {
            if (mapView == null)
                return;

            if (mapUi != null && !legacyStartSpanApplied)
            {
                mapUi.UitkEnsureLegacyStartSpan();
                legacyStartSpanApplied = true;
            }

            RenderTexture cropped = null;
            DMUiToolkitMinimap minimap = cachedMinimap != null ? cachedMinimap : DMUiToolkitMinimap.Instance;
            if (minimap == null)
                minimap = FindAnyObjectByType<DMUiToolkitMinimap>(FindObjectsInactive.Include);
            if (minimap != null)
                cachedMinimap = minimap;
            if (minimap != null)
                cropped = minimap.EnsureCroppedView(mapUi);

            if (cropped != null)
                DMUiToolkitStyle.TrySetRenderTextureBackground(mapView, cropped, ScaleMode.ScaleAndCrop);
            if (!mapOpacityApplied)
            {
                mapView.style.opacity = 0.40f;
                mapOpacityApplied = true;
            }

            RenderTexture fogRt = minimap != null ? minimap.EnsureFogCroppedView(mapUi) : null;
            bool showFog = fogRt != null;
            if (mapFog != null)
            {
                mapFog.style.display = showFog ? DisplayStyle.Flex : DisplayStyle.None;
                if (showFog)
                    DMUiToolkitStyle.TrySetRenderTextureBackground(mapFog, fogRt, ScaleMode.ScaleAndCrop);
            }

            float mapYaw = mapUi != null ? mapUi.MapCompassYaw : 0f;
            WorldMapProvider map = WorldMapProvider.Instance;
            Vector3 mapEuler = map != null ? map.GetMinimapViewEuler(mapYaw) : new Vector3(0f, 0f, -mapYaw);
            if (float.IsNaN(lastMapYaw)
                || !Mathf.Approximately(lastMapYaw, mapYaw)
                || (lastMapEuler - mapEuler).sqrMagnitude > 0.0001f)
            {
                lastMapYaw = mapYaw;
                lastMapEuler = mapEuler;
                DMUiToolkitMenus.SetElementRotateEuler(mapView, mapEuler);
                if (mapFog != null)
                    DMUiToolkitMenus.SetElementRotateEuler(mapFog, mapEuler);
                if (poiHost != null)
                    DMUiToolkitMenus.SetElementRotateEuler(poiHost, mapEuler);
            }

            RefreshMapPois(mapUi);
        }

        private void RefreshMapPois(MapUI mapUi)
        {
            if (poiHost == null)
                return;

            if (Time.unscaledTime < nextMapPoiRefresh)
                return;

            WorldMapProvider provider = WorldMapProvider.Instance;
            float heading = mapUi != null ? mapUi.MapCompassYaw : 0f;
            Vector3 poiEuler = provider != null
                ? provider.GetMinimapViewEuler(heading)
                : new Vector3(0f, 0f, -heading);
            DMUiToolkitMenus.SetElementRotateEuler(poiHost, poiEuler);

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            if (markers == null || markers.Count == 0 || provider == null)
            {
                RecyclePois(0);
                nextMapPoiRefresh = Time.unscaledTime + MapPoiRefreshInterval;
                return;
            }

            Vector2 playerUv = new Vector2(0.5f, 0.5f);
            float uvSpan = 0.25f;
            if (mapUi != null)
            {
                Texture source;
                float facingYaw;
                mapUi.TryGetMinimapViewParams(out source, out playerUv, out uvSpan, out facingYaw);
            }

            Vector3 playerWorld = mapUi != null && mapUi.HasMinimapPlayerPosition
                ? mapUi.MinimapPlayerWorldPosition
                : Vector3.zero;

            int drawn = 0;
            bool combatPulse = false;
            for (int i = 0; i < markers.Count && drawn < MaxPois; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || !marker.ShouldDrawOnMinimap(playerWorld))
                    continue;

                Vector2 markerUv = provider.WorldToPlayerMap01(marker.WorldPosition);
                Vector2 delta = (markerUv - playerUv) / Mathf.Max(0.0001f, uvSpan);
                if (provider.MinimapFlipHorizontal)
                    delta.x = -delta.x;
                if (provider.MinimapFlipVertical)
                    delta.y = -delta.y;
                if (delta.sqrMagnitude > 1f)
                    continue;

                VisualElement dot = AcquirePoi(drawn);
                PoiDotState state = dot.userData as PoiDotState;
                if (state == null)
                {
                    state = new PoiDotState();
                    dot.userData = state;
                }

                if (state.Marker != marker)
                {
                    state.Marker = marker;
                    DMUiToolkitMenus.ApplyMapPoiDot(dot, marker, MinimapPoiSizePx);
                    state.LastOpacity = -1f;
                }

                float opacity = 1f;
                if (marker.RequiresCombatAggro)
                {
                    opacity = marker.GetMinimapDisplayAlpha();
                    if (marker.IsCombatFlashing)
                        combatPulse = true;
                }

                if (!Mathf.Approximately(state.LastOpacity, opacity))
                {
                    state.LastOpacity = opacity;
                    dot.style.opacity = opacity;
                }

                float px = MapRadiusPx + delta.x * MapRadiusPx;
                float py = MapRadiusPx - delta.y * MapRadiusPx;
                if (!Mathf.Approximately(state.LastLeft, px))
                {
                    state.LastLeft = px;
                    dot.style.left = px;
                }

                if (!Mathf.Approximately(state.LastTop, py))
                {
                    state.LastTop = py;
                    dot.style.top = py;
                }

                drawn++;
            }

            RecyclePois(drawn);
            nextMapPoiRefresh = Time.unscaledTime + (combatPulse ? CompassDotsCombatRefreshInterval : MapPoiRefreshInterval);
        }

        private VisualElement AcquirePoi(int index)
        {
            while (poiPool.Count <= index)
            {
                VisualElement dot = new VisualElement();
                dot.AddToClassList("pilot-poi");
                dot.pickingMode = PickingMode.Ignore;
                poiHost.Add(dot);
                poiPool.Add(dot);
            }

            VisualElement pooled = poiPool[index];
            pooled.style.display = DisplayStyle.Flex;
            return pooled;
        }

        private void RecyclePois(int shown)
        {
            for (int i = shown; i < livePoiCount; i++)
            {
                if (i < poiPool.Count)
                    poiPool[i].style.display = DisplayStyle.None;
            }

            livePoiCount = shown;
        }

        private void RefreshCompass(MapUI mapUi)
        {
            float heading = mapUi != null ? mapUi.MapCompassYaw : 0f;
            if (Time.frameCount >= nextCompassWidthFrame)
            {
                nextCompassWidthFrame = Time.frameCount + 30;
                float stripWidth = compassTicks != null ? compassTicks.resolvedStyle.width : 320f;
                if (stripWidth >= 8f)
                    cachedCompassStripWidth = stripWidth;
            }

            float stripWidthStable = cachedCompassStripWidth;
            float halfFov = CompassFov * 0.5f;
            float halfWidth = stripWidthStable * 0.5f;

            bool headingStable = !float.IsNaN(lastCompassHeading)
                && Mathf.Abs(Mathf.DeltaAngle(heading, lastCompassHeading)) < 0.15f
                && Mathf.Approximately(stripWidthStable, lastCompassWidth);
            if (!headingStable)
            {
                lastCompassHeading = heading;
                lastCompassWidth = stripWidthStable;
                for (int i = 0; i < ticks.Count; i++)
                {
                    CompassTick tick = ticks[i];
                    float delta = Mathf.DeltaAngle(heading, tick.Angle);
                    bool visible = Mathf.Abs(delta) <= halfFov;
                    tick.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    if (!visible)
                        continue;

                    float x = (delta / halfFov) * halfWidth;
                    tick.Root.style.left = halfWidth + x - 18f;
                }
            }

            if (Time.unscaledTime >= nextCompassDotsRefresh || !headingStable)
                RefreshCompassDots(mapUi, heading, halfFov, halfWidth, stripWidthStable);
        }

        private void RefreshCompassDots(MapUI mapUi, float heading, float halfFov, float halfWidth, float stripWidth)
        {
            if (compassDots == null)
                return;

            bool combatPulse = false;
            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            Vector3 origin = mapUi != null && mapUi.HasMinimapPlayerPosition
                ? mapUi.MinimapPlayerWorldPosition
                : Vector3.zero;
            bool hasPlayer = mapUi != null && mapUi.HasMinimapPlayerPosition;

            compassDotsSeen.Clear();
            MapMarker focused = null;
            float focusedAbs = 999f;
            float focusedDist = 0f;

            if (hasPlayer && markers != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    MapMarker marker = markers[i];
                    if (marker == null || !marker.ShouldDrawOnCompass(origin))
                        continue;

                    Vector3 toMarker = marker.WorldPosition - origin;
                    toMarker.y = 0f;
                    float distance = toMarker.magnitude;
                    if (distance > MarkerRange)
                        continue;

                    float bearing = WorldMapProvider.WorldDeltaToDisplayBearing(origin, marker.WorldPosition);
                    float delta = Mathf.DeltaAngle(heading, bearing);
                    if (Mathf.Abs(delta) > halfFov)
                        continue;

                    if (marker.RequiresCombatAggro && marker.IsCombatFlashing)
                        combatPulse = true;

                    compassDotsSeen.Add(marker);
                    if (!compassDotLookup.TryGetValue(marker, out VisualElement dot) || dot == null)
                    {
                        dot = new VisualElement { pickingMode = PickingMode.Ignore };
                        dot.usageHints = UsageHints.DynamicTransform;
                        dot.AddToClassList("pilot-compass-dot");
                        dot.userData = new CompassDotState();
                        compassDots.Add(dot);
                        compassDotLookup[marker] = dot;
                    }

                    if (dot.userData is not CompassDotState dotState)
                    {
                        dotState = new CompassDotState();
                        dot.userData = dotState;
                    }

                    float t = 1f - Mathf.Clamp01(Mathf.Abs(delta) / halfFov);
                    float scale = Mathf.Lerp(0.45f, 1.4f, t * t);
                    float size = 8f * scale;
                    float x = (delta / halfFov) * halfWidth;
                    float opacity = Mathf.Lerp(0.35f, 1f, t);
                    float left = halfWidth + x;

                    if (marker.IconSprite != null)
                    {
                        if (dotState.LastSprite != marker.IconSprite)
                        {
                            dotState.LastSprite = marker.IconSprite;
                            DMUiToolkitStyle.TrySetSpriteBackground(dot, marker.IconSprite, ScaleMode.ScaleToFit);
                            dot.style.backgroundColor = Color.clear;
                        }

                        if (dotState.LastColor != marker.Color)
                        {
                            dotState.LastColor = marker.Color;
                            dot.style.unityBackgroundImageTintColor = marker.Color;
                        }
                    }
                    else
                    {
                        if (dotState.LastSprite != null)
                        {
                            dotState.LastSprite = null;
                            DMUiToolkitStyle.ClearBackgroundImage(dot);
                        }

                        if (dotState.LastColor != marker.Color)
                        {
                            dotState.LastColor = marker.Color;
                            dot.style.backgroundColor = marker.Color;
                        }
                    }

                    dot.style.display = DisplayStyle.Flex;
                    if (!Mathf.Approximately(dotState.LastSize, size))
                    {
                        dotState.LastSize = size;
                        dot.style.width = size;
                        dot.style.height = size;
                        dot.style.marginLeft = -size * 0.5f;
                        dot.style.marginTop = -size * 0.5f;
                        dot.style.borderTopLeftRadius = size * 0.5f;
                        dot.style.borderTopRightRadius = size * 0.5f;
                        dot.style.borderBottomLeftRadius = size * 0.5f;
                        dot.style.borderBottomRightRadius = size * 0.5f;
                    }

                    if (!Mathf.Approximately(dotState.LastX, left))
                    {
                        dotState.LastX = left;
                        dot.style.left = left;
                    }

                    if (!Mathf.Approximately(dotState.LastOpacity, opacity))
                    {
                        dotState.LastOpacity = opacity;
                        dot.style.opacity = opacity;
                    }

                    dotState.LastDelta = delta;

                    if (Mathf.Abs(delta) < focusedAbs)
                    {
                        focusedAbs = Mathf.Abs(delta);
                        focused = marker;
                        focusedDist = distance;
                    }
                }
            }

            List<MapMarker> stale = null;
            foreach (KeyValuePair<MapMarker, VisualElement> pair in compassDotLookup)
            {
                if (compassDotsSeen.Contains(pair.Key))
                    continue;
                stale ??= new List<MapMarker>();
                stale.Add(pair.Key);
                if (pair.Value != null)
                    pair.Value.RemoveFromHierarchy();
            }

            if (stale != null)
            {
                for (int i = 0; i < stale.Count; i++)
                    compassDotLookup.Remove(stale[i]);
            }

            nextCompassDotsRefresh = Time.unscaledTime
                + (combatPulse ? CompassDotsCombatRefreshInterval : CompassDotsRefreshInterval);

            if (compassPoi == null)
                return;

            if (focused == null)
            {
                SetLabelText(compassPoi, "", ref lastCompassPoiText);
                return;
            }

            string name = string.IsNullOrEmpty(focused.Label) ? "POI" : focused.Label;
            SetLabelText(compassPoi, name + "  " + Mathf.RoundToInt(focusedDist) + "m", ref lastCompassPoiText);
        }

        private void TickMinimapZoom(MapUI mapUi)
        {
            if (mapUi == null || MinimapZoomBlocked())
                return;

            bool zoomIn = false;
            bool zoomOut = false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftBracketKey.wasPressedThisFrame)
                    zoomIn = true;
                if (keyboard.rightBracketKey.wasPressedThisFrame)
                    zoomOut = true;
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
                zoomIn = true;
            if (Input.GetKeyDown(KeyCode.RightBracket))
                zoomOut = true;

            if (zoomIn)
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomInMultiplier);
            if (zoomOut)
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomOutMultiplier);
        }

        private bool MinimapZoomBlocked()
        {
            if (MainMenuController.BlocksGameplayHud)
                return true;
            if (DMUiToolkitLoadingOverlay.IsShowing)
                return true;
            if (DMUiToolkitMainMenu.IsVisible)
                return true;
            if (DMUiToolkitMenuPanels.IsAnySubPanelOpen)
                return true;
            if (DMUiToolkitMenus.IsOpen)
                return true;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;

            if (cachedJournal == null && Time.frameCount >= nextJournalResolveFrame)
            {
                nextJournalResolveFrame = Time.frameCount + 30;
                cachedJournal = FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            }

            return cachedJournal != null && cachedJournal.IsOpen;
        }


        private float ResolveJetpackFuel()
        {
            if (cachedJetpack == null)
                cachedJetpack = FindAnyObjectByType<DMJetpackController>(FindObjectsInactive.Exclude);

            return cachedJetpack != null ? cachedJetpack.FuelNormalized : 0f;
        }

        private SurvivalStats ResolveStats(GameObject player)
        {
            if (cachedStats != null)
                return cachedStats;
            if (player == null)
                return null;
            cachedStats = player.GetComponent<SurvivalStats>();
            if (cachedStats == null)
                cachedStats = player.GetComponentInChildren<SurvivalStats>();
            return cachedStats;
        }

        private InventorySystem ResolveInventory(GameObject player)
        {
            if (cachedInventory != null)
                return cachedInventory;
            if (player == null)
                return null;
            cachedInventory = player.GetComponent<InventorySystem>();
            if (cachedInventory == null)
                cachedInventory = player.GetComponentInChildren<InventorySystem>();
            return cachedInventory;
        }

        private ExposureController ResolveExposure(SurvivalStats stats)
        {
            if (cachedExposure != null)
                return cachedExposure;
            if (stats != null)
                cachedExposure = stats.GetComponent<ExposureController>()
                    ?? stats.GetComponentInParent<ExposureController>()
                    ?? stats.GetComponentInChildren<ExposureController>();
            if (cachedExposure == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    cachedExposure = player.GetComponent<ExposureController>()
                        ?? player.GetComponentInChildren<ExposureController>();
            }

            return cachedExposure;
        }

        private MapUI ResolveMapUi()
        {
            if (cachedMapUi != null)
                return cachedMapUi;

            cachedMapUi = FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            return cachedMapUi;
        }


        /// <summary>
        /// Health skill points (Vital Boost / Vital Resilience ranks) → extra vertical dashes, max +10.
        /// </summary>
        private static int GetHealthUpgradeDashes()
        {
            return Mathf.Clamp(PlayerSkillAllocator.GetTotalRank(SkillModifierType.MaxHealthPercent), 0, MaxHealthBonusDashes);
        }

        /// <summary>Energy skill ranks unlock up to 4 locked arc dashes (Endurance / Field Conditioning).</summary>
        private static int GetEnergyUpgradeDashes()
        {
            return Mathf.Clamp(PlayerSkillAllocator.GetTotalRank(SkillModifierType.MaxEnergyPercent), 0, LockedArcDashCount);
        }

        /// <summary>Stamina skill ranks unlock up to 4 locked arc dashes (Stamina Core / Survivor's Edge).</summary>
        private static int GetStaminaUpgradeDashes()
        {
            return Mathf.Clamp(PlayerSkillAllocator.GetTotalRank(SkillModifierType.MaxStaminaPercent), 0, LockedArcDashCount);
        }

        /// <summary>Oxygen skill ranks unlock up to 4 locked arc dashes (Lung Capacity).</summary>
        private static int GetOxygenUpgradeDashes()
        {
            return Mathf.Clamp(PlayerSkillAllocator.GetTotalRank(SkillModifierType.MaxOxygenPercent), 0, LockedArcDashCount);
        }


        private static string FormatSignedCoord(int value)
        {
            return value.ToString("+0;-0;+0");
        }

        private void RefreshZoneIcons(Vector3 playerWorld)
        {
            if (zoneIconsHost == null)
                return;

            if (zoneIconsHost.childCount > zoneIconVisuals.Count)
            {
                for (int i = zoneIconsHost.childCount - 1; i >= 0; i--)
                {
                    VisualElement child = zoneIconsHost[i];
                    bool known = false;
                    foreach (KeyValuePair<ExposureZoneKind, ZoneIconVisual> pair in zoneIconVisuals)
                    {
                        if (pair.Value != null && pair.Value.Host == child)
                        {
                            known = true;
                            break;
                        }
                    }
                    if (!known)
                        child.RemoveFromHierarchy();
                }
            }

            Dictionary<ExposureZoneKind, float> wanted = null;
            ExposureReceiver receiver = cachedExposure != null
                ? cachedExposure
                : ResolveExposure(null);
            if (receiver == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    receiver = player.GetComponent<ExposureReceiver>()
                        ?? player.GetComponentInChildren<ExposureReceiver>();
            }

            if (receiver != null)
            {
                IReadOnlyList<ExposureZoneVolume> zones = receiver.ActiveZones;
                if (zones != null)
                {
                    for (int i = 0; i < zones.Count; i++)
                    {
                        ExposureZoneVolume zone = zones[i];
                        ExposureZoneKind kind = DMZoneHudIcons.ResolveKind(zone);
                        if (kind == ExposureZoneKind.Custom)
                            continue;

                        float center01 = zone != null ? zone.EvaluateCenter01(playerWorld) : 0f;
                        if (wanted == null)
                            wanted = new Dictionary<ExposureZoneKind, float>(8);
                        if (!wanted.TryGetValue(kind, out float existing) || center01 > existing)
                            wanted[kind] = center01;
                    }
                }
            }

            ExposureStatusSnapshot snap = ExposureStatusService.Current;
            if (snap != null && !snap.DominantHazard.IsClear && snap.DominantHazard.Kind != ExposureZoneKind.Custom)
            {
                if (wanted == null)
                    wanted = new Dictionary<ExposureZoneKind, float>(8);
                ExposureZoneKind snapKind = snap.DominantHazard.Kind;
                if (!wanted.ContainsKey(snapKind))
                    wanted[snapKind] = Mathf.Clamp01(snap.DominantHazard.Severity);
            }

            foreach (KeyValuePair<ExposureZoneKind, ZoneIconVisual> pair in zoneIconVisuals)
                pair.Value.Active = false;

            if (wanted != null)
            {
                foreach (KeyValuePair<ExposureZoneKind, float> pair in wanted)
                {
                    ZoneIconVisual visual = EnsureZoneIcon(pair.Key);
                    if (visual == null)
                        continue;
                    visual.Active = true;
                    visual.Center01 = pair.Value;
                }
            }

            float fadeStep = Time.unscaledDeltaTime / 0.35f;
            zoneIconRemove.Clear();
            foreach (KeyValuePair<ExposureZoneKind, ZoneIconVisual> pair in zoneIconVisuals)
            {
                ZoneIconVisual visual = pair.Value;
                float target = visual.Active ? 1f : 0f;
                visual.Fade = Mathf.MoveTowards(visual.Fade, target, fadeStep);
                if (!visual.Active && visual.Fade <= 0.001f)
                {
                    if (visual.Host != null)
                        visual.Host.RemoveFromHierarchy();
                    zoneIconRemove.Add(pair.Key);
                    continue;
                }

                if (visual.Host != null)
                    visual.Host.style.visibility = Visibility.Visible;

                float hz = Mathf.Lerp(0.5f, 2f, visual.Center01);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
                float alpha = visual.Fade * (0.42f + 0.58f * pulse);
                if (visual.Glyph != null)
                    visual.Glyph.style.opacity = alpha;
                if (visual.Glow != null)
                    visual.Glow.style.opacity = alpha * 0.45f;
            }

            for (int i = 0; i < zoneIconRemove.Count; i++)
                zoneIconVisuals.Remove(zoneIconRemove[i]);
        }

        private ZoneIconVisual EnsureZoneIcon(ExposureZoneKind kind)
        {
            if (zoneIconVisuals.TryGetValue(kind, out ZoneIconVisual existing) && existing.Host != null)
            {
                if (existing.Host.parent != zoneIconsHost && zoneIconsHost != null)
                    zoneIconsHost.Add(existing.Host);
                return existing;
            }

            Texture2D texture = DMZoneHudIcons.ForKind(kind);
            if (texture == null || zoneIconsHost == null)
                return null;

            VisualElement host = new VisualElement();
            host.AddToClassList("pilot-zone-icon");
            host.pickingMode = PickingMode.Ignore;

            VisualElement glow = new VisualElement();
            glow.AddToClassList("pilot-zone-icon-glow");
            glow.pickingMode = PickingMode.Ignore;
            DMUiToolkitStyle.TrySetTextureBackground(glow, texture, ScaleMode.ScaleToFit);
            glow.style.unityBackgroundImageTintColor = DMZoneHudIcons.EmissionTint(kind);

            VisualElement glyph = new VisualElement();
            glyph.AddToClassList("pilot-zone-icon");
            glyph.pickingMode = PickingMode.Ignore;
            glyph.style.position = Position.Absolute;
            glyph.style.left = 0;
            glyph.style.top = 0;
            glyph.style.marginLeft = 0;
            glyph.style.marginRight = 0;
            DMUiToolkitStyle.TrySetTextureBackground(glyph, texture, ScaleMode.ScaleToFit);

            host.Add(glow);
            host.Add(glyph);
            zoneIconsHost.Add(host);

            ZoneIconVisual visual = new ZoneIconVisual
            {
                Host = host,
                Glow = glow,
                Glyph = glyph
            };
            zoneIconVisuals[kind] = visual;
            return visual;
        }

        private static void SetPercent(Label label, float ratio, ref string lastText)
        {
            if (label == null)
                return;
            string text = Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f) + "%";
            if (string.Equals(text, lastText, System.StringComparison.Ordinal))
                return;
            lastText = text;
            label.text = text;
        }

        private static void SetLabelText(Label label, string text, ref string lastText)
        {
            if (label == null)
                return;
            if (string.Equals(text, lastText, System.StringComparison.Ordinal))
                return;
            lastText = text;
            label.text = text;
        }

        private static float Ratio(float current, float max)
        {
            if (max <= 0f)
                return 0f;
            return Mathf.Clamp01(current / max);
        }

        private static string CardinalFromYaw(float yaw)
        {
            int idx = Mathf.RoundToInt(NormalizeDegrees(yaw) / 45f) % 8;
            if (idx < 0)
                idx += 8;
            switch (idx)
            {
                case 0: return "N";
                case 1: return "NE";
                case 2: return "E";
                case 3: return "SE";
                case 4: return "S";
                case 5: return "SW";
                case 6: return "W";
                default: return "NW";
            }
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private sealed class PerimeterArcs : VisualElement
        {
            private float energy = 1f;
            private float stamina = 1f;
            private float oxygen = 1f;
            private int energyBonus;
            private int staminaBonus;
            private int oxygenBonus;

            public PerimeterArcs()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0;
                style.top = 0;
                style.right = 0;
                style.bottom = 0;
                generateVisualContent += Paint;
            }

            public void SetFills(
                float energy01,
                float stamina01,
                float oxygen01,
                int energyUnlockedBonus,
                int staminaUnlockedBonus,
                int oxygenUnlockedBonus)
            {
                energy01 = Mathf.Clamp01(energy01);
                stamina01 = Mathf.Clamp01(stamina01);
                oxygen01 = Mathf.Clamp01(oxygen01);
                energyUnlockedBonus = Mathf.Clamp(energyUnlockedBonus, 0, LockedArcDashCount);
                staminaUnlockedBonus = Mathf.Clamp(staminaUnlockedBonus, 0, LockedArcDashCount);
                oxygenUnlockedBonus = Mathf.Clamp(oxygenUnlockedBonus, 0, LockedArcDashCount);
                if (Mathf.Approximately(energy, energy01)
                    && Mathf.Approximately(stamina, stamina01)
                    && Mathf.Approximately(oxygen, oxygen01)
                    && energyBonus == energyUnlockedBonus
                    && staminaBonus == staminaUnlockedBonus
                    && oxygenBonus == oxygenUnlockedBonus)
                    return;

                energy = energy01;
                stamina = stamina01;
                oxygen = oxygen01;
                energyBonus = energyUnlockedBonus;
                staminaBonus = staminaUnlockedBonus;
                oxygenBonus = oxygenUnlockedBonus;
                MarkDirtyRepaint();
            }

            private void Paint(MeshGenerationContext ctx)
            {
                Painter2D p = ctx.painter2D;
                Rect r = contentRect;
                if (r.width < 8f || r.height < 8f)
                    return;

                Vector2 center = new Vector2(r.width * 0.5f, r.height * 0.5f);
                // Sit just outside the 168px map ring (inset 16 in a 200px stage).
                float radius = Mathf.Min(r.width, r.height) * 0.5f - 8f;
                p.lineJoin = LineJoin.Miter;

                // 0deg = 3 o'clock, Y-down, clockwise. Energy top-left, oxygen top-right
                // (same 74deg size), stamina bottom spanning both lower quarters.
                DrawDashedArc(p, center, radius, 188f, 74f, energy, EnergyColor, 6f, energyBonus);
                DrawDashedArc(p, center, radius, 278f, 74f, oxygen, OxygenColor, 6f, oxygenBonus);
                DrawDashedArc(p, center, radius, 22f, 136f, stamina, StaminaColor, 6f, staminaBonus);
            }

            private static void DrawDashedArc(
                Painter2D p,
                Vector2 center,
                float radius,
                float startDeg,
                float sweepDeg,
                float fill01,
                Color color,
                float width,
                int unlockedBonus)
            {
                if (radius < 4f || sweepDeg < 2f)
                    return;

                const float DashSweep = 3.35f;
                const float DashGap = 2.05f;

                Color rail = color;
                rail.a = 0.28f;
                p.lineCap = LineCap.Butt;
                p.lineWidth = 1.1f;
                p.strokeColor = rail;
                p.BeginPath();
                p.Arc(center, radius - width * 0.55f, Angle.Degrees(startDeg), Angle.Degrees(startDeg + sweepDeg), ArcDirection.Clockwise);
                p.Stroke();
                p.BeginPath();
                p.Arc(center, radius + width * 0.55f, Angle.Degrees(startDeg), Angle.Degrees(startDeg + sweepDeg), ArcDirection.Clockwise);
                p.Stroke();

                float pitch = DashSweep + DashGap;
                int total = Mathf.Max(LockedArcDashCount + 1, Mathf.FloorToInt((sweepDeg + 0.01f) / pitch));
                int baseDashCount = Mathf.Max(1, total - LockedArcDashCount);
                int lockedDashCount = LockedArcDashCount;
                unlockedBonus = Mathf.Clamp(unlockedBonus, 0, lockedDashCount);
                int unlockedCount = baseDashCount + unlockedBonus;
                total = baseDashCount + lockedDashCount;

                float used = total * DashSweep + (total - 1) * DashGap;
                float pad = Mathf.Max(0f, (sweepDeg - used) * 0.5f);
                // Fill maps across unlocked capacity only (RPG: locked bank is not usable yet).
                float filledDashes = unlockedCount * Mathf.Clamp01(fill01);

                p.lineWidth = width;
                for (int i = 0; i < total; i++)
                {
                    float a0 = startDeg + pad + i * pitch;
                    float a1 = a0 + DashSweep;
                    Color dash;
                    if (i >= unlockedCount)
                    {
                        dash = LockedTint(color);
                    }
                    else
                    {
                        dash = color;
                        if (i + 0.5f > filledDashes)
                            dash.a = 0.22f;
                    }

                    p.strokeColor = dash;
                    p.BeginPath();
                    p.Arc(center, radius, Angle.Degrees(a0), Angle.Degrees(a1), ArcDirection.Clockwise);
                    p.Stroke();
                }
            }
        }

        /// <summary>
        /// Vertical dashed health bar. Same dash language as perimeter arcs; grows upward with health skills.
        /// </summary>

        private sealed class FuelDashes : VisualElement
        {
            private float fill = 1f;

            public FuelDashes()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0;
                style.top = 0;
                style.right = 0;
                style.bottom = 0;
                generateVisualContent += Paint;
            }

            public void SetFill(float fill01)
            {
                fill01 = Mathf.Clamp01(fill01);
                if (Mathf.Approximately(fill, fill01))
                    return;
                fill = fill01;
                MarkDirtyRepaint();
            }

            private void Paint(MeshGenerationContext ctx)
            {
                Painter2D p = ctx.painter2D;
                Rect r = contentRect;
                if (r.width < 8f || r.height < 2f)
                    return;

                // Horizontal dashes matching health dash density.
                const float DashW = 5.5f;
                const float GapW = 3.2f;
                float pitch = DashW + GapW;
                int total = Mathf.Max(1, Mathf.FloorToInt((r.width + 0.01f) / pitch));
                float used = total * DashW + (total - 1) * GapW;
                float padLeft = Mathf.Max(1f, (r.width - used) * 0.5f);
                float filledDashes = total * Mathf.Clamp01(fill);

                float y0 = 1f;
                float y1 = r.height - 1f;
                if (y1 <= y0)
                {
                    y0 = 0f;
                    y1 = r.height;
                }

                for (int i = 0; i < total; i++)
                {
                    float x0 = padLeft + i * pitch;
                    float x1 = x0 + DashW;
                    Color dash = JetFuelColor;
                    if (i + 0.5f > filledDashes)
                        dash.a = 0.22f;

                    p.fillColor = dash;
                    p.BeginPath();
                    p.MoveTo(new Vector2(x0, y0));
                    p.LineTo(new Vector2(x1, y0));
                    p.LineTo(new Vector2(x1, y1));
                    p.LineTo(new Vector2(x0, y1));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }

        private sealed class HealthDashes : VisualElement
        {
            private float fill = 1f;
            private int bonusDashes;

            public HealthDashes()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0;
                style.top = 0;
                style.right = 0;
                style.bottom = 0;
                generateVisualContent += Paint;
            }

            public void SetFill(float fill01, int healthBonusDashes)
            {
                fill01 = Mathf.Clamp01(fill01);
                healthBonusDashes = Mathf.Clamp(healthBonusDashes, 0, MaxHealthBonusDashes);
                if (Mathf.Approximately(fill, fill01) && bonusDashes == healthBonusDashes)
                    return;

                fill = fill01;
                bonusDashes = healthBonusDashes;
                MarkDirtyRepaint();
            }

            private void Paint(MeshGenerationContext ctx)
            {
                Painter2D p = ctx.painter2D;
                Rect r = contentRect;
                if (r.width < 2f || r.height < 8f)
                    return;

                // Match arc visual density (~5.5px dash / ~3.2px gap).
                const float DashH = 5.5f;
                const float GapH = 3.2f;
                float pitch = DashH + GapH;
                int maxTotal = Mathf.Max(1, Mathf.FloorToInt((r.height + 0.01f) / pitch));
                int baseDashCount = Mathf.Max(1, maxTotal - MaxHealthBonusDashes);
                int unlocked = baseDashCount + Mathf.Clamp(bonusDashes, 0, MaxHealthBonusDashes);
                unlocked = Mathf.Min(unlocked, maxTotal);
                int total = maxTotal;

                float used = total * DashH + (total - 1) * GapH;
                float padBottom = Mathf.Max(1f, (r.height - used) * 0.5f);
                float filledDashes = unlocked * Mathf.Clamp01(fill);
                Color locked = LockedTint(HealthColor);

                float x0 = 1f;
                float x1 = r.width - 1f;
                if (x1 <= x0)
                {
                    x0 = 0f;
                    x1 = r.width;
                }

                for (int i = 0; i < total; i++)
                {
                    float y1 = r.height - padBottom - i * pitch;
                    float y0 = y1 - DashH;
                    if (y0 < 0f)
                        break;

                    Color dash;
                    if (i >= unlocked)
                    {
                        dash = locked;
                    }
                    else
                    {
                        dash = HealthColor;
                        if (i + 0.5f > filledDashes)
                            dash.a = 0.22f;
                    }

                    p.fillColor = dash;
                    p.BeginPath();
                    p.MoveTo(new Vector2(x0, y0));
                    p.LineTo(new Vector2(x1, y0));
                    p.LineTo(new Vector2(x1, y1));
                    p.LineTo(new Vector2(x0, y1));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }

        private sealed class ThermalStrip : VisualElement
        {
            private float normalized = 0.5f;

            public ThermalStrip()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0;
                style.top = 0;
                style.right = 0;
                style.bottom = 0;
                generateVisualContent += Paint;
            }

            public void SetNormalized(float value)
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(normalized, value))
                    return;
                normalized = value;
                MarkDirtyRepaint();
            }

            private void Paint(MeshGenerationContext ctx)
            {
                Painter2D p = ctx.painter2D;
                Rect r = contentRect;
                if (r.width < 8f || r.height < 2f)
                    return;

                const int slices = 28;
                float sliceW = r.width / slices;
                for (int i = 0; i < slices; i++)
                {
                    float t = slices == 1 ? 0f : i / (float)(slices - 1);
                    // Darker blue on the cold (left) end, warming to heat on the right.
                    Color coldDark = new Color(0.08f, 0.22f, 0.55f, 1f);
                    Color cold = new Color(0.18f, 0.48f, 0.82f, 1f);
                    Color mid = new Color(0.42f, 0.78f, 0.48f, 1f);
                    Color warm = new Color(0.95f, 0.72f, 0.18f, 1f);
                    Color hot = ExposureHazardPresentation.HeatColor;
                    Color c;
                    if (t < 0.25f) c = Color.Lerp(coldDark, cold, t / 0.25f);
                    else if (t < 0.5f) c = Color.Lerp(cold, mid, (t - 0.25f) / 0.25f);
                    else if (t < 0.75f) c = Color.Lerp(mid, warm, (t - 0.5f) / 0.25f);
                    else c = Color.Lerp(warm, hot, (t - 0.75f) / 0.25f);
                    p.fillColor = c;
                    float x = i * sliceW;
                    p.BeginPath();
                    p.MoveTo(new Vector2(x, 0f));
                    p.LineTo(new Vector2(x + sliceW + 0.5f, 0f));
                    p.LineTo(new Vector2(x + sliceW + 0.5f, r.height));
                    p.LineTo(new Vector2(x, r.height));
                    p.ClosePath();
                    p.Fill();
                }

                float nx = Mathf.Lerp(2f, r.width - 2f, normalized);
                // Bright red circle handle — slightly thicker than the temp strip.
                float radius = r.height * 0.5f + 2.5f;
                p.fillColor = new Color(1f, 0.12f, 0.08f, 1f);
                p.BeginPath();
                p.Arc(new Vector2(nx, r.height * 0.5f), radius, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
            }
        }

    }
}

// fix-stamp 0144
