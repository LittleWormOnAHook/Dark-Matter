using System.Collections;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen toast when a skill/upgrade requires a higher player level.
    /// </summary>
    public class DMIRequireLevelPopupUI : MonoBehaviour
    {
        private static DMIRequireLevelPopupUI instance;

        private RectTransform popupRect;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI headlineLabel;
        private TextMeshProUGUI levelLabel;
        private Transform canvasRoot;
        private Vector2 restAnchoredPosition;
        private Coroutine activeRoutine;

        public static DMIRequireLevelPopupUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("DMIRequireLevelPopupUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<DMIRequireLevelPopupUI>();
            instance.Build(canvasRootTransform);
            return instance;
        }

        public static void Show(int requiredLevel)
        {
            if (requiredLevel < 2)
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            DMIRequireLevelPopupUI popup = EnsureExists(canvas.transform);
            popup.Present(requiredLevel);
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;

            popupRect = transform as RectTransform;
            ApplyAnchor();
            popupRect.sizeDelta = new Vector2(420f, 0f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(420f, 0f);

            Image cardBg = card.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(cardBg);
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(cardBg, 0.94f);
            cardBg.raycastTarget = false;
            DarkMatterGenesisUiPalette.ApplyGoldTrim(card);

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            headlineLabel = CreateLabel(card.transform, "Headline", 26f, FontStyles.Bold);
            headlineLabel.color = DarkMatterGenesisUiPalette.Gold;
            headlineLabel.alignment = TextAlignmentOptions.Center;
            headlineLabel.text = "Require Level";

            levelLabel = CreateLabel(card.transform, "Level", 34f, FontStyles.Bold);
            levelLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            levelLabel.alignment = TextAlignmentOptions.Center;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize, FontStyles style)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private void Present(int requiredLevel)
        {
            ApplyAnchor();
            headlineLabel.text = "Require Level";
            levelLabel.text = requiredLevel.ToString();

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            GameAudioManager.Instance?.PlayButtonClick();
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
            activeRoutine = StartCoroutine(AnimatePopup());
        }

        private void ApplyAnchor()
        {
            restAnchoredPosition = Vector2.zero;
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = restAnchoredPosition;
        }

        private IEnumerator AnimatePopup()
        {
            const float slideInDuration = 0.28f;
            const float holdDuration = 1.8f;
            const float fadeOutDuration = 0.3f;
            const float slideDistance = 28f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(0f, -slideDistance);
            popupRect.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                popupRect.anchoredPosition = Vector2.Lerp(startPosition, restAnchoredPosition, eased);
                canvasGroup.alpha = eased;
                yield return null;
            }

            popupRect.anchoredPosition = restAnchoredPosition;
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdDuration);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                popupRect.anchoredPosition = restAnchoredPosition + new Vector2(0f, t * 18f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
