using System.Collections.Generic;
using Project.Audio;
using Project.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK inventory floating context menu. Same actions via InventoryItemActions.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-369)]
    [DisallowMultipleComponent]
    public class DMUiToolkitContext : MonoBehaviour
    {
        private static DMUiToolkitContext instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dismiss;
        private VisualElement panel;
        private VisualElement ammoPanel;
        private bool bound;
        private bool wired;
        private bool open;
        private int activeSlot = -1;
        private int openedOnFrame = -1;
        private InventoryItemActions actions;

        public static bool IsOpen => instance != null && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitContext EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.ContextName,
                DMUiToolkitOverlayDocument.ContextUxml,
                DMUiToolkitOverlayDocument.ContextUss,
                DMUiToolkitOverlayDocument.ContextSort);
            if (doc == null)
                return null;

            DMUiToolkitContext host = doc.GetComponent<DMUiToolkitContext>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitContext>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(int slotIndex, Vector2 screenPosition, InventoryItemActions itemActions)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (itemActions == null)
                return false;

            DMUiToolkitContext host = EnsureHost();
            if (host == null)
                return false;

            return host.ShowInternal(slotIndex, screenPosition, itemActions);
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

            if (Time.frameCount == openedOnFrame)
                return;

            if (UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                HideInternal();
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            if (open)
                HideUgui();
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

            root = tree.Q<VisualElement>("ctx-root") ?? tree;
            dismiss = tree.Q<VisualElement>("ctx-dismiss");
            panel = tree.Q<VisualElement>("ctx-panel");
            ammoPanel = tree.Q<VisualElement>("ctx-ammo");
            Wire();
            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (dismiss != null)
                dismiss.RegisterCallback<ClickEvent>(_ => HideInternal());
            wired = true;
        }

        private bool ShowInternal(int slotIndex, Vector2 screenPosition, InventoryItemActions itemActions)
        {
            BindTree();
            ItemHoverTooltip.HideAny();
            actions = itemActions;
            activeSlot = slotIndex;
            DMUiToolkitOverlayDocument.SetShown(ammoPanel, false);
            RebuildButtons();
            if (panel == null || panel.childCount == 0)
                return false;

            openedOnFrame = Time.frameCount;
            open = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PositionAtScreen(panel, screenPosition);
            return true;
        }

        private void HideInternal()
        {
            open = false;
            activeSlot = -1;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            DMUiToolkitOverlayDocument.SetShown(ammoPanel, false);
        }

        private void RebuildButtons()
        {
            if (panel == null || actions == null)
                return;

            panel.Clear();
            TryAdd("Use", actions.CanUse(activeSlot) && !actions.CanInstallStorageModule(activeSlot)
                && !actions.CanDeployShelter(activeSlot) && !actions.CanDeployWalkerDrill(activeSlot),
                () => Execute(actions.TryUse(activeSlot)));
            TryAdd("Install", actions.CanInstallStorageModule(activeSlot),
                () => Execute(actions.TryInstallStorageModule(activeSlot)));
            TryAdd("Equip", actions.CanEquip(activeSlot),
                () => Execute(actions.TryEquip(activeSlot)));
            TryAdd("Unequip", actions.CanUnequip(activeSlot),
                () => Execute(actions.TryUnequip(activeSlot)));

            if (actions.CanEquipAmmo(activeSlot))
            {
                Button ammo = DMUiToolkitOverlayDocument.MakeMenuButton("EquipAmmo", "Equip Ammo >");
                ammo.RegisterCallback<PointerEnterEvent>(_ => ShowAmmo());
                panel.Add(ammo);
            }

            TryAdd("Refuel", actions.CanRefuelVehicle(activeSlot),
                () => Execute(actions.TryRefuelVehicle(activeSlot)));
            TryAdd("Refill Mining Tool", actions.CanRefillMiningTool(activeSlot),
                () => Execute(actions.TryRefillMiningTool(activeSlot)));
            TryAdd("Deploy", actions.CanDeploy(activeSlot),
                () => Execute(actions.TryDeploy(activeSlot)));
            TryAdd("Split", actions.CanSplit(activeSlot),
                () => Execute(actions.TrySplit(activeSlot)));
            TryAdd("Drop", actions.CanDrop(activeSlot),
                () => Execute(actions.TryDrop(activeSlot)));
        }

        private void TryAdd(string label, bool visible, System.Action action)
        {
            if (!visible || panel == null)
                return;

            Button button = DMUiToolkitOverlayDocument.MakeMenuButton(label.Replace(" ", string.Empty), label);
            button.clicked += () =>
            {
                action?.Invoke();
                HideInternal();
            };
            panel.Add(button);
        }

        private void ShowAmmo()
        {
            if (actions == null || ammoPanel == null)
                return;

            List<InventoryItemActions.AmmoEquipOption> options = actions.GetAmmoEquipOptions(activeSlot);
            if (options == null || options.Count == 0)
            {
                DMUiToolkitOverlayDocument.SetShown(ammoPanel, false);
                return;
            }

            ammoPanel.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                InventoryItemActions.AmmoEquipOption option = options[i];
                int slot = option.WeaponHotbarSlot;
                Button button = DMUiToolkitOverlayDocument.MakeMenuButton("Ammo_" + slot, option.WeaponLabel);
                button.clicked += () =>
                {
                    Execute(actions.TryEquipAmmoToWeapon(activeSlot, slot));
                    HideInternal();
                };
                ammoPanel.Add(button);
            }

            DMUiToolkitOverlayDocument.SetShown(ammoPanel, true);
            float left = panel.resolvedStyle.left + panel.resolvedStyle.width;
            float top = panel.resolvedStyle.top;
            ammoPanel.style.left = left;
            ammoPanel.style.top = top;
        }

        private static void Execute(bool success)
        {
            if (!success)
                GameAudioManager.Instance?.PlayInventoryItemClick();
        }

        private static void HideUgui()
        {
            InventoryContextMenu menu = Object.FindAnyObjectByType<InventoryContextMenu>(FindObjectsInactive.Include);
            if (menu != null)
                DMUiToolkitOverlayDocument.HideGameObject(menu.gameObject);
        }
    }
}
