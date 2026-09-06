using System.Collections;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneUnload()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            instance = null;
        }

        public static PickupToastUI EnsureExists(Transform canvasRootTransform)
        {
            if (canvasRootTransform == null)
                return null;

            // Unity fake-null: destroyed objects compare equal to null.
            if (instance == null)
                instance = null;
            else if (!instance)
                instance = null;

            if (instance != null)
            {
                instance.EnsureActiveForPresentation();
                return instance;
            }

            GameObject host = new GameObject("PickupToastUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<PickupToastUI>();
            instance.Build(canvasRootTransform);
            instance.EnsureActiveForPresentation();
            return instance;
        }

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (ShouldCenterWarning(message) && DMUiToolkitLevelUp.TryShowCenterNotice(message))
                return;

            if (DMUiToolkitHud.IsDriving)
            {
                DMUiToolkitHud.ShowPopup(message);
                return;
            }

            DMGameLog.Add(message, DMGameLog.KindFromPopupText(message));

            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            ActivateParentChain(canvas.transform);

            PickupToastUI toast = EnsureExists(canvas.transform);
            if (toast == null)
                return;

            toast.EnsureActiveForPresentation();
            toast.Present(message);
        }

        private static void ActivateParentChain(Transform current)
        {
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }
        }

        /// <summary>Click + center fade toast used when a world pickup / gather cannot fit in inventory.</summary>
        public static void ShowInventoryFull()
        {
            Show("Inventory full");
        }

        private static bool ShouldCenterWarning(string message)
        {
            if (message.IndexOf("inventory", System.StringComparison.OrdinalIgnoreCase) >= 0
                && message.IndexOf("full", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (message.IndexOf("Level ", System.StringComparison.OrdinalIgnoreCase) >= 0
                && message.IndexOf("Required", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;

            toastRect = transform as RectTransform;
            ApplyToastAnchor();
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
                label.color = DarkMatterGenesisUiPalette.Gold;
            }
            else
            {
                TmpUiHelper.ApplyDefaultFont(label);
                label.color = DarkMatterGenesisUiPalette.Gold;
            }

            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
        }

        private void Present(string message)
        {
            if (canvasRoot == null)
            {
                Canvas canvas = MainMenuController.ResolveMainCanvas() ?? FindAnyObjectByType<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }

            // Main menu hides gameplay HUD children — reactivate toast + front layer before coroutines.
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
            EnsureActiveForPresentation();

            ApplyToastAnchor();
            label.text = message;
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(AnimateToast());
        }

        private void EnsureActiveForPresentation()
        {
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && canvas.isRootCanvas)
                    break;

                current = current.parent;
            }
        }

        private void ApplyToastAnchor()
        {
            restAnchoredPosition = GameplayHudLayout.PickupToastAnchoredPosition;
            toastRect.anchorMin = new Vector2(0.5f, 0.5f);
            toastRect.anchorMax = new Vector2(0.5f, 0.5f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.anchoredPosition = restAnchoredPosition;
        }

        private IEnumerator AnimateToast()
        {
            const float slideInDuration = 0.35f;
            const float holdDuration = 2.3f;
            const float fadeOutDuration = 0.35f;
            const float slideDistance = 28f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(0f, -slideDistance);
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
                toastRect.anchoredPosition = restAnchoredPosition + new Vector2(0f, t * 18f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
