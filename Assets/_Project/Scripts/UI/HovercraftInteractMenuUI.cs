using Project.Core;
using Project.Inventory;
using Project.Player;
using Project.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Press-E popup shown when the player walks up to a hovercraft (not already mounted): Enter,
    /// Refuel, and Store in Inventory. Reuses the same "modal panel" cursor-unlock/movement-stop hook
    /// as BuildingControlPanelUI (PlayerController.SetBuildingControlOpen) rather than adding a new
    /// flag to PlayerController's already-large combined-state checks.
    /// </summary>
    public class HovercraftInteractMenuUI : MonoBehaviour
    {
        private static HovercraftInteractMenuUI instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI fuelReadoutText;
        private Button enterButton;
        private Button refuelButton;
        private Button storeButton;
        private Transform canvasRoot;

        private HovercraftUsable activeUsable;

        public static bool IsOpen => instance != null && instance.menuRoot != null && instance.menuRoot.activeSelf;

        public static HovercraftInteractMenuUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
            {
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            GameObject host = new GameObject("HovercraftInteractMenu", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            HovercraftInteractMenuUI menu = host.AddComponent<HovercraftInteractMenuUI>();
            menu.canvasRoot = canvasRootTransform;
            menu.Build();
            instance = menu;
            return menu;
        }

        public static void CloseAny()
        {
            instance?.Hide();
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

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "HovercraftMenuRoot", new Color(0f, 0f, 0f, 0.35f), blockRaycasts: true);
            menuRoot.SetActive(false);

            menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            menuPanel.transform.SetParent(menuRoot.transform, false);

            Image panelImage = menuPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.96f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(menuPanel);

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(260f, 0f);

            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = menuPanel.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleText = MenuUiBuilder.CreateTitle(menuPanel.transform, "Hovercraft", 22f);
            fuelReadoutText = CreateBodyLabel(menuPanel.transform, string.Empty);

            enterButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Enter", new Vector2(220f, 40f), 17f);
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(OnEnterClicked);

            refuelButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Refuel", new Vector2(220f, 40f), 17f);
            refuelButton.onClick.RemoveAllListeners();
            refuelButton.onClick.AddListener(OnRefuelClicked);

            storeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Store in Inventory", new Vector2(220f, 40f), 17f);
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(OnStoreClicked);

            Button closeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Cancel", new Vector2(220f, 34f), 15f);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }

        private static TextMeshProUGUI CreateBodyLabel(Transform parent, string text)
        {
            GameObject textObject = new GameObject("Body", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = 16f;
            label.color = DarkMatterGenesisUiPalette.MutedText;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        public void Show(HovercraftUsable usable)
        {
            if (usable == null)
                return;

            activeUsable = usable;
            menuRoot.SetActive(true);
            menuRoot.transform.SetAsLastSibling();

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonHovercraftMenu, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshLabels();
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (UiEscapeGate.TryConsumeEscape())
            {
                Hide();
                return;
            }

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (activeUsable == null)
                return;

            HovercraftFuelSystem fuel = activeUsable.FuelSystem;
            if (fuel != null)
            {
                fuelReadoutText.text = $"Fuel: {Mathf.RoundToInt(fuel.CurrentFuel)} / {Mathf.RoundToInt(fuel.MaxFuel)}";
                refuelButton.interactable = !fuel.IsFull;
            }
            else
            {
                fuelReadoutText.text = string.Empty;
                refuelButton.interactable = false;
            }
        }

        private void OnEnterClicked()
        {
            activeUsable?.TryEnterFromMenu();
            Hide();
        }

        private void OnRefuelClicked()
        {
            if (activeUsable == null)
                return;

            InventorySystem inventory = PlayerLocator.FindPlayerObject()?.GetComponent<InventorySystem>();
            if (activeUsable.TryRefuelFromMenu(inventory, out string message))
                PickupToastUI.Show(message);
            else if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);
        }

        private void OnStoreClicked()
        {
            if (activeUsable == null)
                return;

            InventorySystem inventory = PlayerLocator.FindPlayerObject()?.GetComponent<InventorySystem>();
            bool stored = activeUsable.TryStoreFromMenu(inventory, out string message);
            if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);

            if (stored)
                Hide();
        }

        public void Hide()
        {
            activeUsable = null;

            if (menuRoot != null)
                menuRoot.SetActive(false);

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonHovercraftMenu, false);
        }
    }
}
