using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Legacy bridge for pet taming feedback. UITK center toast is driven by
    /// <see cref="DMUiToolkitWorldMenus"/> proximity while uGUI is retired.
    /// </summary>
    public class PetTamingProgressUI : MonoBehaviour
    {
        private static PetTamingProgressUI instance;

        internal static void ResetRuntimeState()
        {
            instance = null;
        }

        public static void Show(Transform target, float progress01, string message)
        {
            if (DMUiToolkitWorldMenus.TryShowTaming(progress01, message))
                return;

            EnsureExists()?.Present(progress01, message);
        }

        public static void Hide()
        {
            DMUiToolkitWorldMenus.HideTaming();
            if (instance != null)
                instance.gameObject.SetActive(false);
        }

        private static PetTamingProgressUI EnsureExists()
        {
            if (instance != null)
                return instance;

            Transform canvasRoot = MainMenuController.ResolveCombatHudRoot();
            if (canvasRoot == null)
                return null;

            GameObject host = new GameObject("PetTamingProgressUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<PetTamingProgressUI>();
            instance.BuildLegacyFallback();
            return instance;
        }

        private RectTransform barRect;
        private UnityEngine.UI.Image fillImage;
        private TMPro.TextMeshProUGUI label;

        private void BuildLegacyFallback()
        {
            barRect = transform as RectTransform;
            barRect.sizeDelta = new Vector2(420f, 56f);
            barRect.anchorMin = new Vector2(0.5f, 0.54f);
            barRect.anchorMax = new Vector2(0.5f, 0.54f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bgObj.transform.SetParent(transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image bg = bgObj.GetComponent<UnityEngine.UI.Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.92f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObj.GetComponent<UnityEngine.UI.Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = DarkMatterGenesisUiPalette.RichFuchsia;
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(transform, false);
            label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = 18f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = DarkMatterGenesisUiPalette.Gold;
            label.raycastTarget = false;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            gameObject.SetActive(false);
        }

        private void Present(float progress01, string message)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(progress01);
            if (label != null)
                label.text = message;
            gameObject.SetActive(true);
        }
    }
}
