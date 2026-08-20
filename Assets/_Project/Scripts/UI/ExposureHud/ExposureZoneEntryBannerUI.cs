using System.Collections;
using Project.Core;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen zone entry banner. Visible for 3 seconds then fades out.
    /// Uses unscaled Update timing (not a coroutine on this object) so menu/loading
    /// canvas toggles — especially in player builds — cannot leave the banner stuck
    /// at full opacity with no fade running.
    /// </summary>
    public class ExposureZoneEntryBannerUI : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            FadeIn,
            Hold,
            FadeOut
        }

        private const float HoldSeconds = 3f;
        private const float FadeInSeconds = 0.25f;
        private const float FadeOutSeconds = 0.45f;

        private CanvasGroup canvasGroup;
        private TextMeshProUGUI headingLabel;
        private TextMeshProUGUI zoneLabel;
        private Image accentBar;
        private ExposureReceiver boundReceiver;
        private bool built;
        private bool suppressEnableHide;
        private Phase phase = Phase.Idle;
        private float phaseElapsed;
        private Coroutine deferredHideRoutine;

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (built || canvasRoot == null)
                return;

            if (GetComponent<Canvas>() != null)
            {
                Debug.LogError("[ExposureZoneEntryBannerUI] Banner cannot be built on a Canvas root. Use a dedicated child object.");
                return;
            }

            RectTransform root = gameObject.GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            root.SetParent(canvasRoot, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(420f), HudLayoutMetrics.Scaled(92f));
            root.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(48f));

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image panel = gameObject.GetComponent<Image>();
            if (panel == null)
                panel = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panel);
            panel.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.92f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(gameObject, new Vector2(1f, -1f));

            Transform existingAccent = transform.Find("Accent");
            if (existingAccent != null)
            {
                accentBar = existingAccent.GetComponent<Image>();
            }
            else
            {
                GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(transform, false);
                RectTransform accentRect = accentObject.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0.08f, 1f);
                accentRect.anchorMax = new Vector2(0.92f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
                accentRect.sizeDelta = new Vector2(0f, 3f);
                accentRect.anchoredPosition = new Vector2(0f, -2f);
                accentBar = accentObject.GetComponent<Image>();
            }

            MenuUiBuilder.ApplyUiSprite(accentBar);
            accentBar.color = DarkMatterGenesisUiPalette.RichFuchsia;

            headingLabel = FindOrCreateLabel("Heading", "ENTERING ZONE", 13f, FontStyles.Bold, new Vector2(0f, HudLayoutMetrics.Scaled(48f)));
            headingLabel.color = DarkMatterGenesisUiPalette.MutedText;

            zoneLabel = FindOrCreateLabel("ZoneName", "UNKNOWN", 24f, FontStyles.Bold, new Vector2(0f, HudLayoutMetrics.Scaled(18f)));
            zoneLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            gameObject.SetActive(false);
            built = true;
        }

        public void BindController(ExposureController controller)
        {
            BindReceiver(controller);
        }

        public void BindReceiver(ExposureReceiver receiver)
        {
            if (boundReceiver == receiver)
                return;

            UnbindReceiver();
            boundReceiver = receiver;
            if (boundReceiver != null)
                boundReceiver.ZoneEntered += HandleZoneEntered;
        }

        public void UnbindController()
        {
            UnbindReceiver();
        }

        public void UnbindReceiver()
        {
            if (boundReceiver != null)
                boundReceiver.ZoneEntered -= HandleZoneEntered;

            boundReceiver = null;
        }

        /// <summary>Force-dismiss for HUD hide, menu, loading, or vehicle mount.</summary>
        public void DismissImmediate()
        {
            phase = Phase.Idle;
            phaseElapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (deferredHideRoutine != null)
            {
                StopCoroutine(deferredHideRoutine);
                deferredHideRoutine = null;
            }

            if (gameObject.activeSelf)
            {
                suppressEnableHide = true;
                gameObject.SetActive(false);
                suppressEnableHide = false;
            }
        }

        private void OnEnable()
        {
            // Parent canvas/HUD re-enabled us after a menu/loading toggle. If we are not mid-show,
            // hide again so a killed fade cannot leave a stuck full-alpha toast.
            if (!suppressEnableHide && built && phase == Phase.Idle)
                QueueHideSelf();
        }

        private void OnDisable()
        {
            // Stopping mid-fade without clearing alpha is the build stuck-banner path.
            phase = Phase.Idle;
            phaseElapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (deferredHideRoutine != null)
            {
                StopCoroutine(deferredHideRoutine);
                deferredHideRoutine = null;
            }
        }

        private void Update()
        {
            if (!built || phase == Phase.Idle || canvasGroup == null)
                return;

            // Menu / loading: never keep the toast over chrome that blocks gameplay HUD.
            if (MainMenuController.BlocksGameplayHud || !GameSession.HasStarted)
            {
                DismissImmediate();
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                dt = 1f / 60f;
            phaseElapsed += dt;

            switch (phase)
            {
                case Phase.FadeIn:
                {
                    float t = Mathf.Clamp01(phaseElapsed / FadeInSeconds);
                    canvasGroup.alpha = t;
                    if (t >= 1f)
                    {
                        canvasGroup.alpha = 1f;
                        phase = Phase.Hold;
                        phaseElapsed = 0f;
                    }
                    break;
                }
                case Phase.Hold:
                    canvasGroup.alpha = 1f;
                    if (phaseElapsed >= HoldSeconds)
                    {
                        phase = Phase.FadeOut;
                        phaseElapsed = 0f;
                    }
                    break;
                case Phase.FadeOut:
                {
                    float t = Mathf.Clamp01(phaseElapsed / FadeOutSeconds);
                    canvasGroup.alpha = 1f - t;
                    if (t >= 1f)
                        FinishAndHide();
                    break;
                }
            }
        }

        private void HandleZoneEntered(ExposureZoneVolume zone)
        {
            if (!GameSession.HasStarted || zone?.Profile == null)
                return;

            if (MainMenuController.BlocksGameplayHud)
                return;

            string zoneName = zone.Profile.displayName;
            if (string.IsNullOrWhiteSpace(zoneName))
                zoneName = ExposureHazardPresentation.GetShortLabel(zone.Profile.zoneKind);

            Color accent = ExposureHazardPresentation.GetColor(zone.Profile.zoneKind);
            Show(zoneName, accent);
        }

        private void Show(string zoneName, Color accentColor)
        {
            if (!built || zoneLabel == null || accentBar == null || canvasGroup == null)
                return;

            zoneLabel.text = zoneName.ToUpperInvariant();
            accentBar.color = accentColor;
            transform.SetAsLastSibling();

            phase = Phase.FadeIn;
            phaseElapsed = 0f;
            canvasGroup.alpha = 0f;

            suppressEnableHide = true;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            suppressEnableHide = false;
        }

        private void FinishAndHide()
        {
            phase = Phase.Idle;
            phaseElapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void QueueHideSelf()
        {
            if (!isActiveAndEnabled)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
                return;
            }

            if (deferredHideRoutine != null)
                StopCoroutine(deferredHideRoutine);
            deferredHideRoutine = StartCoroutine(HideSelfEndOfFrame());
        }

        private IEnumerator HideSelfEndOfFrame()
        {
            yield return null;
            deferredHideRoutine = null;
            if (phase != Phase.Idle)
                yield break;
            DismissImmediate();
        }

        private TextMeshProUGUI FindOrCreateLabel(string name, string text, float fontSize, FontStyles style, Vector2 anchoredPosition)
        {
            Transform existing = transform.Find(name);
            if (existing != null)
            {
                TextMeshProUGUI existingLabel = existing.GetComponent<TextMeshProUGUI>();
                if (existingLabel != null)
                    return existingLabel;
            }

            return CreateLabel(name, text, fontSize, style, anchoredPosition);
        }

        private TextMeshProUGUI CreateLabel(string name, string text, float fontSize, FontStyles style, Vector2 anchoredPosition)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(380f), HudLayoutMetrics.Scaled(28f));
            rect.anchoredPosition = anchoredPosition;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private void OnDestroy()
        {
            UnbindReceiver();
        }
    }
}
