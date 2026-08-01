using System;
using System.Collections;
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

        private const int OverlaySortingOrder = 32000;
        private const float ProgressCeilingBeforeHandoff = 0.92f;
        /// <summary>Longest the boot pass will wait past its window for a checkpoint that may never arrive.</summary>
        private const float CheckpointGraceSeconds = 4f;
        private const float LogoFrameSize = 430f;

        [SerializeField, Range(0.30f, 0.75f)] private float backgroundImageAlpha = 0.50f;
        /// <summary>Side-to-side tumble (Y axis). Degrees per unscaled second.</summary>
        [SerializeField] private float logoSpinDegreesPerSecond = 36f;
        /// <summary>Screen time and progress bar are both driven by this window.</summary>
        [SerializeField] private float simulatedLoadSeconds = 6f;
        [SerializeField] private float fadeOutSeconds = 0.65f;

        private static bool bootPending;
        private static LoadingOverlayController activeInstance;
        private static LoadingMode pendingMode = LoadingMode.Boot;
        private static Action pendingCompletion;

        private LoadingMode mode = LoadingMode.Boot;
        private Action onCompleted;

        private CanvasGroup canvasGroup;
        private Image glowImage;
        private RectTransform logoArtRect;
        private RectTransform progressFillRect;
        private TextMeshProUGUI percentLabel;
        private int satisfiedCheckpoints;
        private int shownPercent = -1;

        /// <summary>True while the loader owns the screen and the main menu must stay hidden.</summary>
        public static bool IsBlockingMenu => bootPending || activeInstance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            bootPending = false;
            activeInstance = null;
            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;
        }

        // Claimed before any Awake runs so MainMenuController can never win the race and flash its chrome.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClaimBoot()
        {
            if (!Application.isPlaying)
                return;

            bootPending = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying)
                return;

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

        private void Awake()
        {
            mode = pendingMode;
            onCompleted = pendingCompletion;
            pendingMode = LoadingMode.Boot;
            pendingCompletion = null;

            activeInstance = this;
            bootPending = true;

            BuildOverlay();
            StartCoroutine(RunLoadingSequence());
        }

        private void Update()
        {
            // Y-axis = side-to-side tumble (left ↔ right). Unscaled so it keeps turning while timeScale is 0.
            if (logoArtRect != null)
                logoArtRect.Rotate(0f, logoSpinDegreesPerSecond * Time.unscaledDeltaTime, 0f, Space.Self);

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

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // 1. Solid navy backdrop — also swallows clicks so the world stays inert while loading.
            MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "SolidBackdrop",
                SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 1f),
                blockRaycasts: true);

            // 2. Atmospheric Io art at tunable alpha over the navy.
            BuildBackgroundArt();

            // 3. Brand block: soft glow, slow-spinning logo, product title.
            BuildBrandBlock();

            // 4. Progress track + Loading Genesis label.
            BuildProgressBlock();
        }

        private void BuildBackgroundArt()
        {
            Texture backgroundTexture = Resources.Load<Texture>(BackgroundResourcePath);
            if (backgroundTexture == null)
                return;

            GameObject artObject = new GameObject("BackgroundArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            artObject.transform.SetParent(transform, false);
            MenuUiBuilder.StretchRectToFill(artObject.GetComponent<RectTransform>());

            RawImage art = artObject.GetComponent<RawImage>();
            art.texture = backgroundTexture;
            art.color = new Color(1f, 1f, 1f, backgroundImageAlpha);
            art.raycastTarget = false;
        }

        private void BuildBrandBlock()
        {
            Sprite glowSprite = ShiftUiTheme.CircleGlow ?? ShiftUiTheme.SquareGlow;
            if (glowSprite != null)
            {
                GameObject glowObject = new GameObject("LogoGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                glowObject.transform.SetParent(transform, false);
                RectTransform glowRect = glowObject.GetComponent<RectTransform>();
                CenterRect(glowRect, new Vector2(0f, 96f), new Vector2(720f, 720f));

                glowImage = glowObject.GetComponent<Image>();
                glowImage.sprite = glowSprite;
                glowImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.18f);
                glowImage.raycastTarget = false;
            }

            BuildSpinningLogo();

            GameObject titleObject = new GameObject("BrandTitle", typeof(RectTransform));
            titleObject.transform.SetParent(transform, false);
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

        private void BuildSpinningLogo()
        {
            // Prefer Sprite so Unity respects Alpha Is Transparency on the DMI mark; Texture is the fallback.
            Sprite logoSprite = Resources.Load<Sprite>(LogoResourcePath);
            Texture logoTexture = logoSprite != null ? null : Resources.Load<Texture>(LogoResourcePath);
            if (logoSprite == null && logoTexture == null)
                return;

            // Transparent gold DMI lettermark — no circular mask (it clips the outer D/I strokes).
            GameObject artObject = new GameObject("LogoArt", typeof(RectTransform), typeof(CanvasRenderer));
            artObject.transform.SetParent(transform, false);
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

        private void BuildProgressBlock()
        {
            GameObject block = new GameObject("ProgressBlock", typeof(RectTransform));
            block.transform.SetParent(transform, false);
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
            yield return FadeOutOverlay();

            CompleteAndHandOff();
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

        private IEnumerator FadeOutOverlay()
        {
            GameAudioManager audio = GameAudioManager.Instance;
            float duration = Mathf.Max(0.05f, fadeOutSeconds);
            float startedAt = Time.realtimeSinceStartup;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed = Time.realtimeSinceStartup - startedAt;
                float remaining = 1f - Mathf.Clamp01(elapsed / duration);

                if (canvasGroup != null)
                    canvasGroup.alpha = remaining;

                audio?.SetLoadingAmbienceFade(remaining);
                yield return null;
            }

            audio?.StopLoadingAmbience();
        }

        private void CompleteAndHandOff()
        {
            // Clear the gate before handing off so the menu (or gameplay) is allowed to present itself.
            bootPending = false;
            activeInstance = null;

            Action callback = onCompleted;
            onCompleted = null;

            if (callback != null)
                callback();
            else
                MainCanvasFlow.Refresh();

            Destroy(gameObject);
        }
    }
}
