using System.Collections.Generic;
using Project.Core;
using Project.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit host for the live circular minimap and the compass strip stacked under it.
    /// Reuses MapUI camera/texture/marker data. Same UIDocument / Panel Settings as UITK_Hud.
    /// Dual-run: hides uGUI MinimapPanel / CompassHud / InfoPanel while this drives.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public class DMUiToolkitMinimap : MonoBehaviour
    {
        public const string LogStamp = "DMUiToolkit 0901-finish";

        private const float TickIntervalDegrees = 15f;
        private const float FieldOfViewDegrees = 140f;
        private const float MaxMarkerRange = 250f;
        private const int MaxVisibleMarkers = 10;
        private const int ViewRtSize = 256;
        private const float MinimapRefreshInterval = 0.05f;
        private const float MarkerRefreshInterval = 0.25f;

        private static DMUiToolkitMinimap instance;
        private static bool stamped;
        private static bool warnedMissingTexture;

        private UIDocument document;
        private VisualElement minimapHost;
        private VisualElement minimapFrame;
        private VisualElement minimapView;
        private VisualElement minimapPlayer;
        private VisualElement minimapFog;
        private Button minimapZoomIn;
        private Button minimapZoomOut;
        private Button minimapScan;
        private RenderTexture fogViewRt;
        private VisualElement compassHost;
        private VisualElement compassStrip;
        private VisualElement compassTicks;
        private VisualElement compassMarkers;
        private VisualElement compassPointer;
        private Label compassHeading;
        private Label minimapInfo;
        private bool bound;
        private bool uguiHidden;
        private bool hostsVisible = true;
        private float nextViewRefreshTime;
        private float nextMarkerRefreshTime;
        private RenderTexture viewRt;
        private Texture lastBlitSource;
        private Vector2 lastBlitUv;
        private float lastBlitSpan;
        private MapUI cachedMapUi;
        private global::UnityEngine.CanvasGroup hiddenMinimapGroup;
        private GameObject hiddenMinimapRoot;
        private GameObject hiddenCompass;
        private GameObject hiddenInfoPanel;
        private readonly List<CompassTick> ticks = new List<CompassTick>();
        private readonly Dictionary<MapMarker, CompassMarker> markerIcons = new Dictionary<MapMarker, CompassMarker>();

        private struct CompassTick
        {
            public float AngleDegrees;
            public VisualElement Root;
            public Label Label;
        }

        private struct CompassMarker
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Distance;
        }

        public static DMUiToolkitMinimap Instance => instance;

        public static bool IsDriving
        {
            get
            {
                if (!DMUiToolkitConfig.IsEnabled)
                    return false;
                if (!DMUiToolkitBootstrap.IsRootActive)
                    return false;
                if (!GameSession.HasStarted)
                    return false;
                return instance != null && instance.isActiveAndEnabled && instance.bound;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            stamped = false;
            warnedMissingTexture = false;
        }

        public static void Bind(UIDocument hudDocument)
        {
            if (hudDocument == null)
                return;

            DMUiToolkitMinimap minimap = hudDocument.GetComponent<DMUiToolkitMinimap>();
            if (minimap == null)
                minimap = hudDocument.gameObject.AddComponent<DMUiToolkitMinimap>();

            minimap.document = hudDocument;
            minimap.BindTree();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

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
            RestoreUguiCounterparts();
            if (instance == this)
                bound = false;
        }

        private void OnDestroy()
        {
            RestoreUguiCounterparts();
            ReleaseViewRt();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            RefreshPresentation();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            minimapHost = root.Q<VisualElement>("minimap");
            minimapFrame = root.Q<VisualElement>("minimap-frame");
            minimapView = root.Q<VisualElement>("minimap-view");
            minimapPlayer = root.Q<VisualElement>("minimap-player");
            minimapFog = root.Q<VisualElement>("minimap-fog");
            minimapZoomIn = root.Q<Button>("minimap-zoom-in");
            minimapZoomOut = root.Q<Button>("minimap-zoom-out");
            minimapScan = root.Q<Button>("minimap-scan");
            WireMinimapButtons();
            HidePopupAuthoringCopies(root);
            compassHost = root.Q<VisualElement>("compass");
            compassStrip = root.Q<VisualElement>("compass-strip");
            compassTicks = root.Q<VisualElement>("compass-ticks");
            compassMarkers = root.Q<VisualElement>("compass-markers");
            compassPointer = root.Q<VisualElement>("compass-pointer");
            compassHeading = root.Q<Label>("compass-heading");
            minimapInfo = root.Q<Label>("minimap-info");

            EnsureCompassChrome();
            ApplyAuthoredPlaceholders();

            bound = minimapHost != null && minimapView != null && compassHost != null;
            if (bound && !stamped)
            {
                stamped = true;
                Debug.Log(LogStamp + " hosts bound on UITK_Hud (RT blit of MapUI texture)");
            }

            RefreshPresentation();
        }

        private void EnsureCompassChrome()
        {
            if (compassHost == null)
                return;

            if (compassStrip == null)
            {
                compassStrip = new VisualElement { name = "compass-strip", pickingMode = PickingMode.Ignore };
                compassStrip.AddToClassList("dmg-hud-compass-strip");
                compassHost.Insert(0, compassStrip);
            }

            if (compassTicks == null)
            {
                compassTicks = new VisualElement { name = "compass-ticks", pickingMode = PickingMode.Ignore };
                compassTicks.AddToClassList("dmg-hud-compass-ticks");
                compassStrip.Add(compassTicks);
            }

            if (compassMarkers == null)
            {
                compassMarkers = new VisualElement { name = "compass-markers", pickingMode = PickingMode.Ignore };
                compassMarkers.AddToClassList("dmg-hud-compass-markers");
                compassStrip.Add(compassMarkers);
            }

            if (ticks.Count == 0)
                BuildTicks();
        }

        private void ApplyAuthoredPlaceholders()
        {
            if (minimapFrame != null && MapUiSprites.HudCircleRing != null)
            {
                minimapFrame.style.backgroundImage = new StyleBackground(Background.FromSprite(MapUiSprites.HudCircleRing));
                minimapFrame.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            if (minimapPlayer != null && MapUiSprites.PlayerArrow != null)
            {
                minimapPlayer.style.backgroundImage = new StyleBackground(Background.FromSprite(MapUiSprites.PlayerArrow));
                minimapPlayer.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                minimapPlayer.style.backgroundColor = Color.clear;
            }

            if (compassPointer != null && MapUiSprites.PlayerArrow != null)
            {
                compassPointer.style.backgroundImage = new StyleBackground(Background.FromSprite(MapUiSprites.PlayerArrow));
                compassPointer.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
        }

        private void RefreshPresentation()
        {
            MapUI mapUi = ResolveMapUi();
            bool hudLive = DMUiToolkitConfig.IsEnabled
                && DMUiToolkitBootstrap.IsRootActive
                && GameSession.HasStarted
                && !MainMenuController.BlocksGameplayHud
                && !DMUiToolkitLoadingOverlay.IsShowing;

            bool mapOn = hudLive && mapUi != null && mapUi.ShouldPresentMinimap;
            bool drive = bound && hudLive && mapOn;

            SetHostVisible(drive);

            if (drive)
            {
                HideUguiCounterparts(mapUi);
                if (Time.unscaledTime >= nextViewRefreshTime)
                {
                    nextViewRefreshTime = Time.unscaledTime + MinimapRefreshInterval;
                    BindMinimapView(mapUi);
                    PullInfo(mapUi);
                }

                RefreshCompassHeading(mapUi);
                if (Time.unscaledTime >= nextMarkerRefreshTime)
                {
                    nextMarkerRefreshTime = Time.unscaledTime + MarkerRefreshInterval;
                    RefreshCompassMarkers(mapUi);
                }
            }
            else
            {
                RestoreUguiCounterparts();
            }
        }

        private void SetHostVisible(bool visible)
        {
            if (hostsVisible == visible)
            {
                if (minimapHost != null)
                    minimapHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                return;
            }

            hostsVisible = visible;
            if (minimapHost != null)
                minimapHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (compassHost != null && compassHost != minimapHost)
                compassHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private MapUI ResolveMapUi()
        {
            if (cachedMapUi != null)
                return cachedMapUi;

            cachedMapUi = FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            return cachedMapUi;
        }

        private void BindMinimapView(MapUI mapUi)
        {
            if (minimapView == null || mapUi == null)
                return;

            if (!mapUi.TryGetMinimapViewParams(out Texture source, out Vector2 playerUv, out float uvSpan, out float facingYaw))
            {
                if (!warnedMissingTexture)
                {
                    warnedMissingTexture = true;
                    Debug.LogWarning(LogStamp + " MapUI map texture not ready ? keeping Builder placeholder");
                }

                return;
            }

            RenderTexture rt = EnsureViewRt();
            bool dirty = source != lastBlitSource
                || (playerUv - lastBlitUv).sqrMagnitude > 0.00000025f
                || !Mathf.Approximately(uvSpan, lastBlitSpan);

            if (dirty)
            {
                BlitCrop(source, rt, playerUv, uvSpan);
                lastBlitSource = source;
                lastBlitUv = playerUv;
                lastBlitSpan = uvSpan;
            }

            minimapView.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            minimapView.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            DMUiToolkitMenus.SetElementRotate(minimapView, facingYaw);
            BindMinimapFog(source, playerUv, uvSpan);
        }

        private void WireMinimapButtons()
        {
            if (minimapZoomIn != null)
            {
                minimapZoomIn.clicked -= OnMinimapZoomIn;
                minimapZoomIn.clicked += OnMinimapZoomIn;
                minimapZoomIn.pickingMode = PickingMode.Position;
            }

            if (minimapZoomOut != null)
            {
                minimapZoomOut.clicked -= OnMinimapZoomOut;
                minimapZoomOut.clicked += OnMinimapZoomOut;
                minimapZoomOut.pickingMode = PickingMode.Position;
            }

            if (minimapScan != null)
            {
                minimapScan.clicked -= OnMinimapScan;
                minimapScan.clicked += OnMinimapScan;
                minimapScan.pickingMode = PickingMode.Position;
            }
        }

        private void HidePopupAuthoringCopies(VisualElement hudRoot)
        {
            if (hudRoot == null)
                return;
            var previews = hudRoot.Query<VisualElement>(className: "dmg-hud-popup-preview").ToList();
            for (int i = 0; i < previews.Count; i++)
                previews[i].style.display = DisplayStyle.None;
        }

        private void OnMinimapZoomIn()
        {
            MapUI mapUi = ResolveMapUi();
            mapUi?.UitkAdjustMinimapSpan(0.833f);
        }

        private void OnMinimapZoomOut()
        {
            MapUI mapUi = ResolveMapUi();
            mapUi?.UitkAdjustMinimapSpan(1.2f);
        }

        private void OnMinimapScan()
        {
            MapUI mapUi = ResolveMapUi();
            mapUi?.UitkMinimapScanClicked();
        }

        private void BindMinimapFog(Texture source, Vector2 playerUv, float uvSpan)
        {
            if (minimapFog == null)
                return;

            MapFogOfWar fog = MapFogOfWar.Instance;
            bool show = fog != null && MapFogOfWar.SystemEnabled && fog.FogTexture != null;
            minimapFog.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;

            RenderTexture rt = EnsureFogViewRt();
            BlitCrop(fog.FogTexture, rt, playerUv, uvSpan);
            minimapFog.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            minimapFog.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            MapUI mapUi = ResolveMapUi();
            DMUiToolkitMenus.SetElementRotate(minimapFog, mapUi != null ? mapUi.MinimapFacingYaw : 0f);
        }

        private RenderTexture EnsureFogViewRt()
        {
            if (fogViewRt != null)
                return fogViewRt;
            fogViewRt = new RenderTexture(ViewRtSize, ViewRtSize, 0, RenderTextureFormat.ARGB32)
            {
                name = "DM_UITK_MinimapFog",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            fogViewRt.Create();
            return fogViewRt;
        }

        private RenderTexture EnsureViewRt()
        {
            if (viewRt != null)
                return viewRt;

            viewRt = new RenderTexture(ViewRtSize, ViewRtSize, 0, RenderTextureFormat.ARGB32)
            {
                name = "DM_UITK_MinimapView",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            viewRt.Create();
            return viewRt;
        }

        private static void BlitCrop(Texture source, RenderTexture dest, Vector2 playerUv, float uvSpan)
        {
            if (source == null || dest == null)
                return;

            float span = Mathf.Clamp(uvSpan, 0.02f, 1f);
            Vector2 scale = new Vector2(span, span);
            Vector2 offset = new Vector2(playerUv.x - span * 0.5f, playerUv.y - span * 0.5f);
            Graphics.Blit(source, dest, scale, offset);
        }

        private void ReleaseViewRt()
        {
            if (viewRt == null)
                return;

            viewRt.Release();
            Destroy(viewRt);
            viewRt = null;
            lastBlitSource = null;
            if (fogViewRt != null)
            {
                fogViewRt.Release();
                Destroy(fogViewRt);
                fogViewRt = null;
            }
        }

        private void PullInfo(MapUI mapUi)
        {
            if (minimapInfo == null || mapUi == null)
                return;

            string text = mapUi.MinimapInfoText;
            if (!string.IsNullOrEmpty(text) && minimapInfo.text != text)
                minimapInfo.text = text;
        }

        private void BuildTicks()
        {
            if (compassTicks == null)
                return;

            compassTicks.Clear();
            ticks.Clear();

            for (float angle = 0f; angle < 360f; angle += TickIntervalDegrees)
            {
                bool cardinal = Mathf.Approximately(angle % 90f, 0f);
                var tickRoot = new VisualElement { pickingMode = PickingMode.Ignore };
                tickRoot.AddToClassList("dmg-hud-compass-tick");
                tickRoot.style.position = Position.Absolute;
                tickRoot.style.width = 32f;
                tickRoot.style.height = 36f;
                tickRoot.style.bottom = 0f;

                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList(cardinal ? "dmg-hud-compass-tick-line-cardinal" : "dmg-hud-compass-tick-line");
                tickRoot.Add(line);

                var label = new Label(cardinal ? CardinalLabel(angle) : Mathf.RoundToInt(angle).ToString())
                {
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList(cardinal ? "dmg-hud-compass-tick-cardinal" : "dmg-hud-compass-tick-label");
                tickRoot.Add(label);

                compassTicks.Add(tickRoot);
                ticks.Add(new CompassTick
                {
                    AngleDegrees = angle,
                    Root = tickRoot,
                    Label = label
                });
            }
        }

        private static string CardinalLabel(float angle)
        {
            if (Mathf.Approximately(angle, 0f)) return "N";
            if (Mathf.Approximately(angle, 90f)) return "E";
            if (Mathf.Approximately(angle, 180f)) return "S";
            if (Mathf.Approximately(angle, 270f)) return "W";
            return Mathf.RoundToInt(angle).ToString();
        }

        private void RefreshCompassHeading(MapUI mapUi)
        {
            if (mapUi == null || ticks.Count == 0)
                return;

            float heading = mapUi.MinimapFacingYaw;
            float halfFov = FieldOfViewDegrees * 0.5f;
            float stripWidth = compassStrip != null ? compassStrip.resolvedStyle.width : GameplayHudLayout.CompassWidth;
            if (stripWidth < 8f)
                stripWidth = GameplayHudLayout.CompassWidth;
            float halfWidth = stripWidth * 0.5f;

            for (int i = 0; i < ticks.Count; i++)
            {
                CompassTick tick = ticks[i];
                float delta = Mathf.DeltaAngle(heading, tick.AngleDegrees);
                bool visible = Mathf.Abs(delta) <= halfFov;
                tick.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;

                float x = (delta / halfFov) * halfWidth;
                tick.Root.style.left = halfWidth + x - 16f;
            }

            if (compassHeading != null)
                compassHeading.text = $"{Mathf.RoundToInt(NormalizeDegrees(heading)):000}";
        }

        private void RefreshCompassMarkers(MapUI mapUi)
        {
            if (mapUi == null || compassMarkers == null)
                return;

            float heading = mapUi.MinimapFacingYaw;
            float halfFov = FieldOfViewDegrees * 0.5f;
            float stripWidth = compassStrip != null ? compassStrip.resolvedStyle.width : GameplayHudLayout.CompassWidth;
            if (stripWidth < 8f)
                stripWidth = GameplayHudLayout.CompassWidth;
            float halfWidth = stripWidth * 0.5f;

            Vector3 playerPos = mapUi.MinimapPlayerWorldPosition;
            bool hasPlayer = mapUi.HasMinimapPlayerPosition;
            var seen = new HashSet<MapMarker>();
            int shown = 0;
            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;

            if (hasPlayer && markers != null)
            {
                for (int i = 0; i < markers.Count && shown < MaxVisibleMarkers; i++)
                {
                    MapMarker marker = markers[i];
                    if (marker == null || !marker.ShowOnMinimap || !marker.IsRevealedOnMap)
                        continue;

                    Vector3 toMarker = marker.WorldPosition - playerPos;
                    toMarker.y = 0f;
                    float distance = toMarker.magnitude;
                    if (distance > MaxMarkerRange)
                        continue;

                    float bearing = Mathf.Atan2(toMarker.x, toMarker.z) * Mathf.Rad2Deg;
                    if (bearing < 0f)
                        bearing += 360f;

                    float delta = Mathf.DeltaAngle(heading, bearing);
                    if (Mathf.Abs(delta) > halfFov)
                        continue;

                    seen.Add(marker);
                    shown++;

                    if (!markerIcons.TryGetValue(marker, out CompassMarker entry))
                    {
                        entry = CreateMarker();
                        markerIcons[marker] = entry;
                    }

                    if (marker.IconSprite != null)
                    {
                        entry.Icon.style.backgroundImage = new StyleBackground(Background.FromSprite(marker.IconSprite));
                        entry.Icon.style.backgroundColor = Color.clear;
                    }
                    else
                    {
                        entry.Icon.style.backgroundImage = StyleKeyword.None;
                        entry.Icon.style.backgroundColor = marker.Color;
                    }

                    entry.Icon.style.unityBackgroundImageTintColor = marker.Color;
                    entry.Distance.text = $"{Mathf.RoundToInt(distance)}m";
                    float x = (delta / halfFov) * halfWidth;
                    entry.Root.style.left = halfWidth + x - 7f;
                    entry.Root.style.display = DisplayStyle.Flex;
                }
            }

            List<MapMarker> stale = null;
            foreach (KeyValuePair<MapMarker, CompassMarker> pair in markerIcons)
            {
                if (seen.Contains(pair.Key))
                    continue;
                stale ??= new List<MapMarker>();
                stale.Add(pair.Key);
                pair.Value.Root.RemoveFromHierarchy();
            }

            if (stale == null)
                return;

            for (int i = 0; i < stale.Count; i++)
                markerIcons.Remove(stale[i]);
        }

        private CompassMarker CreateMarker()
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("dmg-hud-compass-marker");
            root.style.position = Position.Absolute;
            root.style.top = 2f;

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("dmg-hud-compass-marker-icon");
            root.Add(icon);

            var distance = new Label { pickingMode = PickingMode.Ignore };
            distance.AddToClassList("dmg-hud-compass-marker-distance");
            root.Add(distance);

            compassMarkers.Add(root);
            return new CompassMarker { Root = root, Icon = icon, Distance = distance };
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private void HideUguiCounterparts(MapUI mapUi)
        {
            if (mapUi == null)
                return;

            GameObject panel = mapUi.MinimapPanelObject;
            if (panel != null)
            {
                global::UnityEngine.CanvasGroup group = panel.GetComponent<global::UnityEngine.CanvasGroup>();
                if (group == null)
                    group = panel.AddComponent<global::UnityEngine.CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
                hiddenMinimapGroup = group;
                hiddenMinimapRoot = panel;
            }

            GameObject compass = mapUi.CompassHudObject;
            if (compass != null && compass.activeSelf)
                compass.SetActive(false);
            hiddenCompass = compass;

            GameObject info = mapUi.InfoPanelObject;
            if (info != null && info.activeSelf)
                info.SetActive(false);
            hiddenInfoPanel = info;

            uguiHidden = true;
        }

        private void RestoreUguiCounterparts()
        {
            if (!uguiHidden)
                return;

            if (hiddenMinimapGroup != null)
            {
                hiddenMinimapGroup.alpha = 1f;
                hiddenMinimapGroup.blocksRaycasts = true;
                hiddenMinimapGroup.interactable = true;
            }

            bool uitkOff = !DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive;
            bool mapOn = cachedMapUi != null && cachedMapUi.ShouldPresentMinimap;
            if (uitkOff && mapOn)
            {
                if (hiddenCompass != null)
                    hiddenCompass.SetActive(true);
                if (hiddenInfoPanel != null)
                    hiddenInfoPanel.SetActive(true);
            }

            hiddenMinimapGroup = null;
            hiddenMinimapRoot = null;
            hiddenCompass = null;
            hiddenInfoPanel = null;
            uguiHidden = false;
        }
    }
}
