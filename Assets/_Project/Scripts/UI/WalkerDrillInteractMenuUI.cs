using Project.Core;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Press-E popup for Walker Drill: Start Mining, Stop Mining, Collect Resources (stub).
    /// Matches hovercraft / shelter modal styling (gold prompt palette, Dark Navy panel).
    /// </summary>
    public sealed class WalkerDrillInteractMenuUI : MonoBehaviour
    {
        private static WalkerDrillInteractMenuUI instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI statusText;
        private Button startMiningButton;
        private Button stopMiningButton;
        private Button collectResourcesButton;
        private Button storeButton;
        private Transform canvasRoot;

        private DMWalkerDrillUsable activeUsable;

        public static bool IsOpen => DMUiToolkitWorldMenus.IsDrillOpen
            || (instance != null && instance.menuRoot != null && instance.menuRoot.activeSelf);

        public static bool IsShowing(DMWalkerDrillUsable usable)
        {
            return IsOpen && instance != null && instance.activeUsable == usable;
        }

        public static WalkerDrillInteractMenuUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
            {
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            GameObject host = new GameObject("WalkerDrillInteractMenu", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            WalkerDrillInteractMenuUI menu = host.AddComponent<WalkerDrillInteractMenuUI>();
            menu.canvasRoot = canvasRootTransform;
            menu.Build();
            instance = menu;
            return menu;
        }

        public static void CloseAny()
        {
            DMUiToolkitWorldMenus.HideDrill();
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

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "WalkerDrillMenuRoot", new Color(0f, 0f, 0f, 0.35f), blockRaycasts: true);
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
            panelRect.sizeDelta = new Vector2(280f, 0f);

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

            titleText = MenuUiBuilder.CreateTitle(menuPanel.transform, "Walker Drill", 22f);
            if (titleText != null)
                titleText.color = DarkMatterGenesisUiPalette.InteractionPromptText;

            statusText = CreateBodyLabel(menuPanel.transform, string.Empty);

            startMiningButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Start Mining", new Vector2(240f, 40f), 17f);
            startMiningButton.onClick.RemoveAllListeners();
            startMiningButton.onClick.AddListener(OnStartMiningClicked);

            stopMiningButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Stop Mining", new Vector2(240f, 40f), 17f);
            stopMiningButton.onClick.RemoveAllListeners();
            stopMiningButton.onClick.AddListener(OnStopMiningClicked);

            collectResourcesButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Collect Resources", new Vector2(240f, 40f), 17f);
            collectResourcesButton.onClick.RemoveAllListeners();
            collectResourcesButton.onClick.AddListener(OnCollectResourcesClicked);

            storeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Store in Inventory", new Vector2(240f, 40f), 17f);
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(OnStoreClicked);

            Button closeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Close", new Vector2(240f, 34f), 15f);
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

        public void Show(DMWalkerDrillUsable usable)
        {
            if (usable == null)
                return;

            if (DMUiToolkitWorldMenus.TryShowDrill(usable))
            {
                if (menuRoot != null)
                    menuRoot.SetActive(false);
                return;
            }

            activeUsable = usable;
            menuRoot.SetActive(true);
            menuRoot.transform.SetAsLastSibling();

            ApplyMenuInput(true);
            RefreshLabels();
        }

        private void LateUpdate()
        {
            if (!IsOpen)
                return;

            ApplyMenuInput(true);
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

            if (activeUsable == null || !activeUsable.IsWithinInteractRange(GetPlayerPosition()))
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

            DMWalkerDrillController controller = activeUsable.DrillController;
            bool mining = controller != null && controller.IsMining;
            bool spinning = controller != null && controller.IsSpinning;
            bool retracting = controller != null && controller.IsRetracting;

            if (retracting)
                statusText.text = "Status: Retracting...";
            else if (spinning)
                statusText.text = "Status: Mining (spinning)";
            else if (mining)
                statusText.text = "Status: Starting drill...";
            else
                statusText.text = "Status: Idle";

            startMiningButton.interactable = !mining;
            stopMiningButton.interactable = mining && !retracting;
            collectResourcesButton.interactable = !retracting;
            if (storeButton != null)
                storeButton.interactable = !mining;
        }

        private void OnStartMiningClicked()
        {
            activeUsable?.DrillController?.StartMining();
            RefreshLabels();
        }

        private void OnStopMiningClicked()
        {
            activeUsable?.DrillController?.StopMining();
            RefreshLabels();
        }

        private void OnCollectResourcesClicked()
        {
            Debug.Log("[WalkerDrill] Collect Resources stub — mining payout not implemented yet.");
            PickupToastUI.Show("Collect Resources — coming soon.");
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
            DMUiToolkitWorldMenus.HideDrill();
            activeUsable = null;

            if (menuRoot != null)
                menuRoot.SetActive(false);

            ApplyMenuInput(false);
        }

        private static void ApplyMenuInput(bool menuOpen)
        {
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player == null)
                player = Object.FindAnyObjectByType<PlayerController>();

            player?.SetBuildingControlOpen(menuOpen);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWalkerDrillMenu, menuOpen);

            if (menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (player != null)
            {
                player.ApplyCursorState();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (!menuOpen)
                GameplayInputRecovery.QueueCursorRestore();
        }

        private static Vector3 GetPlayerPosition()
        {
            return PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 position)
                ? position
                : Vector3.positiveInfinity;
        }
    }
}
