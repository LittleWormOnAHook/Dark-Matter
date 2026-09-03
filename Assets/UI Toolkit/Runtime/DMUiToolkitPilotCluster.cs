using System.Collections.Generic;
using Project.Core;
using Project.Inventory;
using Project.Map;
using Project.Survival;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Lower-right combined minimap / stats prototype. Existing HUD stays live.
    /// Toggle from DMUiToolkitConfig.showPilotCluster. No panel background.
    /// Stays up when tilde hides gameplay HUD.
    /// </summary>
    [DefaultExecutionOrder(-360)]
    [DisallowMultipleComponent]
    public class DMUiToolkitPilotCluster : MonoBehaviour
    {
        private const int MaxPois = 8;
        private const float MapRadiusPx = 84f;
        private const float MarkerRange = 250f;
        private const float CompassFov = 140f;
        private const float CompassTickStep = 15f;

        private static readonly Color EnergyColor = DarkMatterGenesisUiPalette.Gold;
        private static readonly Color StaminaColor = DarkMatterGenesisUiPalette.PositiveGreen;
        private static readonly Color OxygenColor = new Color(0.86f, 0.90f, 0.94f, 1f);

        private static DMUiToolkitPilotCluster instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement cluster;
        private VisualElement mapView;
        private VisualElement mapPlayer;
        private VisualElement poiHost;
        private VisualElement healthFill;
        private VisualElement compassTicks;
        private VisualElement compassDots;
        private VisualElement arcsHost;
        private PerimeterArcs arcs;
        private Label energyValue;
        private Label staminaValue;
        private Label oxygenValue;
        private Label loadValue;
        private Label healthValue;
        private Label elevLabel;
        private Label tempLabel;
        private Label gridLabel;
        private Label zoneLabel;
        private Label compassPoi;
        private bool bound;
        private bool playerArrowBound;
        private MapUI cachedMapUi;
        private InventorySystem cachedInventory;
        private SurvivalStats cachedStats;
        private ExposureController cachedExposure;
        private float nextMapPoiRefresh;
        private float lastCompassHeading = float.NaN;
        private float lastCompassWidth;
        private readonly List<CompassTick> ticks = new List<CompassTick>(24);
        private readonly Dictionary<MapMarker, VisualElement> compassDotLookup = new Dictionary<MapMarker, VisualElement>(16);

        private struct CompassTick
        {
            public float Angle;
            public VisualElement Root;
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

            return DMUiToolkitOverlayDocument.GameplayHudWanted();
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
            mapPlayer = tree.Q<VisualElement>("pilot-map-player");
            poiHost = tree.Q<VisualElement>("pilot-pois");
            healthFill = tree.Q<VisualElement>("pilot-health-fill");
            compassTicks = tree.Q<VisualElement>("pilot-compass-ticks");
            compassDots = tree.Q<VisualElement>("pilot-compass-dots");
            arcsHost = tree.Q<VisualElement>("pilot-arcs");
            energyValue = tree.Q<Label>("pilot-energy-value");
            staminaValue = tree.Q<Label>("pilot-stamina-value");
            oxygenValue = tree.Q<Label>("pilot-o2-value");
            loadValue = tree.Q<Label>("pilot-load-value");
            healthValue = tree.Q<Label>("pilot-health-value");
            elevLabel = tree.Q<Label>("pilot-elev");
            tempLabel = tree.Q<Label>("pilot-temp");
            gridLabel = tree.Q<Label>("pilot-grid");
            zoneLabel = tree.Q<Label>("pilot-zone");
            compassPoi = tree.Q<Label>("pilot-compass-poi");

            EnsureArcs();
            if (!playerArrowBound && mapPlayer != null)
            {
                DMUiToolkitStyle.TrySetSpriteBackground(mapPlayer, MapUiSprites.PlayerArrow, ScaleMode.ScaleToFit);
                mapPlayer.style.backgroundColor = Color.clear;
                playerArrowBound = true;
            }

            BuildCompassTicks();
            bound = root != null;
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

            RefreshBars(stats);
            RefreshLoad(inventory);
            RefreshWorld(mapUi, stats);
            RefreshMap(mapUi);
            RefreshCompass(mapUi);
        }

        private void RefreshBars(SurvivalStats stats)
        {
            float energy = Ratio(stats != null ? stats.CurrentEnergy : 0f, stats != null ? stats.maxEnergy : 1f);
            float stamina = Ratio(stats != null ? stats.CurrentStamina : 0f, stats != null ? stats.maxStamina : 1f);
            float oxygen = stats != null ? stats.GetOxygenNormalized() : 0f;
            float health = Ratio(stats != null ? stats.CurrentHealth : 0f, stats != null ? stats.maxHealth : 1f);

            SetPercent(energyValue, energy);
            SetPercent(staminaValue, stamina);
            SetPercent(oxygenValue, oxygen);
            if (healthValue != null)
                healthValue.text = Mathf.RoundToInt(health * 100f).ToString();

            if (arcs != null)
                arcs.SetFills(energy, stamina, oxygen);

            if (healthFill != null)
                healthFill.style.height = Length.Percent(Mathf.Clamp01(health) * 100f);
        }

        private void RefreshLoad(InventorySystem inventory)
        {
            if (loadValue == null)
                return;

            if (inventory == null || inventory.slots == null)
            {
                loadValue.text = "0%";
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
            loadValue.text = Mathf.RoundToInt(ratio * 100f) + "%";
        }

        private void RefreshWorld(MapUI mapUi, SurvivalStats stats)
        {
            Vector3 pos = Vector3.zero;
            bool hasPos = mapUi != null && mapUi.HasMinimapPlayerPosition;
            if (hasPos)
                pos = mapUi.MinimapPlayerWorldPosition;

            int elev = Mathf.RoundToInt(pos.y);
            if (elevLabel != null)
            {
                string sign = elev >= 0 ? "+" : "";
                elevLabel.text = "ELEV " + sign + elev + " M";
            }

            if (gridLabel != null)
            {
                WorldMapProvider provider = WorldMapProvider.Instance;
                if (hasPos && provider != null)
                {
                    Vector2 grid = provider.WorldToMap01(pos);
                    gridLabel.text = "GRID " + grid.x.ToString("0.00") + "  " + grid.y.ToString("0.00");
                }
                else
                {
                    gridLabel.text = "GRID --";
                }
            }

            float fahrenheit = stats != null ? stats.GetDisplayTemperatureFahrenheit() : 70f;
            if (tempLabel != null)
            {
                tempLabel.text = "TEMP " + ExposureTemperatureDisplay.FormatCelsius(fahrenheit);
                float t = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(fahrenheit);
                tempLabel.style.color = Color.Lerp(
                    ExposureHazardPresentation.ColdColor,
                    ExposureHazardPresentation.HeatColor,
                    t);
            }

            if (zoneLabel != null)
            {
                ExposureController exposure = ResolveExposure(stats);
                string zone = "";
                if (exposure != null)
                {
                    string[] names = exposure.GetActiveZoneDisplayNames();
                    if (names != null && names.Length > 0)
                        zone = names[0].ToUpperInvariant();
                }

                zoneLabel.text = zone;
            }
        }

        private void RefreshMap(MapUI mapUi)
        {
            if (mapView == null)
                return;

            Texture texture = null;
            DMUiToolkitMinimap minimap = DMUiToolkitMinimap.Instance;
            if (minimap != null)
                texture = minimap.ViewTexture;
            if (texture == null && mapUi != null)
                texture = mapUi.MinimapSourceTexture;

            if (texture is RenderTexture rt)
                DMUiToolkitStyle.TrySetRenderTextureBackground(mapView, rt, ScaleMode.ScaleAndCrop);
            else
                DMUiToolkitStyle.TrySetTextureBackground(mapView, texture, ScaleMode.ScaleAndCrop);

            float yaw = mapUi != null ? mapUi.MinimapFacingYaw : 0f;
            DMUiToolkitMenus.SetElementRotate(mapView, yaw);
            if (poiHost != null)
                DMUiToolkitMenus.SetElementRotate(poiHost, yaw);

            RefreshMapPois(mapUi);
        }

        private void RefreshMapPois(MapUI mapUi)
        {
            if (poiHost == null)
                return;

            if (Time.unscaledTime < nextMapPoiRefresh)
                return;

            nextMapPoiRefresh = Time.unscaledTime + 0.25f;
            poiHost.Clear();

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            if (markers == null || markers.Count == 0)
                return;

            Vector2 playerUv = new Vector2(0.5f, 0.5f);
            float uvSpan = 0.25f;
            if (mapUi != null)
            {
                Texture source;
                float facingYaw;
                mapUi.TryGetMinimapViewParams(out source, out playerUv, out uvSpan, out facingYaw);
            }

            WorldMapProvider provider = WorldMapProvider.Instance;
            if (provider == null)
                return;

            int drawn = 0;
            for (int i = 0; i < markers.Count && drawn < MaxPois; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || !marker.ShowOnMinimap || !marker.IsRevealedOnMap)
                    continue;

                Vector2 markerUv = provider.WorldToMap01(marker.WorldPosition);
                Vector2 delta = (markerUv - playerUv) / Mathf.Max(0.0001f, uvSpan);
                if (delta.sqrMagnitude > 1f)
                    continue;

                VisualElement dot = new VisualElement();
                dot.AddToClassList("pilot-poi");
                dot.pickingMode = PickingMode.Ignore;
                Sprite sprite = marker.IconSprite != null ? marker.IconSprite : MapUiSprites.Dot;
                DMUiToolkitStyle.TrySetSpriteBackground(dot, sprite, ScaleMode.ScaleToFit);
                dot.style.backgroundColor = marker.Color;

                float px = MapRadiusPx + delta.x * MapRadiusPx;
                float py = MapRadiusPx - delta.y * MapRadiusPx;
                dot.style.left = px;
                dot.style.top = py;
                poiHost.Add(dot);
                drawn++;
            }
        }

        private void RefreshCompass(MapUI mapUi)
        {
            float heading = mapUi != null ? mapUi.MinimapFacingYaw : 0f;
            float stripWidth = compassTicks != null ? compassTicks.resolvedStyle.width : 320f;
            if (stripWidth < 8f)
                stripWidth = 320f;

            float halfFov = CompassFov * 0.5f;
            float halfWidth = stripWidth * 0.5f;

            bool headingStable = !float.IsNaN(lastCompassHeading)
                && Mathf.Abs(Mathf.DeltaAngle(heading, lastCompassHeading)) < 0.15f
                && Mathf.Approximately(stripWidth, lastCompassWidth);
            if (!headingStable)
            {
                lastCompassHeading = heading;
                lastCompassWidth = stripWidth;
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

            RefreshCompassDots(mapUi, heading, halfFov, halfWidth, stripWidth);
        }

        private void RefreshCompassDots(MapUI mapUi, float heading, float halfFov, float halfWidth, float stripWidth)
        {
            if (compassDots == null)
                return;

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            Vector3 origin = mapUi != null && mapUi.HasMinimapPlayerPosition
                ? mapUi.MinimapPlayerWorldPosition
                : Vector3.zero;
            bool hasPlayer = mapUi != null && mapUi.HasMinimapPlayerPosition;

            var seen = new HashSet<MapMarker>();
            MapMarker focused = null;
            float focusedAbs = 999f;
            float focusedDist = 0f;

            if (hasPlayer && markers != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    MapMarker marker = markers[i];
                    if (marker == null || !marker.ShowOnMinimap || !marker.IsRevealedOnMap)
                        continue;

                    Vector3 toMarker = marker.WorldPosition - origin;
                    toMarker.y = 0f;
                    float distance = toMarker.magnitude;
                    if (distance > MarkerRange)
                        continue;

                    float bearing = Mathf.Atan2(toMarker.x, toMarker.z) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(heading, bearing);
                    if (Mathf.Abs(delta) > halfFov)
                        continue;

                    seen.Add(marker);
                    if (!compassDotLookup.TryGetValue(marker, out VisualElement dot) || dot == null)
                    {
                        dot = new VisualElement { pickingMode = PickingMode.Ignore };
                        dot.usageHints = UsageHints.DynamicTransform;
                        dot.AddToClassList("pilot-compass-dot");
                        compassDots.Add(dot);
                        compassDotLookup[marker] = dot;
                    }

                    if (marker.IconSprite != null)
                    {
                        DMUiToolkitStyle.TrySetSpriteBackground(dot, marker.IconSprite, ScaleMode.ScaleToFit);
                        dot.style.backgroundColor = Color.clear;
                        dot.style.unityBackgroundImageTintColor = marker.Color;
                    }
                    else
                    {
                        DMUiToolkitStyle.ClearBackgroundImage(dot);
                        dot.style.backgroundColor = marker.Color;
                    }

                    float t = 1f - Mathf.Clamp01(Mathf.Abs(delta) / halfFov);
                    float scale = Mathf.Lerp(0.45f, 1.4f, t * t);
                    float size = 8f * scale;
                    float x = (delta / halfFov) * halfWidth;
                    dot.style.display = DisplayStyle.Flex;
                    dot.style.width = size;
                    dot.style.height = size;
                    dot.style.marginLeft = -size * 0.5f;
                    dot.style.marginTop = -size * 0.5f;
                    dot.style.left = halfWidth + x;
                    dot.style.top = Length.Percent(50f);
                    dot.style.opacity = Mathf.Lerp(0.35f, 1f, t);
                    dot.style.borderTopLeftRadius = size * 0.5f;
                    dot.style.borderTopRightRadius = size * 0.5f;
                    dot.style.borderBottomLeftRadius = size * 0.5f;
                    dot.style.borderBottomRightRadius = size * 0.5f;

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
                if (seen.Contains(pair.Key))
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

            if (compassPoi == null)
                return;

            if (focused == null)
            {
                compassPoi.text = "";
                return;
            }

            string name = string.IsNullOrEmpty(focused.Label) ? "POI" : focused.Label;
            compassPoi.text = name + "  " + Mathf.RoundToInt(focusedDist) + "m";
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
            if (stats == null)
                return null;
            cachedExposure = stats.GetComponent<ExposureController>();
            return cachedExposure;
        }

        private MapUI ResolveMapUi()
        {
            if (cachedMapUi != null)
                return cachedMapUi;

            cachedMapUi = FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            return cachedMapUi;
        }

        private static void SetPercent(Label label, float ratio)
        {
            if (label == null)
                return;
            label.text = Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f) + "%";
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

            public void SetFills(float energy01, float stamina01, float oxygen01)
            {
                energy01 = Mathf.Clamp01(energy01);
                stamina01 = Mathf.Clamp01(stamina01);
                oxygen01 = Mathf.Clamp01(oxygen01);
                if (Mathf.Approximately(energy, energy01)
                    && Mathf.Approximately(stamina, stamina01)
                    && Mathf.Approximately(oxygen, oxygen01))
                    return;

                energy = energy01;
                stamina = stamina01;
                oxygen = oxygen01;
                MarkDirtyRepaint();
            }

            private void Paint(MeshGenerationContext ctx)
            {
                Painter2D p = ctx.painter2D;
                Rect r = contentRect;
                if (r.width < 8f || r.height < 8f)
                    return;

                Vector2 center = new Vector2(r.width * 0.5f, r.height * 0.5f);
                float maxR = Mathf.Min(r.width, r.height) * 0.5f;
                p.lineCap = LineCap.Round;
                p.lineJoin = LineJoin.Round;

                DrawArc(p, center, maxR - 6f, 198f, 78f, energy, EnergyColor, 5.5f);
                DrawArc(p, center, maxR - 14f, 264f, 78f, stamina, StaminaColor, 5.5f);
                DrawArc(p, center, maxR - 22f, 32f, 116f, oxygen, OxygenColor, 5f);
            }

            private static void DrawArc(
                Painter2D p,
                Vector2 center,
                float radius,
                float startDeg,
                float sweepDeg,
                float fill01,
                Color color,
                float width)
            {
                if (radius < 4f)
                    return;

                p.lineWidth = width;
                Color track = color;
                track.a = 0.22f;
                p.strokeColor = track;
                p.BeginPath();
                p.Arc(center, radius, Angle.Degrees(startDeg), Angle.Degrees(startDeg + sweepDeg), ArcDirection.Clockwise);
                p.Stroke();

                float filled = Mathf.Max(1.5f, sweepDeg * Mathf.Clamp01(fill01));
                p.strokeColor = color;
                p.BeginPath();
                p.Arc(center, radius, Angle.Degrees(startDeg), Angle.Degrees(startDeg + filled), ArcDirection.Clockwise);
                p.Stroke();
            }
        }
    }
}
