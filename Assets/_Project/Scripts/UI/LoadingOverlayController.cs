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
    /// Dark Matter: Genesis identity only — no Pi, wallet, or legacy branding on this surface.
    /// All timing uses unscaled time because both entry points park <c>Time.timeScale</c> at 0.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class LoadingOverlayController : MonoBehaviour
    {
        public const string BackgroundResourcePath = "UI/LoadingGenesis_Background";
        public const string LogoResourcePath = "UI/LoadingGenesis_Logo";

        private enum LoadingMode
        {
            Boot,
            GameStart
        }

        private struct GatedCameraState
        {
            public Camera Camera;
            public int CullingMask;
            public CameraClearFlags ClearFlags;
            public Color BackgroundColor;
        }

        private const int OverlaySortingOrder = 32000;
        private const float ProgressCeilingBeforeHandoff = 0.92f;
        /// <summary>Longest the boot pass will wait past its window for a checkpoint that may never arrive.</summary>
        private const float CheckpointGraceSeconds = 4f;
        private const float LogoFrameSize = 430f;

        [SerializeField, Range(0.30f, 0.75f)] private float backgroundImageAlpha = 0.50f;
        /// <summary>Screen time and progress bar are both driven by this window.</summary>
        [SerializeField] private float simulatedLoadSeconds = 6f;
        [SerializeField] private float fadeOutSeconds = 0.65f;
        [SerializeField] private float fadeInFromBlackSeconds = 0.55f;

        private static bool bootPending;
        private static LoadingOverlayController activeInstance;
        private static LoadingMode pendingMode = LoadingMode.Boot;
        private static Action pendingCompletion;
        private static GameObject earlyBlackoutHost;
        private static readonly List<GatedCameraState> gatedCameras = new List<GatedCameraState>(8);

        private LoadingMode mode = LoadingMode.Boot;
        private Action onCompleted;

        private CanvasGroup contentGroup;
        private CanvasGroup blackVeilGroup;
        private Image glowImage;
        private RectTransform logoArtRect;
        private RectTransform progressFillRect;
        private TextMeshProUGUI percentLabel;
        private int satisfiedCheckpoints;
        private int shownPercent = -1;
        private bool cameraGateReleased;

        /// <summary>True while the loader owns the screen and the main menu must stay hidden.</summary>
        public static bool IsBlockingMenu => bootPending || activeInstance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            bootPending = false;
            activeInstance = null;
            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;
            earlyBlackoutHost = null;
            gatedCameras.Clear();
        }

        // Claimed before any Awake runs so MainMenuController can never win the race and flash its chrome.
        // Early black veil + camera gate cover the gap before AfterSceneLoad builds the full overlay.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClaimBoot()
        {
            if (!Application.isPlaying)
                return;

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

            GateGameplayCameras(true);
            EnsureExists();
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

            activeInstance = this;
            bootPending = true;

            DestroyEarlyBlackout();
            BuildOverlay();
            GateGameplayCameras(true);
            StartCoroutine(RunLoadingSequence());
        }

        private float nextCameraGateCheckUnscaled = -1f;

        private void Update()
        {
            // Re-gate cameras that spawn mid-load (Invector tpCamera, etc.) so they cannot flash.
            // Throttled — FindObjectsByType every frame during boot was hammering the editor.
            if (!cameraGateReleased && blackVeilGroup != null && blackVeilGroup.alpha > 0.99f
                && Time.unscaledTime >= nextCameraGateCheckUnscaled)
            {
                nextCameraGateCheckUnscaled = Time.unscaledTime + 0.25f;
                GateGameplayCameras(true);
            }

            // Soft glow pulse only — DMI lettermark stays static (no tumble/spin).
            if (glowImage != null)
            {
                float pulse = 0.16f + 0.08f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f));
                glowImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, pulse);
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
                activeInstance = null;

            // Safety net if destroyed mid-sequence (domain reload / stop play).
            if (gatedCameras.Count > 0)
                RestoreGatedCameras();
        }

        private void ForceOpaqueCover()
        {
            if (blackVeilGroup != null)
                blackVeilGroup.alpha = 1f;
            if (contentGroup != null)
                contentGroup.alpha = 1f;
        }

        private void BuildOverlay()
        {
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

            // 1. Solid navy backdrop — also swallows clicks so the world stays inert while loading.
            MenuUiBuilder.CreateFullScreenPanel(
                contentRoot.transform,
                "SolidBackdrop",
                SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 1f),
                blockRaycasts: true);

            // 2. Atmospheric Io art at tunable alpha over the navy.
            BuildBackgroundArt(contentRoot.transform);

            // 3. Brand block: soft glow, static DMI logo, product title.
            BuildBrandBlock(contentRoot.transform);

            // 4. Progress track + Loading Genesis label.
            BuildProgressBlock(contentRoot.transform);
        }

        private void BuildBackgroundArt(Transform parent)
        {
            Texture backgroundTexture = Resources.Load<Texture>(BackgroundResourcePath);
            if (backgroundTexture == null)
                return;

            GameObject artObject = new GameObject("BackgroundArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            artObject.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(artObject.GetComponent<RectTransform>());

            RawImage art = artObject.GetComponent<RawImage>();
            art.texture = backgroundTexture;
            art.color = new Color(1f, 1f, 1f, backgroundImageAlpha);
            art.raycastTarget = false;
        }

        private void BuildBrandBlock(Transform parent)
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
                glowImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.18f);
                glowImage.raycastTarget = false;
            }

            BuildStaticLogo(parent);

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
            title.color = SurvivalPioneerUiPalette.BodyText;
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
            label.text = "Loading Genesis...";
            label.fontSize = 24f;
            label.characterSpacing = 6f;
            label.color = SurvivalPioneerUiPalette.BodyText;

            percentLabel = CreateProgressLabel(block.transform, "PercentLabel", TextAlignmentOptions.MidlineRight);
            percentLabel.text = "0%";
            percentLabel.fontSize = 22f;
            percentLabel.color = SurvivalPioneerUiPalette.Gold;

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
            trackImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.9f);
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
            fillImage.color = SurvivalPioneerUiPalette.RichFuchsia;
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
            float startedAt = Time.realtimeSinceStartup;
            float window = Mathf.Max(0.5f, simulatedLoadSeconds);
            ApplyProgress(0f);

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;

                // Bar is driven directly by the load window so screen time and fill stay in sync.
                float timeProgress = Mathf.Clamp01(elapsed / window);

                // Boot only: if in-scene bootstrap is still catching up, hold short of full instead of
                // handing off to a menu that is not ready yet. The grace cap keeps a checkpoint that
                // never arrives from parking the player on a 92% bar forever.
                bool ready = mode != LoadingMode.Boot
                             || AreBootstrapCheckpointsReady()
                             || elapsed >= window + CheckpointGraceSeconds;
                ApplyProgress(ready ? timeProgress : Mathf.Min(timeProgress, ProgressCeilingBeforeHandoff));

                if (ready && timeProgress >= 1f)
                    break;

                yield return null;
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
            if (group == null)
                yield break;

            GameAudioManager audio = fadeAmbience ? GameAudioManager.Instance : null;
            float duration = Mathf.Max(0.05f, durationSeconds);
            float startedAt = Time.realtimeSinceStartup;
            group.alpha = from;

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                group.alpha = alpha;

                if (audio != null)
                {
                    float ambience = from > to ? alpha : 1f - alpha;
                    audio.SetLoadingAmbienceFade(ambience);
                }

                if (t >= 1f)
                    break;

                yield return null;
            }

            group.alpha = to;
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

            if (callback != null)
                callback();
            else
                MainCanvasFlow.Refresh();
        }
    }
}
