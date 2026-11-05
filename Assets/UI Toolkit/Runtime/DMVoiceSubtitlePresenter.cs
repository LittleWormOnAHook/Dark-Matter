using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Runtime voice subtitles (UITK). Speaker name + cleaned subtitle, fade in/out, hidden while a
    /// modal menu is open. On/off comes from the shared GameSettings.SubtitlesEnabled value, so the
    /// main menu Game Options toggle and the Kade Player Systems profile tick stay in lockstep.
    /// </summary>
    [DefaultExecutionOrder(-149)]
    [DisallowMultipleComponent]
    public sealed class DMVoiceSubtitlePresenter : MonoBehaviour
    {
        public const string ObjectName = "UITK_VoiceSubtitle";
        public const int Sort = 21;
        public const string UxmlPath = "Assets/UI Toolkit/Screens/DMVoiceSubtitle.uxml";
        public const string UssPath = "Assets/UI Toolkit/Screens/DMVoiceSubtitle.uss";

        private const float FadeInSeconds = 0.12f;
        private const float FadeOutSeconds = 0.2f;

        private static DMVoiceSubtitlePresenter instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement card;
        private Label speakerLabel;
        private Label bodyLabel;

        private string pendingSpeaker;
        private string pendingBody;
        private Project.Audio.Voice.DMVoiceOutputMode pendingMode;
        private bool bound;
        private bool showing;
        private float targetOpacity;
        private float holdUntilUnscaled;
        private float fadeFrom;
        private float fadeStartUnscaled;
        private float fadeDuration;
        private float currentOpacity;

        public static DMVoiceSubtitlePresenter Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            // UITK_Root is rebuilt per scene, so re-create the subtitle host after every load.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureHost();
            Project.Audio.Voice.DMVoiceManager.EnsureExists();
        }

        private static void HandleSceneLoaded(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;
            EnsureHost();
        }

        public static DMVoiceSubtitlePresenter EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(ObjectName, UxmlPath, UssPath, Sort);
            if (doc == null)
                return null;

            DMVoiceSubtitlePresenter host = doc.GetComponent<DMVoiceSubtitlePresenter>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMVoiceSubtitlePresenter>();

            host.document = doc;
            host.BindTree();
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
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (instance == this)
                instance = null;
        }

        private static Project.Audio.Voice.DMVoiceManager manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            manager = null;
        }

        private void Subscribe()
        {
            if (manager == null)
                manager = Project.Audio.Voice.DMVoiceManager.EnsureExists();

            if (manager == null)
                return;

            manager.OnLineStarted -= HandleLineStarted;
            manager.OnLineFinished -= HandleLineFinished;
            manager.OnLineStarted += HandleLineStarted;
            manager.OnLineFinished += HandleLineFinished;
        }

        private void Unsubscribe()
        {
            if (manager == null)
                return;

            manager.OnLineStarted -= HandleLineStarted;
            manager.OnLineFinished -= HandleLineFinished;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("subtitle-root") ?? tree;
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(root);
            card = tree.Q<VisualElement>("subtitle-card");
            speakerLabel = tree.Q<Label>("subtitle-speaker");
            bodyLabel = tree.Q<Label>("subtitle-body");
            bound = card != null && bodyLabel != null;

            if (card != null)
            {
                card.style.opacity = 0f;
                card.style.display = DisplayStyle.None;
            }

            ApplyFontSize();
        }

        /// <summary>Called by the settings toggle so an open subtitle picks the change up at once.</summary>
        public static void RefreshEnabled()
        {
            DMVoiceSubtitlePresenter host = instance;
            if (host == null)
                return;

            host.ApplyEnabledImmediate();
        }

        private void ApplyEnabledImmediate()
        {
            if (!GameSettings.SubtitlesEnabled)
                HideImmediate();
            else if (showing && card != null)
                card.style.display = DisplayStyle.Flex;
        }

        /// <summary>Applies the font size pref (main menu / Studio changes).</summary>
        public static void RefreshFontSize()
        {
            instance?.ApplyFontSize();
        }

        private void ApplyFontSize()
        {
            if (bodyLabel == null)
                return;

            bodyLabel.style.fontSize = Project.Audio.Voice.DMVoiceSubtitleSettings.FontSize;
        }

        private void HandleLineStarted(Project.Audio.Voice.DMVoiceLine line)
        {
            if (line == null || manager == null)
                return;

            pendingSpeaker = manager.CurrentSpeakerName;
            pendingBody = line.SubtitleClean;
            pendingMode = manager.CurrentOutputMode;

            if (!ShouldPresent())
                return;

            if (!bound)
                BindTree();
            if (!bound)
                return;

            if (speakerLabel != null)
            {
                speakerLabel.text = string.IsNullOrWhiteSpace(pendingSpeaker) ? string.Empty : pendingSpeaker.ToUpperInvariant();
                speakerLabel.style.display = string.IsNullOrWhiteSpace(pendingSpeaker)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            bodyLabel.text = pendingBody;
            bodyLabel.EnableInClassList("dmg-subtitle-body-radio", pendingMode == Project.Audio.Voice.DMVoiceOutputMode.Radio);
            bodyLabel.EnableInClassList("dmg-subtitle-body-narration", pendingMode == Project.Audio.Voice.DMVoiceOutputMode.Narration);
            ApplyFontSize();

            card.style.display = DisplayStyle.Flex;
            showing = true;
            holdUntilUnscaled = float.PositiveInfinity;
            StartFade(1f, FadeInSeconds);
        }

        private void HandleLineFinished(Project.Audio.Voice.DMVoiceLine line, bool skipped)
        {
            if (!showing)
                return;

            holdUntilUnscaled = Time.unscaledTime
                                + (skipped ? 0f : Project.Audio.Voice.DMVoiceSubtitleSettings.HoldSeconds);
        }

        private bool ShouldPresent()
        {
            if (!GameSettings.SubtitlesEnabled)
                return false;
            if (string.IsNullOrWhiteSpace(pendingBody))
                return false;
            return true;
        }

        private void LateUpdate()
        {
            if (!bound)
                return;

            bool allowed = ShouldShowNow();
            if (!allowed)
            {
                if (showing || currentOpacity > 0.001f)
                {
                    showing = false;
                    holdUntilUnscaled = 0f;
                    StartFade(0f, FadeOutSeconds);
                }
                TickOpacity();
                return;
            }

            if (showing && Time.unscaledTime >= holdUntilUnscaled)
            {
                showing = false;
                StartFade(0f, FadeOutSeconds);
            }

            TickOpacity();
        }

        private static bool ShouldShowNow()
        {
            if (!GameSettings.SubtitlesEnabled)
                return false;
            if (!DMUiToolkitOverlayDocument.GameplayHudWanted())
                return false;
            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return false;
            if (DMUiToolkitLoadingOverlay.IsShowing)
                return false;
            return true;
        }

        private void StartFade(float target, float duration)
        {
            targetOpacity = Mathf.Clamp01(target);
            fadeFrom = currentOpacity;
            fadeStartUnscaled = Time.unscaledTime;
            fadeDuration = Mathf.Max(0.0001f, duration);

            if (card != null && targetOpacity > 0f)
                card.style.display = DisplayStyle.Flex;
        }

        private void TickOpacity()
        {
            if (card == null)
                return;

            float t = Mathf.Clamp01((Time.unscaledTime - fadeStartUnscaled) / fadeDuration);
            currentOpacity = Mathf.Lerp(fadeFrom, targetOpacity, t);
            card.style.opacity = currentOpacity;

            if (currentOpacity <= 0.001f && targetOpacity <= 0f)
                card.style.display = DisplayStyle.None;
        }

        private void HideImmediate()
        {
            showing = false;
            targetOpacity = 0f;
            currentOpacity = 0f;
            if (card == null)
                return;

            card.style.opacity = 0f;
            card.style.display = DisplayStyle.None;
        }
    }
}
