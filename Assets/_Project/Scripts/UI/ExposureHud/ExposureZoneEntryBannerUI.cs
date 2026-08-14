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
    /// </summary>
    public class ExposureZoneEntryBannerUI : MonoBehaviour
    {
        private const float HoldSeconds = 3f;
        private const float FadeInSeconds = 0.25f;
        private const float FadeOutSeconds = 0.45f;

        private CanvasGroup canvasGroup;
        private TextMeshProUGUI headingLabel;
        private TextMeshProUGUI zoneLabel;
        private Image accentBar;
        private ExposureReceiver boundReceiver;
        private Coroutine sequenceRoutine;
        private bool built;

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

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image panel = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panel);
            panel.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.92f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(gameObject, new Vector2(1f, -1f));

            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(transform, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0.08f, 1f);
            accentRect.anchorMax = new Vector2(0.92f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 3f);
            accentRect.anchoredPosition = new Vector2(0f, -2f);
            accentBar = accentObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(accentBar);
            accentBar.color = DarkMatterGenesisUiPalette.RichFuchsia;

            headingLabel = CreateLabel("Heading", "ENTERING ZONE", 13f, FontStyles.Bold, new Vector2(0f, HudLayoutMetrics.Scaled(48f)));
            headingLabel.color = DarkMatterGenesisUiPalette.MutedText;

            zoneLabel = CreateLabel("ZoneName", "UNKNOWN", 24f, FontStyles.Bold, new Vector2(0f, HudLayoutMetrics.Scaled(18f)));
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

        private void HandleZoneEntered(ExposureZoneVolume zone)
        {
            if (!GameSession.HasStarted || zone?.Profile == null)
                return;

            string zoneName = zone.Profile.displayName;
            if (string.IsNullOrWhiteSpace(zoneName))
                zoneName = ExposureHazardPresentation.GetShortLabel(zone.Profile.zoneKind);

            Color accent = ExposureHazardPresentation.GetColor(zone.Profile.zoneKind);
            Show(zoneName, accent);
        }

        private void Show(string zoneName, Color accentColor)
        {
            if (!built)
                return;

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            zoneLabel.text = zoneName.ToUpperInvariant();
            accentBar.color = accentColor;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            sequenceRoutine = StartCoroutine(BannerSequence());
        }

        private IEnumerator BannerSequence()
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < FadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeInSeconds);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(HoldSeconds);

            elapsed = 0f;
            while (elapsed < FadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeOutSeconds);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            sequenceRoutine = null;
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
