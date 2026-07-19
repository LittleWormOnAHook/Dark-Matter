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
    // Runtime UI construction: minimap panel, full-map overlay panel, chrome layout (header/title
    // bar/zoom row/close button), edge controls, and binding to a pre-authored manual layout when
    // preserveManualLayout is set. Split out of MapUI.cs.
    public partial class MapUI
    {
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
            RequestImmediateMarkerRefresh();
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
            fullMapMarkerTooltipLabel = null;
            fullMapMarkerTooltipRect = null;
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

            RemoveMinimapTitleBar();

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
                    circleRect.anchoredPosition = new Vector2(0f, -2f);
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

        private void RemoveMinimapTitleBar()
        {
            if (minimapRoot == null)
                return;

            Transform titleBar = minimapRoot.transform.Find("TitleBar");
            if (titleBar != null)
                DestroyUiObject(titleBar.gameObject);
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

                if (fullMapMarkerLayer != null)
                {
                    Transform arrow = fullMapMarkerLayer.Find("PlayerArrow");
                    if (arrow == null && viewport != null)
                        arrow = viewport.Find("PlayerArrow");
                    fullMapPlayerIconRect = arrow as RectTransform;
                }
                else if (viewport != null)
                {
                    fullMapPlayerIconRect = viewport.Find("PlayerArrow") as RectTransform;
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

            GameObject circleAssembly = new GameObject("CircleAssembly", typeof(RectTransform));
            circleAssembly.transform.SetParent(minimapRoot.transform, false);
            RectTransform circleAssemblyRect = circleAssembly.GetComponent<RectTransform>();
            if (applyRuntimeLayout)
            {
                circleAssemblyRect.anchorMin = new Vector2(0.5f, 1f);
                circleAssemblyRect.anchorMax = new Vector2(0.5f, 1f);
                circleAssemblyRect.pivot = new Vector2(0.5f, 1f);
                circleAssemblyRect.anchoredPosition = new Vector2(0f, -2f);
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

            fullMapPlayerIconRect = CreatePlayerArrow(fullMapMarkerLayer, FullMapPlayerIconSize);

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

            CreateFullMapMarkerTooltip(mapFrame.transform);

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
    }
}
