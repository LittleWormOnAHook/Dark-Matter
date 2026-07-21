using System.Collections;
using TMPro;
using UnityEngine;

namespace Project.UI
{
    public class PickupToastUI : MonoBehaviour
    {
        private static PickupToastUI instance;

        private RectTransform toastRect;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI label;
        private Coroutine activeRoutine;
        private Transform canvasRoot;
        private Vector2 restAnchoredPosition;

        public static PickupToastUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("PickupToastUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<PickupToastUI>();
            instance.Build(canvasRootTransform);
            return instance;
        }

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            PickupToastUI toast = EnsureExists(canvas.transform);
            toast.Present(message);
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;

            toastRect = transform as RectTransform;
            toastRect.anchorMin = new Vector2(1f, 1f);
            toastRect.anchorMax = new Vector2(1f, 1f);
            toastRect.pivot = new Vector2(1f, 1f);
            restAnchoredPosition = GameplayHudLayout.PickupToastAnchoredPosition;
            toastRect.anchoredPosition = restAnchoredPosition;
            toastRect.sizeDelta = new Vector2(GameplayHudLayout.ToastWidth, 48f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            label = gameObject.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
            {
                theme.ApplyFont(label, semiBold: true);
                label.color = SurvivalPioneerUiPalette.Gold;
            }
            else
            {
                TmpUiHelper.ApplyDefaultFont(label);
                label.color = SurvivalPioneerUiPalette.Gold;
            }

            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.TopRight;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
        }

        private void Present(string message)
        {
            ApplyToastAnchor();
            label.text = message;
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(AnimateToast());
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
        }

        private void ApplyToastAnchor()
        {
            restAnchoredPosition = GameplayHudLayout.PickupToastAnchoredPosition;
            toastRect.anchorMin = new Vector2(1f, 1f);
            toastRect.anchorMax = new Vector2(1f, 1f);
            toastRect.pivot = new Vector2(1f, 1f);
            toastRect.anchoredPosition = restAnchoredPosition;
        }

        private IEnumerator AnimateToast()
        {
            const float slideInDuration = 0.35f;
            const float holdDuration = 2.3f;
            const float fadeOutDuration = 0.35f;
            const float slideDistance = 36f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(slideDistance, 0f);
            toastRect.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                toastRect.anchoredPosition = Vector2.Lerp(startPosition, restAnchoredPosition, eased);
                canvasGroup.alpha = eased;
                yield return null;
            }

            toastRect.anchoredPosition = restAnchoredPosition;
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdDuration);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                toastRect.anchoredPosition = restAnchoredPosition + new Vector2(t * 12f, 0f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
