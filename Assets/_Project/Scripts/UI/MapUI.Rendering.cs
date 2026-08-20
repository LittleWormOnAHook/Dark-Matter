using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.Map;
using Project.Player;
using Project.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    // Minimap/full-map content positioning math (pan/zoom/rotation), marker-icon layer refresh and
    // icon lifecycle, player-arrow icon visuals, zoom controls, the minimap info panel, and the
    // full-map marker hover tooltip. Split out of MapUI.cs.
    public partial class MapUI
    {
        private void HandleMarkerRegistryChanged(MapMarker _)
        {
            RequestImmediateMarkerRefresh();
        }

        private void HandleMarkerRegistryChanged()
        {
            RequestImmediateMarkerRefresh();
        }

        private void RequestImmediateMarkerRefresh()
        {
            nextMarkerRefreshTime = 0f;
            if (!uiBuilt || !GameSession.HasStarted)
                return;

            EnsureMapProvider();
            RefreshMarkerIcons();
        }

        private void RefreshMarkerIcons()
        {
            if (mapProvider == null)
                return;

            if (minimapContentRect != null && minimapViewportRect != null)
            {
                Vector2 contentSize = GetMinimapContentSize();
                UpdateMarkerLayer(minimapMarkerLayer, minimapMarkerIcons, contentSize);
            }

            if (fullMapOpen && fullMapContentRect != null)
            {
                Vector2 contentSize = GetFullMapContentSize();
                UpdateMarkerLayer(fullMapMarkerLayer, fullMapMarkerIcons, contentSize);
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
                viewportSize = new Vector2(640f, 640f);

            Vector2 contentSize = GetFullMapContentSize();
            if (HasMapWorldPosition())
            {
                Vector2 mapUv = mapProvider.WorldToMap01(GetMapWorldPosition());
                fullMapPanOffset = -MapUvToContentLocal(mapUv, contentSize);
            }
            else
            {
                fullMapPanOffset = Vector2.zero;
            }

            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            UpdateFullMap();
        }

        private void UpdateMinimap()
        {
            if (mapProvider == null || minimapContentRect == null || minimapViewportRect == null)
                return;

            Vector2 contentSize = GetMinimapContentSize();
            minimapContentRect.sizeDelta = contentSize;

            Vector2 mapUv = HasMapWorldPosition()
                ? mapProvider.WorldToMap01(GetMapWorldPosition())
                : new Vector2(0.5f, 0.5f);
            float facingYaw = GetMapFacingYaw();
            minimapContentRect.anchoredPosition = GetMinimapContentPan(mapUv, contentSize, facingYaw);
            minimapContentRect.localEulerAngles = new Vector3(0f, 0f, facingYaw);

            if (minimapMarkerLayer is RectTransform markerLayerRect)
            {
                markerLayerRect.sizeDelta = contentSize;
                markerLayerRect.anchoredPosition = Vector2.zero;
                markerLayerRect.localEulerAngles = Vector3.zero;
            }

            if (minimapPlayerIconRect != null)
            {
                minimapPlayerIconRect.anchoredPosition = Vector2.zero;
                minimapPlayerIconRect.localEulerAngles = Vector3.zero;
                ApplyPlayerMapIconColor(minimapPlayerIconRect);
                minimapPlayerIconRect.SetAsLastSibling();
            }

            UpdateMinimapInfoPanel();
        }

        private Vector2 GetMinimapContentSize()
        {
            Vector2 viewportSize = minimapViewportRect != null
                ? minimapViewportRect.rect.size
                : Vector2.one * DefaultMinimapSize;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = Vector2.one * DefaultMinimapSize;

            if (mapProvider == null)
                return viewportSize;

            float span = Mathf.Max(32f, minimapWorldSpan);
            float zoom = Mathf.Max(
                mapProvider.WorldBounds.size.x / span,
                mapProvider.WorldBounds.size.z / span);

            // Cap zoom so the circular viewport still shows enough texels from the map texture
            // (extreme zoom on 256–512px bakes reads as a broken/pixelated RawImage).
            Texture mapTexture = mapProvider.MapTexture;
            if (mapTexture != null && mapTexture.width > 0)
            {
                float viewportPx = Mathf.Max(viewportSize.x, viewportSize.y);
                float maxZoom = mapTexture.width / Mathf.Max(48f, viewportPx * 0.35f);
                zoom = Mathf.Min(zoom, Mathf.Max(1f, maxZoom));
            }

            return viewportSize * zoom;
        }

        private Vector2 GetMinimapContentPan(Vector2 playerMapUv, Vector2 contentSize, float facingYawDegrees)
        {
            Vector2 pan = new Vector2(
                (0.5f - playerMapUv.x) * contentSize.x,
                (0.5f - playerMapUv.y) * contentSize.y);

            float yaw = facingYawDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            return new Vector2(
                pan.x * cos - pan.y * sin,
                pan.x * sin + pan.y * cos);
        }

        private static float GetFullMapZoomStep()
        {
            return (MaxFullMapZoom - MinFullMapZoom) / FullMapScrollNotchesFullRange;
        }

        private Vector2 GetFullMapContentSize()
        {
            Vector2 viewportSize = fullMapViewportRect != null
                ? fullMapViewportRect.rect.size
                : new Vector2(640f, 640f);
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = new Vector2(640f, 640f);

            float aspect = GetFullMapTextureAspect();
            float fittedWidth;
            float fittedHeight;
            if (viewportSize.x / Mathf.Max(1f, viewportSize.y) > aspect)
            {
                fittedHeight = viewportSize.y;
                fittedWidth = fittedHeight * aspect;
            }
            else
            {
                fittedWidth = viewportSize.x;
                fittedHeight = fittedWidth / Mathf.Max(0.0001f, aspect);
            }

            return new Vector2(fittedWidth, fittedHeight) * fullMapZoom;
        }

        private float GetFullMapTextureAspect()
        {
            Texture mapTexture = fullMapImage != null ? fullMapImage.texture : null;
            if (mapTexture == null && mapProvider != null)
                mapTexture = mapProvider.MapTexture;

            if (mapTexture != null && mapTexture.height > 0)
                return mapTexture.width / (float)mapTexture.height;

            if (mapProvider != null)
            {
                float worldX = mapProvider.WorldBounds.size.x;
                float worldZ = mapProvider.WorldBounds.size.z;
                if (worldX > 1f && worldZ > 1f)
                    return worldX / worldZ;
            }

            return 1f;
        }

        private static Vector2 MapUvToContentLocal(Vector2 mapUv, Vector2 contentSize)
        {
            return new Vector2(
                (mapUv.x - 0.5f) * contentSize.x,
                (mapUv.y - 0.5f) * contentSize.y);
        }

        private void UpdateFullMap()
        {
            if (mapProvider == null || fullMapContentRect == null || fullMapViewportRect == null)
                return;

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            if (viewportSize.sqrMagnitude < 1f)
                viewportSize = new Vector2(640f, 640f);

            Vector2 contentSize = GetFullMapContentSize();
            fullMapContentRect.sizeDelta = contentSize;
            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            fullMapContentRect.anchoredPosition = fullMapPanOffset;
            fullMapContentRect.localEulerAngles = Vector3.zero;

            if (fullMapMarkerLayer is RectTransform markerLayerRect)
            {
                markerLayerRect.sizeDelta = contentSize;
                markerLayerRect.anchoredPosition = Vector2.zero;
            }

            EnsureFullMapPlayerIconOnContentLayer();
            UpdateFullMapPlayerIconPosition(contentSize);
        }

        private void EnsureFullMapPlayerIconOnContentLayer()
        {
            if (fullMapPlayerIconRect == null || fullMapMarkerLayer == null)
                return;

            if (fullMapPlayerIconRect.parent == fullMapMarkerLayer)
                return;

            fullMapPlayerIconRect.SetParent(fullMapMarkerLayer, false);
            fullMapPlayerIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            fullMapPlayerIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            fullMapPlayerIconRect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void UpdateFullMapPlayerIconPosition(Vector2 contentSize)
        {
            if (fullMapPlayerIconRect == null || mapProvider == null)
                return;

            EnsureFullMapPlayerIconOnContentLayer();

            if (HasMapWorldPosition() && fullMapPlayerIconRect.parent == fullMapMarkerLayer)
            {
                Vector2 mapUv = mapProvider.WorldToMap01(GetMapWorldPosition());
                fullMapPlayerIconRect.anchoredPosition = MapUvToContentLocal(mapUv, contentSize);
            }
            else
            {
                fullMapPlayerIconRect.anchoredPosition = Vector2.zero;
            }

            ApplyPlayerArrowRotation(fullMapPlayerIconRect);
            ApplyPlayerMapIconColor(fullMapPlayerIconRect);
            fullMapPlayerIconRect.SetAsLastSibling();
        }

        private void HandleFullMapPanDelta(Vector2 delta)
        {
            if (!fullMapOpen || fullMapViewportRect == null || fullMapContentRect == null)
                return;

            Vector2 viewportSize = fullMapViewportRect.rect.size;
            Vector2 contentSize = GetFullMapContentSize();
            fullMapPanOffset += delta;
            fullMapPanOffset = ClampMapPan(fullMapPanOffset, contentSize, viewportSize);
            fullMapContentRect.anchoredPosition = fullMapPanOffset;
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

            Vector2 mapUv = HasMapWorldPosition()
                ? mapProvider.WorldToMap01(GetMapWorldPosition())
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
                ApplyPlayerArrowRotation(playerIconRect);

                if (!forFullMap)
                    playerIconRect.SetAsLastSibling();
            }
        }

        private void UpdateMarkerLayer(
            Transform layer,
            Dictionary<MapMarker, RectTransform> iconLookup,
            Vector2 contentSize)
        {
            if (layer == null || mapProvider == null)
                return;

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            var seen = new HashSet<MapMarker>();
            bool forFullMap = layer == fullMapMarkerLayer;
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

                if (!marker.IsRevealedOnMap)
                    continue;

                seen.Add(marker);
                if (!iconLookup.TryGetValue(marker, out RectTransform iconRect) || iconRect == null)
                {
                    iconRect = CreateMarkerIcon(layer, marker);
                    iconLookup[marker] = iconRect;
                }
                else
                {
                    ApplyMarkerIconVisual(iconRect, marker);
                }

                Vector2 markerUv = mapProvider.WorldToMap01(marker.WorldPosition);
                iconRect.anchoredPosition = MapUvToContentLocal(markerUv, contentSize);
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
            ApplyMarkerIconVisual(rect, marker);

            return rect;
        }

        private static void ApplyMarkerIconVisual(RectTransform iconRect, MapMarker marker)
        {
            if (iconRect == null || marker == null)
                return;

            Image image = iconRect.GetComponent<Image>();
            if (image == null)
                return;

            iconRect.sizeDelta = marker.IconSprite != null ? new Vector2(16f, 16f) : new Vector2(10f, 10f);

            if (marker.IconSprite != null)
            {
                image.sprite = marker.IconSprite;
                image.color = marker.Color;
            }
            else
            {
                MenuUiBuilder.ApplyUiSprite(image);
                image.sprite = MapUiSprites.Dot;
                image.color = marker.Color;
            }
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

        private void OnMinimapScanClicked()
        {
            UpdateMinimapInfoPanel("Scan queued — extension hook ready.");
        }

        private void AdjustMinimapSpan(float multiplier)
        {
            minimapWorldSpan = Mathf.Clamp(minimapWorldSpan * multiplier, MinMinimapSpan, MaxMinimapSpan);
            UpdateMinimapInfoPanel();
        }

        private void ApplyMinimapScrollZoom(int zoomInDirection)
        {
            float step = (MaxMinimapSpan - MinMinimapSpan) / MinimapScrollNotchesFullRange;
            minimapWorldSpan = Mathf.Clamp(
                minimapWorldSpan - zoomInDirection * step,
                MinMinimapSpan,
                MaxMinimapSpan);
            lastMinimapInfoRange = int.MinValue;
            UpdateMinimap();
            RequestImmediateMarkerRefresh();
            UpdateMinimapInfoPanel("Hold M  |  Scroll to zoom");
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
                CenterFullMapOnPlayer();
        }

        private void UpdateMinimapInfoPanel(string message = null)
        {
            if (minimapInfoLabel == null)
                return;

            if (!string.IsNullOrEmpty(message))
            {
                if (minimapInfoLabel.text != message)
                    minimapInfoLabel.text = message;
                return;
            }

            float rangeMeters = MapFogOfWar.GetScanRevealRadius();
            bool scanning = IsScannerSweepActive();
            int rangeRounded = Mathf.RoundToInt(rangeMeters);
            if (rangeRounded == lastMinimapInfoRange && scanning == lastMinimapInfoScanning)
                return;

            lastMinimapInfoRange = rangeRounded;
            lastMinimapInfoScanning = scanning;
            string scanState = scanning ? "scanning...." : "standby";
            minimapInfoLabel.text = $"Range {rangeRounded}m  |  Scan: {scanState}";
        }

        private static bool IsScannerSweepActive()
        {
            if (cachedScannerSweep == null)
                cachedScannerSweep = Object.FindAnyObjectByType<ScannerSweepController>();

            return cachedScannerSweep != null && cachedScannerSweep.IsSweeping;
        }

        private void CreateFullMapMarkerTooltip(Transform parent)
        {
            GameObject tooltipObject = new GameObject("MarkerTooltip", typeof(RectTransform));
            tooltipObject.transform.SetParent(parent, false);
            fullMapMarkerTooltipRect = tooltipObject.GetComponent<RectTransform>();
            fullMapMarkerTooltipRect.pivot = new Vector2(0f, 1f);
            fullMapMarkerTooltipRect.sizeDelta = new Vector2(280f, 36f);

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

            fullMapMarkerTooltipLabel = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(fullMapMarkerTooltipLabel);
            fullMapMarkerTooltipLabel.fontSize = 14f;
            fullMapMarkerTooltipLabel.alignment = TextAlignmentOptions.MidlineLeft;
            fullMapMarkerTooltipLabel.color = new Color(0.86f, 0.9f, 0.96f, 1f);
            fullMapMarkerTooltipLabel.overflowMode = TextOverflowModes.Ellipsis;
            fullMapMarkerTooltipLabel.raycastTarget = false;

            tooltipObject.SetActive(false);
        }

        private void HideFullMapMarkerTooltip()
        {
            if (fullMapMarkerTooltipRect != null)
                fullMapMarkerTooltipRect.gameObject.SetActive(false);
        }

        private void UpdateFullMapMarkerTooltip()
        {
            if (fullMapMarkerTooltipLabel == null || fullMapMarkerTooltipRect == null)
            {
                if (!fullMapOpen || fullMapPanelRect == null)
                    return;

                Transform mapFrame = fullMapPanelRect.Find("MapFrame");
                if (mapFrame == null)
                    return;

                Transform existingTooltip = mapFrame.Find("MarkerTooltip") ?? mapFrame.Find("ResourceTooltip");
                if (existingTooltip != null)
                {
                    fullMapMarkerTooltipRect = existingTooltip as RectTransform;
                    fullMapMarkerTooltipLabel = existingTooltip.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                else
                {
                    CreateFullMapMarkerTooltip(mapFrame);
                }
            }

            if (fullMapMarkerTooltipLabel == null || fullMapMarkerTooltipRect == null)
                return;

            if (!fullMapOpen || fullMapOverlay == null || !fullMapOverlay.activeSelf)
            {
                HideFullMapMarkerTooltip();
                return;
            }

            MapMarker hoveredMarker = GetFullMapMarkerUnderMouse();
            if (hoveredMarker == null)
            {
                HideFullMapMarkerTooltip();
                return;
            }

            string hint = hoveredMarker.GetInteractionHintText();
            if (string.IsNullOrEmpty(hint))
            {
                HideFullMapMarkerTooltip();
                return;
            }

            fullMapMarkerTooltipLabel.text = hint;
            fullMapMarkerTooltipRect.gameObject.SetActive(true);
            fullMapMarkerTooltipRect.SetAsLastSibling();

            if (Mouse.current == null)
                return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            fullMapMarkerTooltipRect.position = mousePosition + new Vector2(18f, -18f);
            ItemHoverTooltip.ClampTooltipToScreen(fullMapMarkerTooltipRect);
        }

        private MapMarker GetFullMapMarkerUnderMouse()
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
                if (marker == null || iconRect == null)
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
    }
}
