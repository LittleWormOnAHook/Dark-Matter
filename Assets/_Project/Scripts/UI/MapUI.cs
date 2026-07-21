using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Map;
using Project.Player;
using Project.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Fixed-position circular minimap plus full world map overlay (static snapshot, pan with mouse).
    /// Split across partials by responsibility — see MapUI.Tracking.cs (WorldMapProvider binding,
    /// player position/camera-facing resolution, map texture application), MapUI.Rendering.cs
    /// (minimap/full-map content positioning math, marker-icon layer updates, zoom/tooltip), and
    /// MapUI.Layout.cs (all runtime UI construction — panels, buttons, chrome). This file keeps
    /// fields/config, lifecycle (Awake/Start/Update/LateUpdate), and the open/close/toggle API.
    /// Purely a mechanical reorganization (partial class split) — no behavior changed by the split.
    /// </summary>
    public partial class MapUI : MonoBehaviour
    {
        private const float DefaultMinimapSize = 147f;
        private const float DefaultMinimapWorldSpan = 96f;
        private const float ReferenceTerrainSpan = 512f;
        private const float MinimapScreenDownShift = 0f;
        private const float MinimapEdgeInset = 16f;
        private const float MinimapTitleBarHeight = 0f;
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
        [SerializeField] private bool autoScaleMinimapToTerrain = true;
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
        private TextMeshProUGUI fullMapMarkerTooltipLabel;
        private RectTransform fullMapMarkerTooltipRect;
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
        private const float VehicleMapPositionFreezeSpeed = 0.25f;
        private readonly Dictionary<MapMarker, RectTransform> minimapMarkerIcons = new Dictionary<MapMarker, RectTransform>();
        private readonly Dictionary<MapMarker, RectTransform> fullMapMarkerIcons = new Dictionary<MapMarker, RectTransform>();
        private Vector3 stableMapWorldPosition;
        private bool hasStableMapWorldPosition;

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

        private void OnEnable()
        {
            MapRegistry.MarkerRegistered += HandleMarkerRegistryChanged;
            MapRegistry.MarkerUnregistered += HandleMarkerRegistryChanged;
            RequestImmediateMarkerRefresh();
        }

        private void OnDisable()
        {
            MapRegistry.MarkerRegistered -= HandleMarkerRegistryChanged;
            MapRegistry.MarkerUnregistered -= HandleMarkerRegistryChanged;

            if (mapProvider != null)
                mapProvider.MapTextureReady -= HandleMapTextureReady;

            if (fullMapOpen)
                CloseFullMap();
        }

        private void Start()
        {
            EnsureMapProvider();
            if (mapProvider != null)
                mapProvider.MapTextureReady += HandleMapTextureReady;

            SyncMinimapSpanFromWorldBounds();
            EnsureUiBuilt();
            BindPlayer();
            ApplyMapTexture();
            if (minimapRoot != null)
                minimapRoot.SetActive(false);

            RefreshMapShellVisibility();
            RequestImmediateMarkerRefresh();
        }

        private void OnDestroy()
        {
            if (mapProvider != null)
                mapProvider.MapTextureReady -= HandleMapTextureReady;

            ClearMarkerIcons(minimapMarkerIcons);
            ClearMarkerIcons(fullMapMarkerIcons);
            uiBuilt = false;
        }

        private void LateUpdate()
        {
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

        private void Update()
        {
            if (!GameSession.HasStarted)
                return;

            if (fullMapOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (FullscreenUiNavigator.Instance != null && FullscreenUiNavigator.Instance.IsAnyOpen)
                    return;

                CloseFullMap();
            }

            if (!fullMapOpen || Mouse.current == null)
                return;

            UpdateFullMapMarkerTooltip();

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                SetFullMapZoom(fullMapZoom + scroll * 0.0015f);
        }

        public void OnToggleMap(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted)
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

        public static void ApplyMinimapEnabled(bool enabled)
        {
            foreach (MapUI mapUi in FindObjectsByType<MapUI>(FindObjectsInactive.Include))
            {
                if (mapUi != null)
                    mapUi.ApplyMinimapVisibility(enabled);
            }
        }

        private void ApplyMinimapVisibility(bool enabled)
        {
            if (!enabled && minimapRoot != null)
                minimapRoot.SetActive(false);

            RefreshMapShellVisibility();
        }

        [System.Obsolete("Use ApplyMinimapEnabled instead.")]
        public static void ApplyMapSystemEnabled(bool enabled) => ApplyMinimapEnabled(enabled);

        private void ApplySystemEnabled(bool enabled)
        {
            ApplyMinimapVisibility(enabled);
        }

        private void RefreshMapShellVisibility()
        {
            bool journalOpen = IsJournalOpen();
            if (minimapRoot != null)
                minimapRoot.SetActive(GameSettings.MinimapEnabled && GameSession.HasStarted && !journalOpen);

            if (fullMapOverlay == null)
                return;

            bool showFullMapOverlay = fullMapOpen && GameSession.HasStarted;

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
            RequestImmediateMarkerRefresh();

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
            HideFullMapMarkerTooltip();
            RefreshMapShellVisibility();
        }

        public void ToggleFullMap()
        {
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
                RequestImmediateMarkerRefresh();
            }
            else
            {
                HideFullMapMarkerTooltip();
            }
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
            HideFullMapMarkerTooltip();
            RefreshMapShellVisibility();
            PauseForFullMap(false);
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
