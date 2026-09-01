using Project.Data;
using Project.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private static readonly string[] HotbarKeyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        private VisualElement inventoryHotbarRow;
        private VisualElement invCtxRoot;
        private VisualElement invCtxDismiss;
        private VisualElement invCtxPanel;
        private VisualElement invCtxAmmo;
        private bool inventoryHotbarBuilt;
        private readonly System.Collections.Generic.List<VisualElement> invHotbarSlots = new System.Collections.Generic.List<VisualElement>();
        private readonly System.Collections.Generic.List<VisualElement> invHotbarIcons = new System.Collections.Generic.List<VisualElement>();
        private readonly System.Collections.Generic.List<Label> invHotbarAmounts = new System.Collections.Generic.List<Label>();

        public static bool TryDropOnInventoryHotbar(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (instance == null || !instance.menusVisible || instance.inventoryBody == null)
                return false;
            if (instance.inventoryBody.resolvedStyle.display == DisplayStyle.None)
                return false;
            return instance.DropOnInventoryHotbar(screenPosition, sourceAbsoluteIndex);
        }

        private void BindInventoryHotbar(VisualElement tree)
        {
            if (tree == null)
                return;

            inventoryHotbarRow = tree.Q<VisualElement>("inventory-hotbar-row");
            invCtxRoot = tree.Q<VisualElement>("inv-ctx-root");
            if (invCtxRoot == null && document != null)
                invCtxRoot = document.rootVisualElement?.Q<VisualElement>("inv-ctx-root");
            invCtxDismiss = tree.Q<VisualElement>("inv-ctx-dismiss") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-dismiss");
            invCtxPanel = tree.Q<VisualElement>("inv-ctx-panel") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-panel");
            invCtxAmmo = tree.Q<VisualElement>("inv-ctx-ammo") ?? invCtxRoot?.Q<VisualElement>("inv-ctx-ammo");

            if (invCtxDismiss != null)
            {
                invCtxDismiss.UnregisterCallback<ClickEvent>(OnInvCtxDismissClicked);
                invCtxDismiss.RegisterCallback<ClickEvent>(OnInvCtxDismissClicked);
            }

            DMUiToolkitOverlayDocument.SetShown(invCtxRoot, false);
            inventoryHotbarBuilt = false;
            EnsureInventoryHotbarSlots();
        }

        private void OnInvCtxDismissClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            HideInventoryContextMenu();
        }

        private void EnsureInventoryHotbarSlots()
        {
            if (inventoryHotbarBuilt || inventoryHotbarRow == null)
                return;

            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;

            invHotbarSlots.Clear();
            invHotbarIcons.Clear();
            invHotbarAmounts.Clear();
            inventoryHotbarRow.Clear();

            int hotbarCount = Mathf.Min(10, boundInventory.hotbarSize);
            BuildHotbarRow(inventoryHotbarRow, hotbarCount);
            inventoryHotbarBuilt = true;
        }

        private void BuildHotbarRow(VisualElement row, int count)
        {
            if (row == null || count <= 0)
                return;

            for (int localIndex = 0; localIndex < count; localIndex++)
            {
                int absoluteIndex = boundInventory.HotbarStartIndex + localIndex;

                VisualElement slot = new VisualElement();
                slot.AddToClassList("dmg-inv-hotbar-slot");
                slot.name = "inv-hotbar-slot-" + localIndex;
                slot.pickingMode = PickingMode.Position;
                AttachInventorySlotDrag(slot, absoluteIndex);

                VisualElement icon = new VisualElement();
                icon.AddToClassList("dmg-inv-hotbar-icon");
                icon.pickingMode = PickingMode.Ignore;
                slot.Add(icon);

                string keyText = localIndex < HotbarKeyLabels.Length ? HotbarKeyLabels[localIndex] : string.Empty;
                Label keyLabel = new Label(keyText);
                keyLabel.AddToClassList("dmg-inv-hotbar-key");
                keyLabel.pickingMode = PickingMode.Ignore;
                slot.Add(keyLabel);

                Label amount = new Label();
                amount.AddToClassList("dmg-inv-hotbar-amount");
                amount.pickingMode = PickingMode.Ignore;
                slot.Add(amount);

                row.Add(slot);
                invHotbarSlots.Add(slot);
                invHotbarIcons.Add(icon);
                invHotbarAmounts.Add(amount);
            }
        }

        private void RefreshInventoryHotbar()
        {
            if (!inventoryHotbarBuilt)
                EnsureInventoryHotbarSlots();
            if (invHotbarSlots.Count == 0)
                return;

            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;

            EquipmentController equipment = boundInventory.GetComponent<EquipmentController>();

            for (int i = 0; i < invHotbarSlots.Count; i++)
            {
                VisualElement slot = invHotbarSlots[i];
                VisualElement icon = invHotbarIcons[i];
                Label amount = invHotbarAmounts[i];
                if (slot?.userData is not int absoluteIndex)
                    continue;

                InventorySystem.InventorySlot data = absoluteIndex >= 0 && absoluteIndex < boundInventory.slots.Count
                    ? boundInventory.slots[absoluteIndex]
                    : null;
                ItemData item = data != null && !data.IsEmpty ? data.item : null;

                if (item != null && item.icon != null)
                {
                    if (DMUiToolkitStyle.TrySetSpriteBackground(icon, item.icon, ScaleMode.ScaleToFit))
                        DMUiToolkitOverlayDocument.SetShown(icon, true);
                    else
                        DMUiToolkitOverlayDocument.SetShown(icon, false);
                }
                else
                {
                    DMUiToolkitStyle.ClearBackgroundImage(icon);
                    DMUiToolkitOverlayDocument.SetShown(icon, false);
                }

                int stack = data != null ? data.amount : 0;
                amount.text = item != null && stack > 1 ? stack.ToString() : string.Empty;

                bool selected = false;
                if (item != null && equipment != null)
                {
                    if (boundInventory.IsToolbarIndex(absoluteIndex))
                        selected = equipment.IsSelectedToolbarAbsoluteIndex(absoluteIndex);
                    else if (equipment.IsWeaponHotbarSlot(absoluteIndex - boundInventory.inventorySize))
                        selected = equipment.IsActiveWeaponHotbarIndex(absoluteIndex);
                    else
                        selected = absoluteIndex == equipment.SelectedSlotIndex;
                }

                slot.EnableInClassList("dmg-inv-hotbar-slot--selected", selected);
            }
        }

        private bool DropOnInventoryHotbar(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return false;

            Vector2 panelPos = ScreenToMenuPanel(screenPosition);
            return TryDropOnHotbarPanelPos(panelPos, sourceAbsoluteIndex);
        }

        private bool TryDropOnHotbarPanelPos(Vector2 panelPos, int sourceAbsoluteIndex)
        {
            int dest = FindInventoryHotbarSlotAtPanel(panelPos);
            if (dest < 0)
                return false;
            if (dest == sourceAbsoluteIndex)
                return true;

            InventorySystem.InventorySlot from = sourceAbsoluteIndex >= 0 && sourceAbsoluteIndex < boundInventory.slots.Count
                ? boundInventory.slots[sourceAbsoluteIndex]
                : null;
            if (from == null || from.IsEmpty || from.item == null)
                return true;

            if (boundInventory.CanAcceptItemAt(dest, from.item, showLevelToast: true))
                boundInventory.MoveOrMergeSlots(sourceAbsoluteIndex, dest);
            return true;
        }

        private int FindInventoryHotbarSlotAtPanel(Vector2 panelPos)
        {
            for (int i = 0; i < invHotbarSlots.Count; i++)
            {
                VisualElement slot = invHotbarSlots[i];
                if (slot == null || slot.resolvedStyle.display == DisplayStyle.None)
                    continue;
                if (!slot.worldBound.Contains(panelPos))
                    continue;
                if (slot.userData is int absoluteIndex)
                    return absoluteIndex;
            }

            return -1;
        }
    }
}
