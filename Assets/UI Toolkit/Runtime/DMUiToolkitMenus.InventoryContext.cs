using System.Collections.Generic;
using Project.Audio;
using Project.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private int invContextSlot = -1;
        private bool invContextOpen;
        private int invContextOpenedFrame = -1;
        private bool invContextAwaitingRightRelease;
        private Button invCtxAmmoButton;

        public static bool IsInventoryContextOpen => instance != null && instance.invContextOpen;

        public static bool TryGetInventoryContextLayer(
            out VisualElement menuRoot,
            out VisualElement menuDismiss,
            out VisualElement menuPanel,
            out VisualElement menuAmmo)
        {
            menuRoot = null;
            menuDismiss = null;
            menuPanel = null;
            menuAmmo = null;

            if (instance == null || !instance.menusVisible || !IsInventoryOpen)
                return false;

            instance.EnsureInventoryContextLayer();
            if (instance.invCtxRoot == null || instance.invCtxPanel == null)
                return false;

            menuRoot = instance.invCtxRoot;
            menuDismiss = instance.invCtxDismiss;
            menuPanel = instance.invCtxPanel;
            menuAmmo = instance.invCtxAmmo;
            return true;
        }

        private void EnsureInventoryContextLayer()
        {
            if (invCtxRoot != null && invCtxPanel != null)
                return;

            VisualElement tree = menuRoot;
            if (tree == null && document != null)
                tree = document.rootVisualElement;
            if (tree == null)
                return;

            invCtxRoot = tree.Q<VisualElement>("inv-ctx-root");
            invCtxDismiss = tree.Q<VisualElement>("inv-ctx-dismiss") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-dismiss");
            invCtxPanel = tree.Q<VisualElement>("inv-ctx-panel") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-panel");
            invCtxAmmo = tree.Q<VisualElement>("inv-ctx-ammo") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-ammo");

            if (invCtxDismiss != null)
            {
                invCtxDismiss.UnregisterCallback<ClickEvent>(OnInvCtxDismissClicked);
                invCtxDismiss.RegisterCallback<ClickEvent>(OnInvCtxDismissClicked);
            }
        }

        private void HideInventoryContextMenu()
        {
            invContextOpen = false;
            invContextSlot = -1;
            invCtxAmmoButton = null;
            invContextAwaitingRightRelease = false;
            DMUiToolkitOverlayDocument.SetShown(invCtxAmmo, false);
            DMUiToolkitOverlayDocument.SetShown(invCtxRoot, false);
            if (invCtxRoot != null)
                invCtxRoot.pickingMode = PickingMode.Ignore;
        }

        private void ShowInventoryContextMenu(int slotIndex, Vector2 panelPosition)
        {
            EnsureInventoryContextLayer();
            if (invCtxPanel == null || invCtxRoot == null)
            {
                Debug.LogWarning("[DMUiToolkitMenus] Inventory context layer missing (inv-ctx-root/panel).");
                return;
            }

            boundItemActions = EnsureBoundItemActions();
            if (boundItemActions == null)
            {
                Debug.LogWarning("[DMUiToolkitMenus] InventoryItemActions not found — cannot open context menu.");
                return;
            }

            ItemHoverTooltip.HideAny();
            DMUiToolkitWorldMenus.HideItemTooltip();
            DMUiToolkitContext.Hide();
            invContextSlot = slotIndex;
            invContextOpenedFrame = Time.frameCount;
            invContextAwaitingRightRelease = Mouse.current != null && Mouse.current.rightButton.isPressed;
            DMUiToolkitOverlayDocument.SetShown(invCtxAmmo, false);
            RebuildInventoryContextButtons();

            if (invCtxPanel.childCount == 0)
            {
                // Guarantee Drop when the slot has an item so the menu is never empty.
                if (boundInventory != null && boundInventory.GetItemAt(slotIndex) != null)
                {
                    TryAddContextBtn("Drop", true, () => ExecuteContextAction(boundItemActions.TryDrop(slotIndex)));
                }

                if (invCtxPanel.childCount == 0)
                {
                    HideInventoryContextMenu();
                    return;
                }
            }

            invContextOpen = true;
            invCtxRoot.pickingMode = PickingMode.Position;
            if (invCtxDismiss != null)
                invCtxDismiss.pickingMode = PickingMode.Position;
            invCtxPanel.pickingMode = PickingMode.Position;
            invCtxRoot.style.display = DisplayStyle.Flex;
            invCtxRoot.BringToFront();
            menuRoot?.BringToFront();
            DMUiToolkitOverlayDocument.SetShown(invCtxRoot, true);
            VisualElement clampRoot = invCtxRoot ?? menuRoot;
            DMUiToolkitOverlayDocument.PositionContextMenuAtPanel(invCtxPanel, panelPosition, clampRoot);
            invCtxPanel.schedule.Execute(() =>
            {
                if (!invContextOpen || invCtxPanel == null)
                    return;
                DMUiToolkitOverlayDocument.PositionContextMenuAtPanel(invCtxPanel, panelPosition, clampRoot);
                invCtxPanel.BringToFront();
                invCtxAmmo?.BringToFront();
            }).ExecuteLater(1);
            invCtxPanel.BringToFront();
        }

        private void RebuildInventoryContextButtons()
        {
            if (invCtxPanel == null || boundItemActions == null)
                return;

            invCtxPanel.Clear();
            int slot = invContextSlot;

            TryAddContextBtn("Use",
                boundItemActions.CanUse(slot)
                && !boundItemActions.CanInstallStorageModule(slot)
                && !boundItemActions.CanDeployShelter(slot)
                && !boundItemActions.CanDeployWalkerDrill(slot),
                () => ExecuteContextAction(boundItemActions.TryUse(slot)));
            TryAddContextBtn("Install", boundItemActions.CanInstallStorageModule(slot),
                () => ExecuteContextAction(boundItemActions.TryInstallStorageModule(slot)));
            TryAddContextBtn("Equip", boundItemActions.CanEquip(slot),
                () => ExecuteContextAction(boundItemActions.TryEquip(slot)));
            TryAddContextBtn("Add to Hotbar", boundItemActions.CanAddToHotbar(slot),
                () => ExecuteContextAction(boundItemActions.TryAddToHotbar(slot)));
            TryAddContextBtn("Unequip", boundItemActions.CanUnequip(slot),
                () => ExecuteContextAction(boundItemActions.TryUnequip(slot)));

            if (boundItemActions.CanEquipAmmo(slot))
                AddInventoryAmmoContextButton();

            TryAddContextBtn("Refuel", boundItemActions.CanRefuelVehicle(slot),
                () => ExecuteContextAction(boundItemActions.TryRefuelVehicle(slot)));
            TryAddContextBtn("Refill Mining Tool", boundItemActions.CanRefillMiningTool(slot),
                () => ExecuteContextAction(boundItemActions.TryRefillMiningTool(slot)));
            TryAddContextBtn("Deploy", boundItemActions.CanDeploy(slot),
                () => ExecuteContextAction(boundItemActions.TryDeploy(slot)));
            TryAddContextBtn("Split", boundItemActions.CanSplit(slot),
                () => ExecuteContextAction(boundItemActions.TrySplit(slot)));
            TryAddContextBtn("Drop", boundItemActions.CanDrop(slot),
                () => ExecuteContextAction(boundItemActions.TryDrop(slot)));
        }

        private void AddInventoryAmmoContextButton()
        {
            if (invCtxPanel == null)
                return;

            invCtxAmmoButton = DMUiToolkitOverlayDocument.MakeMenuButton("EquipAmmo", "Add to >");
            invCtxAmmoButton.RegisterCallback<PointerEnterEvent>(_ => ShowInventoryAmmoFlyout());
            invCtxAmmoButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                ShowInventoryAmmoFlyout();
                evt.StopPropagation();
            });
            invCtxPanel.Add(invCtxAmmoButton);
        }

        private void TryAddContextBtn(string label, bool visible, System.Action action)
        {
            if (!visible || invCtxPanel == null)
                return;

            Button button = DMUiToolkitOverlayDocument.MakeMenuButton(label.Replace(" ", string.Empty), label);
            button.clicked += () =>
            {
                action?.Invoke();
                HideInventoryContextMenu();
            };
            invCtxPanel.Add(button);
        }

        private void ShowInventoryAmmoFlyout()
        {
            if (boundItemActions == null || invCtxAmmo == null || invCtxPanel == null)
                return;

            List<InventoryItemActions.AmmoEquipOption> options = boundItemActions.GetAmmoEquipOptions(invContextSlot);
            if (options == null || options.Count == 0)
            {
                DMUiToolkitOverlayDocument.SetShown(invCtxAmmo, false);
                return;
            }

            invCtxAmmo.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                InventoryItemActions.AmmoEquipOption option = options[i];
                int weaponSlot = option.WeaponHotbarSlot;
                Button button = DMUiToolkitOverlayDocument.MakeMenuButton("Ammo_" + weaponSlot, option.WeaponLabel);
                button.clicked += () =>
                {
                    ExecuteContextAction(boundItemActions.TryEquipAmmoToWeapon(invContextSlot, weaponSlot));
                    HideInventoryContextMenu();
                };
                invCtxAmmo.Add(button);
            }

            DMUiToolkitOverlayDocument.SetShown(invCtxAmmo, true);
            invCtxAmmo.BringToFront();
            // Anchor to the "Add to >" row; flyout is a sibling of the panel under inv-ctx-root.
            DMUiToolkitOverlayDocument.PositionContextMenuFlyout(
                invCtxAmmo,
                invCtxAmmoButton != null ? invCtxAmmoButton : invCtxPanel,
                invCtxRoot,
                invCtxAmmoButton != null ? invCtxAmmoButton : invCtxPanel);
        }

        private void ExecuteContextAction(bool success)
        {
            if (!success)
                GameAudioManager.Instance?.PlayInventoryItemClick();
            RefreshInventory();
        }

        private void TickInventoryContextDismiss()
        {
            if (!invContextOpen)
                return;

            if (DMUiToolkitContext.IsOpen)
            {
                invContextOpen = false;
                invContextSlot = -1;
                invContextAwaitingRightRelease = false;
                return;
            }

            if (invContextAwaitingRightRelease)
            {
                if (Mouse.current == null || !Mouse.current.rightButton.isPressed)
                    invContextAwaitingRightRelease = false;
                return;
            }

            if (Time.frameCount <= invContextOpenedFrame + 2)
                return;

            if (UiEscapeGate.TryConsumeEscape())
            {
                HideInventoryContextMenu();
                return;
            }

            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
                return;

            HideInventoryContextMenu();
        }
    }
}
