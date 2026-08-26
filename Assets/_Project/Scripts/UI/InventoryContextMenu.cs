using System.Collections.Generic;
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
        private GameObject ammoSubmenuPanel;
        private Transform ammoSubmenuContent;
        private InventoryItemActions itemActions;
        private Transform canvasRoot;
        private int activeSlotIndex = -1;
        private int openedOnFrame = -1;
        private readonly List<GameObject> ammoSubmenuButtons = new List<GameObject>();

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
            CreateMenuButton("Install", () => Execute(itemActions?.TryInstallStorageModule(activeSlotIndex) ?? false));
            CreateMenuButton("Equip", () => Execute(itemActions?.TryEquip(activeSlotIndex) ?? false));
            CreateMenuButton("Unequip", () => Execute(itemActions?.TryUnequip(activeSlotIndex) ?? false));
            CreateAmmoSubmenuButton();
            CreateMenuButton("Refuel", () => Execute(itemActions?.TryRefuelVehicle(activeSlotIndex) ?? false));
            CreateMenuButton("Refill Mining Tool", () => Execute(itemActions?.TryRefillMiningTool(activeSlotIndex) ?? false));
            CreateMenuButton("Deploy", () => Execute(itemActions?.TryDeploy(activeSlotIndex) ?? false));
            CreateMenuButton("Split", () => Execute(itemActions?.TrySplit(activeSlotIndex) ?? false));
            CreateMenuButton("Drop", () => Execute(itemActions?.TryDrop(activeSlotIndex) ?? false));

            BuildAmmoSubmenuPanel();

            menuRoot.SetActive(false);
        }

        /// <summary>
        /// "Equip Ammo ▸" row: hovering (not clicking) reveals the weapon flyout, matching the
        /// right-click-then-hover submenu behavior requested for ammo equip.
        /// </summary>
        private void CreateAmmoSubmenuButton()
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, "Equip Ammo >", new Vector2(164f, 34f), 18f);
            button.name = "EquipAmmoContextButton";

            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowAmmoSubmenu());
            trigger.triggers.Add(enterEntry);
        }

        private void BuildAmmoSubmenuPanel()
        {
            ammoSubmenuPanel = new GameObject("AmmoSubmenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            ammoSubmenuPanel.transform.SetParent(menuRoot.transform, false);

            Image panelImage = ammoSubmenuPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            panelImage.raycastTarget = true;

            RectTransform panelRect = ammoSubmenuPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(200f, 0f);

            VerticalLayoutGroup layout = ammoSubmenuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = ammoSubmenuPanel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ammoSubmenuContent = ammoSubmenuPanel.transform;
            ammoSubmenuPanel.SetActive(false);
        }

        private void ShowAmmoSubmenu()
        {
            if (itemActions == null || ammoSubmenuPanel == null)
                return;

            List<InventoryItemActions.AmmoEquipOption> options = itemActions.GetAmmoEquipOptions(activeSlotIndex);
            if (options.Count == 0)
            {
                ammoSubmenuPanel.SetActive(false);
                return;
            }

            for (int i = 0; i < ammoSubmenuButtons.Count; i++)
            {
                if (ammoSubmenuButtons[i] != null)
                    Destroy(ammoSubmenuButtons[i]);
            }
            ammoSubmenuButtons.Clear();

            for (int i = 0; i < options.Count; i++)
            {
                InventoryItemActions.AmmoEquipOption option = options[i];
                Button optionButton = MenuUiBuilder.CreateButton(ammoSubmenuContent, option.WeaponLabel, new Vector2(184f, 34f), 16f);
                optionButton.name = "AmmoOption_" + option.WeaponHotbarSlot;
                optionButton.onClick.RemoveAllListeners();
                optionButton.onClick.AddListener(() =>
                {
                    Execute(itemActions?.TryEquipAmmoToWeapon(activeSlotIndex, option.WeaponHotbarSlot) ?? false);
                    Hide();
                });
                ammoSubmenuButtons.Add(optionButton.gameObject);
            }

            ammoSubmenuPanel.SetActive(true);
            ammoSubmenuPanel.transform.SetAsLastSibling();

            RectTransform mainRect = menuPanel.GetComponent<RectTransform>();
            RectTransform submenuRect = ammoSubmenuPanel.GetComponent<RectTransform>();
            Transform equipAmmoButton = menuPanel.transform.Find("EquipAmmoContextButton");
            RectTransform alignRow = equipAmmoButton != null
                ? equipAmmoButton.GetComponent<RectTransform>()
                : mainRect;

            LayoutRebuilder.ForceRebuildLayoutImmediate(mainRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(submenuRect);
            PositionFlyoutBeside(mainRect, submenuRect, alignRow);
            ClampToScreen(submenuRect);
        }

        /// <summary>
        /// Places a flyout flush to the anchor panel's right edge in world space so UI scale
        /// does not separate submenu buttons from the parent menu.
        /// </summary>
        private static void PositionFlyoutBeside(RectTransform anchorPanel, RectTransform flyoutPanel, RectTransform alignRow)
        {
            if (anchorPanel == null || flyoutPanel == null)
                return;

            Vector3[] anchorCorners = new Vector3[4];
            anchorPanel.GetWorldCorners(anchorCorners);

            RectTransform row = alignRow != null ? alignRow : anchorPanel;
            Vector3[] rowCorners = new Vector3[4];
            row.GetWorldCorners(rowCorners);

            float rightX = anchorCorners[2].x;
            float topY = rowCorners[1].y;
            Vector3 pos = flyoutPanel.position;
            flyoutPanel.position = new Vector3(rightX, topY, pos.z);
        }

        private void HideAmmoSubmenu()
        {
            if (ammoSubmenuPanel != null)
                ammoSubmenuPanel.SetActive(false);
        }

        private void CreateMenuButton(string label, System.Action action)
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, label, new Vector2(164f, 34f), 18f);
            button.name = label + "ContextButton";
            button.onClick.RemoveAllListeners();
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
            HideAmmoSubmenu();
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
            HideAmmoSubmenu();
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
            SetButtonVisible("Use", itemActions.CanUse(activeSlotIndex) && !itemActions.CanInstallStorageModule(activeSlotIndex) && !itemActions.CanDeployShelter(activeSlotIndex) && !itemActions.CanDeployWalkerDrill(activeSlotIndex));
            SetButtonVisible("Install", itemActions.CanInstallStorageModule(activeSlotIndex));
            SetButtonVisible("Equip", itemActions.CanEquip(activeSlotIndex));
            SetButtonVisible("Unequip", itemActions.CanUnequip(activeSlotIndex));
            SetButtonVisible("EquipAmmo", itemActions.CanEquipAmmo(activeSlotIndex));
            SetButtonVisible("Refuel", itemActions.CanRefuelVehicle(activeSlotIndex));
            SetButtonVisible("Refill Mining Tool", itemActions.CanRefillMiningTool(activeSlotIndex));
            SetButtonVisible("Deploy", itemActions.CanDeploy(activeSlotIndex));
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

            if (UiEscapeGate.TryConsumeEscape())
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
