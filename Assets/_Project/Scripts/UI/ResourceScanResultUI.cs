using System.Collections;
using Project.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen scan identify toast: icon + resource name only (matches PickupToastUI font).
    /// </summary>
    public class ResourceScanResultUI : MonoBehaviour
    {
        private const float IconSize = 48f;
        private const float IconTextGap = 12f;
        private const float ToastHeight = 56f;

        private static ResourceScanResultUI instance;

        private RectTransform panelRect;
        private CanvasGroup canvasGroup;
        private Image iconImage;
        private TextMeshProUGUI nameLabel;
        private Coroutine activeRoutine;
        private Transform canvasRoot;
        private Vector2 restAnchoredPosition;

        public static ResourceScanResultUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
            {
                // Drop legacy multi-label cards from earlier builds.
                if (instance.nameLabel == null || instance.transform.Find("Category") != null)
                {
                    Object.Destroy(instance.gameObject);
                    instance = null;
                }
                else
                {
                    return instance;
                }
            }

            GameObject host = new GameObject("ResourceScanResultUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<ResourceScanResultUI>();
            instance.Build(canvasRootTransform);
            return instance;
        }

        public static void Show(ItemData item, string category, string yieldText)
        {
            if (item == null)
                return;

            if (DMUiToolkitWorldMenus.TryShowScan(item))
                return;

            Transform canvasRoot = ResolveGameplayCanvasRoot();
            if (canvasRoot == null)
                return;

            ResourceScanResultUI ui = EnsureExists(canvasRoot);
            ui.Present(item);
        }

        private static Transform ResolveGameplayCanvasRoot()
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas host = uiManager.GetComponent<Canvas>();
                if (host != null)
                    return host.transform;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && canvas.isActiveAndEnabled && canvas.renderMode != RenderMode.WorldSpace)
                    return canvas.transform;
            }

            return null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;
            panelRect = transform as RectTransform;
            ApplyToastAnchor();
            panelRect.sizeDelta = new Vector2(GameplayHudLayout.ToastWidth, ToastHeight);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(1f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-IconTextGap * 0.5f, 0f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconImage = iconGo.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;

            GameObject nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(transform, false);
            // RectTransform is already on the GameObject — AddComponent returns null and NRE'd here.
            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(IconTextGap * 0.5f, 0f);
            nameRect.sizeDelta = new Vector2(GameplayHudLayout.ToastWidth * 0.65f, ToastHeight);

            nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
            {
                theme.ApplyFont(nameLabel, semiBold: true);
                nameLabel.color = DarkMatterGenesisUiPalette.Gold;
            }
            else
            {
                TmpUiHelper.ApplyDefaultFont(nameLabel);
                nameLabel.color = DarkMatterGenesisUiPalette.Gold;
            }

            nameLabel.fontSize = 24f;
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            nameLabel.raycastTarget = false;
        }

        private void Present(ItemData item)
        {
            Transform freshRoot = ResolveGameplayCanvasRoot();
            if (freshRoot != null)
                canvasRoot = freshRoot;

            ApplyToastAnchor();

            bool hasIcon = item.icon != null;
            iconImage.enabled = hasIcon;
            iconImage.sprite = item.icon;
            nameLabel.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;

            RectTransform iconRect = iconImage.rectTransform;
            RectTransform nameRect = nameLabel.rectTransform;
            if (hasIcon)
            {
                iconRect.pivot = new Vector2(1f, 0.5f);
                iconRect.anchoredPosition = new Vector2(-IconTextGap * 0.5f, 0f);
                nameRect.pivot = new Vector2(0f, 0.5f);
                nameRect.anchoredPosition = new Vector2(IconTextGap * 0.5f, 0f);
                nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                nameRect.pivot = new Vector2(0.5f, 0.5f);
                nameRect.anchoredPosition = Vector2.zero;
                nameLabel.alignment = TextAlignmentOptions.Center;
            }

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(AnimateToast());
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
        }

        private void ApplyToastAnchor()
        {
            // Same center band as pickup / XP toasts.
            restAnchoredPosition = GameplayHudLayout.PickupToastAnchoredPosition;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = restAnchoredPosition;
        }

        private IEnumerator AnimateToast()
        {
            const float slideInDuration = 0.35f;
            const float holdDuration = 2.3f;
            const float fadeOutDuration = 0.35f;
            const float slideDistance = 28f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(0f, -slideDistance);
            panelRect.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                panelRect.anchoredPosition = Vector2.Lerp(startPosition, restAnchoredPosition, eased);
                canvasGroup.alpha = eased;
                yield return null;
            }

            panelRect.anchoredPosition = restAnchoredPosition;
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdDuration);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                panelRect.anchoredPosition = restAnchoredPosition + new Vector2(0f, t * 18f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
