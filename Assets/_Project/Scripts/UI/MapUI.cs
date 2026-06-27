using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Map;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Fixed-position circular minimap plus full world map overlay (static snapshot, pan with mouse).
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        private const float DefaultMinimapSize = 128f;
        private const float DefaultMinimapWorldSpan = 96f;
        private const float MinimapScreenDownShift = 0f;
        private const float MinimapEdgeInset = 16f;
        private const float MinimapTitleBarHeight = 29f;
        private const float MinimapInfoPanelHeight = 24f;
        private const float MinimapEdgeButtonSize = 22f;
        private const float MinMinimapSpan = 40f;
        private const float MaxMinimapSpan = 420f;
        private const float DefaultFullMapZoom = 5f;
        private const float MinFullMapZoom = 1f;
        private const float MaxFullMapZoom = 8f;
        private const float MinimapPlayerIconSize = 24f;
        private const float FullMapPlayerIconSize = 48f;

        private static readonly Color PlayerMapIconColor = new Color(0.95f, 0.18f, 0.18f, 1f);

        [Header("Minimap")]
        [SerializeField] private float minimapWorldSpan = DefaultMinimapWorldSpan;
        [SerializeField] private bool rotateMinimapWithPlayer;
        [SerializeField] private Sprite minimapRingSprite;

        [Header("Layout")]
        [Tooltip("When enabled, existing MinimapPanel / FullMapOverlay hierarchy is kept and default runtime layout is skipped.")]
        [SerializeField] private bool preserveManualLayout;
        [Tooltip("When disabled, default anchors and sizes are applied when map UI is built.")]
        [SerializeField] private bool applyRuntimeLayout = true;
        [SerializeField] private UiLayoutProfile minimapLayoutProfile;
        [SerializeField] private UiLayoutProfile fullMapLayoutProfile;
        [SerializeField] private bool applyLayoutProfiles = true;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        public bool PreservesManualLayout => preserveManualLayout;

        private PlayerController playerController;
        private RectTransform minimapRootRect;
        private GameObject minimapRoot;
        private GameObject fullMapOverlay;
        private RectTransform fullMapPanelRect;
        private RectTransform fullMapViewportRect;
        private RectTransform fullMapContentRect;
        private RectTransform minimapViewportRect;
        private RectTransform minimapContentRect;
        private RectTransform minimapPlayerIconRect;
        private RectTransform fullMapPlayerIconRect;
        private Transform minimapMarkerLayer;
        private Transform fullMapMarkerLayer;
        private RawImage minimapImage;
        private RawImage fullMapImage;
        private TextMeshProUGUI minimapInfoLabel;
        private TextMeshProUGUI fullMapZoomLabel;
        private TextMeshProUGUI fullMapResourceTooltipLabel;
        private RectTransform fullMapResourceTooltipRect;
        private Button fullMapCloseButton;
        private Button minimapScanButton;
        private WorldMapProvider mapProvider;
        private bool uiBuilt;
        private bool fullMapOpen;
        private bool openedViaNavigator;
        private float fullMapZoom = DefaultFullMapZoom;
        private int lastMapToggleFrame = -1;
        private Vector2 lastFullMapViewportSize;
        private Vector2 lastFullMapPanelSize;
        private Canvas rootCanvas;
        private float nextMinimapRefreshTime;
        private float nextMarkerRefreshTime;
        private const float MarkerRefreshInterval = 0.25f;
        private const float MinimapRefreshInterval = 0.05f;
        private const int MaxMinimapMarkers = 128;
        private readonly Dictionary<MapMarker, RectTransform> minimapMarkerIcons = new Dictionary<MapMarker, RectTransform>();
        private readonly Dictionary<MapMarker, RectTransform> fullMapMarkerIcons = new Dictionary<MapMarker, RectTransform>();

        private const float FullMapHeaderHeight = 64f;
        private const float FullMapTitleBarHeight = 34f;

        private GameObject fullMapTitleBar;
        private Vector2 fullMapPanOffset;

        private void Awake()
        {
            DetectSceneLayoutShells();
            EnsureMapProvider();
            if (minimapRingSprite == null)
                minimapRingSprite = ShiftUiTheme.CircleOutline ?? MapUiSprites.CircleRing;
        }

        private void DetectSceneLayoutShells()
        {
            if (!preserveManualLayout)
                return;

            applyRuntimeLayout = false;
        }

        private void OnDisable()
        {
            if (mapProvider != null)
                mapProvider.MapTextureReady -= HandleMapTextureReady;

            if (fullMapOpen)
                CloseFullMap();
        }

        private void Start()
        {
            if (!GameSettings.MapSystemEnabled)
            {
                SetMapProviderActive(false);
                return;
            }

            EnsureMapProvider();
            if (mapProvider != null)
                mapProvider.MapTextureReady += HandleMapTextureReady;

            EnsureUiBuilt();
            BindPlayer();
            ApplyMapTexture();
            if (minimapRoot != null)
                minimapRoot.SetActive(false);

            RefreshMapShellVisibility();
        }

        private void OnDestroy()
        {
            if (mapProvider != null)
                mapProvider.MapTextureReady -= HandleMapTextureReady;

            ClearMarkerIcons(minimapMarkerIcons);
            ClearMarkerIcons(fullMapMarkerIcons);
            uiBuilt = false;
        }

        private void HandleMapTextureReady()
        {
            ApplyMapTexture();
        }

        private void LateUpdate()
        {
            if (!GameSettings.MapSystemEnabled)
            {
                if (minimapRoot != null)
                    minimapRoot.SetActive(false);
                return;
            }

            RefreshMapShellVisibility();

            if (!GameSession.HasStarted)
                return;

            if (mapProvider == null)
                EnsureMapProvider();

            if (playerTransform == null)
                BindPlayer();

            if (Time.unscaledTime >= nextMinimapRefreshTime)
            {
                nextMinimapRefreshTime = Time.unscaledTime + MinimapRefreshInterval;
                UpdateMinimap();

                if (fullMapOpen)
                {
                    TrackFullMapLayoutChanges();
                    UpdateFullMap();
                }
            }

            if (minimapImage != null && minimapImage.texture == null)
                ApplyMapTexture();

            if (Time.unscaledTime >= nextMarkerRefreshTime)
            {
                nextMarkerRefreshTime = Time.unscaledTime + MarkerRefreshInterval;
                RefreshMarkerIcons();
            }
        }

        private void RefreshMarkerIcons()
        {
            if (mapProvider == null)
                return;

            Vector2 mapUv = playerTransform != null
                ? mapProvider.WorldToMap01(playerTransform.position)
                : new Vector2(0.5f, 0.5f);

            if (playerTransform != null && minimapContentRect != null && minimapViewportRect != null)
            {
                Vector2 contentSize = minimapContentRect.sizeDelta;
                if (contentSize.sqrMagnitude < 1f)
                    contentSize = minimapViewportRect.rect.size;
                UpdateMarkerLayer(minimapMarkerLayer, minimapMarkerIcons, mapUv, contentSize, relativeToPlayer: true, forFullMap: false);
            }

            if (fullMapOpen && fullMapContentRect != null)
            {
                Vector2 contentSize = fullMapContentRect.sizeDelta;
                if (contentSize.sqrMagnitude < 1f && fullMapViewportRect != null)
                    contentSize = fullMapViewportRect.rect.size * fullMapZoom;
                UpdateMarkerLayer(fullMapMarkerLayer, fullMapMarkerIcons, mapUv, contentSize, relativeToPlayer: false, forFullMap: true);
            }
        }

        private void Update()
        {
            if (!GameSettings.MapSystemEnabled || !GameSession.HasStarted)
                return;

            if (fullMapOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (FullscreenUiNavigator.Instance != null && FullscreenUiNavigator.Instance.IsAnyOpen)
                    return;

                CloseFullMap();
            }

            if (!fullMapOpen || Mouse.current == null)
                return;

            UpdateFullMapResourceTooltip();

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                SetFullMapZoom(fullMapZoom + scroll * 0.0015f);
        }

        public void OnToggleMap(InputAction.CallbackContext context)
        {
            if (!GameSettings.MapSystemEnabled || !GameSession.HasStarted)
                return;

            if (!context.performed)
                return;

            try
            {
                ToggleFullMap();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void ApplyMapSystemEnabled(bool enabled)
        {
            foreach (MapUI mapUi in FindObjectsByType<MapUI>(FindObjectsInactive.Include))
            {
                if (mapUi != null)
                    mapUi.ApplySystemEnabled(enabled);
            }

            foreach (WorldMapProvider provider in FindObjectsByType<WorldMapProvider>(FindObjectsInactive.Include))
            {
                if (provider != null)
                    provider.ApplySystemEnabled(enabled);
            }
        }

        private void ApplySystemEnabled(bool enabled)
        {
            if (!enabled)
            {
                CloseFullMap();
                if (minimapRoot != null)
                    minimapRoot.SetActive(false);
                RefreshMapShellVisibility();
                SetMapProviderActive(false);
                return;
            }

            SetMapProviderActive(true);
            EnsureMapProvider();
            if (mapProvider != null)
                mapProvider.MapTextureReady -= HandleMapTextureReady;

            if (mapProvider != null)
                mapProvider.MapTextureReady += HandleMapTextureReady;

            if (!uiBuilt)
            {
                EnsureUiBuilt();
                BindPlayer();
            }

            fullMapOpen = false;
            ApplyMapTexture();
            RefreshMapShellVisibility();
        }

        private void RefreshMapShellVisibility()
        {
            bool journalOpen = IsJournalOpen();
            if (minimapRoot != null)
                minimapRoot.SetActive(GameSettings.MapSystemEnabled && GameSession.HasStarted && !journalOpen);

            if (fullMapOverlay == null)
                return;

            bool showFullMapOverlay = GameSettings.MapSystemEnabled
                && fullMapOpen
                && GameSession.HasStarted;

            fullMapOverlay.SetActive(showFullMapOverlay);
        }

        public static void CloseAnyOpenMap()
        {
            foreach (MapUI mapUi in FindObjectsByType<MapUI>(FindObjectsInactive.Include))
            {
                if (mapUi != null)
                    mapUi.CloseFullMap();
            }
        }

        private static bool IsJournalOpen()
        {
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            return journal != null && journal.IsOpen;
        }

        public void OpenMapFullscreen()
        {
            if (!GameSettings.MapSystemEnabled)
                return;

            if (!uiBuilt)
                EnsureUiBuilt();

            EnsureMapProvider();
            if (mapProvider == null)
                mapProvider = EnsureWorldMapProviderExists();

            fullMapOpen = true;
            openedViaNavigator = true;
            RefreshMapShellVisibility();
            if (fullMapOverlay != null)
                fullMapOverlay.transform.SetAsLastSibling();

            fullMapZoom = DefaultFullMapZoom;
            UpdateFullMapZoomLabel();
            CenterFullMapOnPlayer();
            ApplyPlayerArrowSizes();

            if (openedViaNavigator)
                StartCoroutine(EnsureJournalChromeAboveMap());
            else if (fullMapOverlay != null)
            {
                UiFrontLayer.BringLayerToFront(transform);
                StartCoroutine(BringFullMapToFrontAfterJournalLayout());
            }
        }

        private IEnumerator EnsureJournalChromeAboveMap()
        {
            yield return null;
            if (!fullMapOpen)
                yield break;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            journal?.BringJournalChromeToFront();
        }

        private IEnumerator BringFullMapToFrontAfterJournalLayout()
        {
            yield return null;
            if (!fullMapOpen || fullMapOverlay == null)
                yield break;

            fullMapOverlay.transform.SetAsLastSibling();
            UiFrontLayer.BringLayerToFront(transform);
        }

        private void ApplyPlayerArrowSizes()
        {
            if (minimapPlayerIconRect != null)
                minimapPlayerIconRect.sizeDelta = new Vector2(MinimapPlayerIconSize, MinimapPlayerIconSize);

            if (fullMapPlayerIconRect != null)
                fullMapPlayerIconRect.sizeDelta = new Vector2(FullMapPlayerIconSize, FullMapPlayerIconSize);
        }

        public void CloseFullMapFromNavigator()
        {
            if (!fullMapOpen)
                return;

            fullMapOpen = false;
            openedViaNavigator = false;
            RefreshMapShellVisibility();
        }

        public void ToggleFullMap()
        {
            if (!GameSettings.MapSystemEnabled)
                return;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            if (journal != null && journal.TryToggleMapTab())
                return;

            if (IsJournalOpen())
                return;

            if (Time.frameCount == lastMapToggleFrame)
                return;

            lastMapToggleFrame = Time.frameCount;

            if (!uiBuilt)
                EnsureUiBuilt();

            EnsureMapProvider();
            if (mapProvider == null)
                mapProvider = EnsureWorldMapProviderExists();

            fullMapOpen = !fullMapOpen;
            openedViaNavigator = false;
            RefreshMapShellVisibility();
            if (fullMapOpen && fullMapOverlay != null)
            {
                fullMapOverlay.transform.SetAsLastSibling();
                UiFrontLayer.BringLayerToFront(transform);
            }

            PauseForFullMap(fullMapOpen);
            if (fullMapOpen)
            {
                fullMapZoom = DefaultFullMapZoom;
                UpdateFullMapZoomLabel();
                CenterFullMapOnPlayer();
            }
        }

        private void TrackFullMapLayoutChanges()
        {
            if (preserveManualLayout || !applyRuntimeLayout)
                return;

            if (fullMapViewportRect == null || fullMapPanelRect == null)
                return;

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            Vector2 panelSize = fullMapPanelRect.rect.size;
            if ((viewportSize - lastFullMapViewportSize).sqrMagnitude <= 0.25f
                && (panelSize - lastFullMapPanelSize).sqrMagnitude <= 0.25f)
            {
                return;
            }

            lastFullMapViewportSize = viewportSize;
            lastFullMapPanelSize = panelSize;
            EnsureFullMapChromeLayout();
        }

        private void CenterFullMapOnPlayer()
        {
            if (mapProvider == null || fullMapViewportRect == null || fullMapContentRect == null)
            {
                UpdateFullMap();
                return;
            }

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = new Vector2(640f, 480f);

            Vector2 contentSize = viewportSize * fullMapZoom;
            if (playerTransform != null)
            {
                Vector2 mapUv = mapProvider.WorldToMap01(playerTransform.position);
                fullMapPanOffset = -new Vector2(
                    (mapUv.x - 0.5f) * contentSize.x,
                    (mapUv.y - 0.5f) * contentSize.y);
            }
            else
            {
                fullMapPanOffset = Vector2.zero;
            }

            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            UpdateFullMap();
        }

        public void CloseFullMap()
        {
            if (!fullMapOpen)
                return;

            if (openedViaNavigator)
            {
                FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
                if (navigator != null && navigator.CurrentWindow == JournalWindowId.Map)
                {
                    navigator.PopWindow();
                    return;
                }
            }

            fullMapOpen = false;
            openedViaNavigator = false;
            RefreshMapShellVisibility();
            PauseForFullMap(false);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void EnsureMapProvider()
        {
            mapProvider = WorldMapProvider.Instance;
            if (mapProvider != null)
                return;

            mapProvider = FindAnyObjectByType<WorldMapProvider>();
        }

        private static WorldMapProvider EnsureWorldMapProviderExists()
        {
            WorldMapProvider existing = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();
            if (existing != null)
                return existing;

            Terrain terrain = FindAnyObjectByType<Terrain>();
            GameObject host = terrain != null ? terrain.gameObject : new GameObject("WorldMapProvider");
            if (terrain == null)
                host.hideFlags = HideFlags.None;

            return host.GetComponent<WorldMapProvider>() ?? host.AddComponent<WorldMapProvider>();
        }

        private void SetMapProviderActive(bool active)
        {
            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider != null)
                mapProvider.ApplySystemEnabled(active);
        }

        private void BindPlayer()
        {
            if (playerTransform != null && playerController != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (playerTransform == null)
                playerTransform = player.transform;

            if (playerController == null)
                playerController = player.GetComponent<PlayerController>();
        }

        private float GetMapFacingYaw()
        {
            if (playerController != null)
                return playerController.CameraYaw;

            return playerTransform != null ? playerTransform.eulerAngles.y : 0f;
        }

        private void ApplyMapTexture()
        {
            Texture mapTexture = ResolveMapTexture();
            if (mapTexture == null)
                return;

            if (minimapImage != null)
            {
                minimapImage.texture = mapTexture;
                minimapImage.color = Color.white;
            }

            if (fullMapImage != null)
            {
                fullMapImage.texture = mapTexture;
                fullMapImage.color = Color.white;
            }

            SyncMapContentLayout();
        }

        private Texture ResolveMapTexture()
        {
            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider != null && mapProvider.MapTexture != null)
                return mapProvider.MapTexture;

            return WorldMapProvider.CreateDisplayFallback();
        }

        private void SyncMapContentLayout()
        {
            if (!uiBuilt)
                return;

            UpdateMinimap();
            if (fullMapOpen)
                UpdateFullMap();
        }

        private void UpdateMinimap()
        {
            if (mapProvider == null || minimapContentRect == null || minimapViewportRect == null)
                return;

            Vector2 mapUv = playerTransform != null
                ? mapProvider.WorldToMap01(playerTransform.position)
                : new Vector2(0.5f, 0.5f);

            float span = Mathf.Max(32f, minimapWorldSpan);
            float zoom = Mathf.Max(
                mapProvider.WorldBounds.size.x / span,
                mapProvider.WorldBounds.size.z / span);

            Vector2 viewportSize = minimapViewportRect.rect.size;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = Vector2.one * DefaultMinimapSize;

            Vector2 contentSize = viewportSize * zoom;
            minimapContentRect.sizeDelta = contentSize;

            Vector2 uvOffset = new Vector2(0.5f - mapUv.x, 0.5f - mapUv.y);
            if (rotateMinimapWithPlayer)
            {
                float yaw = GetMapFacingYaw();
                float rad = yaw * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                Vector2 rotatedOffset = new Vector2(
                    uvOffset.x * cos - uvOffset.y * sin,
                    uvOffset.x * sin + uvOffset.y * cos);
                minimapContentRect.anchoredPosition = new Vector2(
                    rotatedOffset.x * contentSize.x,
                    rotatedOffset.y * contentSize.y);
                minimapContentRect.localEulerAngles = new Vector3(0f, 0f, yaw);
            }
            else
            {
                minimapContentRect.anchoredPosition = new Vector2(
                    uvOffset.x * contentSize.x,
                    uvOffset.y * contentSize.y);
                minimapContentRect.localEulerAngles = Vector3.zero;
            }

            if (minimapMarkerLayer is RectTransform markerLayerRect)
                markerLayerRect.sizeDelta = contentSize;

            if (minimapPlayerIconRect != null)
            {
                minimapPlayerIconRect.anchoredPosition = Vector2.zero;
                minimapPlayerIconRect.localEulerAngles = rotateMinimapWithPlayer
                    ? Vector3.zero
                    : new Vector3(0f, 0f, -GetMapFacingYaw());
                ApplyPlayerMapIconColor(minimapPlayerIconRect);
                minimapPlayerIconRect.SetAsLastSibling();
            }
        }

        private void UpdateFullMap()
        {
            if (mapProvider == null || fullMapContentRect == null || fullMapViewportRect == null)
                return;

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = new Vector2(640f, 480f);

            Vector2 contentSize = viewportSize * fullMapZoom;
            fullMapContentRect.sizeDelta = contentSize;
            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            fullMapContentRect.anchoredPosition = fullMapPanOffset;
            fullMapContentRect.localEulerAngles = Vector3.zero;

            if (fullMapMarkerLayer is RectTransform markerLayerRect)
                markerLayerRect.sizeDelta = contentSize;

            if (fullMapPlayerIconRect != null)
            {
                if (playerTransform != null)
                {
                    Vector2 mapUv = mapProvider.WorldToMap01(playerTransform.position);
                    fullMapPlayerIconRect.anchoredPosition = fullMapPanOffset + new Vector2(
                        (mapUv.x - 0.5f) * contentSize.x,
                        (mapUv.y - 0.5f) * contentSize.y);
                    fullMapPlayerIconRect.localEulerAngles = new Vector3(0f, 0f, -GetMapFacingYaw());
                ApplyPlayerMapIconColor(fullMapPlayerIconRect);
                }

                fullMapPlayerIconRect.SetAsLastSibling();
            }
        }

        private void HandleFullMapPanDelta(Vector2 delta)
        {
            if (!fullMapOpen || fullMapViewportRect == null || fullMapContentRect == null)
                return;

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            Vector2 contentSize = fullMapContentRect.sizeDelta;
            if (contentSize.sqrMagnitude < 1f)
                contentSize = viewportSize * fullMapZoom;

            fullMapPanOffset += delta;
            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            fullMapContentRect.anchoredPosition = fullMapPanOffset;

            if (fullMapPlayerIconRect != null && playerTransform != null && mapProvider != null)
            {
                Vector2 mapUv = mapProvider.WorldToMap01(playerTransform.position);
                fullMapPlayerIconRect.anchoredPosition = fullMapPanOffset + new Vector2(
                    (mapUv.x - 0.5f) * contentSize.x,
                    (mapUv.y - 0.5f) * contentSize.y);
            }
        }

        private static Vector2 ClampMapPan(Vector2 pan, Vector2 contentSize, Vector2 viewportSize)
        {
            float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
            float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);

            if (contentSize.x <= viewportSize.x)
                pan.x = 0f;
            else
                pan.x = Mathf.Clamp(pan.x, -maxX, maxX);

            if (contentSize.y <= viewportSize.y)
                pan.y = 0f;
            else
                pan.y = Mathf.Clamp(pan.y, -maxY, maxY);

            return pan;
        }

        private void ApplyMapView(
            RectTransform contentRect,
            RectTransform viewportRect,
            RectTransform playerIconRect,
            Transform markerLayer,
            Dictionary<MapMarker, RectTransform> markerIcons,
            bool rotateWithPlayer,
            bool rotatePlayerIcon,
            float worldSpan,
            float zoomMultiplier,
            bool forFullMap)
        {
            if (mapProvider == null)
            {
                EnsureMapProvider();
                if (mapProvider == null)
                    return;
            }

            Vector2 mapUv = playerTransform != null
                ? mapProvider.WorldToMap01(playerTransform.position)
                : new Vector2(0.5f, 0.5f);
            float span = Mathf.Max(32f, worldSpan);
            float zoom = Mathf.Max(
                mapProvider.WorldBounds.size.x / span,
                mapProvider.WorldBounds.size.z / span) * zoomMultiplier;

            Vector2 viewportSize = viewportRect.rect.size;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = Vector2.one * DefaultMinimapSize;

            Vector2 contentSize = viewportSize * zoom;
            contentRect.sizeDelta = contentSize;
            contentRect.anchoredPosition = (Vector2.one * 0.5f - mapUv) * contentSize;
            contentRect.localEulerAngles = rotateWithPlayer
                ? new Vector3(0f, 0f, GetMapFacingYaw())
                : Vector3.zero;

            if (playerIconRect != null)
            {
                playerIconRect.localEulerAngles = rotatePlayerIcon
                    ? new Vector3(0f, 0f, -GetMapFacingYaw())
                    : Vector3.zero;

                if (!forFullMap)
                    playerIconRect.SetAsLastSibling();
            }
        }

        private void UpdateMarkerLayer(
            Transform layer,
            Dictionary<MapMarker, RectTransform> iconLookup,
            Vector2 playerUv,
            Vector2 contentSize,
            bool relativeToPlayer,
            bool forFullMap)
        {
            if (layer == null)
                return;

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            var seen = new HashSet<MapMarker>();
            int markerBudget = forFullMap ? markers.Count : MaxMinimapMarkers;

            for (int i = 0; i < markers.Count; i++)
            {
                if (seen.Count >= markerBudget)
                    break;

                MapMarker marker = markers[i];
                if (marker == null)
                    continue;

                if (forFullMap ? !marker.ShowOnFullMap : !marker.ShowOnMinimap)
                    continue;

                seen.Add(marker);
                if (!iconLookup.TryGetValue(marker, out RectTransform iconRect) || iconRect == null)
                {
                    iconRect = CreateMarkerIcon(layer, marker);
                    iconLookup[marker] = iconRect;
                }

                Vector2 markerUv = mapProvider.WorldToMap01(marker.WorldPosition);
                iconRect.anchoredPosition = relativeToPlayer
                    ? (markerUv - playerUv) * contentSize
                    : new Vector2(
                        (markerUv.x - 0.5f) * contentSize.x,
                        (markerUv.y - 0.5f) * contentSize.y);
            }

            List<MapMarker> toRemove = null;
            foreach (KeyValuePair<MapMarker, RectTransform> pair in iconLookup)
            {
                if (pair.Key == null || !seen.Contains(pair.Key))
                {
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);

                    toRemove ??= new List<MapMarker>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                iconLookup.Remove(toRemove[i]);
        }

        private static void ClearMarkerIcons(Dictionary<MapMarker, RectTransform> iconLookup)
        {
            foreach (KeyValuePair<MapMarker, RectTransform> pair in iconLookup)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            iconLookup.Clear();
        }

        private static RectTransform CreateMarkerIcon(Transform parent, MapMarker marker)
        {
            GameObject iconObject = new GameObject("MapMarkerIcon", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = marker.IconSprite != null ? new Vector2(16f, 16f) : new Vector2(10f, 10f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = iconObject.AddComponent<Image>();
            image.raycastTarget = false;
            if (marker.IconSprite != null)
            {
                image.sprite = marker.IconSprite;
                image.color = Color.white;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(image);
                image.sprite = MapUiSprites.Dot;
                image.color = marker.Color;
            }

            return rect;
        }

        private static RectTransform CreatePlayerArrow(Transform parent, float size)
        {
            GameObject iconObject = new GameObject("PlayerArrow", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = iconObject.AddComponent<Image>();
            image.sprite = MapUiSprites.PlayerArrow;
            image.color = PlayerMapIconColor;
            image.raycastTarget = false;
            image.maskable = false;
            return rect;
        }

        private static void ApplyPlayerMapIconColor(RectTransform playerIconRect)
        {
            if (playerIconRect == null)
                return;

            Image image = playerIconRect.GetComponent<Image>();
            if (image != null)
                image.color = PlayerMapIconColor;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt)
                return;

            if (transform.localScale.sqrMagnitude < 0.001f)
                transform.localScale = Vector3.one;

            bool hasMinimap = transform.Find("MinimapPanel") != null;
            bool hasFullMap = transform.Find("FullMapOverlay") != null;

            if (preserveManualLayout && (hasMinimap || hasFullMap))
            {
                if (!hasMinimap)
                    BuildMinimap();
                if (!hasFullMap)
                    BuildFullMapPanel();

                BindExistingUiReferences();
                FinalizeUiBuilt();
                return;
            }

            DestroyExistingMapUi();
            BuildMinimap();
            BuildFullMapPanel();
            FinalizeUiBuilt();
        }

        /// <summary>
        /// Creates MinimapPanel and FullMapOverlay under this MapUI for edit-mode layout work.
        /// </summary>
        public void EnsureLayoutShells()
        {
            uiBuilt = false;
            EnsureUiBuilt();

            if (Application.isPlaying)
                return;

            if (minimapRoot != null)
                minimapRoot.SetActive(false);
            if (fullMapOverlay != null)
                fullMapOverlay.SetActive(false);
            fullMapOpen = false;
        }

        private void FinalizeUiBuilt()
        {
            uiBuilt = true;
            ApplyMapTexture();

            if (fullMapCloseButton != null)
            {
                fullMapCloseButton.onClick.RemoveListener(CloseFullMap);
                fullMapCloseButton.onClick.AddListener(CloseFullMap);
            }

            UpdateMinimapInfoPanel();
            UpdateFullMapZoomLabel();
            EnsureMinimapChromeLayout();
            EnsureMinimapPlayerIconCentered();
            EnsureFullMapChromeLayout();
            EnsureFullMapPanHandler();
            ApplySavedLayoutProfiles();
            ApplyPlayerArrowSizes();
            RefreshMapShellVisibility();
        }

        private void ApplySavedLayoutProfiles()
        {
            if (!applyLayoutProfiles)
                return;

            if (minimapRoot != null)
            {
                UiLayoutProfile profile = minimapLayoutProfile ?? UiLayoutProfileResolver.Load(UiPanelIds.Minimap);
                if (profile != null)
                    UiLayoutProfileApplier.Apply(minimapRoot.transform, profile);
            }

            if (fullMapOverlay != null)
            {
                UiLayoutProfile profile = fullMapLayoutProfile ?? UiLayoutProfileResolver.Load(UiPanelIds.MapFull);
                if (profile != null)
                    UiLayoutProfileApplier.Apply(fullMapOverlay.transform, profile);
            }
        }

        private void DestroyExistingMapUi()
        {
            Transform existingMinimap = transform.Find("MinimapPanel");
            if (existingMinimap != null)
                DestroyUiObject(existingMinimap.gameObject);

            Transform existingOverlay = transform.Find("FullMapOverlay");
            if (existingOverlay != null)
                DestroyUiObject(existingOverlay.gameObject);

            ClearMarkerIcons(minimapMarkerIcons);
            ClearMarkerIcons(fullMapMarkerIcons);

            minimapRoot = null;
            minimapRootRect = null;
            fullMapOverlay = null;
            fullMapPanelRect = null;
            fullMapViewportRect = null;
            fullMapContentRect = null;
            minimapViewportRect = null;
            minimapContentRect = null;
            minimapPlayerIconRect = null;
            fullMapPlayerIconRect = null;
            minimapMarkerLayer = null;
            fullMapMarkerLayer = null;
            minimapImage = null;
            fullMapImage = null;
            minimapInfoLabel = null;
            fullMapZoomLabel = null;
            fullMapResourceTooltipLabel = null;
            fullMapResourceTooltipRect = null;
            fullMapCloseButton = null;
            minimapScanButton = null;
            fullMapTitleBar = null;
            fullMapPanOffset = Vector2.zero;
        }

        private void EnsureFullMapPanHandler()
        {
            if (fullMapViewportRect == null)
                return;

            Transform panHit = fullMapViewportRect.Find("PanHitArea");
            GameObject panObject;
            if (panHit == null)
            {
                panObject = new GameObject("PanHitArea", typeof(RectTransform));
                panObject.transform.SetParent(fullMapViewportRect, false);
                StretchToParent(panObject.GetComponent<RectTransform>());
            }
            else
            {
                panObject = panHit.gameObject;
            }

            Image hitImage = panObject.GetComponent<Image>();
            if (hitImage == null)
                hitImage = panObject.AddComponent<Image>();

            hitImage.color = Color.clear;
            hitImage.raycastTarget = true;

            MapViewportPanHandler panHandler = panObject.GetComponent<MapViewportPanHandler>();
            if (panHandler == null)
                panHandler = panObject.AddComponent<MapViewportPanHandler>();

            panHandler.Initialize(HandleFullMapPanDelta);

            MapViewportPanHandler legacyHandler = fullMapViewportRect.GetComponent<MapViewportPanHandler>();
            if (legacyHandler != null && legacyHandler != panHandler)
                DestroyUiObject(legacyHandler);

            Image legacyImage = fullMapViewportRect.GetComponent<Image>();
            if (legacyImage != null && fullMapViewportRect.GetComponent<RectMask2D>() != null)
                DestroyUiObject(legacyImage);

            if (fullMapPlayerIconRect != null)
                fullMapPlayerIconRect.SetAsLastSibling();
        }

        private void EnsureFullMapChromeLayout()
        {
            if (fullMapPanelRect == null)
                return;

            if (preserveManualLayout || !applyRuntimeLayout)
            {
                Transform existingClose = fullMapPanelRect.Find("CloseButton");
                if (existingClose != null)
                    fullMapCloseButton = existingClose.GetComponent<Button>();
                return;
            }

            Transform panel = fullMapPanelRect;
            Transform header = panel.Find("HeaderChrome");
            if (header == null)
            {
                GameObject headerObject = new GameObject("HeaderChrome", typeof(RectTransform));
                headerObject.transform.SetParent(panel, false);
                header = headerObject.transform;
                ConfigureTopStretchBar(header as RectTransform, FullMapHeaderHeight);

                Transform titleBar = FindMapTitleBar(panel);
                if (titleBar != null)
                {
                    titleBar.SetParent(header, false);
                    ConfigureTopStretchBar(titleBar as RectTransform, FullMapTitleBarHeight);
                }

                Transform zoomRow = panel.Find("ZoomControls");
                if (zoomRow != null)
                {
                    zoomRow.SetParent(header, false);
                    ConfigureHeaderZoomRow(zoomRow as RectTransform);
                }
            }

            Transform mapFrame = panel.Find("MapFrame");
            if (mapFrame is RectTransform mapFrameRect)
            {
                mapFrameRect.anchorMin = Vector2.zero;
                mapFrameRect.anchorMax = Vector2.one;
                mapFrameRect.offsetMin = new Vector2(12f, 12f);
                mapFrameRect.offsetMax = new Vector2(-12f, -(FullMapHeaderHeight + 4f));
            }

            Transform zoomOnPanel = panel.Find("ZoomControls");
            if (zoomOnPanel != null && zoomOnPanel.parent == panel)
            {
                Transform headerChrome = panel.Find("HeaderChrome");
                if (headerChrome != null)
                {
                    zoomOnPanel.SetParent(headerChrome, false);
                    ConfigureHeaderZoomRow(zoomOnPanel as RectTransform);
                }
            }

            Transform titleOnPanel = FindMapTitleBar(panel);
            if (titleOnPanel != null && titleOnPanel.parent == panel)
            {
                Transform headerChrome = panel.Find("HeaderChrome");
                if (headerChrome != null)
                {
                    titleOnPanel.SetParent(headerChrome, false);
                    ConfigureTopStretchBar(titleOnPanel as RectTransform, FullMapTitleBarHeight);
                }
            }

            header = panel.Find("HeaderChrome");
            Transform closeButton = panel.Find("CloseButton");
            if (closeButton != null && header != null && closeButton.parent != header)
            {
                closeButton.SetParent(header, false);
                if (closeButton is RectTransform closeRect)
                {
                    closeRect.anchorMin = new Vector2(1f, 1f);
                    closeRect.anchorMax = new Vector2(1f, 1f);
                    closeRect.pivot = new Vector2(1f, 1f);
                    closeRect.sizeDelta = new Vector2(28f, 28f);
                    closeRect.anchoredPosition = new Vector2(-6f, -3f);
                }

                fullMapCloseButton = closeButton.GetComponent<Button>();
            }

            if (header != null)
                header.SetAsLastSibling();
            if (closeButton != null)
                closeButton.SetAsLastSibling();

            RemoveFullMapResizeHandles();
        }

        private void RemoveFullMapResizeHandles()
        {
            if (fullMapPanelRect == null)
                return;

            for (int i = fullMapPanelRect.childCount - 1; i >= 0; i--)
            {
                Transform child = fullMapPanelRect.GetChild(i);
                if (child != null && child.name.StartsWith("Resize_"))
                    DestroyUiObject(child.gameObject);
            }
        }

        private void EnsureMinimapPlayerIconCentered()
        {
            if (preserveManualLayout || !applyRuntimeLayout)
                return;

            if (minimapViewportRect == null || minimapPlayerIconRect == null)
                return;

            if (minimapPlayerIconRect.parent != minimapViewportRect)
                minimapPlayerIconRect.SetParent(minimapViewportRect, false);

            minimapPlayerIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            minimapPlayerIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            minimapPlayerIconRect.pivot = new Vector2(0.5f, 0.5f);
            minimapPlayerIconRect.anchoredPosition = Vector2.zero;
            ApplyPlayerMapIconColor(minimapPlayerIconRect);
        }

        private void EnsureMinimapChromeLayout()
        {
            if (minimapRoot == null)
                return;

            if (preserveManualLayout && !applyRuntimeLayout)
            {
                WireMinimapScanButton();
                UpdateMinimapInfoPanel();
                return;
            }

            Transform zoomRow = minimapRoot.transform.Find("ZoomControls");
            if (zoomRow != null)
                DestroyUiObject(zoomRow.gameObject);

            Transform circleAssembly = minimapRoot.transform.Find("CircleAssembly");
            if (circleAssembly == null)
                return;

            if (circleAssembly.Find("EdgeControls") == null)
                BuildMinimapEdgeControls(circleAssembly);

            Transform infoPanel = minimapRoot.transform.Find("InfoPanel");
            if (infoPanel == null)
            {
                minimapInfoLabel = CreateMinimapInfoPanel(minimapRoot.transform);
                infoPanel = minimapRoot.transform.Find("InfoPanel");
            }
            else
            {
                minimapInfoLabel = infoPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (applyRuntimeLayout && minimapRootRect != null)
            {
                float totalChrome = MinimapTitleBarHeight + MinimapInfoPanelHeight;
                minimapRootRect.sizeDelta = new Vector2(DefaultMinimapSize, DefaultMinimapSize + totalChrome);

                if (circleAssembly is RectTransform circleRect)
                {
                    circleRect.anchorMin = new Vector2(0.5f, 1f);
                    circleRect.anchorMax = new Vector2(0.5f, 1f);
                    circleRect.pivot = new Vector2(0.5f, 1f);
                    circleRect.anchoredPosition = new Vector2(0f, -(MinimapTitleBarHeight + 2f));
                    circleRect.sizeDelta = Vector2.one * (DefaultMinimapSize - 10f);
                }

                if (infoPanel is RectTransform infoRect)
                {
                    infoRect.anchorMin = new Vector2(0f, 0f);
                    infoRect.anchorMax = new Vector2(1f, 0f);
                    infoRect.pivot = new Vector2(0.5f, 0f);
                    infoRect.sizeDelta = new Vector2(0f, MinimapInfoPanelHeight);
                    infoRect.anchoredPosition = Vector2.zero;
                }
            }

            WireMinimapScanButton();
            UpdateMinimapInfoPanel();
        }

        private void BuildMinimapEdgeControls(Transform circleAssembly)
        {
            GameObject edgeControls = new GameObject("EdgeControls", typeof(RectTransform));
            edgeControls.transform.SetParent(circleAssembly, false);
            StretchToParent(edgeControls.GetComponent<RectTransform>());

            CreateMinimapEdgeButton(
                edgeControls.transform,
                "+",
                new Vector2(1f, 0.5f),
                new Vector2(6f, 0f),
                () => AdjustMinimapSpan(0.833f));

            CreateMinimapEdgeButton(
                edgeControls.transform,
                "-",
                new Vector2(0f, 0.5f),
                new Vector2(-6f, 0f),
                () => AdjustMinimapSpan(1.2f));

            minimapScanButton = CreateMinimapEdgeButton(
                edgeControls.transform,
                "Scan",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 6f),
                OnMinimapScanClicked,
                compactLabel: true);
        }

        private static Button CreateMinimapEdgeButton(
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 anchoredPosition,
            System.Action onClick,
            bool compactLabel = false)
        {
            GameObject buttonObject = new GameObject($"{label}Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * MinimapEdgeButtonSize;
            rect.anchoredPosition = anchoredPosition;

            Image image = buttonObject.AddComponent<Image>();
            Sprite circleSprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
            image.sprite = circleSprite;
            image.type = Image.Type.Simple;
            image.color = new Color(0.12f, 0.16f, 0.22f, 0.94f);
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = label;
            text.fontSize = compactLabel ? 7f : 13f;
            text.fontStyle = compactLabel ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.82f, 0.88f, 0.95f, 1f);
            text.raycastTarget = false;

            return button;
        }

        private static TextMeshProUGUI CreateMinimapInfoPanel(Transform minimapParent)
        {
            GameObject infoPanel = new GameObject("InfoPanel", typeof(RectTransform));
            infoPanel.transform.SetParent(minimapParent, false);
            RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.sizeDelta = new Vector2(0f, MinimapInfoPanelHeight);
            infoRect.anchoredPosition = Vector2.zero;

            Image infoBg = infoPanel.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(infoBg);
            infoBg.color = new Color(0.06f, 0.08f, 0.11f, 0.95f);
            infoBg.raycastTarget = false;

            GameObject textObject = new GameObject("InfoText", typeof(RectTransform));
            textObject.transform.SetParent(infoPanel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 0f);
            textRect.offsetMax = new Vector2(-6f, 0f);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = 10f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = new Color(0.68f, 0.74f, 0.82f, 1f);
            label.text = "Scan: standby";
            label.raycastTarget = false;
            return label;
        }

        private void WireMinimapScanButton()
        {
            if (minimapRoot == null)
                return;

            Transform scanTransform = minimapRoot.transform.Find("CircleAssembly/EdgeControls/ScanButton");
            if (scanTransform == null)
                return;

            minimapScanButton = scanTransform.GetComponent<Button>();
            if (minimapScanButton == null)
                return;

            minimapScanButton.onClick.RemoveListener(OnMinimapScanClicked);
            minimapScanButton.onClick.AddListener(OnMinimapScanClicked);
        }

        private void OnMinimapScanClicked()
        {
            UpdateMinimapInfoPanel("Scan queued — extension hook ready.");
        }

        private static void ConfigureTopStretchBar(RectTransform rect, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void ConfigureHeaderZoomRow(RectTransform rowRect)
        {
            if (rowRect == null)
                return;

            rowRect.anchorMin = new Vector2(0.5f, 0f);
            rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, 2f);
            rowRect.sizeDelta = new Vector2(160f, 24f);
        }

        private void BindExistingUiReferences()
        {
            Transform minimap = transform.Find("MinimapPanel");
            if (minimap != null)
            {
                minimapRoot = minimap.gameObject;
                minimapRootRect = minimap as RectTransform;

                Transform circleAssembly = minimap.Find("CircleAssembly");
                Transform viewport = circleAssembly != null ? circleAssembly.Find("CircularViewport") : null;
                minimapViewportRect = viewport as RectTransform;

                Transform content = viewport != null ? viewport.Find("MapContent") : null;
                minimapContentRect = content as RectTransform;
                minimapMarkerLayer = content != null ? content.Find("MarkerLayer") : null;

                if (content != null)
                {
                    Transform mapImageTransform = content.Find("MapImage");
                    if (mapImageTransform != null)
                        minimapImage = mapImageTransform.GetComponent<RawImage>();
                }

                if (circleAssembly != null)
                {
                    Transform arrow = circleAssembly.Find("PlayerArrow");
                    if (arrow == null && viewport != null)
                        arrow = viewport.Find("PlayerArrow");
                    minimapPlayerIconRect = arrow as RectTransform;
                }

                Transform infoPanel = minimap.Find("InfoPanel");
                if (infoPanel != null)
                    minimapInfoLabel = infoPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            Transform overlay = transform.Find("FullMapOverlay");
            if (overlay != null)
            {
                fullMapOverlay = overlay.gameObject;
                Transform panel = overlay.Find("FullMapPanel");
                fullMapPanelRect = panel as RectTransform;

                Transform mapFrame = panel != null ? panel.Find("MapFrame") : null;
                Transform viewport = mapFrame != null ? mapFrame.Find("MapViewport") : null;
                fullMapViewportRect = viewport as RectTransform;

                Transform content = viewport != null ? viewport.Find("MapContent") : null;
                fullMapContentRect = content as RectTransform;
                fullMapMarkerLayer = content != null ? content.Find("MarkerLayer") : null;

                if (content != null)
                {
                    Transform mapImageTransform = content.Find("MapImage");
                    if (mapImageTransform != null)
                        fullMapImage = mapImageTransform.GetComponent<RawImage>();
                }

                if (viewport != null)
                {
                    Transform arrow = viewport.Find("PlayerArrow");
                    fullMapPlayerIconRect = arrow as RectTransform;
                }

                if (panel != null)
                {
                    Transform header = panel.Find("HeaderChrome");
                    Transform closeTransform = header != null ? header.Find("CloseButton") : panel.Find("CloseButton");
                    if (closeTransform != null)
                        fullMapCloseButton = closeTransform.GetComponent<Button>();

                    Transform zoomRow = header != null ? header.Find("ZoomControls") : panel.Find("ZoomControls");
                    if (zoomRow != null)
                    {
                        TextMeshProUGUI[] labels = zoomRow.GetComponentsInChildren<TextMeshProUGUI>(true);
                        if (labels.Length > 0)
                            fullMapZoomLabel = labels[0];
                    }

                    Transform titleBar = FindMapTitleBar(panel);
                    fullMapTitleBar = titleBar != null ? titleBar.gameObject : null;
                }
            }
        }

        private static void DestroyUiObject(Object target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }

        private void BuildMinimap()
        {
            float totalChrome = MinimapTitleBarHeight + MinimapInfoPanelHeight;

            minimapRoot = new GameObject("MinimapPanel", typeof(RectTransform));
            minimapRoot.transform.SetParent(transform, false);
            minimapRootRect = minimapRoot.GetComponent<RectTransform>();
            if (applyRuntimeLayout)
            {
                minimapRootRect.anchorMin = new Vector2(1f, 1f);
                minimapRootRect.anchorMax = new Vector2(1f, 1f);
                minimapRootRect.pivot = new Vector2(1f, 1f);
                minimapRootRect.anchoredPosition = new Vector2(-MinimapEdgeInset, -MinimapEdgeInset - MinimapScreenDownShift);
                minimapRootRect.sizeDelta = new Vector2(DefaultMinimapSize, DefaultMinimapSize + totalChrome);
            }

            CreateTitleBar(minimapRoot.transform, "Minimap", MinimapTitleBarHeight);

            GameObject circleAssembly = new GameObject("CircleAssembly", typeof(RectTransform));
            circleAssembly.transform.SetParent(minimapRoot.transform, false);
            RectTransform circleAssemblyRect = circleAssembly.GetComponent<RectTransform>();
            if (applyRuntimeLayout)
            {
                circleAssemblyRect.anchorMin = new Vector2(0.5f, 1f);
                circleAssemblyRect.anchorMax = new Vector2(0.5f, 1f);
                circleAssemblyRect.pivot = new Vector2(0.5f, 1f);
                circleAssemblyRect.anchoredPosition = new Vector2(0f, -(MinimapTitleBarHeight + 2f));
                circleAssemblyRect.sizeDelta = Vector2.one * (DefaultMinimapSize - 10f);
            }

            AspectRatioFitter aspect = circleAssembly.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspect.aspectRatio = 1f;

            GameObject ringObject = new GameObject("RingBorder", typeof(RectTransform));
            ringObject.transform.SetParent(circleAssembly.transform, false);
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = Vector2.zero;
            ringRect.offsetMax = Vector2.zero;
            Image ringImage = ringObject.AddComponent<Image>();
            ringImage.sprite = minimapRingSprite != null ? minimapRingSprite : MapUiSprites.CircleRing;
            ringImage.color = ShiftUiTheme.IsReady
                ? new Color(ShiftUiTheme.PrimaryColor.r, ShiftUiTheme.PrimaryColor.g, ShiftUiTheme.PrimaryColor.b, 0.85f)
                : new Color(0.78f, 0.86f, 0.95f, 1f);
            ringImage.raycastTarget = false;
            ringImage.preserveAspect = true;

            CreateCircularMapViewport(
                circleAssembly.transform,
                inset: 8f,
                out minimapViewportRect,
                out minimapContentRect,
                out minimapMarkerLayer,
                out minimapImage);

            BuildMinimapEdgeControls(circleAssembly.transform);

            minimapPlayerIconRect = CreatePlayerArrow(minimapViewportRect, MinimapPlayerIconSize);
            minimapPlayerIconRect.SetAsLastSibling();

            minimapInfoLabel = CreateMinimapInfoPanel(minimapRoot.transform);
            WireMinimapScanButton();
        }

        private void BuildFullMapPanel()
        {
            fullMapOverlay = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "FullMapOverlay",
                new Color(0f, 0f, 0f, 0.82f),
                blockRaycasts: true);

            fullMapOverlay.transform.SetAsLastSibling();

            GameObject panelObject = new GameObject("FullMapPanel", typeof(RectTransform));
            panelObject.transform.SetParent(fullMapOverlay.transform, false);
            fullMapPanelRect = panelObject.GetComponent<RectTransform>();
            if (applyRuntimeLayout)
            {
                fullMapPanelRect.anchorMin = Vector2.zero;
                fullMapPanelRect.anchorMax = Vector2.one;
                fullMapPanelRect.offsetMin = Vector2.zero;
                fullMapPanelRect.offsetMax = Vector2.zero;
            }

            Image panelBg = panelObject.AddComponent<Image>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = new Color(0.07f, 0.09f, 0.12f, 0.98f);
            }
            panelBg.raycastTarget = false;

            GameObject headerObject = new GameObject("HeaderChrome", typeof(RectTransform));
            headerObject.transform.SetParent(panelObject.transform, false);
            ConfigureTopStretchBar(headerObject.GetComponent<RectTransform>(), FullMapHeaderHeight);

            GameObject mapFrame = new GameObject("MapFrame", typeof(RectTransform));
            mapFrame.transform.SetParent(panelObject.transform, false);
            RectTransform mapFrameRect = mapFrame.GetComponent<RectTransform>();
            mapFrameRect.anchorMin = Vector2.zero;
            mapFrameRect.anchorMax = Vector2.one;
            mapFrameRect.offsetMin = new Vector2(12f, 12f);
            mapFrameRect.offsetMax = new Vector2(-12f, -(FullMapHeaderHeight + 4f));
            Image mapFrameBg = mapFrame.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(mapFrameBg);
            mapFrameBg.color = new Color(0.04f, 0.06f, 0.08f, 0.98f);
            mapFrameBg.raycastTarget = false;

            GameObject viewportObject = new GameObject("MapViewport", typeof(RectTransform));
            viewportObject.transform.SetParent(mapFrame.transform, false);
            fullMapViewportRect = viewportObject.GetComponent<RectTransform>();
            fullMapViewportRect.anchorMin = Vector2.zero;
            fullMapViewportRect.anchorMax = Vector2.one;
            fullMapViewportRect.offsetMin = new Vector2(8f, 8f);
            fullMapViewportRect.offsetMax = new Vector2(-8f, -8f);
            viewportObject.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("MapContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            fullMapContentRect = contentObject.GetComponent<RectTransform>();
            fullMapContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            fullMapContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            fullMapContentRect.pivot = new Vector2(0.5f, 0.5f);

            GameObject mapImageObject = new GameObject("MapImage", typeof(RectTransform));
            mapImageObject.transform.SetParent(contentObject.transform, false);
            RectTransform mapImageRect = mapImageObject.GetComponent<RectTransform>();
            mapImageRect.anchorMin = Vector2.zero;
            mapImageRect.anchorMax = Vector2.one;
            mapImageRect.offsetMin = Vector2.zero;
            mapImageRect.offsetMax = Vector2.zero;
            fullMapImage = mapImageObject.AddComponent<RawImage>();
            fullMapImage.raycastTarget = false;
            fullMapImage.color = Color.white;
            fullMapImage.texture = WorldMapProvider.CreateDisplayFallback();

            GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
            markerLayerObject.transform.SetParent(contentObject.transform, false);
            fullMapMarkerLayer = markerLayerObject.transform;
            RectTransform markerLayerRect = markerLayerObject.GetComponent<RectTransform>();
            markerLayerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerLayerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerLayerRect.pivot = new Vector2(0.5f, 0.5f);

            GameObject panHitObject = new GameObject("PanHitArea", typeof(RectTransform));
            panHitObject.transform.SetParent(viewportObject.transform, false);
            StretchToParent(panHitObject.GetComponent<RectTransform>());
            Image panHitImage = panHitObject.AddComponent<Image>();
            panHitImage.color = Color.clear;
            panHitImage.raycastTarget = true;
            MapViewportPanHandler panHandler = panHitObject.AddComponent<MapViewportPanHandler>();
            panHandler.Initialize(HandleFullMapPanDelta);

            fullMapPlayerIconRect = CreatePlayerArrow(viewportObject.transform, FullMapPlayerIconSize);

            fullMapTitleBar = CreateTitleBar(headerObject.transform, "World Map", FullMapTitleBarHeight);

            CreateHeaderZoomControls(
                headerObject.transform,
                out fullMapZoomLabel,
                () => SetFullMapZoom(fullMapZoom - 0.25f),
                () =>
                {
                    SetFullMapZoom(DefaultFullMapZoom);
                    CenterFullMapOnPlayer();
                },
                () => SetFullMapZoom(fullMapZoom + 0.25f));

            fullMapCloseButton = MenuUiBuilder.CreateCircleCloseButton(headerObject.transform, 28f);
            RectTransform closeRect = fullMapCloseButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-6f, -3f);

            headerObject.transform.SetAsLastSibling();
            fullMapCloseButton.transform.SetAsLastSibling();

            CreateFullMapResourceTooltip(mapFrame.transform);

            SetFullMapZoom(DefaultFullMapZoom);
            RefreshMapShellVisibility();
        }

        private void CreateCircularMapViewport(
            Transform parent,
            float inset,
            out RectTransform viewportRect,
            out RectTransform contentRect,
            out Transform markerLayer,
            out RawImage mapImage)
        {
            GameObject viewportObject = new GameObject("CircularViewport", typeof(RectTransform));
            viewportObject.transform.SetParent(parent, false);
            viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.one * inset;
            viewportRect.offsetMax = Vector2.one * -inset;

            Image maskImage = viewportObject.AddComponent<Image>();
            maskImage.sprite = MapUiSprites.CircleMask;
            maskImage.type = Image.Type.Simple;
            maskImage.preserveAspect = true;
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("MapContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);

            GameObject mapImageObject = new GameObject("MapImage", typeof(RectTransform));
            mapImageObject.transform.SetParent(contentObject.transform, false);
            RectTransform mapImageRect = mapImageObject.GetComponent<RectTransform>();
            mapImageRect.anchorMin = Vector2.zero;
            mapImageRect.anchorMax = Vector2.one;
            mapImageRect.offsetMin = Vector2.zero;
            mapImageRect.offsetMax = Vector2.zero;
            mapImage = mapImageObject.AddComponent<RawImage>();
            mapImage.raycastTarget = false;
            mapImage.maskable = true;
            mapImage.color = Color.white;
            mapImage.texture = WorldMapProvider.CreateDisplayFallback();

            GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
            markerLayerObject.transform.SetParent(contentObject.transform, false);
            markerLayer = markerLayerObject.transform;
            RectTransform markerLayerRect = markerLayerObject.GetComponent<RectTransform>();
            markerLayerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerLayerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerLayerRect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Transform FindMapTitleBar(Transform panel)
        {
            if (panel == null)
                return null;

            Transform header = panel.Find("HeaderChrome");
            Transform titleBar = header != null ? header.Find("TitleBar") : null;
            if (titleBar == null)
                titleBar = panel.Find("TitleBar");
            if (titleBar == null && header != null)
                titleBar = header.Find("DragHandle");
            if (titleBar == null)
                titleBar = panel.Find("DragHandle");
            return titleBar;
        }

        private static GameObject CreateTitleBar(Transform parent, string title, float height)
        {
            GameObject titleBarObject = MenuUiBuilder.CreatePanelTitleBar(parent, title, height);
            LayoutElement layout = titleBarObject.GetComponent<LayoutElement>();
            if (layout != null)
                DestroyUiObject(layout);

            ConfigureTopStretchBar(titleBarObject.GetComponent<RectTransform>(), height);
            return titleBarObject;
        }

        private static void CreateHeaderZoomControls(
            Transform headerParent,
            out TextMeshProUGUI zoomLabel,
            System.Action onZoomOut,
            System.Action onReset,
            System.Action onZoomIn)
        {
            GameObject rowObject = new GameObject("ZoomControls", typeof(RectTransform));
            rowObject.transform.SetParent(headerParent, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            ConfigureHeaderZoomRow(rowRect);

            HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            CreateMapButton(rowObject.transform, "-", onZoomOut, 28f);
            zoomLabel = CreateZoomLabel(rowObject.transform);
            CreateMapButton(rowObject.transform, "Reset", onReset, 52f);
            CreateMapButton(rowObject.transform, "+", onZoomIn, 28f);
        }

        private static void CreateZoomControls(
            Transform parent,
            float anchorY,
            out TextMeshProUGUI zoomLabel,
            System.Action onZoomOut,
            System.Action onReset,
            System.Action onZoomIn,
            float topOffset = 0f,
            bool anchorToTop = false)
        {
            GameObject rowObject = new GameObject("ZoomControls", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(160f, 24f);

            if (anchorToTop)
            {
                rowRect.anchorMin = new Vector2(0.5f, 1f);
                rowRect.anchorMax = new Vector2(0.5f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -topOffset);
            }
            else
            {
                rowRect.anchorMin = new Vector2(0.5f, anchorY);
                rowRect.anchorMax = new Vector2(0.5f, anchorY);
                rowRect.pivot = new Vector2(0.5f, anchorY);
                rowRect.anchoredPosition = new Vector2(0f, anchorY > 0.5f ? topOffset : 6f);
            }

            HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            CreateMapButton(rowObject.transform, "-", onZoomOut, 28f);
            zoomLabel = CreateZoomLabel(rowObject.transform);
            CreateMapButton(rowObject.transform, "Reset", onReset, 52f);
            CreateMapButton(rowObject.transform, "+", onZoomIn, 28f);
        }

        private static TextMeshProUGUI CreateZoomLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("ZoomLabel", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            layout.minWidth = 56f;
            layout.preferredWidth = 56f;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = 11f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            label.text = "100%";
            return label;
        }

        private static void CreateMapButton(Transform parent, string text, System.Action onClick, float width)
        {
            GameObject buttonObject = new GameObject(text + "Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 22f;

            Image image = buttonObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = new Color(0.16f, 0.18f, 0.24f, 0.98f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = 11f;
            label.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void AdjustMinimapSpan(float multiplier)
        {
            minimapWorldSpan = Mathf.Clamp(minimapWorldSpan * multiplier, MinMinimapSpan, MaxMinimapSpan);
            UpdateMinimapInfoPanel();
        }

        private void ResetMinimapSpan()
        {
            minimapWorldSpan = DefaultMinimapWorldSpan;
            UpdateMinimapInfoPanel();
        }

        private void SetFullMapZoom(float zoom)
        {
            fullMapZoom = Mathf.Clamp(zoom, MinFullMapZoom, MaxFullMapZoom);
            UpdateFullMapZoomLabel();
            if (fullMapOpen)
            {
                Vector2 viewportSize = fullMapViewportRect != null ? fullMapViewportRect.rect.size : Vector2.zero;
                Vector2 contentSize = viewportSize * fullMapZoom;
                fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
                UpdateFullMap();
            }
        }

        private void UpdateMinimapInfoPanel(string message = null)
        {
            if (minimapInfoLabel == null)
                return;

            if (!string.IsNullOrEmpty(message))
            {
                minimapInfoLabel.text = message;
                return;
            }

            float percent = DefaultMinimapWorldSpan / minimapWorldSpan * 100f;
            minimapInfoLabel.text = $"Range {Mathf.RoundToInt(percent)}%  |  Scan: standby";
        }

        private void CreateFullMapResourceTooltip(Transform parent)
        {
            GameObject tooltipObject = new GameObject("ResourceTooltip", typeof(RectTransform));
            tooltipObject.transform.SetParent(parent, false);
            fullMapResourceTooltipRect = tooltipObject.GetComponent<RectTransform>();
            fullMapResourceTooltipRect.pivot = new Vector2(0f, 1f);
            fullMapResourceTooltipRect.sizeDelta = new Vector2(280f, 36f);

            Image background = tooltipObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = new Color(0.06f, 0.08f, 0.11f, 0.94f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(tooltipObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);

            fullMapResourceTooltipLabel = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(fullMapResourceTooltipLabel);
            fullMapResourceTooltipLabel.fontSize = 14f;
            fullMapResourceTooltipLabel.alignment = TextAlignmentOptions.MidlineLeft;
            fullMapResourceTooltipLabel.color = new Color(0.86f, 0.9f, 0.96f, 1f);
            fullMapResourceTooltipLabel.overflowMode = TextOverflowModes.Ellipsis;
            fullMapResourceTooltipLabel.raycastTarget = false;

            tooltipObject.SetActive(false);
        }

        private void UpdateFullMapResourceTooltip()
        {
            if (fullMapResourceTooltipLabel == null || fullMapResourceTooltipRect == null)
            {
                if (!fullMapOpen || fullMapPanelRect == null)
                    return;

                Transform mapFrame = fullMapPanelRect.Find("MapFrame");
                if (mapFrame == null)
                    return;

                CreateFullMapResourceTooltip(mapFrame);
            }

            if (fullMapResourceTooltipLabel == null || fullMapResourceTooltipRect == null)
                return;

            if (!fullMapOpen || fullMapOverlay == null || !fullMapOverlay.activeSelf)
            {
                fullMapResourceTooltipRect.gameObject.SetActive(false);
                return;
            }

            MapMarker hoveredMarker = GetFullMapResourceMarkerUnderMouse();
            if (hoveredMarker == null)
            {
                fullMapResourceTooltipRect.gameObject.SetActive(false);
                return;
            }

            string hint = hoveredMarker.GetInteractionHintText();
            if (string.IsNullOrEmpty(hint))
            {
                fullMapResourceTooltipRect.gameObject.SetActive(false);
                return;
            }

            fullMapResourceTooltipLabel.text = hint;
            fullMapResourceTooltipRect.gameObject.SetActive(true);
            fullMapResourceTooltipRect.SetAsLastSibling();

            if (Mouse.current == null)
                return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            fullMapResourceTooltipRect.position = mousePosition + new Vector2(18f, -18f);
            ItemHoverTooltip.ClampTooltipToScreen(fullMapResourceTooltipRect);
        }

        private MapMarker GetFullMapResourceMarkerUnderMouse()
        {
            if (fullMapViewportRect == null || Mouse.current == null)
                return null;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Camera uiCamera = ResolveUiCamera();
            if (!RectTransformUtility.RectangleContainsScreenPoint(fullMapViewportRect, mousePosition, uiCamera))
                return null;

            const float hitRadiusPixels = 14f;
            MapMarker bestMarker = null;
            float bestDistance = hitRadiusPixels;

            foreach (KeyValuePair<MapMarker, RectTransform> pair in fullMapMarkerIcons)
            {
                MapMarker marker = pair.Key;
                RectTransform iconRect = pair.Value;
                if (marker == null || iconRect == null || !marker.IsResourceMarker)
                    continue;

                Vector3 iconScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, iconRect.position);
                float distance = Vector2.Distance(iconScreen, mousePosition);
                if (distance > bestDistance)
                    continue;

                bestDistance = distance;
                bestMarker = marker;
            }

            return bestMarker;
        }

        private Camera ResolveUiCamera()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
                return null;

            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }

        private void UpdateFullMapZoomLabel()
        {
            if (fullMapZoomLabel == null)
                return;

            fullMapZoomLabel.text = $"{Mathf.RoundToInt(fullMapZoom * 100f)}%";
        }

        private static void PauseForFullMap(bool pause)
        {
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pause;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetMapOpen(pause);
        }
    }
}
