using System.Collections.Generic;
using Project.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Fullscreen ADS / scanner chrome as a sibling UIDocument above HUD.
    /// Optics camera / RT stay on OpticsCameraRig; this is the Toolkit overlay.
    /// </summary>
    [DefaultExecutionOrder(32500)]
    [DisallowMultipleComponent]
    public sealed class DMUiToolkitOpticsOverlay : MonoBehaviour
    {
        public const string HostName = "UITK_Optics";
        public const int SortingOrder = 1100;
        public const string UxmlPath = "Assets/UI Toolkit/Screens/OpticsOverlay.uxml";
        public const string UssPath = "Assets/UI Toolkit/Screens/OpticsOverlay.uss";
        private const int MaxMarkers = 24;
        private const int PassthroughMaskResolution = 512;

        private static DMUiToolkitOpticsOverlay instance;
        private UIDocument document;
        private VisualElement opticsRoot;
        private VisualElement viewport;
        private VisualElement dim;
        private VisualElement binocularRoot;
        private VisualElement scopeOuter;
        private VisualElement scopeInner;
        private VisualElement scannerRoot;
        private VisualElement scannerBorderTop;
        private VisualElement scannerBorderBottom;
        private VisualElement scannerBorderLeft;
        private VisualElement scannerBorderRight;
        private VisualElement markersRoot;
        private Label modeLabel;
        private Label hintLabel;
        private Label rangeLabel;
        private Label scanningLabel;
        private bool visible;
        private bool passthrough;
        private ToolType toolType = ToolType.None;
        private RenderTexture boundTexture;
        private Texture2D passthroughMask;
        private bool maskBuiltForScanner;
        private float maskBuiltAspect = -1f;
        private readonly List<VisualElement> markerPool = new List<VisualElement>();
        private OpticsOverlayUI boundUgui;
        private Canvas boundOpticsCanvas;
        private DMScopeReticleElement reticle;      // 2026-10-13 (h) new scope art
        private VisualElement crosshairH;
        private VisualElement crosshairV;
        private VisualElement scopeCrosshairSprite;
        private bool scopeShowing;
        private float maskBuiltRadius = -1f;
        private const float RifleCrosshairBasePixels = 196f;

        private static bool WeaponScopeActive => Project.Combat.DMScopeMode.IsScopeViewActive;

        public static bool IsShowing => instance != null && instance.visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }


        public static void EnsureAttached()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            if (instance != null)
                return;

            GameObject host = FindHost();
            if (host == null)
                host = CreateHost();

            if (host.GetComponent<DMUiToolkitOpticsOverlay>() == null)
                host.AddComponent<DMUiToolkitOpticsOverlay>();
        }

        public static void SyncFromUgui(OpticsOverlayUI ugui)
        {
            EnsureAttached();
            if (instance == null || ugui == null)
                return;

            if (WeaponScopeActive)
                return;

            instance.ApplyState(
                ugui.IsVisible,
                ugui.ActiveToolType,
                ugui.IsPassthroughMode,
                ugui.BoundRenderTexture);
        }

        public static void UpdateScannerMarkers(
            Camera worldCamera,
            IReadOnlyList<OpticsScanTarget> targets,
            float halfWidthPixels,
            float halfHeightPixels)
        {
            if (instance == null || !instance.visible)
                return;

            instance.PullScannerMarkers(worldCamera, targets, halfWidthPixels, halfHeightPixels);
        }

        public static void ClearScannerMarkers()
        {
            instance?.HideMarkers();
        }

        private static GameObject FindHost()
        {
            return DMUiToolkitOverlayDocument.FindNamed(HostName);
        }

        private static GameObject CreateHost()
        {
            GameObject host = new GameObject(HostName);
            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap != null)
                host.transform.SetParent(bootstrap.transform.parent, false);
            DMUiToolkitOverlayDocument.RegisterNamed(HostName, host);
            return host;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            document = GetComponent<UIDocument>();
            if (document == null)
                document = gameObject.AddComponent<UIDocument>();

            BindDocument();
        }

        private void OnEnable()
        {
            instance = this;
            BindDocument();
        }

        private void LateUpdate()
        {
            if (document == null)
                return;

            Project.Combat.DM_ScopeProfile scopeProfile = Project.Combat.DM_ScopeProfile.Live;
            bool scopeActive = WeaponScopeActive;
            if (scopeActive && DMUiToolkitConfig.IsEnabled)
                EnsureAttached();

            // Weapon scope + binoculars/scanner: UITK drives; retire uGUI optics canvas (no MainCanvas duplicate).
            bool driving = DMUiToolkitConfig.IsEnabled
                && (scopeActive || DMUiToolkitHud.IsDriving || scopeProfile.retireUguiOptics);
            SetOpticsCanvasEnabled(scopeActive && DMUiToolkitConfig.IsEnabled ? false : !driving);
            if (!driving)
                return;

            if (scopeActive)
            {
                ApplyScopeState(scopeProfile);
                return;
            }
            if (scopeShowing)
            {
                scopeShowing = false;
                if (opticsRoot != null) opticsRoot.style.opacity = 1f;
                SetDisplay(scopeCrosshairSprite, false);
                ClearPassthroughDim();
                ApplyState(false, ToolType.None, true, null);
            }

            if (boundUgui == null)
                boundUgui = FindAnyObjectByType<OpticsOverlayUI>(FindObjectsInactive.Include);
            if (boundUgui != null)
                ApplyState(boundUgui.IsVisible, boundUgui.ActiveToolType, boundUgui.IsPassthroughMode, boundUgui.BoundRenderTexture);
        }

        private void ApplyScopeState(Project.Combat.DM_ScopeProfile p)
        {
            scopeShowing = true;
            visible = true;
            toolType = ToolType.Binoculars;
            passthrough = true;
            boundTexture = null;

            bool rifle = Project.Combat.DMScopeMode.IsScopedRifle;
            Vector2 sway = Project.Combat.DMScopeMode.ReticleSway;
            float ringDiameterPx = p.ResolveRingOuterDiameterPx(rifle);

            SetHostVisible(true);
            SetDisplay(binocularRoot, true);
            SetDisplay(scannerRoot, false);
            SetScannerBorderVisible(false);
            SetDisplay(scanningLabel, false);
            SetDisplay(modeLabel, false);
            SetDisplay(hintLabel, true);
            SetDisplay(viewport, false);

            bool useNewArt = p.useNewScopeArt;
            bool useSpriteOuter = p.scopeOverlayUseSpriteCrosshair && useNewArt;
            Sprite crossSprite = useSpriteOuter ? p.ResolveRifleScopeCrosshairSprite() : null;
            bool showOuterSprite = crossSprite != null && useSpriteOuter;

            SetDisplay(scopeOuter, !useNewArt);
            SetDisplay(scopeInner, false);
            SetDisplay(crosshairH, false);
            SetDisplay(crosshairV, false);

            float reticlePresentationScale = rifle
                ? 1f
                : Mathf.Clamp(p.pistolScopeReticleScale, 0.1f, 1f);
            float ringLayoutDiameterPx = p.ResolveCenterMaskOuterDiameterPx(rifle, reticlePresentationScale);

            ClearPassthroughDim();

            if (reticle != null)
            {
                reticle.Offset = sway;
                reticle.UseWeaponScopedColors = useNewArt;
                reticle.WeaponIsRifle = rifle;
                if (useNewArt)
                {
                    reticle.Style = DMScopeReticleElement.ReticleStyle.ScopedInnerRing;
                    reticle.RingOuterDiameterPx = ringDiameterPx;
                    reticle.PresentationScale = reticlePresentationScale;
                    reticle.DrawCrosshair = true;
                    reticle.DrawHexGrid = false;
                    SetDisplay(reticle, true);
                    reticle.Refresh();
                    reticle.BringToFront();
                }
                else
                {
                    reticle.PresentationScale = 1f;
                    SetDisplay(reticle, false);
                }
            }

            SetDisplay(scopeCrosshairSprite, false);
            if (showOuterSprite && scopeCrosshairSprite != null)
            {
                float crossPx = RifleCrosshairBasePixels * p.ResolveOuterCrosshairScale(rifle);
                scopeCrosshairSprite.style.width = crossPx;
                scopeCrosshairSprite.style.height = crossPx;
                scopeCrosshairSprite.style.left = Length.Percent(50f);
                scopeCrosshairSprite.style.top = Length.Percent(50f);
                scopeCrosshairSprite.style.right = StyleKeyword.Auto;
                scopeCrosshairSprite.style.bottom = StyleKeyword.Auto;
                scopeCrosshairSprite.style.marginLeft = -crossPx * 0.5f + sway.x;
                scopeCrosshairSprite.style.marginTop = -crossPx * 0.5f + sway.y;
                scopeCrosshairSprite.style.unityBackgroundImageTintColor = p.ResolveOuterCrosshairTint(rifle);
                if (!DMUiToolkitStyle.TrySetSpriteBackground(scopeCrosshairSprite, crossSprite, ScaleMode.ScaleToFit)
                    && crossSprite.texture != null)
                    DMUiToolkitStyle.TrySetTextureBackground(scopeCrosshairSprite, crossSprite.texture, ScaleMode.ScaleToFit);

                SetDisplay(scopeCrosshairSprite, true);
                scopeCrosshairSprite.SendToBack();

                if (reticle != null)
                    reticle.BringToFront();
            }

            if (rangeLabel != null)
            {
                float viewportRadiusPx = ringLayoutDiameterPx * 0.5f;
                float bottomInset = Screen.height * 0.5f - viewportRadiusPx * 0.88f;
                rangeLabel.style.bottom = Mathf.Max(36f, bottomInset);
            }

            if (reticle != null && reticle.resolvedStyle.display != DisplayStyle.None)
                reticle.BringToFront();

            if (opticsRoot != null)
                opticsRoot.style.opacity = 1f;

            if (hintLabel != null)
            {
                hintLabel.text = Time.unscaledTime < Project.Combat.DMScopeMode.BlockedAmmoUntil
                    ? Project.Combat.DMScopeMode.BlockedAmmoMessage
                    : p.scopeHint;
                SetDisplay(hintLabel, true);
            }

            if (rangeLabel != null)
            {
                rangeLabel.text = Project.Combat.DMScopeMode.FormatScopedRangeReadout();
                SetDisplay(rangeLabel, true);
            }
        }

        private void SetOpticsCanvasEnabled(bool enabled)
        {
            if (boundOpticsCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    if (canvas != null && canvas.gameObject.name == "OpticsOverlayCanvas")
                    {
                        boundOpticsCanvas = canvas;
                        break;
                    }
                }
            }

            if (boundOpticsCanvas != null && boundOpticsCanvas.enabled != enabled)
                boundOpticsCanvas.enabled = enabled;

            if (!enabled)
                OpticsOverlayUI.EnsureRetiredWhenUitkOwns();
        }

        private void OnDestroy()
        {
            if (passthroughMask != null)
            {
                Destroy(passthroughMask);
                passthroughMask = null;
            }

            if (instance == this)
                instance = null;
        }

        private void BindDocument()
        {
            document = DMUiToolkitOverlayDocument.Ensure(HostName, UxmlPath, UssPath, SortingOrder);
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            opticsRoot = root.Q<VisualElement>("optics-root") ?? root;
            viewport = root.Q<VisualElement>("optics-viewport");
            dim = root.Q<VisualElement>("optics-dim");
            binocularRoot = root.Q<VisualElement>("optics-binocular");
            scopeOuter = root.Q<VisualElement>("optics-scope-outer");
            scopeInner = root.Q<VisualElement>("optics-scope-inner");
            scannerRoot = root.Q<VisualElement>("optics-scanner");
            scannerBorderTop = root.Q<VisualElement>("optics-scanner-border-top");
            scannerBorderBottom = root.Q<VisualElement>("optics-scanner-border-bottom");
            scannerBorderLeft = root.Q<VisualElement>("optics-scanner-border-left");
            scannerBorderRight = root.Q<VisualElement>("optics-scanner-border-right");
            markersRoot = root.Q<VisualElement>("optics-markers");
            modeLabel = root.Q<Label>("optics-mode");
            hintLabel = root.Q<Label>("optics-hint");
            rangeLabel = root.Q<Label>("optics-range");
            scanningLabel = root.Q<Label>("optics-scanning");
            crosshairH = root.Q<VisualElement>("optics-crosshair-h");
            crosshairV = root.Q<VisualElement>("optics-crosshair-v");
            VisualElement scopeHost = opticsRoot ?? root;
            if (reticle == null || reticle.parent == null)
            {
                reticle = new DMScopeReticleElement();
                int idx = binocularRoot != null && binocularRoot.parent == scopeHost
                    ? scopeHost.IndexOf(binocularRoot) + 1
                    : scopeHost.childCount;
                scopeHost.Insert(idx, reticle);
            }

            scopeCrosshairSprite = root.Q<VisualElement>("optics-scope-crosshair");
            if (scopeCrosshairSprite == null || scopeCrosshairSprite.parent != scopeHost)
            {
                if (scopeCrosshairSprite != null)
                    scopeCrosshairSprite.RemoveFromHierarchy();

                scopeCrosshairSprite = new VisualElement { name = "optics-scope-crosshair" };
                scopeCrosshairSprite.AddToClassList("dmg-optics-scope-crosshair");
                scopeCrosshairSprite.pickingMode = PickingMode.Ignore;
                scopeCrosshairSprite.style.position = Position.Absolute;
                scopeHost.Add(scopeCrosshairSprite);
            }

            ApplySprites();
            SetHostVisible(false);
        }

        private void ApplySprites()
        {
            SetBackground(scopeOuter, OpticsUiSprites.BinocularScopeOuter);
            SetBackground(scopeInner, OpticsUiSprites.BinocularScopeInnerGlow);
            if (viewport != null && OpticsUiSprites.ViewportBackground != null)
                SetBackground(viewport, OpticsUiSprites.ViewportBackground);
        }

        private static void SetBackground(VisualElement element, Sprite sprite)
        {
            if (element == null)
                return;

            DMUiToolkitStyle.TrySetSpriteBackground(element, sprite, ScaleMode.ScaleToFit);
        }

        private void ApplyState(bool show, ToolType type, bool passthroughMode, RenderTexture texture)
        {
            if (WeaponScopeActive)
                return;

            visible = show;
            toolType = type;
            passthrough = passthroughMode;
            boundTexture = texture;

            if (!show)
            {
                SetHostVisible(false);
                HideMarkers();
                return;
            }

            SetHostVisible(true);
            bool scanner = type == ToolType.Scanner;
            SetDisplay(binocularRoot, !scanner);
            SetDisplay(scannerRoot, scanner);
            SetScannerBorderVisible(scanner);
            SetDisplay(scanningLabel, false);
            SetDisplay(modeLabel, !scanner);

            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsModeLabelSettings modeSettings = library != null ? library.modeLabel : null;
            OpticsHintLabelSettings hintSettings = library != null ? library.hintLabel : null;

            if (modeLabel != null && !scanner)
            {
                modeLabel.text = modeSettings != null ? modeSettings.binocularText : "BINOCULARS";
                modeLabel.style.color = modeSettings != null
                    ? modeSettings.binocularColor
                    : new Color(0.85f, 0.95f, 1f, 0.95f);
            }

            if (hintLabel != null)
            {
                hintLabel.text = scanner
                    ? "[RMB] Close  |  [MMB] Sweep"
                    : hintSettings != null ? hintSettings.binocularHint : "[RMB] Close  |  [Scroll] Zoom";
                SetDisplay(hintLabel, true);
            }

            ApplyViewport(scanner);

            // 2026-10-13 (h): new procedural scope art for binoculars + scope (old sprite frame hidden).
            bool newArt = !scanner && Project.Combat.DM_ScopeProfile.Live.useNewScopeArt;
            SetDisplay(scopeOuter, !newArt);
            SetDisplay(scopeInner, !newArt);
            SetDisplay(crosshairH, !newArt);
            SetDisplay(crosshairV, !newArt);
            if (reticle != null)
            {
                SetDisplay(reticle, newArt);
                if (newArt)
                {
                    OpticsCrosshairLibrary lib = OpticsUiSprites.Current;
                    reticle.RadiusFraction = lib != null && lib.viewport != null ? Mathf.Max(0.2f, lib.viewport.binocularRadius) : 0.38f;
                    reticle.Offset = Vector2.zero;
                    reticle.Refresh();
                }
            }
        }

        private void SetScannerBorderVisible(bool show)
        {
            float borderWidth = ResolveScannerBorderWidthPixels();
            ApplyScannerBorderWidth(borderWidth);
            SetDisplay(scannerBorderTop, show);
            SetDisplay(scannerBorderBottom, show);
            SetDisplay(scannerBorderLeft, show);
            SetDisplay(scannerBorderRight, show);
        }

        private static float ResolveScannerBorderWidthPixels()
        {
            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsViewportPresentation viewport = library != null ? library.viewport : null;
            return viewport != null && viewport.scannerBorderWidthPixels > 0f
                ? viewport.scannerBorderWidthPixels
                : 30f;
        }

        private void ApplyScannerBorderWidth(float borderWidth)
        {
            if (scannerBorderTop != null)
                scannerBorderTop.style.height = borderWidth;
            if (scannerBorderBottom != null)
                scannerBorderBottom.style.height = borderWidth;
            if (scannerBorderLeft != null)
                scannerBorderLeft.style.width = borderWidth;
            if (scannerBorderRight != null)
                scannerBorderRight.style.width = borderWidth;
        }

        private void ApplyViewport(bool scanner)
        {
            if (viewport == null)
                return;

            if (passthrough || boundTexture == null)
            {
                viewport.style.backgroundImage = StyleKeyword.None;
                viewport.style.backgroundColor = Color.clear;
                SetDisplay(viewport, false);
                if (scanner || scopeShowing)
                    ClearPassthroughDim();
                else
                    ApplyPassthroughDim(scanner: false);
                return;
            }

            SetDisplay(viewport, true);
            viewport.style.backgroundColor = Color.black;
            DMUiToolkitStyle.TrySetRenderTextureBackground(viewport, boundTexture, ScaleMode.ScaleAndCrop);
            if (dim != null)
            {
                dim.style.backgroundImage = StyleKeyword.None;
                dim.style.backgroundColor = scanner ? Color.clear : new Color(0f, 0f, 0f, 0.35f);
                SetDisplay(dim, !scanner);
            }
        }

        private void ClearPassthroughDim()
        {
            if (dim == null)
                return;

            dim.style.backgroundImage = StyleKeyword.None;
            dim.style.backgroundColor = Color.clear;
            SetDisplay(dim, false);
        }

        private void ApplyPassthroughDim(bool scanner)
        {
            if (dim == null)
                return;

            if (scopeShowing)
            {
                ClearPassthroughDim();
                return;
            }

            EnsurePassthroughMask(scanner);
            if (passthroughMask != null)
            {
                dim.style.backgroundColor = Color.clear;
                DMUiToolkitStyle.TrySetTextureBackground(dim, passthroughMask, ScaleMode.StretchToFill);
                SetDisplay(dim, true);
                return;
            }

            dim.style.backgroundImage = StyleKeyword.None;
            dim.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            SetDisplay(dim, true);
        }

        private void EnsurePassthroughMask(bool scanner)
        {
            float aspect = 1.777f;
            if (Screen.height > 0)
                aspect = Screen.width / (float)Mathf.Max(1, Screen.height);

            OpticsCrosshairLibrary library = OpticsUiSprites.Current;
            OpticsViewportPresentation settings = library != null ? library.viewport : null;
            float radius = settings != null ? Mathf.Max(0.2f, settings.binocularRadius) : 0.38f;

            bool needsRebuild = passthroughMask == null
                || maskBuiltForScanner != scanner
                || Mathf.Abs(maskBuiltAspect - aspect) > 0.01f
                || Mathf.Abs(maskBuiltRadius - radius) > 0.005f;
            if (!needsRebuild)
                return;

            if (passthroughMask == null)
            {
                passthroughMask = new Texture2D(PassthroughMaskResolution, PassthroughMaskResolution, TextureFormat.RGBA32, false)
                {
                    name = "UITKOpticsPassthroughMask",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            float halfW = settings != null ? settings.scannerRectHalfWidth : 0.4f;
            float halfH = settings != null ? settings.scannerRectHalfHeight : 0.24f;
            float softness = scanner
                ? Mathf.Max(0.02f, settings != null ? settings.scannerEdgeSoftness : 0.02f)
                : Mathf.Max(0.03f, settings != null ? settings.binocularEdgeSoftness : 0.03f);

            Color32[] pixels = new Color32[PassthroughMaskResolution * PassthroughMaskResolution];
            for (int y = 0; y < PassthroughMaskResolution; y++)
            {
                float v = (y / (float)(PassthroughMaskResolution - 1) - 0.5f) * 2f;
                for (int x = 0; x < PassthroughMaskResolution; x++)
                {
                    float u = (x / (float)(PassthroughMaskResolution - 1) - 0.5f) * 2f * aspect;
                    float dist;
                    if (scanner)
                        dist = Mathf.Max(Mathf.Abs(u) / Mathf.Max(0.01f, halfW * 2f * aspect), Mathf.Abs(v) / Mathf.Max(0.01f, halfH * 2f));
                    else
                        dist = Mathf.Sqrt(u * u + v * v) / Mathf.Max(0.01f, radius * 2f);

                    float a = Mathf.Clamp01((dist - 1f + softness) / Mathf.Max(0.001f, softness));
                    byte alpha = (byte)Mathf.RoundToInt(a * 220f);
                    pixels[y * PassthroughMaskResolution + x] = new Color32(0, 0, 0, alpha);
                }
            }

            passthroughMask.SetPixels32(pixels);
            passthroughMask.Apply(false, false);
            maskBuiltForScanner = scanner;
            maskBuiltAspect = aspect;
            maskBuiltRadius = radius;
        }

        private void PullScannerMarkers(
            Camera worldCamera,
            IReadOnlyList<OpticsScanTarget> targets,
            float halfWidthPixels,
            float halfHeightPixels)
        {
            if (markersRoot == null || worldCamera == null)
                return;

            if (halfWidthPixels <= 0f || halfHeightPixels <= 0f)
            {
                OpticsCrosshairLibrary library = OpticsUiSprites.Current;
                OpticsViewportPresentation viewport = library != null ? library.viewport : null;
                if (viewport != null)
                    viewport.GetScannerMarkerHalfExtents(out halfWidthPixels, out halfHeightPixels);
                else
                {
                    float border = ResolveScannerBorderWidthPixels();
                    halfWidthPixels = Screen.width * 0.5f - border;
                    halfHeightPixels = Screen.height * 0.5f - border;
                }
            }

            EnsureMarkerPool(targets != null ? targets.Count : 0);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            int count = targets != null ? targets.Count : 0;
            for (int i = 0; i < markerPool.Count; i++)
            {
                VisualElement marker = markerPool[i];
                if (i >= count)
                {
                    SetDisplay(marker, false);
                    continue;
                }

                OpticsScanTarget target = targets[i];
                Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.WorldPosition);
                if (screenPoint.z <= 0f)
                {
                    SetDisplay(marker, false);
                    continue;
                }

                Vector2 offset = new Vector2(screenPoint.x, screenPoint.y) - screenCenter;
                offset.x = Mathf.Clamp(offset.x, -halfWidthPixels, halfWidthPixels);
                offset.y = Mathf.Clamp(offset.y, -halfHeightPixels, halfHeightPixels);

                float pulseSpeed = target.IsPostScan ? 2.5f : 4f;
                float pulseBase = target.IsPostScan ? 0.55f : 0.7f;
                float pulse = pulseBase + (1f - pulseBase) * Mathf.Sin(Time.unscaledTime * pulseSpeed + i);
                Color c = target.MarkerColor;
                c.a = pulse;

                marker.style.left = Length.Percent(50f);
                marker.style.bottom = Length.Percent(50f);
                marker.style.marginLeft = offset.x - 9f;
                marker.style.marginBottom = offset.y - 9f;
                marker.style.backgroundColor = c;
                SetDisplay(marker, true);

                Label label = marker.Q<Label>();
                if (label != null)
                    label.text = target.Label ?? string.Empty;
            }
        }

        private void EnsureMarkerPool(int requiredCount)
        {
            requiredCount = Mathf.Min(requiredCount, MaxMarkers);
            while (markerPool.Count < requiredCount)
            {
                VisualElement marker = new VisualElement();
                marker.name = "optics-marker-" + markerPool.Count;
                marker.AddToClassList("dmg-optics-marker");
                marker.pickingMode = PickingMode.Ignore;
                Label label = new Label();
                label.AddToClassList("dmg-optics-marker-label");
                label.pickingMode = PickingMode.Ignore;
                marker.Add(label);
                markersRoot.Add(marker);
                markerPool.Add(marker);
            }
        }

        private void HideMarkers()
        {
            for (int i = 0; i < markerPool.Count; i++)
                SetDisplay(markerPool[i], false);
        }

        private void SetHostVisible(bool show)
        {
            if (opticsRoot != null)
                opticsRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (document != null && document.gameObject.activeSelf != show)
                document.gameObject.SetActive(true);

            if (!show)
            {
                SetDisplay(binocularRoot, false);
                SetDisplay(scannerRoot, false);
                SetDisplay(scanningLabel, false);
            }
        }

        private static void SetDisplay(VisualElement element, bool show)
        {
            if (element != null)
                element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

    }
}
