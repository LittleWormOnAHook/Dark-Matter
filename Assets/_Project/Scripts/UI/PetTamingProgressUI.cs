using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class PetTamingProgressUI : MonoBehaviour
    {
        private static PetTamingProgressUI instance;

        private RectTransform barRect;
        private Image fillImage;
        private TextMeshProUGUI label;
        private Transform trackedTarget;
        private Camera worldCamera;
        private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

        public static PetTamingProgressUI EnsureExists()
        {
            if (instance != null)
                return instance;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            GameObject host = new GameObject("PetTamingProgressUI", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            instance = host.AddComponent<PetTamingProgressUI>();
            instance.Build();
            return instance;
        }

        public static void Show(Transform target, float progress01, string message)
        {
            PetTamingProgressUI ui = EnsureExists();
            ui?.Present(target, progress01, message);
        }

        public static void Hide()
        {
            if (instance != null)
                instance.gameObject.SetActive(false);
        }

        private void Build()
        {
            barRect = transform as RectTransform;
            barRect.sizeDelta = new Vector2(160f, 28f);

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bg = bgObj.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.92f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObj.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = SurvivalPioneerUiPalette.RichFuchsia;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(transform, false);
            label = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = 12f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = SurvivalPioneerUiPalette.Gold;
            label.raycastTarget = false;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!gameObject.activeSelf || trackedTarget == null)
                return;

            worldCamera ??= Camera.main;
            if (worldCamera == null)
                return;

            Vector3 screen = worldCamera.WorldToScreenPoint(trackedTarget.position + worldOffset);
            if (screen.z < 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            barRect.position = screen;
        }

        private void Present(Transform target, float progress01, string message)
        {
            trackedTarget = target;
            worldCamera = Camera.main;
            fillImage.fillAmount = Mathf.Clamp01(progress01);
            label.text = message;
            gameObject.SetActive(true);
        }
    }
}
