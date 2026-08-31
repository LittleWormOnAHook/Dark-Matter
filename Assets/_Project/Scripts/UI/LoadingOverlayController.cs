using System;
using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// "Loading Genesis" overlay. Built in the current scene (no dedicated Loading scene) so it matches this
    /// project's code-built UI idiom. Runs twice per session:
    ///
    /// 1. <see cref="LoadingMode.Boot"/> — covers the first Play Mode frames, then hands off to the main menu.
    /// 2. <see cref="LoadingMode.GameStart"/> — runs after "New Expedition" and starts gameplay on completion.
    ///
    /// Camera flash prevention: a solid black veil stays fully opaque until load + destination handoff finish,
    /// gameplay cameras are blacked out while the loader owns the screen, then the veil fades in from black.
    /// Backdrop: deep-space void with RectTransform flying stars and a distant Blackhole2 sprite.
    /// Dark Matter: Genesis identity only — no Pi, wallet, or legacy branding on this surface.
    /// DMI lettermark is disabled by default on this surface (<see cref="showDmiLogo"/>).
    /// All timing uses unscaled time because both entry points park <c>Time.timeScale</c> at 0.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class LoadingOverlayController : MonoBehaviour
    {
        public const string BackgroundResourcePath = "UI/LoadingGenesis_Background";
        public const string LogoResourcePath = "UI/LoadingGenesis_Logo";
        public const string BlackholeResourcePath = "UI/LoadingGenesis_Blackhole";
        public const string StarfieldShaderName = "Project/DMLoadingStarfield";
        public const string StarfieldMaterialResourcePath = "UI/DMLoadingStarfield";

        private enum LoadingMode
        {
            Boot,
            GameStart,
            SettingsReload
        }

        private struct GatedCameraState
        {
            public Camera Camera;
            public int CullingMask;
            public CameraClearFlags ClearFlags;
            public Color BackgroundColor;
        }

        private struct FlyingStar
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Direction;
            public float Depth;
            public float Speed;
            public float BaseSize;
            public float TwinklePhase;
            public bool Accent;
        }

        private const int OverlaySortingOrder = 32000;
        private const float ProgressCeilingBeforeHandoff = 0.92f;
        /// <summary>Longest the boot pass will wait past its window for a checkpoint that may never arrive.</summary>
        private const float CheckpointGraceSeconds = 4f;
        private const float LogoFrameSize = 430f;
        private const int FlyingStarCount = 48;
        private const float StarTravelRadius = 1180f;
        /// <summary>Up + left from screen center (UI: -x left, +y up). Small distant hole keeps prior offset.</summary>
        private static readonly Vector2 BlackholeAnchoredPosition = new Vector2(-100f, 100f);

        [SerializeField, Range(40f, 200f)] private float blackholeSize = 150f;
        [SerializeField, Range(0.4f, 1f)] private float blackholeAlpha = 0.8f;
        [SerializeField, Range(0.0f, 0.08f)] private float blackholeApproachScale = 0.02f;
        [SerializeField] private bool showDmiLogo = false;
        /// <summary>Screen time and progress bar are both driven by this window.</summary>
        [SerializeField] private float simulatedLoadSeconds = 6f;
        [SerializeField] private float fadeOutSeconds = 0.65f;
        [SerializeField] private float fadeInFromBlackSeconds = 0.55f;

        private static bool bootPending;
        private static LoadingOverlayController activeInstance;
        private static LoadingMode pendingMode = LoadingMode.Boot;
        private static Action pendingCompletion;
        private static string pendingStatusText;
        private static float pendingSimulatedLoadSeconds = -1f;
        private static bool suppressNextBootOverlay;
        private static GameObject earlyBlackoutHost;
        private static readonly List<GatedCameraState> gatedCameras = new List<GatedCameraState>(8);

        private LoadingMode mode = LoadingMode.Boot;
        private Action onCompleted;

        private CanvasGroup contentGroup;
        private CanvasGroup blackVeilGroup;
        private Image glowImage;
        private RectTransform logoArtRect;
        private RectTransform blackholeRect;
        private RectTransform progressFillRect;
        private TextMeshProUGUI percentLabel;
        private Material starfieldMaterial;
        private FlyingStar[] flyingStars;
        private Vector2 blackholeBaseSize;
        private int satisfiedCheckpoints;
        private int shownPercent = -1;
        private bool cameraGateReleased;
        private bool useToolkit;
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");
        private static readonly int SpaceColorId = Shader.PropertyToID("_SpaceColor");
        private static readonly int StarColorId = Shader.PropertyToID("_StarColor");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int StarDensityId = Shader.PropertyToID("_StarDensity");
        private static readonly int StarBrightnessId = Shader.PropertyToID("_StarBrightness");

        /// <summary>True while the loader owns the screen and the main menu must stay hidden.</summary>
        public static bool IsBlockingMenu => bootPending || activeInstance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            bootPending = false;
            activeInstance = null;
            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;
            pendingStatusText = null;
            pendingSimulatedLoadSeconds = -1f;
            suppressNextBootOverlay = false;
            earlyBlackoutHost = null;
            gatedCameras.Clear();
            ExpeditionSceneLoadProgress.Reset();
        }

        // Claimed before any Awake runs so MainMenuController can never win the race and flash its chrome.
        // Early black veil + camera gate cover the gap before AfterSceneLoad builds the full overlay.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClaimBoot()
        {
            if (!Application.isPlaying)
                return;

            if (suppressNextBootOverlay)
            {
                bootPending = false;
                return;
            }

            bootPending = true;
            // Kill world SFX immediately — Invector footsteps/reloads ignore timeScale and will
            // otherwise leak under the loader before GameAudioManager awakens.
            // Only via the gated helper (never from SubsystemRegistration / edit-mode reload).
            GameAudioManager.SyncWorldAudioGate();
            EnsureEarlyBlackout();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying)
                return;

            if (suppressNextBootOverlay)
            {
                suppressNextBootOverlay = false;
                bootPending = false;
                EnsureEarlyBlackout();
                GateGameplayCameras(true);
                return;
            }

            GateGameplayCameras(true);
            EnsureExists();
        }

        /// <summary>Skips the full boot loader on the next scene load (settings Apply reload).</summary>
        public static void SuppressNextBootOverlay()
        {
            suppressNextBootOverlay = true;
        }

        public static void EnsureExists()
        {
            if (!Application.isPlaying || activeInstance != null)
                return;

            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;
            Create();
        }

        /// <summary>
        /// Raise an opaque black cover before menu chrome is torn down so the player camera cannot flash
        /// between the main menu and the expedition loading pass.
        /// </summary>
        public static void EnsureOpaqueCover()
        {
            if (!Application.isPlaying)
                return;

            if (activeInstance != null)
            {
                activeInstance.ForceOpaqueCover();
                GateGameplayCameras(true);
                return;
            }

            EnsureEarlyBlackout();
            GateGameplayCameras(true);
        }

        /// <summary>
        /// Second loading pass: shown when the player leaves the menu, then <paramref name="onComplete"/>
        /// starts gameplay. Replaces the old start-screen popup step.
        /// </summary>
        public static void ShowForGameStart(Action onComplete)
        {
            if (!Application.isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            ExpeditionSceneLoadProgress.Begin();
            EnsureOpaqueCover();

            if (activeInstance != null)
            {
                activeInstance.onCompleted = onComplete;
                return;
            }

            pendingMode = LoadingMode.GameStart;
            pendingCompletion = onComplete;
            Create();
        }

        /// <summary>
        /// Short loading pass while graphics settings are applied via scene reload.
        /// </summary>
        public static void ShowForSettingsReload(Action onComplete)
        {
            if (!Application.isPlaying)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureOpaqueCover();

            if (activeInstance != null)
            {
                activeInstance.mode = LoadingMode.SettingsReload;
                activeInstance.onCompleted = onComplete;
                return;
            }

            pendingStatusText = "Applying settings...";
            pendingSimulatedLoadSeconds = 2f;
            pendingMode = LoadingMode.SettingsReload;
            pendingCompletion = onComplete;
            Create();
        }

        /// <summary>Removes the early blackout veil after a settings reload restore completes.</summary>
        public static void ReleaseOpaqueCover()
        {
            if (!Application.isPlaying)
                return;

            GateGameplayCameras(false);
            DestroyEarlyBlackout();
        }

        private static void Create()
        {
            GameObject host = new GameObject("LoadingGenesisOverlay");
            activeInstance = host.AddComponent<LoadingOverlayController>();
        }

        private static void EnsureEarlyBlackout()
        {
            if (earlyBlackoutHost != null || activeInstance != null)
                return;

            earlyBlackoutHost = new GameObject("LoadingGenesisEarlyBlackout");
            UnityEngine.Object.DontDestroyOnLoad(earlyBlackoutHost);

            Canvas canvas = earlyBlackoutHost.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder + 1;

            earlyBlackoutHost.AddComponent<CanvasScaler>();
            earlyBlackoutHost.AddComponent<GraphicRaycaster>();

            CanvasGroup group = earlyBlackoutHost.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = false;

            MenuUiBuilder.CreateFullScreenPanel(
                earlyBlackoutHost.transform,
                "SolidBlack",
                Color.black,
                blockRaycasts: true);
        }

        private static void DestroyEarlyBlackout()
        {
            if (earlyBlackoutHost == null)
                return;

            UnityEngine.Object.Destroy(earlyBlackoutHost);
            earlyBlackoutHost = null;
        }

        private static void GateGameplayCameras(bool gate)
        {
            if (!gate)
            {
                RestoreGatedCameras();
                return;
            }

            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || !camera.enabled || camera.targetTexture != null)
                    continue;

                if (IsAlreadyGated(camera))
                    continue;

                gatedCameras.Add(new GatedCameraState
                {
                    Camera = camera,
                    CullingMask = camera.cullingMask,
                    ClearFlags = camera.clearFlags,
                    BackgroundColor = camera.backgroundColor
                });

                camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
            }
        }

        private static bool IsAlreadyGated(Camera camera)
        {
            for (int i = 0; i < gatedCameras.Count; i++)
            {
                if (gatedCameras[i].Camera == camera)
                    return true;
            }

            return false;
        }

        private static void RestoreGatedCameras()
        {
            for (int i = 0; i < gatedCameras.Count; i++)
            {
                GatedCameraState state = gatedCameras[i];
                if (state.Camera == null)
                    continue;

                state.Camera.cullingMask = state.CullingMask;
                state.Camera.clearFlags = state.ClearFlags;
                state.Camera.backgroundColor = state.BackgroundColor;
            }

            gatedCameras.Clear();
        }

        private void Awake()
        {
            mode = pendingMode;
            onCompleted = pendingCompletion;
            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;

            if (pendingSimulatedLoadSeconds > 0f)
            {
                simulatedLoadSeconds = pendingSimulatedLoadSeconds;
                pendingSimulatedLoadSeconds = -1f;
            }

            activeInstance = this;
            bootPending = true;

            useToolkit = DMUiToolkitConfig.IsEnabled && DMUiToolkitBootstrap.EnsureExists();
            if (useToolkit)
            {
                BuildOverlay();
                DestroyEarlyBlackout();
            }
            else
            {
                DestroyEarlyBlackout();
                BuildOverlay();
            }

            GateGameplayCameras(true);
            StartCoroutine(RunLoadingSequence());
        }

        private float nextCameraGateCheckUnscaled = -1f;

        private void Update()
        {
            // Re-gate cameras that spawn mid-load (Invector tpCamera, etc.) so they cannot flash.
            // Throttled — FindObjectsByType every frame during boot was hammering the editor.
            bool veilOpaque = useToolkit
                ? DMUiToolkitLoadingOverlay.IsVeilOpaque
                : blackVeilGroup != null && blackVeilGroup.alpha > 0.99f;
            if (!cameraGateReleased && veilOpaque
                && Time.unscaledTime >= nextCameraGateCheckUnscaled)
            {
                nextCameraGateCheckUnscaled = Time.unscaledTime + 0.25f;
                GateGameplayCameras(true);
            }

            if (useToolkit)
            {
                DMUiToolkitLoadingOverlay.Tick(Time.unscaledDeltaTime);
                return;
            }

            // Soft glow pulse only when the DMI lettermark is enabled.
            if (showDmiLogo && glowImage != null)
            {
                float pulse = 0.16f + 0.08f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f));
                glowImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, pulse);
            }

            if (starfieldMaterial != null)
            {
                float aspect = Screen.height > 0
                    ? (float)Screen.width / Screen.height
                    : 16f / 9f;
                starfieldMaterial.SetFloat(AspectId, aspect);
                // Loader parks timeScale at 0 — drive motion with unscaled time.
                starfieldMaterial.SetFloat(UnscaledTimeId, Time.unscaledTime);
            }

            UpdateFlyingStars(Time.unscaledDeltaTime);

            // Subtle distant approach — scale eases in over the load window.
            if (blackholeRect != null && blackholeBaseSize.x > 0f)
            {
                float approach = 1f + blackholeApproachScale
                    * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.55f));
                blackholeRect.sizeDelta = blackholeBaseSize * approach;
            }
        }

        private void UpdateFlyingStars(float unscaledDelta)
        {
            if (flyingStars == null || flyingStars.Length == 0)
                return;

            float dt = Mathf.Max(0f, unscaledDelta);
            for (int i = 0; i < flyingStars.Length; i++)
            {
                FlyingStar star = flyingStars[i];
                if (star.Rect == null || star.Image == null)
                    continue;

                star.Depth += star.Speed * dt;
                if (star.Depth >= 1f)
                {
                    RespawnFlyingStar(ref star, randomizeDepth: false);
                }

                // Ease outward: sparse near the vanishing point, streak toward the camera.
                float travel = star.Depth * star.Depth;
                float radius = Mathf.Lerp(36f, StarTravelRadius, travel);
                star.Rect.anchoredPosition = star.Direction * radius;

                float size = star.BaseSize * Mathf.Lerp(0.55f, 3.4f, travel);
                star.Rect.sizeDelta = new Vector2(size, size);

                float twinkle = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * (1.6f + star.TwinklePhase) + star.TwinklePhase);
                float alpha = Mathf.Clamp01(0.18f + travel * 0.85f) * twinkle;
                Color tint = star.Accent
                    ? DarkMatterGenesisUiPalette.RichFuchsia
                    : DarkMatterGenesisUiPalette.BodyText;
                star.Image.color = DarkMatterGenesisUiPalette.WithAlpha(tint, alpha);

                flyingStars[i] = star;
            }
        }

        private static void RespawnFlyingStar(ref FlyingStar star, bool randomizeDepth)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            // Mild elliptical bias so stars are not a perfect circle ring of directions.
            float ellipticity = UnityEngine.Random.Range(0.82f, 1.18f);
            star.Direction = new Vector2(Mathf.Cos(angle) * ellipticity, Mathf.Sin(angle) / ellipticity).normalized;
            star.Depth = randomizeDepth ? UnityEngine.Random.Range(0f, 0.82f) : UnityEngine.Random.Range(0f, 0.08f);
            star.Speed = UnityEngine.Random.Range(0.085f, 0.22f);
            star.BaseSize = UnityEngine.Random.Range(1.4f, 3.6f);
            star.TwinklePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            star.Accent = UnityEngine.Random.value < 0.08f;
        }

        private void OnDestroy()
        {
            if (useToolkit)
                DMUiToolkitLoadingOverlay.Hide();

            if (activeInstance == this)
                activeInstance = null;

            if (starfieldMaterial != null)
            {
                Destroy(starfieldMaterial);
                starfieldMaterial = null;
            }

            // Safety net if destroyed mid-sequence (domain reload / stop play).
            if (gatedCameras.Count > 0)
                RestoreGatedCameras();
        }

        private void ForceOpaqueCover()
        {
            if (useToolkit)
            {
                DMUiToolkitLoadingOverlay.ForceOpaque();
                return;
            }

            if (blackVeilGroup != null)
                blackVeilGroup.alpha = 1f;
            if (contentGroup != null)
                contentGroup.alpha = 1f;
        }

        private void BuildOverlay()
        {
            if (useToolkit)
            {
                string status = string.IsNullOrEmpty(pendingStatusText) ? "Loading Genesis..." : pendingStatusText;
                pendingStatusText = null;
                DMUiToolkitLoadingOverlay.Show(status);
                return;
            }

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Black veil sits behind branded content so content can fade to black (not to the world camera).
            GameObject veilObject = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "SolidBlackVeil",
                Color.black,
                blockRaycasts: true);
            blackVeilGroup = veilObject.AddComponent<CanvasGroup>();
            blackVeilGroup.alpha = 1f;
            blackVeilGroup.blocksRaycasts = true;
            blackVeilGroup.interactable = false;

            GameObject contentRoot = new GameObject("LoadingContent", typeof(RectTransform));
            contentRoot.transform.SetParent(transform, false);
            MenuUiBuilder.StretchRectToFill(contentRoot.GetComponent<RectTransform>());
            contentGroup = contentRoot.AddComponent<CanvasGroup>();
            contentGroup.alpha = 1f;
            contentGroup.blocksRaycasts = true;
            contentGroup.interactable = false;

            // 1. Deep space void — also swallows clicks so the world stays inert while loading.
            MenuUiBuilder.CreateFullScreenPanel(
                contentRoot.transform,
                "SolidBackdrop",
                Color.black,
                blockRaycasts: true);

            // 2. Procedural starfield + distant black hole (replaces static Io background art).
            BuildBackgroundArt(contentRoot.transform);

            // 3. Brand block: product title (+ optional DMI logo / glow).
            BuildBrandBlock(contentRoot.transform);

            // 4. Progress track + Loading Genesis label.
            BuildProgressBlock(contentRoot.transform);
        }

        private void BuildBackgroundArt(Transform parent)
        {
            BuildStarfieldLayer(parent);
            BuildBlackholeLayer(parent);

            // Light wash so title + progress stay readable without painting rings or grey panels.
            MenuUiBuilder.CreateFullScreenPanel(
                parent,
                "ReadabilityWash",
                DarkMatterGenesisUiPalette.WithAlpha(Color.black, 0.18f),
                blockRaycasts: false);
        }

        private void BuildStarfieldLayer(Transform parent)
        {
            // Soft distant dust only — sparse, no dense cell grid that paints even rings.
            // Primary flying stars are RectTransform Images updated in UpdateFlyingStars.
            Material starfieldTemplate = Resources.Load<Material>(StarfieldMaterialResourcePath);
            Shader starfieldShader = starfieldTemplate != null
                ? starfieldTemplate.shader
                : Shader.Find(StarfieldShaderName);
            GameObject starfieldObject = new GameObject(
                "StarfieldArt",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            starfieldObject.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(starfieldObject.GetComponent<RectTransform>());

            RawImage starfield = starfieldObject.GetComponent<RawImage>();
            starfield.texture = Texture2D.whiteTexture;
            starfield.raycastTarget = false;
            starfield.color = Color.white;

            if (starfieldShader != null)
            {
                starfieldMaterial = starfieldTemplate != null
                    ? new Material(starfieldTemplate)
                    : new Material(starfieldShader);
                starfieldMaterial.name = "DMLoadingStarfield (Instance)";
                starfieldMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Near-black void — not navy/grey panels that read as rings.
                starfieldMaterial.SetColor(SpaceColorId, new Color(0.01f, 0.012f, 0.02f, 1f));
                starfieldMaterial.SetColor(StarColorId, DarkMatterGenesisUiPalette.BodyText);
                starfieldMaterial.SetColor(AccentColorId, DarkMatterGenesisUiPalette.RichFuchsia);
                starfieldMaterial.SetFloat(StarDensityId, 10f);
                starfieldMaterial.SetFloat(StarBrightnessId, 0.55f);
                float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
                starfieldMaterial.SetFloat(AspectId, aspect);
                starfieldMaterial.SetFloat(UnscaledTimeId, Time.unscaledTime);
                starfield.material = starfieldMaterial;
            }

            BuildTransformFlyingStars(parent);
        }

        private void BuildTransformFlyingStars(Transform parent)
        {
            GameObject rootObject = new GameObject("FlyingStars", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(rootObject.GetComponent<RectTransform>());

            Sprite starSprite = ShiftUiTheme.CircleFilled ?? ShiftUiTheme.CircleGlow;
            flyingStars = new FlyingStar[FlyingStarCount];

            for (int i = 0; i < FlyingStarCount; i++)
            {
                GameObject starObject = new GameObject(
                    "Star_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                starObject.transform.SetParent(rootObject.transform, false);

                RectTransform rect = starObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image image = starObject.GetComponent<Image>();
                if (starSprite != null)
                {
                    image.sprite = starSprite;
                    image.type = Image.Type.Simple;
                }
                else
                {
                    MenuUiBuilder.ApplyUiSprite(image);
                    image.type = Image.Type.Simple;
                }

                image.raycastTarget = false;
                image.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.BodyText, 0.35f);

                FlyingStar star = new FlyingStar
                {
                    Rect = rect,
                    Image = image
                };
                RespawnFlyingStar(ref star, randomizeDepth: true);
                flyingStars[i] = star;
            }

            // Place once before first Update so the field is not empty for a frame.
            UpdateFlyingStars(0f);
        }

        private void BuildBlackholeLayer(Transform parent)
        {
            // Prefer the Sprite sub-asset (Texture Type = Sprite + Alpha Is Transparency).
            // Fallback builds a sprite from the Texture2D so a Default import never shows as opaque checkerboard.
            Sprite holeSprite = Resources.Load<Sprite>(BlackholeResourcePath);
            if (holeSprite == null)
            {
                Texture2D holeTexture = Resources.Load<Texture2D>(BlackholeResourcePath);
                if (holeTexture != null)
                {
                    holeSprite = Sprite.Create(
                        holeTexture,
                        new Rect(0f, 0f, holeTexture.width, holeTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    holeSprite.name = "LoadingGenesis_Blackhole_Runtime";
                }
            }

            if (holeSprite == null)
                return;

            GameObject holeObject = new GameObject("BlackholeArt", typeof(RectTransform), typeof(CanvasRenderer));
            holeObject.transform.SetParent(parent, false);
            blackholeRect = holeObject.GetComponent<RectTransform>();
            blackholeBaseSize = new Vector2(blackholeSize, blackholeSize);
            // Distant black hole: 100px left and 100px up from screen center.
            CenterRect(blackholeRect, BlackholeAnchoredPosition, blackholeBaseSize);

            // Transparent Image only — no panel chrome / RawImage behind the sprite.
            Image hole = holeObject.AddComponent<Image>();
            hole.sprite = holeSprite;
            hole.type = Image.Type.Simple;
            hole.preserveAspect = true;
            hole.raycastTarget = false;
            hole.maskable = false;
            hole.color = new Color(1f, 1f, 1f, blackholeAlpha);
            hole.material = null;
        }

        private void BuildBrandBlock(Transform parent)
        {
            if (showDmiLogo)
            {
                Sprite glowSprite = ShiftUiTheme.CircleGlow ?? ShiftUiTheme.SquareGlow;
                if (glowSprite != null)
                {
                    GameObject glowObject = new GameObject("LogoGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    glowObject.transform.SetParent(parent, false);
                    RectTransform glowRect = glowObject.GetComponent<RectTransform>();
                    CenterRect(glowRect, new Vector2(0f, 96f), new Vector2(720f, 720f));

                    glowImage = glowObject.GetComponent<Image>();
                    glowImage.sprite = glowSprite;
                    glowImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.18f);
                    glowImage.raycastTarget = false;
                }

                BuildStaticLogo(parent);
            }

            GameObject titleObject = new GameObject("BrandTitle", typeof(RectTransform));
            titleObject.transform.SetParent(parent, false);
            CenterRect(titleObject.GetComponent<RectTransform>(), new Vector2(0f, -190f), new Vector2(1200f, 90f));

            TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(title);
            ShiftUiTheme.Current?.ApplyFont(title, bold: true);
            title.text = "DARK MATTER : GENESIS";
            title.fontSize = 52f;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 14f;
            title.color = DarkMatterGenesisUiPalette.BodyText;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;
        }

        private void BuildStaticLogo(Transform parent)
        {
            // Prefer Sprite so Unity respects Alpha Is Transparency on the DMI mark; Texture is the fallback.
            Sprite logoSprite = Resources.Load<Sprite>(LogoResourcePath);
            Texture logoTexture = logoSprite != null ? null : Resources.Load<Texture>(LogoResourcePath);
            if (logoSprite == null && logoTexture == null)
                return;

            // Transparent gold DMI lettermark — static, no circular mask (it clips the outer D/I strokes).
            GameObject artObject = new GameObject("LogoArt", typeof(RectTransform), typeof(CanvasRenderer));
            artObject.transform.SetParent(parent, false);
            logoArtRect = artObject.GetComponent<RectTransform>();
            CenterRect(logoArtRect, new Vector2(0f, 96f), new Vector2(LogoFrameSize, LogoFrameSize));

            if (logoSprite != null)
            {
                Image art = artObject.AddComponent<Image>();
                art.sprite = logoSprite;
                art.preserveAspect = true;
                art.raycastTarget = false;
            }
            else
            {
                RawImage art = artObject.AddComponent<RawImage>();
                art.texture = logoTexture;
                art.raycastTarget = false;
            }
        }

        private void BuildProgressBlock(Transform parent)
        {
            GameObject block = new GameObject("ProgressBlock", typeof(RectTransform));
            block.transform.SetParent(parent, false);
            RectTransform blockRect = block.GetComponent<RectTransform>();
            blockRect.anchorMin = new Vector2(0.5f, 0f);
            blockRect.anchorMax = new Vector2(0.5f, 0f);
            blockRect.pivot = new Vector2(0.5f, 0f);
            blockRect.anchoredPosition = new Vector2(0f, 120f);
            blockRect.sizeDelta = new Vector2(760f, 74f);

            TextMeshProUGUI label = CreateProgressLabel(block.transform, "LoadingLabel", TextAlignmentOptions.MidlineLeft);
            label.text = string.IsNullOrEmpty(pendingStatusText) ? "Loading Genesis..." : pendingStatusText;
            pendingStatusText = null;
            label.fontSize = 24f;
            label.characterSpacing = 6f;
            label.color = DarkMatterGenesisUiPalette.BodyText;

            percentLabel = CreateProgressLabel(block.transform, "PercentLabel", TextAlignmentOptions.MidlineRight);
            percentLabel.text = "0%";
            percentLabel.fontSize = 22f;
            percentLabel.color = DarkMatterGenesisUiPalette.Gold;

            GameObject track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            track.transform.SetParent(block.transform, false);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(0f, 10f);

            Image trackImage = track.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(trackImage);
            trackImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.9f);
            trackImage.raycastTarget = false;

            GameObject fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            progressFillRect = fill.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = new Vector2(0f, 1f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = DarkMatterGenesisUiPalette.RichFuchsia;
            fillImage.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateProgressLabel(Transform parent, string name, TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 34f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            ShiftUiTheme.Current?.ApplyFont(label, semiBold: true);
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static void CenterRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private IEnumerator RunLoadingSequence()
        {
            GameAudioManager.EnsureExists();
            GameAudioManager.SyncWorldAudioGate();
            GameAudioManager.Instance?.StartLoadingAmbience();

            // Wall clock, not Time.unscaledDeltaTime: during scene bootstrap a frame can take a full
            // second, and Unity clamps unscaled delta to Time.maximumDeltaTime, which stretched this
            // "6 second" window out to 15-20 real seconds.
            ApplyProgress(0f);

            if (mode == LoadingMode.GameStart)
            {
                yield return RunGameStartSceneLoad();
            }
            else
            {
                float startedAt = Time.realtimeSinceStartup;
                float window = Mathf.Max(0.5f, simulatedLoadSeconds);

                while (true)
                {
                    float elapsed = Time.realtimeSinceStartup - startedAt;
                    float timeProgress = Mathf.Clamp01(elapsed / window);
                    bool ready = mode == LoadingMode.SettingsReload
                                 || AreBootstrapCheckpointsReady()
                                 || elapsed >= window + CheckpointGraceSeconds;
                    ApplyProgress(ready ? timeProgress : Mathf.Min(timeProgress, ProgressCeilingBeforeHandoff));

                    if (ready && timeProgress >= 1f)
                        break;

                    yield return null;
                }
            }

            ApplyProgress(1f);

            // Branded content fades to the solid black veil — never directly onto the player camera.
            yield return FadeCanvasGroup(contentGroup, 1f, 0f, fadeOutSeconds, fadeAmbience: true);
            GameAudioManager.Instance?.StopLoadingAmbience();

            // Destination (menu / gameplay) presents while still fully blacked out.
            HandOffDestination();

            // Cameras restore under the opaque veil, then we fade in from black.
            cameraGateReleased = true;
            GateGameplayCameras(false);
            yield return null;
            yield return FadeCanvasGroup(blackVeilGroup, 1f, 0f, fadeInFromBlackSeconds, fadeAmbience: false);

            Destroy(gameObject);
        }

        private IEnumerator RunGameStartSceneLoad()
        {
            ExpeditionSceneLoadProgress.Begin();
            float shown = 0f;

            while (true)
            {
                float actual = ExpeditionSceneLoadProgress.GetProgress();
                bool worldReady = ExpeditionSceneLoadProgress.IsReady();
                float target = worldReady ? 1f : actual;
                shown = Mathf.MoveTowards(shown, Mathf.Max(shown, target), Time.unscaledDeltaTime * 2.5f);

                if (worldReady && shown >= 0.98f)
                {
                    ApplyProgress(1f);
                    yield break;
                }

                ApplyProgress(worldReady ? shown : Mathf.Min(shown, ProgressCeilingBeforeHandoff));
                yield return null;
            }
        }

        private bool AreBootstrapCheckpointsReady()
        {
            if (satisfiedCheckpoints < 1 && ShiftUiTheme.IsReady)
                satisfiedCheckpoints = 1;
            if (satisfiedCheckpoints < 2 && GameAudioManager.Instance != null)
                satisfiedCheckpoints = 2;
            if (satisfiedCheckpoints < 3 && FindAnyObjectByType<MainMenuController>() != null)
                satisfiedCheckpoints = 3;
            if (satisfiedCheckpoints < 4 && FindAnyObjectByType<UIManager>() != null)
                satisfiedCheckpoints = 4;

            return satisfiedCheckpoints >= 4;
        }

        private void ApplyProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (useToolkit)
            {
                DMUiToolkitLoadingOverlay.SetProgress(progress);
                int toolkitPercent = Mathf.RoundToInt(progress * 100f);
                if (toolkitPercent != shownPercent)
                {
                    shownPercent = toolkitPercent;
                    DMUiToolkitLoadingOverlay.SetPercent(toolkitPercent);
                }

                return;
            }

            if (progressFillRect != null)
                progressFillRect.anchorMax = new Vector2(progress, 1f);

            // Only rebuild the string when the whole-percent readout actually changes.
            int percent = Mathf.RoundToInt(progress * 100f);
            if (percentLabel == null || percent == shownPercent)
                return;

            shownPercent = percent;
            percentLabel.SetText("{0}%", percent);
        }

        private IEnumerator FadeCanvasGroup(
            CanvasGroup group,
            float from,
            float to,
            float durationSeconds,
            bool fadeAmbience)
        {
            if (!useToolkit && group == null)
                yield break;

            GameAudioManager audio = fadeAmbience ? GameAudioManager.Instance : null;
            float duration = Mathf.Max(0.05f, durationSeconds);
            float startedAt = Time.realtimeSinceStartup;
            if (useToolkit)
            {
                if (fadeAmbience)
                    DMUiToolkitLoadingOverlay.SetContentOpacity(from);
                else
                    DMUiToolkitLoadingOverlay.SetVeilOpacity(from);
            }
            else
            {
                group.alpha = from;
            }

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                if (useToolkit)
                {
                    if (fadeAmbience)
                        DMUiToolkitLoadingOverlay.SetContentOpacity(alpha);
                    else
                        DMUiToolkitLoadingOverlay.SetVeilOpacity(alpha);
                }
                else
                {
                    group.alpha = alpha;
                }

                if (audio != null)
                {
                    float ambience = from > to ? alpha : 1f - alpha;
                    audio.SetLoadingAmbienceFade(ambience);
                }

                if (t >= 1f)
                    break;

                yield return null;
            }

            if (useToolkit)
            {
                if (fadeAmbience)
                    DMUiToolkitLoadingOverlay.SetContentOpacity(to);
                else
                    DMUiToolkitLoadingOverlay.SetVeilOpacity(to);
            }
            else
            {
                group.alpha = to;
            }
        }

        private void HandOffDestination()
        {
            // Clear the menu gate before handing off so the menu (or gameplay) is allowed to present
            // itself — still under the opaque black veil until fade-in completes.
            // activeInstance must be cleared here (same as the old CompleteAndHandOff) or
            // ShowMainMenu sees IsBlockingMenu and hides chrome again under the veil.
            bootPending = false;
            activeInstance = null;
            // Stay muted through the main menu; MarkStarted() releases the gate for gameplay.
            GameAudioManager.SyncWorldAudioGate();

            Action callback = onCompleted;
            onCompleted = null;

            // Never let destination handoff exceptions abort the fade-in / camera restore
            // that follows in RunLoadingSequence (that left players on a permanent black veil).
            try
            {
                if (callback != null)
                    callback();
                else
                    MainCanvasFlow.Refresh();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
