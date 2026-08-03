using System.Collections;
using Project.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen result card after a successful multi-tool resource scan.
    /// </summary>
    public class ResourceScanResultUI : MonoBehaviour
    {
        private static ResourceScanResultUI instance;

        private RectTransform panelRect;
        private CanvasGroup canvasGroup;
        private Image iconImage;
        private TextMeshProUGUI categoryLabel;
        private TextMeshProUGUI nameLabel;
        private TextMeshProUGUI yieldLabel;
        private Coroutine activeRoutine;
        private Transform canvasRoot;

        public static ResourceScanResultUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
                return instance;

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

            Transform canvasRoot = ResolveGameplayCanvasRoot();
            if (canvasRoot == null)
                return;

            ResourceScanResultUI ui = EnsureExists(canvasRoot);
            ui.Present(item, category, yieldText);
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

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;
            panelRect = transform as RectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(380f, 130f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image bg = gameObject.AddComponent<Image>();
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.92f);
            bg.raycastTarget = false;

            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = SurvivalPioneerUiPalette.SlateGray;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(16f, 0f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
            iconImage = iconGo.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            categoryLabel = CreateLabel("Category", new Vector2(96f, 28f), new Vector2(-16f, -16f), 14f, SurvivalPioneerUiPalette.SoftBeigeGray);
            nameLabel = CreateLabel("Name", new Vector2(96f, -4f), new Vector2(-16f, -44f), 22f, SurvivalPioneerUiPalette.WarmOffWhite, semiBold: true);
            yieldLabel = CreateLabel("Yield", new Vector2(96f, -40f), new Vector2(-16f, -16f), 15f, SurvivalPioneerUiPalette.Gold);
        }

        private TextMeshProUGUI CreateLabel(
            string objectName,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize,
            Color color,
            bool semiBold = false)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold);
            else
                TmpUiHelper.ApplyDefaultFont(label);

            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private void Present(ItemData item, string category, string yieldText)
        {
            iconImage.enabled = item.icon != null;
            iconImage.sprite = item.icon;
            categoryLabel.text = string.IsNullOrEmpty(category) ? "Resource" : category;
            nameLabel.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            yieldLabel.text = string.IsNullOrEmpty(yieldText) ? string.Empty : yieldText;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(AnimateCard());
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
        }

        private IEnumerator AnimateCard()
        {
            const float fadeIn = 0.25f;
            const float hold = 3f;
            const float fadeOut = 0.35f;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
