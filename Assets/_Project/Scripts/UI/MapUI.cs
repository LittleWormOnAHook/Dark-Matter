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
    /// Draggable circular minimap plus resizable full world map with zoom controls.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        private const float DefaultMinimapSize = 255f;
        private const float DefaultMinimapWorldSpan = 96f;
        private const float MinimapScreenDownShift = 54f;
        private const float MinimapEdgeInset = 24f;
        private const float MinimapChromeHeight = 75f;
        private const float MinMinimapSpan = 40f;
        private const float MaxMinimapSpan = 420f;
        private const float MinFullMapZoom = 0.75f;
        private const float MaxFullMapZoom = 3.5f;

        [Header("Minimap")]
        [SerializeField] private float minimapWorldSpan = DefaultMinimapWorldSpan;
        [SerializeField] private bool rotateMinimapWithPlayer = true;
        [SerializeField] private Sprite minimapRingSprite;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

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
        private TextMeshProUGUI minimapZoomLabel;
        private TextMeshProUGUI fullMapZoomLabel;
        private Button fullMapCloseButton;
        private WorldMapProvider mapProvider;
        private bool uiBuilt;
        private bool fullMapOpen;
        private float fullMapZoom = 1f;
        private int lastMapToggleFrame = -1;
        private float nextMarkerRefreshTime;
        private float nextMinimapRefreshTime;
        private const float MarkerRefreshInterval = 0.25f;
        private const float MinimapRefreshInterval = 0.05f;
        private const int MaxMinimapMarkers = 128;
        private readonly Dictionary<MapMarker, RectTransform> minimapMarkerIcons = new Dictionary<MapMarker, RectTransform>();
        private readonly Dictionary<MapMarker, RectTransform> fullMapMarkerIcons = new Dictionary<MapMarker, RectTransform>();

        private Transform fullMapPanelOriginalParent;
        private GameObject fullMapDragHandle;
        private bool journalEmbedded;

        private void Awake()
        {
            EnsureMapProvider();
            if (minimapRingSprite == null)
                minimapRingSprite = ShiftUiTheme.CircleOutline ?? MapUiSprites.CircleRing;
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
            if (fullMapOverlay != null)
                fullMapOverlay.SetActive(false);
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

            if (minimapRoot != null)
                minimapRoot.SetActive(GameSession.HasStarted);

            if (!GameSession.HasStarted || mapProvider == null)
                return;

            if (playerTransform == null)
                BindPlayer();

            if (playerTransform == null)
                return;

            if (Time.unscaledTime >= nextMinimapRefreshTime)
            {
                nextMinimapRefreshTime = Time.unscaledTime + MinimapRefreshInterval;
                UpdateMinimap();

                if (fullMapOpen)
                    UpdateFullMap();
            }

            if (Time.unscaledTime >= nextMarkerRefreshTime)
            {
                nextMarkerRefreshTime = Time.unscaledTime + MarkerRefreshInterval;
                RefreshMarkerIcons();
            }
        }

        private void RefreshMarkerIcons()
        {
            if (mapProvider == null || playerTransform == null)
                return;

            Vector2 mapUv = mapProvider.WorldToMap01(playerTransform.position);

            if (minimapContentRect != null && minimapViewportRect != null)
            {
                Vector2 contentSize = minimapContentRect.sizeDelta;
                if (contentSize.sqrMagnitude < 1f)
                    contentSize = minimapViewportRect.rect.size;
                UpdateMarkerLayer(minimapMarkerLayer, minimapMarkerIcons, mapUv, contentSize, relativeToPlayer: true, forFullMap: false);
            }

            if (fullMapOpen && fullMapContentRect != null)
            {
                UpdateMarkerLayer(fullMapMarkerLayer, fullMapMarkerIcons, mapUv, fullMapContentRect.sizeDelta, relativeToPlayer: true, forFullMap: true);
            }
        }

        private void Update()
        {
            if (!GameSettings.MapSystemEnabled || !GameSession.HasStarted)
                return;

            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                ToggleFullMap();

            if (fullMapOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseFullMap();

            if (!fullMapOpen || Mouse.current == null)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                SetFullMapZoom(fullMapZoom + scroll * 0.0015f);
        }

        public void OnToggleMap(InputAction.CallbackContext context)
        {
            if (!GameSettings.MapSystemEnabled || !GameSession.HasStarted || !context.performed)
                return;

            ToggleFullMap();
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
                if (fullMapOverlay != null)
                    fullMapOverlay.SetActive(false);
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

            ApplyMapTexture();
        }

        public void ToggleFullMap()
        {
            if (!GameSettings.MapSystemEnabled || journalEmbedded)
                return;
            if (Time.frameCount == lastMapToggleFrame)
                return;

            lastMapToggleFrame = Time.frameCount;

            if (!uiBuilt)
                EnsureUiBuilt();

            fullMapOpen = !fullMapOpen;
            if (fullMapOverlay != null)
            {
                fullMapOverlay.SetActive(fullMapOpen);
                if (fullMapOpen)
                    fullMapOverlay.transform.SetAsLastSibling();
            }

            PauseForFullMap(fullMapOpen);
            if (fullMapOpen)
                UpdateFullMap();
        }

        public void CloseFullMap()
        {
            if (!fullMapOpen)
                return;

            fullMapOpen = false;
            if (fullMapOverlay != null)
                fullMapOverlay.SetActive(false);

            PauseForFullMap(false);
        }

        public void EmbedFullMapPanel(Transform container)
        {
            if (!GameSettings.MapSystemEnabled || container == null)
                return;

            if (!uiBuilt)
                EnsureUiBuilt();

            if (fullMapPanelRect == null)
                return;

            fullMapPanelOriginalParent = fullMapPanelRect.parent;
            fullMapPanelRect.SetParent(container, false);

            if (container is RectTransform tabRect)
            {
                fullMapPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
                fullMapPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
                fullMapPanelRect.pivot = new Vector2(0.5f, 0.5f);
                fullMapPanelRect.sizeDelta = tabRect.rect.size * 0.96f;
                fullMapPanelRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                StretchToParent(fullMapPanelRect);
            }

            journalEmbedded = true;
            fullMapOpen = true;
            fullMapPanelRect.anchoredPosition = Vector2.zero;
            SetFullMapDragHandleActive(false);

            if (fullMapOverlay != null)
                fullMapOverlay.SetActive(false);

            UpdateFullMap();
        }

        public void RestoreFullMapPanel()
        {
            if (!journalEmbedded || fullMapPanelRect == null || fullMapPanelOriginalParent == null)
                return;

            if (!UiEmbedRestore.TryRestoreParent(fullMapPanelRect, fullMapPanelOriginalParent))
            {
                journalEmbedded = false;
                fullMapOpen = false;
                return;
            }

            journalEmbedded = false;
            fullMapOpen = false;
            SetFullMapDragHandleActive(true);
        }

        private void SetFullMapDragHandleActive(bool active)
        {
            if (fullMapDragHandle != null)
                fullMapDragHandle.SetActive(active);
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
            if (mapProvider == null || mapProvider.MapTexture == null)
                return;

            if (minimapImage != null)
                minimapImage.texture = mapProvider.MapTexture;

            if (fullMapImage != null)
                fullMapImage.texture = mapProvider.MapTexture;
        }

        private void UpdateMinimap()
        {
            if (minimapContentRect == null || minimapViewportRect == null)
                return;

            ApplyMapView(
                minimapContentRect,
                minimapViewportRect,
                minimapPlayerIconRect,
                minimapMarkerLayer,
                minimapMarkerIcons,
                rotateWithPlayer: rotateMinimapWithPlayer,
                rotatePlayerIcon: false,
                worldSpan: minimapWorldSpan,
                zoomMultiplier: 1f,
                forFullMap: false);
        }

        private void UpdateFullMap()
        {
            if (fullMapContentRect == null || fullMapViewportRect == null)
                return;

            ApplyMapView(
                fullMapContentRect,
                fullMapViewportRect,
                fullMapPlayerIconRect,
                fullMapMarkerLayer,
                fullMapMarkerIcons,
                rotateWithPlayer: false,
                rotatePlayerIcon: true,
                worldSpan: Mathf.Max(mapProvider.WorldBounds.size.x, mapProvider.WorldBounds.size.z),
                zoomMultiplier: fullMapZoom,
                forFullMap: true);
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
            Vector2 mapUv = mapProvider.WorldToMap01(playerTransform.position);
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

        private static RectTransform CreatePlayerArrow(Transform parent)
        {
            GameObject iconObject = new GameObject("PlayerArrow", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(18f, 18f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = iconObject.AddComponent<Image>();
            image.sprite = MapUiSprites.PlayerArrow;
            image.color = new Color(0.95f, 0.98f, 1f, 1f);
            image.raycastTarget = false;
            return rect;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt)
                return;

            Transform existingMinimap = transform.Find("MinimapPanel");
            if (existingMinimap != null)
                Destroy(existingMinimap.gameObject);

            Transform existingOverlay = transform.Find("FullMapOverlay");
            if (existingOverlay != null)
                Destroy(existingOverlay.gameObject);

            ClearMarkerIcons(minimapMarkerIcons);
            ClearMarkerIcons(fullMapMarkerIcons);

            BuildMinimap();
            BuildFullMapPanel();
            uiBuilt = true;
            ApplyMapTexture();

            if (fullMapCloseButton != null)
                fullMapCloseButton.onClick.AddListener(CloseFullMap);

            UpdateMinimapZoomLabel();
            UpdateFullMapZoomLabel();
        }

        private void BuildMinimap()
        {
            minimapRoot = new GameObject("MinimapPanel", typeof(RectTransform));
            minimapRoot.transform.SetParent(transform, false);
            minimapRootRect = minimapRoot.GetComponent<RectTransform>();
            minimapRootRect.anchorMin = new Vector2(1f, 1f);
            minimapRootRect.anchorMax = new Vector2(1f, 1f);
            minimapRootRect.pivot = new Vector2(1f, 1f);
            minimapRootRect.anchoredPosition = new Vector2(-MinimapEdgeInset, -MinimapEdgeInset - MinimapScreenDownShift);
            minimapRootRect.sizeDelta = new Vector2(DefaultMinimapSize, DefaultMinimapSize + MinimapChromeHeight);

            CreateDragHandle(minimapRoot.transform, minimapRootRect, "Minimap", height: 29f);

            GameObject circleAssembly = new GameObject("CircleAssembly", typeof(RectTransform));
            circleAssembly.transform.SetParent(minimapRoot.transform, false);
            RectTransform circleAssemblyRect = circleAssembly.GetComponent<RectTransform>();
            circleAssemblyRect.anchorMin = new Vector2(0.5f, 1f);
            circleAssemblyRect.anchorMax = new Vector2(0.5f, 1f);
            circleAssemblyRect.pivot = new Vector2(0.5f, 1f);
            circleAssemblyRect.anchoredPosition = new Vector2(0f, -34f);
            circleAssemblyRect.sizeDelta = Vector2.one * (DefaultMinimapSize - 10f);

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

            GameObject backingObject = new GameObject("CircleBacking", typeof(RectTransform));
            backingObject.transform.SetParent(circleAssembly.transform, false);
            RectTransform backingRect = backingObject.GetComponent<RectTransform>();
            backingRect.anchorMin = Vector2.zero;
            backingRect.anchorMax = Vector2.one;
            backingRect.offsetMin = new Vector2(6f, 6f);
            backingRect.offsetMax = new Vector2(-6f, -6f);
            Image backingImage = backingObject.AddComponent<Image>();
            backingImage.sprite = MapUiSprites.CircleMask;
            backingImage.color = new Color(0.04f, 0.06f, 0.08f, 0.98f);
            backingImage.raycastTarget = false;

            CreateCircularMapViewport(
                circleAssembly.transform,
                inset: 8f,
                out minimapViewportRect,
                out minimapContentRect,
                out minimapMarkerLayer,
                out minimapImage,
                out minimapPlayerIconRect);

            CreateZoomControls(
                minimapRoot.transform,
                0f,
                out minimapZoomLabel,
                () => AdjustMinimapSpan(1.2f),
                ResetMinimapSpan,
                () => AdjustMinimapSpan(0.833f));

            CreateResizeHandle(
                minimapRoot.transform,
                minimapRootRect,
                lockAspect: true,
                minSize: new Vector2(166f, 218f),
                maxSize: new Vector2(468f, 520f),
                corner: UIResizeHandler.HandleCorner.BottomLeft);
        }

        private void BuildFullMapPanel()
        {
            fullMapOverlay = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "FullMapOverlay",
                new Color(0f, 0f, 0f, 0.45f),
                blockRaycasts: true);

            fullMapOverlay.transform.SetAsLastSibling();

            GameObject panelObject = new GameObject("FullMapPanel", typeof(RectTransform));
            panelObject.transform.SetParent(fullMapOverlay.transform, false);
            fullMapPanelRect = panelObject.GetComponent<RectTransform>();
            fullMapPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            fullMapPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            fullMapPanelRect.pivot = new Vector2(0.5f, 0.5f);
            fullMapPanelRect.sizeDelta = new Vector2(980f, 680f);

            Image panelBg = panelObject.AddComponent<Image>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = new Color(0.07f, 0.09f, 0.12f, 0.98f);
            }
            panelBg.raycastTarget = true;

            fullMapDragHandle = CreateDragHandle(panelObject.transform, fullMapPanelRect, "World Map", height: 34f);

            fullMapCloseButton = MenuUiBuilder.CreateCircleCloseButton(panelObject.transform, 34f);
            RectTransform closeRect = fullMapCloseButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-8f, -4f);

            CreateZoomControls(
                panelObject.transform,
                1f,
                out fullMapZoomLabel,
                () => SetFullMapZoom(fullMapZoom - 0.25f),
                () => SetFullMapZoom(1f),
                () => SetFullMapZoom(fullMapZoom + 0.25f),
                topOffset: -38f);

            GameObject mapFrame = new GameObject("MapFrame", typeof(RectTransform));
            mapFrame.transform.SetParent(panelObject.transform, false);
            RectTransform mapFrameRect = mapFrame.GetComponent<RectTransform>();
            mapFrameRect.anchorMin = Vector2.zero;
            mapFrameRect.anchorMax = Vector2.one;
            mapFrameRect.offsetMin = new Vector2(12f, 42f);
            mapFrameRect.offsetMax = new Vector2(-12f, -12f);
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

            GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
            markerLayerObject.transform.SetParent(contentObject.transform, false);
            fullMapMarkerLayer = markerLayerObject.transform;
            RectTransform markerLayerRect = markerLayerObject.GetComponent<RectTransform>();
            markerLayerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerLayerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerLayerRect.pivot = new Vector2(0.5f, 0.5f);

            fullMapPlayerIconRect = CreatePlayerArrow(viewportObject.transform);

            CreateResizeHandle(
                panelObject.transform,
                fullMapPanelRect,
                lockAspect: false,
                minSize: new Vector2(520f, 380f),
                maxSize: new Vector2(1400f, 980f),
                corner: UIResizeHandler.HandleCorner.BottomRight);

            SetFullMapZoom(1f);
        }

        private void CreateCircularMapViewport(
            Transform parent,
            float inset,
            out RectTransform viewportRect,
            out RectTransform contentRect,
            out Transform markerLayer,
            out RawImage mapImage,
            out RectTransform playerIconRect)
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

            GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
            markerLayerObject.transform.SetParent(contentObject.transform, false);
            markerLayer = markerLayerObject.transform;
            RectTransform markerLayerRect = markerLayerObject.GetComponent<RectTransform>();
            markerLayerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerLayerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerLayerRect.pivot = new Vector2(0.5f, 0.5f);

            playerIconRect = CreatePlayerArrow(viewportObject.transform);
        }

        private static GameObject CreateDragHandle(
            Transform parent,
            RectTransform targetWindow,
            string title,
            float height)
        {
            GameObject dragObject = new GameObject("DragHandle", typeof(RectTransform));
            dragObject.transform.SetParent(parent, false);
            RectTransform dragRect = dragObject.GetComponent<RectTransform>();
            dragRect.anchorMin = new Vector2(0f, 1f);
            dragRect.anchorMax = new Vector2(1f, 1f);
            dragRect.pivot = new Vector2(0.5f, 1f);
            dragRect.sizeDelta = new Vector2(0f, height);
            dragRect.anchoredPosition = Vector2.zero;

            Image dragBg = dragObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(dragBg);
            dragBg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

            UIDragHandler dragHandler = dragObject.AddComponent<UIDragHandler>();
            dragHandler.targetWindow = targetWindow;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(dragObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(8f, 0f);
            titleRect.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI label = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = title;
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.78f, 0.84f, 0.92f, 1f);
            label.raycastTarget = false;

            return dragObject;
        }

        private static void CreateZoomControls(
            Transform parent,
            float anchorY,
            out TextMeshProUGUI zoomLabel,
            System.Action onZoomOut,
            System.Action onReset,
            System.Action onZoomIn,
            float topOffset = 0f)
        {
            GameObject rowObject = new GameObject("ZoomControls", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, anchorY);
            rowRect.anchorMax = new Vector2(0.5f, anchorY);
            rowRect.pivot = new Vector2(0.5f, anchorY);
            rowRect.sizeDelta = new Vector2(160f, 24f);
            rowRect.anchoredPosition = new Vector2(0f, anchorY > 0.5f ? topOffset : 6f);

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

        private static void CreateResizeHandle(
            Transform parent,
            RectTransform targetWindow,
            bool lockAspect,
            Vector2 minSize,
            Vector2 maxSize,
            UIResizeHandler.HandleCorner corner)
        {
            GameObject handleObject = new GameObject("ResizeHandle", typeof(RectTransform));
            handleObject.transform.SetParent(parent, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16f, 16f);

            if (corner == UIResizeHandler.HandleCorner.BottomLeft)
            {
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.zero;
                handleRect.pivot = Vector2.zero;
                handleRect.anchoredPosition = new Vector2(4f, 4f);
            }
            else
            {
                handleRect.anchorMin = new Vector2(1f, 0f);
                handleRect.anchorMax = new Vector2(1f, 0f);
                handleRect.pivot = new Vector2(1f, 0f);
                handleRect.anchoredPosition = new Vector2(-4f, 4f);
            }

            Image handleImage = handleObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(handleImage);
            handleImage.color = new Color(0.55f, 0.65f, 0.78f, 0.85f);

            UIResizeHandler resizeHandler = handleObject.AddComponent<UIResizeHandler>();
            resizeHandler.targetWindow = targetWindow;
            resizeHandler.lockAspectRatio = lockAspect;
            resizeHandler.minSize = minSize;
            resizeHandler.maxSize = maxSize;
            resizeHandler.corner = corner;
        }

        private void AdjustMinimapSpan(float multiplier)
        {
            minimapWorldSpan = Mathf.Clamp(minimapWorldSpan * multiplier, MinMinimapSpan, MaxMinimapSpan);
            UpdateMinimapZoomLabel();
        }

        private void ResetMinimapSpan()
        {
            minimapWorldSpan = DefaultMinimapWorldSpan;
            UpdateMinimapZoomLabel();
        }

        private void SetFullMapZoom(float zoom)
        {
            fullMapZoom = Mathf.Clamp(zoom, MinFullMapZoom, MaxFullMapZoom);
            UpdateFullMapZoomLabel();
            if (fullMapOpen)
                UpdateFullMap();
        }

        private void UpdateMinimapZoomLabel()
        {
            if (minimapZoomLabel == null)
                return;

            float percent = DefaultMinimapWorldSpan / minimapWorldSpan * 100f;
            minimapZoomLabel.text = $"{Mathf.RoundToInt(percent)}%";
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
