using System.Collections.Generic;
using Project.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// First-person optics overlay with masked render-texture viewport.
    /// Binoculars use circular scope art; scanner uses a rectangular HUD viewport.
    /// </summary>
    public class OpticsOverlayUI : MonoBehaviour
    {
        private const int MaxScannerMarkers = 24;
        private const string OverlayCanvasName = "OpticsOverlayCanvas";

        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int RectHalfWidthId = Shader.PropertyToID("_RectHalfWidth");
        private static readonly int RectHalfHeightId = Shader.PropertyToID("_RectHalfHeight");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int ScannerFuzzId = Shader.PropertyToID("_ScannerFuzz");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int PassthroughId = Shader.PropertyToID("_Passthrough");
        private const int PassthroughMaskResolution = 512;

        private GameObject overlayRoot;
        private GameObject binocularRoot;
        private GameObject scannerRoot;
        private RectTransform markerLayer;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI hintLabel;
        private Image scannerTint;
        private Image viewportBackground;
        private Image passthroughVignetteImage;
        private Image binocularCircleMaskImage;
        private RectTransform binocularCircleMaskRect;
        private Image binocularOuterImage;
        private Image binocularFrameImage;
        private Image binocularInnerImage;
        private Image scannerMaskFrameImage;
        private Image scannerFrameImage;
        private Image scannerReticleImage;
        private RawImage viewportImage;
        private Material viewportMaterial;
        private Texture2D passthroughMaskTexture;
        private Sprite passthroughMaskSprite;
        private bool passthroughMaskBuiltForScanner;
        private float passthroughMaskBuiltAspect = -1f;
        private readonly List<RectTransform> markerPool = new List<RectTransform>();
        private static OpticsOverlayUI instance;
        private bool uiBuilt;
        private bool isVisible;
        internal ToolType ActiveToolType { get; private set; }
        internal RenderTexture BoundRenderTexture { get; private set; }
        internal bool IsPassthroughMode => passthroughMode;
        private bool passthroughMode;
        private bool sceneAuthoredOverlay;
        private TextMeshProUGUI scanningPopupLabel;
        private float scannerHalfWidthPixels = 420f;
        private float scannerHalfHeightPixels = 250f;
        private float markerActivePulseBase = 0.7f;
        private float markerPostScanPulseBase = 0.55f;

        public bool IsBuilt => uiBuilt;
        public bool IsVisible => isVisible && overlayRoot != null && overlayRoot.activeSelf;

        public static OpticsOverlayUI EnsureExists()
        {
            if (instance != null)
            {
                instance.EnsureBuilt();
                return instance;
            }

            ConsolidateOverlayCanvases();
            Transform canvasTransform = GetOrCreateOverlayCanvas();
            if (canvasTransform == null)
                return null;

            instance = canvasTransform.GetComponentInChildren<OpticsOverlayUI>(true);
            if (instance == null)
            {
                GameObject host = new GameObject("OpticsOverlayHost");
                host.transform.SetParent(canvasTransform, false);
                instance = host.AddComponent<OpticsOverlayUI>();
            }

            instance.EnsureBuilt();
            return instance;
        }

        internal static void CleanupStaleRuntimeObjects()
        {
            ConsolidateOverlayCanvases();
        }

        public static void ResetRuntimeState()
        {
            instance = null;
        }

        /// <summary>
        /// Editor/play entry: destroy unsafe for edit-mode rebuilds of scene canvases.
        /// </summary>
        private static void DestroyOverlayObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        public void EnsureBuilt(Transform unusedCanvasRoot = null)
        {
            if (uiBuilt && overlayRoot != null)
                return;

            ConsolidateOverlayCanvases();

            Transform overlayParent = GetOrCreateOverlayCanvas();
            if (overlayParent == null)
                return;

            Transform existingOverlay = overlayParent.Find("OpticsOverlay");
            if (existingOverlay != null)
            {
                if (TryBindExistingOverlay(existingOverlay.gameObject))
                    return;

                DestroyOverlayObject(existingOverlay.gameObject);
            }

            overlayRoot = new GameObject("OpticsOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayRoot.transform.SetParent(overlayParent, false);

            RectTransform rootRect = overlayRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CanvasGroup group = overlayRoot.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Visual defaults come from OpticsCrosshairLibrary.
            viewportBackground = CreateStretchImage(
                overlayRoot.transform,
                "ViewportBackground",
                OpticsUiSprites.ViewportBackground);

            viewportMaterial = CreateViewportMaterial();
            GameObject viewportObject = new GameObject("OpticsViewport", typeof(RectTransform), typeof(RawImage));
            viewportObject.transform.SetParent(overlayRoot.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewportImage = viewportObject.GetComponent<RawImage>();
            viewportImage.material = viewportMaterial;
            viewportImage.raycastTarget = false;
            viewportImage.enabled = false;

            // HDRP-safe look-through mask: UI Image sprite (never RawImage — DrawRawMesh crash).
            passthroughVignetteImage = CreateStretchImage(overlayRoot.transform, "PassthroughVignette", null);
            passthroughVignetteImage.color = Color.white;
            passthroughVignetteImage.enabled = false;

            binocularRoot = new GameObject("BinocularOverlay", typeof(RectTransform));
            binocularRoot.transform.SetParent(overlayRoot.transform, false);
            StretchRect(binocularRoot.GetComponent<RectTransform>());

            binocularOuterImage = CreateStretchImage(binocularRoot.transform, "ScopeOuter", OpticsUiSprites.BinocularScopeOuter);
            binocularFrameImage = CreateStretchImage(binocularRoot.transform, "ScopeFull", OpticsUiSprites.BinocularScopeFull);
            binocularInnerImage = CreateCenteredImage(
                binocularRoot.transform,
                "ScopeInnerGlow",
                OpticsUiSprites.BinocularScopeInnerGlow,
                680f);

            scannerRoot = new GameObject("ScannerOverlay", typeof(RectTransform));
            scannerRoot.transform.SetParent(overlayRoot.transform, false);
            StretchRect(scannerRoot.GetComponent<RectTransform>());

            scannerMaskFrameImage = CreateStretchImage(scannerRoot.transform, "ScannerMaskFrame", OpticsUiSprites.ScannerRectMask);
            scannerFrameImage = CreateCenteredImage(
                scannerRoot.transform,
                "ScannerFrame",
                OpticsUiSprites.ScannerHolographicGlow,
                900f);
            scannerReticleImage = CreateCenteredImage(
                scannerRoot.transform,
                "ScannerReticle",
                OpticsUiSprites.ScannerHolographic,
                420f);
            scannerTint = CreateStretchImage(scannerRoot.transform, "ScannerTint", null);

            GameObject markerRoot = new GameObject("ScannerMarkers", typeof(RectTransform));
            markerRoot.transform.SetParent(overlayRoot.transform, false);
            markerLayer = markerRoot.GetComponent<RectTransform>();
            StretchRect(markerLayer);

            modeLabel = CreateLabel(overlayRoot.transform, "ModeLabel", new Vector2(0.5f, 0.9f), 22f);
            hintLabel = CreateLabel(overlayRoot.transform, "HintLabel", new Vector2(0.5f, 0.06f), 20f);

            overlayRoot.SetActive(false);
            uiBuilt = true;
            ApplyLibraryPresentation(forceRebuildStyles: true);
        }

        public void ApplyLibraryPresentation(bool forceRebuildStyles = false)
        {
            if (!uiBuilt)
                return;

            // Never overwrite scene-authored BinocularOverlay / ScannerOverlay stacks.
            if (sceneAuthoredOverlay)
                return;

            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            if (library == null)
                return;

            OpticsCrosshairLibrary.ApplyImageLayer(
                binocularOuterImage,
                library.binocularScopeOuterLayer,
                OpticsUiSprites.BinocularScopeOuter);
            OpticsCrosshairLibrary.ApplyImageLayer(
                binocularFrameImage,
                library.binocularScopeFullLayer,
                OpticsUiSprites.BinocularScopeFull);
            OpticsCrosshairLibrary.ApplyImageLayer(
                binocularInnerImage,
                library.binocularScopeInnerGlowLayer,
                OpticsUiSprites.BinocularScopeInnerGlow);

            // Triangle overlay (ScopeFull / Triangle3Split) stays off.
            if (binocularFrameImage != null)
                binocularFrameImage.enabled = false;

            OpticsCrosshairLibrary.ApplyImageLayer(
                scannerMaskFrameImage,
                library.scannerMaskFrameLayer,
                OpticsUiSprites.ScannerRectMask);
            OpticsCrosshairLibrary.ApplyImageLayer(
                scannerFrameImage,
                library.scannerFrameLayer,
                OpticsUiSprites.ScannerHolographicGlow);
            OpticsCrosshairLibrary.ApplyImageLayer(
                scannerReticleImage,
                library.scannerReticleLayer,
                OpticsUiSprites.ScannerHolographic);
            OpticsCrosshairLibrary.ApplyImageLayer(
                scannerTint,
                library.scannerTintOverlayLayer,
                null);

            if (passthroughMode && scannerMaskFrameImage != null)
                scannerMaskFrameImage.enabled = false;

            if (viewportBackground != null && library.viewport != null)
            {
                viewportBackground.color = library.viewport.backgroundColor;
                if (library.viewportBackgroundSprite != null)
                    viewportBackground.sprite = library.viewportBackgroundSprite;
            }

            if (viewportImage != null && library.viewport != null)
                viewportImage.color = library.viewport.rawImageColor;

            Material template = library.ResolveViewportMaterial();
            if (forceRebuildStyles && viewportImage != null && template != null)
            {
                if (viewportMaterial != null)
                    DestroyOverlayObject(viewportMaterial);

                viewportMaterial = new Material(template) { name = template.name + "_Runtime" };
                viewportImage.material = viewportMaterial;
            }

            if (library.viewport != null)
            {
                scannerHalfWidthPixels = library.viewport.scannerHalfWidthPixels;
                scannerHalfHeightPixels = library.viewport.scannerHalfHeightPixels;
            }

            if (library.scannerMarkers != null)
            {
                markerActivePulseBase = library.scannerMarkers.activePulseBase;
                markerPostScanPulseBase = library.scannerMarkers.postScanPulseBase;
            }

            OpticsModeLabelSettings modeSettings = library.modeLabel;
            if (modeSettings != null)
            {
                OpticsCrosshairLibrary.ApplyTextLabel(
                    modeLabel,
                    modeSettings.anchor,
                    modeSettings.sizeDelta,
                    modeSettings.fontSize,
                    modeSettings.scannerColor,
                    modeSettings.scannerText);
            }

            OpticsHintLabelSettings hintSettings = library.hintLabel;
            if (hintSettings != null)
            {
                OpticsCrosshairLibrary.ApplyTextLabel(
                    hintLabel,
                    hintSettings.anchor,
                    hintSettings.sizeDelta,
                    hintSettings.fontSize,
                    hintSettings.color,
                    hintSettings.scannerHint);
            }

            ApplyViewportMode(scannerRoot != null && scannerRoot.activeSelf);
        }

        private bool TryBindExistingOverlay(GameObject existingRoot)
        {
            if (existingRoot == null)
                return false;

            overlayRoot = existingRoot;
            viewportBackground = existingRoot.transform.Find("ViewportBackground")?.GetComponent<Image>();
            viewportImage = existingRoot.transform.Find("OpticsViewport")?.GetComponent<RawImage>();
            viewportMaterial = viewportImage != null ? viewportImage.material : null;
            passthroughVignetteImage = existingRoot.transform.Find("PassthroughVignette")?.GetComponent<Image>();
            binocularRoot = existingRoot.transform.Find("BinocularOverlay")?.gameObject;
            scannerRoot = existingRoot.transform.Find("ScannerOverlay")?.gameObject;
            markerLayer = existingRoot.transform.Find("ScannerMarkers") as RectTransform;
            modeLabel = existingRoot.transform.Find("ModeLabel")?.GetComponent<TextMeshProUGUI>();
            hintLabel = existingRoot.transform.Find("HintLabel")?.GetComponent<TextMeshProUGUI>();
            scannerTint = existingRoot.transform.Find("ScannerOverlay/ScannerTint")?.GetComponent<Image>();
            binocularOuterImage = existingRoot.transform.Find("BinocularOverlay/ScopeOuter")?.GetComponent<Image>();
            binocularFrameImage = existingRoot.transform.Find("BinocularOverlay/ScopeFull")?.GetComponent<Image>();
            binocularInnerImage = existingRoot.transform.Find("BinocularOverlay/ScopeInnerGlow")?.GetComponent<Image>();
            scannerMaskFrameImage = existingRoot.transform.Find("ScannerOverlay/ScannerMaskFrame")?.GetComponent<Image>();
            scannerFrameImage = existingRoot.transform.Find("ScannerOverlay/ScannerFrame")?.GetComponent<Image>();
            scannerReticleImage = existingRoot.transform.Find("ScannerOverlay/ScannerReticle")?.GetComponent<Image>();

            if (binocularRoot == null || scannerRoot == null)
                return false;

            if (passthroughVignetteImage == null && overlayRoot != null)
            {
                // Optional leftover — keep disabled for scene-authored stacks.
                Transform vignette = overlayRoot.transform.Find("PassthroughVignette");
                if (vignette != null)
                    passthroughVignetteImage = vignette.GetComponent<Image>();
            }

            if (passthroughVignetteImage != null)
                passthroughVignetteImage.enabled = false;

            if (viewportImage != null)
            {
                viewportImage.enabled = false;
                viewportImage.texture = null;
            }

            // Scene/play-mode leftovers often keep the overlay visible with wrong sprites.
            overlayRoot.SetActive(false);
            if (binocularRoot != null)
                binocularRoot.SetActive(false);
            if (scannerRoot != null)
                scannerRoot.SetActive(false);
            if (markerLayer != null)
                markerLayer.gameObject.SetActive(false);

            uiBuilt = true;
            isVisible = false;
            sceneAuthoredOverlay = true;
            // Preserve scene-authored BinocularOverlay / ScannerOverlay children — do not restyle.
            SuppressRuntimeBinocularCircleMask();
            BindScanningPopup(existingRoot.transform);
            return true;
        }

        public void SetPassthroughMode(bool enabled)
        {
            passthroughMode = enabled;
            ApplyPassthroughVisibility();
            if (passthroughMode)
                BoundRenderTexture = null;
            DMUiToolkitOpticsOverlay.SyncFromUgui(this);
        }

        public void BindRenderTexture(RenderTexture texture)
        {
            if (viewportImage == null)
                return;

            // HDRP passthrough: never enable RawImage (Canvas DrawRawMesh / D3D12 crash).
            if (passthroughMode || texture == null)
            {
                viewportImage.texture = null;
                viewportImage.enabled = false;
                BoundRenderTexture = null;
                ApplyPassthroughVisibility();
                DMUiToolkitOpticsOverlay.SyncFromUgui(this);
                return;
            }

            viewportImage.texture = texture;
            viewportImage.color = Color.white;
            viewportImage.enabled = true;
            BoundRenderTexture = texture;

            if (viewportMaterial != null)
            {
                if (viewportMaterial.HasProperty("_MainTex"))
                    viewportMaterial.SetTexture("_MainTex", texture);
                if (viewportMaterial.HasProperty("_BaseMap"))
                    viewportMaterial.SetTexture("_BaseMap", texture);
            }

            ApplyPassthroughVisibility();
            DMUiToolkitOpticsOverlay.SyncFromUgui(this);
        }

        private void ApplyPassthroughVisibility()
        {
            // Never draw RawImage under HDRP passthrough.
            if (viewportImage != null && passthroughMode)
            {
                viewportImage.texture = null;
                viewportImage.enabled = false;
            }
            else if (viewportImage != null)
            {
                viewportImage.enabled = viewportImage.texture != null;
            }

            if (viewportBackground != null)
            {
                Color c = viewportBackground.color;
                c.a = passthroughMode ? 0f : Mathf.Max(c.a, 0.85f);
                if (!passthroughMode && c.a < 0.01f)
                    c.a = 0.92f;
                viewportBackground.color = c;
                viewportBackground.enabled = !passthroughMode;
            }

            bool scanner = scannerRoot != null && scannerRoot.activeSelf;
            // Scene-authored optics UI owns the look-through frame — skip generated vignette.
            if (sceneAuthoredOverlay)
            {
                if (passthroughVignetteImage != null)
                    passthroughVignetteImage.enabled = false;
                return;
            }

            RefreshPassthroughVignette(scanner);
        }

        private void RefreshPassthroughVignette(bool scanner)
        {
            if (passthroughVignetteImage == null && overlayRoot != null)
            {
                passthroughVignetteImage = CreateStretchImage(overlayRoot.transform, "PassthroughVignette", null);
                passthroughVignetteImage.color = Color.white;
            }

            if (passthroughVignetteImage == null)
                return;

            if (!passthroughMode || !isVisible)
            {
                passthroughVignetteImage.enabled = false;
                return;
            }

            EnsurePassthroughMaskSprite(scanner);
            if (passthroughMaskSprite == null)
            {
                passthroughVignetteImage.enabled = false;
                return;
            }

            passthroughVignetteImage.sprite = passthroughMaskSprite;
            passthroughVignetteImage.material = null; // default UI material — HDRP-safe
            passthroughVignetteImage.type = Image.Type.Simple;
            passthroughVignetteImage.preserveAspect = false;
            passthroughVignetteImage.color = Color.white;
            passthroughVignetteImage.raycastTarget = false;
            passthroughVignetteImage.enabled = true;

            // Under scope/crosshair art, above clear game view. Never cover binocular layers.
            if (binocularRoot != null && !scanner)
                passthroughVignetteImage.transform.SetSiblingIndex(binocularRoot.transform.GetSiblingIndex());
            else if (scannerRoot != null && scanner)
                passthroughVignetteImage.transform.SetSiblingIndex(scannerRoot.transform.GetSiblingIndex());
            else if (viewportImage != null)
                passthroughVignetteImage.transform.SetSiblingIndex(viewportImage.transform.GetSiblingIndex() + 1);
        }

        private void EnsurePassthroughMaskSprite(bool scanner)
        {
            // Prefer overlay canvas aspect (Game view) over Screen (can differ in the Editor).
            float aspect = 1.777f;
            if (overlayRoot != null && overlayRoot.transform is RectTransform overlayRect && overlayRect.rect.height > 1f)
                aspect = overlayRect.rect.width / Mathf.Max(1f, overlayRect.rect.height);
            else if (Screen.height > 0)
                aspect = Screen.width / (float)Mathf.Max(1, Screen.height);

            bool needsRebuild = passthroughMaskSprite == null
                || passthroughMaskBuiltForScanner != scanner
                || Mathf.Abs(passthroughMaskBuiltAspect - aspect) > 0.01f;

            if (!needsRebuild)
                return;

            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsViewportPresentation viewportSettings = library != null ? library.viewport : null;
            // Slightly larger default hole so the circular “looking through” read is obvious.
            float radius = viewportSettings != null ? viewportSettings.binocularRadius : 0.38f;
            if (radius < 0.2f)
                radius = 0.38f;
            float halfW = viewportSettings != null ? viewportSettings.scannerRectHalfWidth : 0.4f;
            float halfH = viewportSettings != null ? viewportSettings.scannerRectHalfHeight : 0.24f;
            float softness = scanner
                ? Mathf.Max(0.02f, viewportSettings != null ? viewportSettings.scannerEdgeSoftness : 0.02f)
                : Mathf.Max(0.03f, viewportSettings != null ? viewportSettings.binocularEdgeSoftness : 0.03f);

            if (passthroughMaskTexture == null)
            {
                passthroughMaskTexture = new Texture2D(
                    PassthroughMaskResolution,
                    PassthroughMaskResolution,
                    TextureFormat.RGBA32,
                    false)
                {
                    name = "OpticsPassthroughMask",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            Color32[] pixels = new Color32[PassthroughMaskResolution * PassthroughMaskResolution];
            float inv = 1f / (PassthroughMaskResolution - 1);
            for (int y = 0; y < PassthroughMaskResolution; y++)
            {
                float v = y * inv;
                float cy = v - 0.5f;
                for (int x = 0; x < PassthroughMaskResolution; x++)
                {
                    float u = x * inv;
                    float cx = (u - 0.5f) * aspect;
                    float outside;

                    if (scanner)
                    {
                        float edgeX = Mathf.Abs(cx) - halfW;
                        float edgeY = Mathf.Abs(cy) - halfH;
                        float edge = Mathf.Max(edgeX, edgeY);
                        outside = Mathf.SmoothStep(-softness, softness, edge);
                    }
                    else
                    {
                        float dist = Mathf.Sqrt(cx * cx + cy * cy);
                        outside = Mathf.SmoothStep(radius - softness, radius + softness, dist);
                    }

                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(outside * 255f), 0, 255);
                    pixels[y * PassthroughMaskResolution + x] = new Color32(0, 0, 0, a);
                }
            }

            passthroughMaskTexture.SetPixels32(pixels);
            passthroughMaskTexture.Apply(false, false);

            if (passthroughMaskSprite != null)
                DestroyOverlayObject(passthroughMaskSprite);

            passthroughMaskSprite = Sprite.Create(
                passthroughMaskTexture,
                new Rect(0f, 0f, PassthroughMaskResolution, PassthroughMaskResolution),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            passthroughMaskSprite.name = scanner ? "OpticsScannerVignette" : "OpticsBinocularVignette";

            passthroughMaskBuiltForScanner = scanner;
            passthroughMaskBuiltAspect = aspect;
        }

        public void SetVisible(bool visible, ToolType toolType)
        {
            if (!uiBuilt || overlayRoot == null)
            {
                isVisible = false;
                return;
            }

            isVisible = visible;
            ActiveToolType = visible ? toolType : ToolType.None;
            overlayRoot.SetActive(visible);
            if (!visible)
            {
                if (passthroughVignetteImage != null)
                    passthroughVignetteImage.enabled = false;
                SetScanningPopupVisible(false);
                DMUiToolkitOpticsOverlay.SyncFromUgui(this);
                return;
            }

            overlayRoot.transform.SetAsLastSibling();

            bool scanner = toolType == ToolType.Scanner;
            if (binocularRoot != null)
                binocularRoot.SetActive(!scanner);

            if (scannerRoot != null)
                scannerRoot.SetActive(scanner);

            if (sceneAuthoredOverlay && !scanner)
                SuppressRuntimeBinocularCircleMask();

            if (!scanner && !sceneAuthoredOverlay)
                FitBinocularOverlayToCircle();

            // Scene-authored stacks: only toggle roots — leave child Images as the user set them.
            if (!sceneAuthoredOverlay && !scanner)
            {
                if (binocularOuterImage != null)
                    binocularOuterImage.enabled = true;
                if (binocularFrameImage != null)
                    binocularFrameImage.enabled = false;
                if (binocularInnerImage != null)
                    binocularInnerImage.enabled = true;
            }

            if (markerLayer != null)
                markerLayer.gameObject.SetActive(scanner);

            // Authored binocular/scanner art replaces our generated vignette.
            if (passthroughVignetteImage != null)
                passthroughVignetteImage.enabled = false;

            if (viewportImage != null && passthroughMode)
            {
                viewportImage.texture = null;
                viewportImage.enabled = false;
            }

            if (viewportBackground != null && passthroughMode)
                viewportBackground.enabled = false;

            ApplyViewportMode(scanner);
            SetScanningPopupVisible(scanner);

            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsModeLabelSettings modeSettings = library != null ? library.modeLabel : null;
            OpticsHintLabelSettings hintSettings = library != null ? library.hintLabel : null;

            if (modeLabel != null)
            {
                modeLabel.color = scanner
                    ? modeSettings != null ? modeSettings.scannerColor : new Color(0.549f, 1f, 0.82f, 0.949f)
                    : modeSettings != null ? modeSettings.binocularColor : new Color(0.85f, 0.95f, 1f, 0.95f);
                modeLabel.text = scanner
                    ? modeSettings != null ? modeSettings.scannerText : "SCANNER MODE"
                    : modeSettings != null ? modeSettings.binocularText : "BINOCULARS";
            }

            if (hintLabel != null)
            {
                hintLabel.color = hintSettings != null
                    ? hintSettings.color
                    : new Color(0.992f, 0.71f, 0.29f, 1f);
                hintLabel.fontSize = hintSettings != null ? hintSettings.fontSize : 20f;
                hintLabel.text = scanner
                    ? "[RMB] Close  |  [MMB] Sweep"
                    : hintSettings != null ? hintSettings.binocularHint : "[RMB] Close  |  [Scroll] Zoom";
            }

            if (!scanner)
                ClearScannerMarkers();

            DMUiToolkitOpticsOverlay.SyncFromUgui(this);
        }

        private void BindScanningPopup(Transform root)
        {
            if (root == null)
                return;

            Transform existing = root.Find("ScanningPopup");
            if (existing != null)
            {
                scanningPopupLabel = existing.GetComponent<TextMeshProUGUI>()
                    ?? existing.GetComponentInChildren<TextMeshProUGUI>(true);
                if (scanningPopupLabel != null)
                    scanningPopupLabel.gameObject.SetActive(false);
                return;
            }

            EnsureScanningPopup();
        }

        private void EnsureScanningPopup()
        {
            if (scanningPopupLabel != null || overlayRoot == null)
                return;

            GameObject popup = new GameObject("ScanningPopup", typeof(RectTransform));
            popup.transform.SetParent(overlayRoot.transform, false);
            RectTransform rect = popup.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.78f);
            rect.anchorMax = new Vector2(0.5f, 0.78f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 64f);

            scanningPopupLabel = popup.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(scanningPopupLabel);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(scanningPopupLabel, semiBold: true);
            scanningPopupLabel.fontSize = 36f;
            scanningPopupLabel.fontStyle = FontStyles.Bold;
            scanningPopupLabel.alignment = TextAlignmentOptions.Center;
            scanningPopupLabel.color = DarkMatterGenesisUiPalette.Gold;
            scanningPopupLabel.text = "SCANNING";
            scanningPopupLabel.raycastTarget = false;
            popup.SetActive(false);
        }

        private void SetScanningPopupVisible(bool visible)
        {
            EnsureScanningPopup();
            if (scanningPopupLabel == null)
                return;

            scanningPopupLabel.text = "SCANNING";
            scanningPopupLabel.gameObject.SetActive(visible);
            if (visible)
                scanningPopupLabel.transform.SetAsLastSibling();
        }

        private void LateUpdate()
        {
            if (sceneAuthoredOverlay || !isVisible || binocularRoot == null || !binocularRoot.activeSelf)
                return;

            FitBinocularOverlayToCircle();
        }

        /// <summary>
        /// Crosshair/scope art as a centered square; full-screen aspect-correct circular hole mask drawn on top.
        /// Skipped for scene-authored overlays so editor layout, scale, and child active states are preserved.
        /// </summary>
        private void FitBinocularOverlayToCircle()
        {
            if (sceneAuthoredOverlay)
            {
                SuppressRuntimeBinocularCircleMask();
                return;
            }

            RectTransform root = binocularRoot != null ? binocularRoot.transform as RectTransform : null;
            if (root == null)
                return;

            RectTransform parent = root.parent as RectTransform;
            if (parent == null)
                return;

            float parentW = parent.rect.width;
            float parentH = parent.rect.height;
            if (parentW < 1f || parentH < 1f)
                return;

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.sizeDelta = Vector2.zero;
            root.anchoredPosition = Vector2.zero;
            root.localScale = Vector3.one;
            root.localEulerAngles = Vector3.zero;

            // Root image must not fight the circular hole mask.
            Image rootImage = binocularRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = false;
                rootImage.raycastTarget = false;
            }

            float side = Mathf.Min(parentW, parentH);

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                string childName = child.name;
                if (childName.IndexOf("BinocularCircleMask", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                bool isAuthoredMask = childName.IndexOf("mask", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isAuthoredMask)
                {
                    // Authored mask is replaced by the aspect-correct top mask.
                    child.gameObject.SetActive(false);
                    continue;
                }

                // Scope / crosshair: centered square so sprites stay circular and behind the hole mask.
                child.gameObject.SetActive(true);
                child.localScale = Vector3.one;
                child.localEulerAngles = Vector3.zero;
                child.anchorMin = new Vector2(0.5f, 0.5f);
                child.anchorMax = new Vector2(0.5f, 0.5f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = new Vector2(side, side);

                AspectRatioFitter legacyFitter = child.GetComponent<AspectRatioFitter>();
                if (legacyFitter != null)
                    legacyFitter.enabled = false;

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.preserveAspect = true;
                    image.type = Image.Type.Simple;
                    image.enabled = true;
                }
            }

            EnsureBinocularCircleMask(root, side);
            if (binocularCircleMaskRect != null)
                binocularCircleMaskRect.SetAsLastSibling();
        }

        private void EnsureBinocularCircleMask(RectTransform root, float side)
        {
            if (binocularCircleMaskImage == null)
            {
                Transform existing = root.Find("BinocularCircleMask");
                if (existing != null)
                {
                    binocularCircleMaskRect = existing as RectTransform;
                    binocularCircleMaskImage = existing.GetComponent<Image>();
                }
            }

            if (binocularCircleMaskImage == null)
            {
                GameObject maskObject = new GameObject("BinocularCircleMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                maskObject.transform.SetParent(root, false);
                binocularCircleMaskRect = maskObject.GetComponent<RectTransform>();
                binocularCircleMaskImage = maskObject.GetComponent<Image>();
                binocularCircleMaskImage.raycastTarget = false;
                binocularCircleMaskImage.color = Color.white;
            }

            // Full-screen plate; hole is baked with screen aspect so it stays circular.
            binocularCircleMaskRect.anchorMin = Vector2.zero;
            binocularCircleMaskRect.anchorMax = Vector2.one;
            binocularCircleMaskRect.offsetMin = Vector2.zero;
            binocularCircleMaskRect.offsetMax = Vector2.zero;
            binocularCircleMaskRect.localScale = Vector3.one;

            EnsurePassthroughMaskSprite(scanner: false);
            binocularCircleMaskImage.sprite = passthroughMaskSprite;
            binocularCircleMaskImage.type = Image.Type.Simple;
            binocularCircleMaskImage.preserveAspect = false;
            binocularCircleMaskImage.enabled = true;
            binocularCircleMaskImage.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide runtime-generated circular mask when the scene owns binocular presentation.
        /// </summary>
        private void SuppressRuntimeBinocularCircleMask()
        {
            if (binocularRoot == null)
                return;

            Transform existing = binocularRoot.transform.Find("BinocularCircleMask");
            if (existing == null)
                return;

            Image maskImage = existing.GetComponent<Image>();
            if (maskImage != null)
                maskImage.enabled = false;

            existing.gameObject.SetActive(false);
        }

        private void ApplyViewportMode(bool scanner)
        {
            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsViewportPresentation viewportSettings = library != null ? library.viewport : null;

            if (viewportSettings != null)
            {
                scannerHalfWidthPixels = viewportSettings.scannerHalfWidthPixels;
                scannerHalfHeightPixels = viewportSettings.scannerHalfHeightPixels;
            }
            else
            {
                scannerHalfWidthPixels = Screen.height * 0.4f * (Screen.width / (float)Mathf.Max(1, Screen.height));
                scannerHalfHeightPixels = Screen.height * 0.24f;
            }

            if (viewportMaterial == null)
                return;

            if (viewportMaterial.HasProperty(ModeId))
                viewportMaterial.SetFloat(ModeId, scanner ? 1f : 0f);
            if (viewportMaterial.HasProperty(RadiusId))
                viewportMaterial.SetFloat(RadiusId, viewportSettings != null ? viewportSettings.binocularRadius : 0.33f);
            if (viewportMaterial.HasProperty(RectHalfWidthId))
                viewportMaterial.SetFloat(RectHalfWidthId, viewportSettings != null ? viewportSettings.scannerRectHalfWidth : 0.4f);
            if (viewportMaterial.HasProperty(RectHalfHeightId))
                viewportMaterial.SetFloat(RectHalfHeightId, viewportSettings != null ? viewportSettings.scannerRectHalfHeight : 0.24f);
            if (viewportMaterial.HasProperty(EdgeSoftnessId))
            {
                viewportMaterial.SetFloat(
                    EdgeSoftnessId,
                    scanner
                        ? viewportSettings != null ? viewportSettings.scannerEdgeSoftness : 0.012f
                        : viewportSettings != null ? viewportSettings.binocularEdgeSoftness : 0.014f);
            }

            if (viewportMaterial.HasProperty(ScannerFuzzId))
            {
                viewportMaterial.SetFloat(
                    ScannerFuzzId,
                    scanner ? viewportSettings != null ? viewportSettings.scannerFuzz : 0.035f : 0f);
            }

            if (viewportMaterial.HasProperty(TintId))
            {
                viewportMaterial.SetColor(
                    TintId,
                    scanner
                        ? viewportSettings != null ? viewportSettings.scannerTint : new Color(0.82f, 1f, 0.9f, 1f)
                        : viewportSettings != null ? viewportSettings.binocularTint : new Color(0.95f, 0.98f, 1f, 1f));
            }
        }

        public float ScannerHalfWidthPixels => scannerHalfWidthPixels;
        public float ScannerHalfHeightPixels => scannerHalfHeightPixels;

        public void UpdateScannerMarkers(Camera worldCamera, IReadOnlyList<OpticsScanTarget> targets, float viewportRadiusPixels)
        {
            UpdateScannerMarkers(worldCamera, targets, scannerHalfWidthPixels, scannerHalfHeightPixels);
        }

        public void UpdateScannerMarkers(
            Camera worldCamera,
            IReadOnlyList<OpticsScanTarget> targets,
            float halfWidthPixels,
            float halfHeightPixels)
        {
            if (!uiBuilt || markerLayer == null || worldCamera == null)
                return;

            scannerHalfWidthPixels = halfWidthPixels;
            scannerHalfHeightPixels = halfHeightPixels;

            EnsureMarkerPool(targets.Count);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            for (int i = 0; i < markerPool.Count; i++)
            {
                RectTransform marker = markerPool[i];
                if (i >= targets.Count)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                OpticsScanTarget target = targets[i];
                Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.WorldPosition);
                if (screenPoint.z <= 0f)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                Vector2 offset = new Vector2(screenPoint.x, screenPoint.y) - screenCenter;
                offset.x = Mathf.Clamp(offset.x, -halfWidthPixels, halfWidthPixels);
                offset.y = Mathf.Clamp(offset.y, -halfHeightPixels, halfHeightPixels);

                marker.gameObject.SetActive(true);
                marker.anchoredPosition = offset;

                Image dot = marker.GetComponent<Image>();
                if (dot != null)
                {
                    float pulseSpeed = target.IsPostScan ? 2.5f : 4f;
                    float pulseBase = target.IsPostScan ? markerPostScanPulseBase : markerActivePulseBase;
                    float pulse = pulseBase + (1f - pulseBase) * Mathf.Sin(Time.unscaledTime * pulseSpeed + i);
                    Color c = target.MarkerColor;
                    c.a = pulse;
                    dot.color = c;
                }

                TextMeshProUGUI label = marker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = target.Label;
            }

            DMUiToolkitOpticsOverlay.UpdateScannerMarkers(
                worldCamera, targets, halfWidthPixels, halfHeightPixels);
        }

        public void ClearScannerMarkers()
        {
            for (int i = 0; i < markerPool.Count; i++)
            {
                if (markerPool[i] != null)
                    markerPool[i].gameObject.SetActive(false);
            }

            DMUiToolkitOpticsOverlay.ClearScannerMarkers();
        }

        private void EnsureMarkerPool(int requiredCount)
        {
            requiredCount = Mathf.Min(requiredCount, MaxScannerMarkers);
            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsScannerMarkerSettings markerSettings = library != null ? library.scannerMarkers : null;

            while (markerPool.Count < requiredCount)
            {
                GameObject markerObject = new GameObject("ScannerMarker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(markerLayer, false);

                RectTransform markerRect = markerObject.GetComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                Vector2 markerSize = markerSettings != null ? markerSettings.markerSize : new Vector2(18f, 18f);
                markerRect.sizeDelta = markerSize;

                Image dot = markerObject.GetComponent<Image>();
                dot.sprite = markerSettings != null && markerSettings.markerSprite != null
                    ? markerSettings.markerSprite
                    : OpticsUiSprites.ScannerHolographic != null
                        ? OpticsUiSprites.ScannerHolographic
                        : MapUiSprites.Dot;
                dot.material = markerSettings != null ? markerSettings.markerMaterial : null;
                dot.raycastTarget = false;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(markerObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = markerSettings != null ? markerSettings.labelOffset : new Vector2(12f, 0f);
                labelRect.sizeDelta = markerSettings != null ? markerSettings.labelSize : new Vector2(160f, 24f);

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                TmpUiHelper.ApplyDefaultFont(label);
                label.fontSize = markerSettings != null ? markerSettings.labelFontSize : 14f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = markerSettings != null
                    ? markerSettings.labelColor
                    : new Color(0.55f, 1f, 0.85f, 0.95f);

                markerPool.Add(markerRect);
            }
        }

        private static Transform GetOrCreateOverlayCanvas()
        {
            ConsolidateOverlayCanvases();

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && canvas.gameObject.name == OverlayCanvasName)
                    return canvas.transform;
            }

            GameObject canvasObject = new GameObject(
                OverlayCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas newCanvas = canvasObject.GetComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 1000;
            newCanvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return canvasObject.transform;
        }

        private static void ConsolidateOverlayCanvases()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Transform keeper = null;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.gameObject.name != OverlayCanvasName)
                    continue;

                if (keeper == null)
                {
                    keeper = canvas.transform;
                    PurgeForeignHudFromOpticsCanvas(keeper);
                    RemoveDuplicateOverlayRoots(keeper);
                    continue;
                }

                DestroyOverlayObject(canvas.gameObject);
            }
        }

        private static void PurgeForeignHudFromOpticsCanvas(Transform opticsCanvas)
        {
            for (int i = opticsCanvas.childCount - 1; i >= 0; i--)
            {
                Transform child = opticsCanvas.GetChild(i);
                string childName = child.name;
                if (childName == "OpticsOverlay" || childName == "OpticsOverlayHost")
                    continue;

                if (childName.StartsWith("FloatingTargetHealthBar") || childName == "PetTamingProgressUI")
                    DestroyOverlayObject(child.gameObject);
            }
        }

        private static void RemoveDuplicateOverlayRoots(Transform opticsCanvas)
        {
            Transform keeperOverlay = null;
            for (int i = opticsCanvas.childCount - 1; i >= 0; i--)
            {
                Transform child = opticsCanvas.GetChild(i);
                if (child.name != "OpticsOverlay")
                    continue;

                if (keeperOverlay == null)
                    keeperOverlay = child;
                else
                    DestroyOverlayObject(child.gameObject);
            }
        }

        private static Material CreateViewportMaterial()
        {
            Material template = OpticsUiSprites.ViewportMaterialTemplate;
            if (template != null)
                return new Material(template) { name = template.name + "_Runtime" };

            Material resourceTemplate = Resources.Load<Material>("UI/OpticsViewport");
            if (resourceTemplate != null)
                return new Material(resourceTemplate) { name = resourceTemplate.name + "_Runtime" };

            Shader shader = Shader.Find("Project/OpticsViewport");
            if (shader == null)
                shader = Shader.Find("UI/Default");

            return new Material(shader) { name = "OpticsViewportMaterial" };
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image CreateStretchImage(Transform parent, string name, Sprite sprite)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            StretchRect(rect);

            Image image = imageObject.GetComponent<Image>();
            if (sprite != null)
                image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateCenteredImage(Transform parent, string name, Sprite sprite, float size)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            Image image = imageObject.GetComponent<Image>();
            if (sprite != null)
                image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchorY, float fontSize)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorY;
            rect.anchorMax = anchorY;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 36f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            return label;
        }

        private void OnDestroy()
        {
            if (viewportMaterial != null)
                DestroyOverlayObject(viewportMaterial);

            if (passthroughMaskSprite != null)
                DestroyOverlayObject(passthroughMaskSprite);

            if (passthroughMaskTexture != null)
                DestroyOverlayObject(passthroughMaskTexture);

            if (instance == this)
                instance = null;
        }
    }

    public readonly struct OpticsScanTarget
    {
        public OpticsScanTarget(
            Vector3 worldPosition,
            string label,
            Color markerColor,
            OutlineController outline = null,
            bool isPostScan = false)
        {
            WorldPosition = worldPosition;
            Label = label;
            MarkerColor = markerColor;
            Outline = outline;
            IsPostScan = isPostScan;
        }

        public Vector3 WorldPosition { get; }
        public string Label { get; }
        public Color MarkerColor { get; }
        public OutlineController Outline { get; }
        public bool IsPostScan { get; }
    }
}
