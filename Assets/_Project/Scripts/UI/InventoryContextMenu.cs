using Project.Audio;
using Project.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class InventoryContextMenu : MonoBehaviour
    {
        private static InventoryContextMenu instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private InventoryItemActions itemActions;
        private Transform canvasRoot;
        private int activeSlotIndex = -1;
        private int openedOnFrame = -1;

        public static InventoryContextMenu Instance => instance;

        public static InventoryContextMenu EnsureExists(Transform canvasRootTransform, InventoryItemActions actions)
        {
            if (instance != null)
            {
                instance.itemActions = actions;
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            GameObject host = new GameObject("InventoryContextMenu", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            InventoryContextMenu menu = host.AddComponent<InventoryContextMenu>();
            menu.itemActions = actions;
            menu.canvasRoot = canvasRootTransform;
            menu.Build();
            instance = menu;
            return menu;
        }

        private void Build()
        {
            RectTransform hostRect = transform as RectTransform;
            if (hostRect != null)
            {
                hostRect.anchorMin = Vector2.zero;
                hostRect.anchorMax = Vector2.one;
                hostRect.offsetMin = Vector2.zero;
                hostRect.offsetMax = Vector2.zero;
            }

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "InventoryContextMenuRoot", Color.clear, blockRaycasts: false);
            menuRoot.SetActive(false);

            GameObject dismissOverlay = MenuUiBuilder.CreateFullScreenPanel(menuRoot.transform, "DismissOverlay", new Color(0f, 0f, 0f, 0.01f), blockRaycasts: true);
            dismissOverlay.transform.SetAsFirstSibling();
            EventTrigger dismissTrigger = dismissOverlay.AddComponent<EventTrigger>();
            EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => Hide());
            dismissTrigger.triggers.Add(clickEntry);

            menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            menuPanel.transform.SetParent(menuRoot.transform, false);

            Image panelImage = menuPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            panelImage.raycastTarget = true;

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(180f, 0f);

            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = menuPanel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateMenuButton("Use", () => Execute(itemActions?.TryUse(activeSlotIndex) ?? false));
            CreateMenuButton("Equip", () => Execute(itemActions?.TryEquip(activeSlotIndex) ?? false));
            CreateMenuButton("Unequip", () => Execute(itemActions?.TryUnequip(activeSlotIndex) ?? false));
            CreateMenuButton("Split", () => Execute(itemActions?.TrySplit(activeSlotIndex) ?? false));
            CreateMenuButton("Drop", () => Execute(itemActions?.TryDrop(activeSlotIndex) ?? false));

            menuRoot.SetActive(false);
        }

        private void CreateMenuButton(string label, System.Action action)
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, label, new Vector2(164f, 34f), 18f);
            button.name = label + "ContextButton";
            button.onClick.AddListener(() =>
            {
                action?.Invoke();
                Hide();
            });
        }

        private void Execute(bool success)
        {
            if (!success)
                GameAudioManager.Instance?.PlayInventoryItemClick();
        }

        public void Show(int slotIndex, Vector2 screenPosition)
        {
            ItemHoverTooltip.HideAny();

            if (itemActions == null)
                return;

            activeSlotIndex = slotIndex;
            UpdateButtonVisibility();

            if (!HasAnyVisibleOption())
                return;

            openedOnFrame = Time.frameCount;
            menuRoot.SetActive(true);

            if (canvasRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }

            if (canvasRoot != null)
                UiFrontLayer.ReparentFullScreenToFront(transform, canvasRoot);

            menuRoot.transform.SetAsLastSibling();

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.position = screenPosition;

            ClampToScreen(panelRect);
        }

        public void Hide()
        {
            activeSlotIndex = -1;
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (canvasRoot != null)
            {
                transform.SetParent(canvasRoot, false);
                RectTransform hostRect = transform as RectTransform;
                if (hostRect != null)
                {
                    hostRect.anchorMin = Vector2.zero;
                    hostRect.anchorMax = Vector2.one;
                    hostRect.offsetMin = Vector2.zero;
                    hostRect.offsetMax = Vector2.zero;
                }
            }
        }

        private void UpdateButtonVisibility()
        {
            SetButtonVisible("Use", itemActions.CanUse(activeSlotIndex));
            SetButtonVisible("Equip", itemActions.CanEquip(activeSlotIndex));
            SetButtonVisible("Unequip", itemActions.CanUnequip(activeSlotIndex));
            SetButtonVisible("Split", itemActions.CanSplit(activeSlotIndex));
            SetButtonVisible("Drop", itemActions.CanDrop(activeSlotIndex));
        }

        private void SetButtonVisible(string label, bool visible)
        {
            Transform buttonTransform = menuPanel.transform.Find(label + "ContextButton");
            if (buttonTransform != null)
                buttonTransform.gameObject.SetActive(visible);
        }

        private static void ClampToScreen(RectTransform panelRect)
        {
            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);

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

            panelRect.position += (Vector3)offset;
        }

        private bool HasAnyVisibleOption()
        {
            for (int i = 0; i < menuPanel.transform.childCount; i++)
            {
                if (menuPanel.transform.GetChild(i).gameObject.activeSelf)
                    return true;
            }

            return false;
        }

        private void Update()
        {
            if (menuRoot == null || !menuRoot.activeSelf)
                return;

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            if (Time.frameCount == openedOnFrame)
                return;

            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                Hide();
        }
    }
}
