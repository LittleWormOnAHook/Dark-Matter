using Project.Core;
using Project.Inventory;
using Project.Player;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK hovercraft walk-up menu: Enter / Refuel / Store. Forwards from HovercraftInteractMenuUI.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-372)]
    [DisallowMultipleComponent]
    public class DMUiToolkitHoverInteract : MonoBehaviour
    {
        private static DMUiToolkitHoverInteract instance;

        private UIDocument document;
        private VisualElement root;
        private Label fuelLabel;
        private Button enterButton;
        private Button refuelButton;
        private Button storeButton;
        private Button cancelButton;
        private bool bound;
        private bool uguiHidden;
        private bool wired;
        private bool open;
        private HovercraftUsable activeUsable;

        public static bool IsOpen => instance != null && instance.open;


        public static DMUiToolkitHoverInteract EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.HoverInteractName,
                DMUiToolkitOverlayDocument.HoverInteractUxml,
                DMUiToolkitOverlayDocument.HoverInteractUss,
                DMUiToolkitOverlayDocument.HoverInteractSort);
            if (doc == null)
                return null;

            DMUiToolkitHoverInteract host = doc.GetComponent<DMUiToolkitHoverInteract>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitHoverInteract>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(HovercraftUsable usable)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (usable == null)
                return false;

            DMUiToolkitHoverInteract host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal(usable);
            return true;
        }

        public static void Hide()
        {
            instance?.HideInternal();
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!open)
                return;

            if (UiEscapeGate.TryConsumeEscape())
            {
                HideInternal();
                return;
            }

            RefreshLabels();
        }

        private void LateUpdate()
        {
            if (!bound)
                return;

            if (open)
            {
                if (!uguiHidden)
                {
                    HideUgui();
                    uguiHidden = true;
                }
            }
            else
            {
                uguiHidden = false;
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("hover-root") ?? tree;
            fuelLabel = tree.Q<Label>("hover-fuel");
            enterButton = tree.Q<Button>("hover-enter");
            refuelButton = tree.Q<Button>("hover-refuel");
            storeButton = tree.Q<Button>("hover-store");
            cancelButton = tree.Q<Button>("hover-cancel");
            Wire();
            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (enterButton != null)
                enterButton.clicked += OnEnter;
            if (refuelButton != null)
                refuelButton.clicked += OnRefuel;
            if (storeButton != null)
                storeButton.clicked += OnStore;
            if (cancelButton != null)
                cancelButton.clicked += HideInternal;

            VisualElement veil = root?.Q<VisualElement>("hover-veil");
            if (veil != null)
                veil.RegisterCallback<ClickEvent>(_ => HideInternal());

            wired = true;
        }

        private void ShowInternal(HovercraftUsable usable)
        {
            BindTree();
            activeUsable = usable;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            open = true;
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            if (root != null)
            {
                VisualElement panel = root.Q<VisualElement>("hover-panel");
                panel?.BringToFront();
            }

            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonHovercraftMenu, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            RefreshLabels();
        }

        private void HideInternal()
        {
            activeUsable = null;
            open = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);

            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonHovercraftMenu, false);
            player?.ApplyCursorState();
            GameplayInputRecovery.QueueCursorRestore();
        }

        private void RefreshLabels()
        {
            if (activeUsable == null)
                return;

            HovercraftFuelSystem fuel = activeUsable.FuelSystem;
            if (fuel != null)
            {
                if (fuelLabel != null)
                    fuelLabel.text = $"Fuel: {Mathf.RoundToInt(fuel.CurrentFuel)} / {Mathf.RoundToInt(fuel.MaxFuel)}";
                if (refuelButton != null)
                    refuelButton.SetEnabled(!fuel.IsFull);
            }
            else
            {
                if (fuelLabel != null)
                    fuelLabel.text = string.Empty;
                if (refuelButton != null)
                    refuelButton.SetEnabled(false);
            }
        }

        private void OnEnter()
        {
            activeUsable?.TryEnterFromMenu();
            HideInternal();
        }

        private void OnRefuel()
        {
            if (activeUsable == null)
                return;

            InventorySystem inventory = PlayerLocator.FindPlayerObject()?.GetComponent<InventorySystem>();
            if (activeUsable.TryRefuelFromMenu(inventory, out string message) || !string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);
        }

        private void OnStore()
        {
            if (activeUsable == null)
                return;

            InventorySystem inventory = PlayerLocator.FindPlayerObject()?.GetComponent<InventorySystem>();
            bool stored = activeUsable.TryStoreFromMenu(inventory, out string message);
            if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);

            if (stored)
                HideInternal();
        }

        private static void HideUgui()
        {
            HovercraftInteractMenuUI menu = Object.FindAnyObjectByType<HovercraftInteractMenuUI>(FindObjectsInactive.Include);
            if (menu != null)
                DMUiToolkitOverlayDocument.HideGameObject(menu.gameObject);
        }
    }
}
