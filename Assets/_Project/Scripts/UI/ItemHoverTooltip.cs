using Project.Crafting;
using Project.Data;
using Project.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    public class ItemHoverTooltip : MonoBehaviour
    {
        private static ItemHoverTooltip instance;
        private static InventorySlotUI activeHoverSlot;

        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Vector2 screenOffset = new Vector2(18f, -18f);
        private bool isVisible;

        public static ItemHoverTooltip Instance => instance;

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            if (activeHoverSlot != null)
                activeHoverSlot = null;
        }

        public static void HideAny()
        {
            activeHoverSlot = null;
            if (instance == null)
                return;

            instance.Hide();
        }

        public static void NotifyHover(InventorySlotUI slot)
        {
            activeHoverSlot = slot;
        }

        public static void NotifyHoverEnd(InventorySlotUI slot)
        {
            if (activeHoverSlot == slot)
                activeHoverSlot = null;
        }

        public static ItemHoverTooltip EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("ItemHoverTooltip", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            ItemHoverTooltip tooltip = host.AddComponent<ItemHoverTooltip>();
            tooltip.Build();
            instance = tooltip;
            return tooltip;
        }

        private void Build()
        {
            tooltipRect = transform as RectTransform;
            tooltipRect.pivot = new Vector2(0f, 1f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.transform.SetParent(transform, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = new Color(0.06f, 0.07f, 0.1f, 0.96f);
            panelImage.raycastTarget = false;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = 240f;
            panelLayout.preferredWidth = 300f;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(panel.transform, false);
            titleText = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.fontSize = 24f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            titleText.textWrappingMode = TextWrappingModes.Normal;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(panel.transform, false);
            bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyText);
            bodyText.fontSize = 18f;
            bodyText.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            bodyText.raycastTarget = false;
            bodyText.richText = true;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            tooltipRect = panelRect;
            gameObject.SetActive(false);
        }

        public void Show(ItemData item, int amount, Vector2 screenPosition)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            RecipeHoverTooltip.HideAny();

            titleText.text = ItemTooltipFormatter.BuildTitle(item);
            bodyText.text = ItemTooltipFormatter.BuildBody(item, amount);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            isVisible = true;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                UiFrontLayer.ReparentToFront(transform, canvas.transform);

            SetScreenPosition(screenPosition);
        }

        public void Hide()
        {
            if (this == null)
                return;

            isVisible = false;
            activeHoverSlot = null;

            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isVisible)
                return;

            if (activeHoverSlot == null || activeHoverSlot.slot == null || activeHoverSlot.slot.IsEmpty)
            {
                Hide();
                return;
            }

            if (!activeHoverSlot.isActiveAndEnabled)
            {
                Hide();
            }
        }

        private void LateUpdate()
        {
            if (!isVisible)
                return;

            SetScreenPosition(GetPointerScreenPosition());
        }

        private static Vector2 GetPointerScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Input.mousePosition;
        }

        private void SetScreenPosition(Vector2 screenPosition)
        {
            if (tooltipRect == null)
                return;

            tooltipRect.position = screenPosition + screenOffset;
            ClampTooltipToScreen(tooltipRect);
        }

        internal static void ClampTooltipToScreen(RectTransform panelRect)
        {
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);

            Vector2 min = corners[0];
            Vector2 max = corners[2];
            Vector2 shift = Vector2.zero;

            if (max.x > Screen.width)
                shift.x = Screen.width - max.x;
            if (min.x < 0f)
                shift.x = -min.x;
            if (max.y > Screen.height)
                shift.y = Screen.height - max.y;
            if (min.y < 0f)
                shift.y = -min.y;

            panelRect.position += (Vector3)shift;
        }
    }

    public class RecipeHoverTooltip : MonoBehaviour
    {
        private static RecipeHoverTooltip instance;

        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Vector2 screenOffset = new Vector2(18f, -18f);
        private bool isVisible;

        public static RecipeHoverTooltip Instance => instance;

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static void HideAny()
        {
            if (instance == null)
                return;

            instance.Hide();
        }

        public static RecipeHoverTooltip EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("RecipeHoverTooltip", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            RecipeHoverTooltip tooltip = host.AddComponent<RecipeHoverTooltip>();
            tooltip.Build();
            instance = tooltip;
            return instance;
        }

        private void Build()
        {
            tooltipRect = transform as RectTransform;
            tooltipRect.pivot = new Vector2(0f, 1f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.transform.SetParent(transform, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = new Color(0.06f, 0.07f, 0.1f, 0.96f);
            panelImage.raycastTarget = false;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = 220f;
            panelLayout.preferredWidth = 280f;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(panel.transform, false);
            titleText = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.fontSize = 22f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            titleText.textWrappingMode = TextWrappingModes.Normal;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform));
            bodyObject.transform.SetParent(panel.transform, false);
            bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyText);
            bodyText.fontSize = 16f;
            bodyText.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            bodyText.raycastTarget = false;
            bodyText.richText = true;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            tooltipRect = panelRect;
            gameObject.SetActive(false);
        }

        public void Show(RecipeDefinition recipe, InventorySystem inventory, Vector2 screenPosition, bool pendingScroll = false)
        {
            if (recipe == null)
            {
                Hide();
                return;
            }

            ItemHoverTooltip.HideAny();
            titleText.text = RecipeTooltipFormatter.BuildTitle(recipe);
            bodyText.text = pendingScroll
                ? RecipeTooltipFormatter.BuildScrollBody(recipe)
                : RecipeTooltipFormatter.BuildBody(recipe, inventory);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            isVisible = true;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                UiFrontLayer.ReparentToFront(transform, canvas.transform);

            SetScreenPosition(screenPosition);
        }

        public void Hide()
        {
            if (this == null)
                return;

            isVisible = false;

            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!isVisible)
                return;

            SetScreenPosition(GetPointerScreenPosition());
        }

        private static Vector2 GetPointerScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Input.mousePosition;
        }

        private void SetScreenPosition(Vector2 screenPosition)
        {
            if (tooltipRect == null)
                return;

            tooltipRect.position = screenPosition + screenOffset;
            ItemHoverTooltip.ClampTooltipToScreen(tooltipRect);
        }
    }
}
