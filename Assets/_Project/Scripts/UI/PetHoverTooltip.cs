using Project.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class PetHoverTooltip : MonoBehaviour
    {
        private static PetHoverTooltip instance;

        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Vector2 screenOffset = new Vector2(18f, -18f);

        public static PetHoverTooltip Instance => instance;

        public static void HideAny()
        {
            instance?.Hide();
        }

        public static PetHoverTooltip EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("PetHoverTooltip", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            PetHoverTooltip tooltip = host.AddComponent<PetHoverTooltip>();
            tooltip.Build();
            instance = tooltip;
            return tooltip;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Build()
        {
            tooltipRect = transform as RectTransform;
            tooltipRect.pivot = new Vector2(0f, 1f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.transform.SetParent(transform, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.PanelBackground, 0.96f);
            panelImage.raycastTarget = false;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = 180f;
            panelLayout.preferredWidth = 240f;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(panel.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.fontSize = 18f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = SurvivalPioneerUiPalette.WarmOffWhite;

            GameObject bodyObj = new GameObject("Body", typeof(RectTransform));
            bodyObj.transform.SetParent(panel.transform, false);
            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyText);
            bodyText.fontSize = 14f;
            bodyText.color = SurvivalPioneerUiPalette.SoftBeigeGray;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            Hide();
        }

        public void Show(PetController pet, Vector2 screenPosition)
        {
            if (pet == null || titleText == null || bodyText == null)
                return;

            titleText.text = pet.DisplayName;
            bool assigned = PetManager.Instance != null && PetManager.Instance.ToolbarPet == pet;
            string status = assigned ? "Active companion" : pet.IsOwned ? "Owned" : "Wild";
            bodyText.text = string.IsNullOrWhiteSpace(pet.Description)
                ? status
                : $"{pet.Description}\n\n{status}";

            gameObject.SetActive(true);
            tooltipRect.position = screenPosition + screenOffset;
            ClampToScreen();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ClampToScreen()
        {
            if (tooltipRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Vector2 offset = Vector2.zero;

            if (max.x > Screen.width)
                offset.x = Screen.width - max.x;
            if (min.y < 0f)
                offset.y = -min.y;
            if (max.y > Screen.height)
                offset.y = Screen.height - max.y;
            if (min.x < 0f)
                offset.x = -min.x;

            tooltipRect.position += (Vector3)offset;
        }
    }
}
