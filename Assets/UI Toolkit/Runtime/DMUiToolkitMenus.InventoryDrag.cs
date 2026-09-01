using Project.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private const float InvDragThresholdPx = 8f;

        private InventoryItemActions boundItemActions;
        private bool invDragActive;
        private int invDragSource = -1;
        private int invPointerSlot = -1;
        private int invCapturedPointerId = -1;
        private Vector2 invPointerDown;
        private Vector2 invLastPanelPos;
        private VisualElement invDragGhost;
        private Label inventoryStorageLabel;
        private Button inventoryStorageInstall;

        public static bool TryDropOnInventorySlot(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (instance == null || !instance.menusVisible || instance.inventoryBody == null)
                return false;
            if (instance.inventoryBody.resolvedStyle.display == DisplayStyle.None)
                return false;
            return instance.DropOnInventorySlot(screenPosition, sourceAbsoluteIndex);
        }

        public static bool IsPointerOverInventory(Vector2 screenPosition)
        {
            if (instance == null || instance.inventoryBody == null || instance.root == null)
                return false;
            if (instance.inventoryBody.resolvedStyle.display == DisplayStyle.None)
                return false;

            IPanel panel = instance.root.panel;
            if (panel == null)
                return false;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            return instance.inventoryBody.worldBound.Contains(panelPos);
        }

        private void BindInventoryStorage(VisualElement tree)
        {
            if (tree == null)
                return;

            inventoryStorageLabel = tree.Q<Label>("inventory-storage-label");
            inventoryStorageInstall = tree.Q<Button>("inventory-storage-install");
            if (inventoryStorageInstall != null)
            {
                inventoryStorageInstall.clicked -= OnInstallStorageClicked;
                inventoryStorageInstall.clicked += OnInstallStorageClicked;
            }
        }

        private void RefreshInventoryStorage()
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            boundItemActions ??= boundInventory != null ? boundInventory.GetComponent<InventoryItemActions>() : null;
            EnsureInventoryContextMenu();

            int unlocked = boundInventory != null ? boundInventory.unlockedMainSlots : 0;
            int total = boundInventory != null ? boundInventory.inventorySize : 0;
            if (inventoryStorageLabel != null)
            {
                inventoryStorageLabel.text = boundInventory == null
                    ? "Storage unavailable."
                    : "Storage " + unlocked + "/" + total + " unlocked  ·  Install Increase Storage Module to unlock the next 10-slot row.";
            }

            int moduleSlot = FindStorageModuleSlot();
            bool canInstall = boundItemActions != null && moduleSlot >= 0 && boundItemActions.CanInstallStorageModule(moduleSlot);
            if (inventoryStorageInstall != null)
            {
                inventoryStorageInstall.SetEnabled(canInstall);
                inventoryStorageInstall.text = canInstall ? "Install Storage Module" : "No module to install";
            }
        }

        private int FindStorageModuleSlot()
        {
            if (boundInventory == null)
                return -1;
            for (int i = 0; i < boundInventory.slots.Count && i < boundInventory.inventorySize; i++)
            {
                InventorySystem.InventorySlot data = boundInventory.slots[i];
                if (data == null || data.IsEmpty || data.item == null)
                    continue;
                if (data.item.IsInventoryStorageModule)
                    return i;
            }

            return -1;
        }

        private void OnInstallStorageClicked()
        {
            boundItemActions ??= boundInventory != null ? boundInventory.GetComponent<InventoryItemActions>() : null;
            int slot = FindStorageModuleSlot();
            if (boundItemActions == null || slot < 0)
                return;
            boundItemActions.TryInstallStorageModule(slot);
            RefreshInventory();
        }

        private void EnsureInventoryContextMenu()
        {
            boundItemActions ??= boundInventory != null ? boundInventory.GetComponent<InventoryItemActions>() : null;
            if (boundItemActions == null)
                return;
            InventoryContextMenu.EnsureExists(transform, boundItemActions);
        }

        private void AttachInventorySlotDrag(VisualElement slot, int index)
        {
            if (slot == null)
                return;

            slot.userData = index;
            slot.pickingMode = PickingMode.Position;
            slot.RegisterCallback<PointerDownEvent>(OnInvPointerDown);
            slot.RegisterCallback<PointerMoveEvent>(OnInvPointerMove);
            slot.RegisterCallback<PointerUpEvent>(OnInvPointerUp);
            slot.RegisterCallback<PointerCaptureOutEvent>(OnInvPointerCaptureOut);
            slot.RegisterCallback<PointerEnterEvent>(OnInvPointerEnter);
            slot.RegisterCallback<PointerLeaveEvent>(OnInvPointerLeave);
        }

        private bool DropOnInventorySlot(Vector2 screenPosition, int sourceAbsoluteIndex)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return false;

            Vector2 panelPos = ScreenToMenuPanel(screenPosition);
            int dest = FindInventorySlotAtPanel(panelPos);
            if (dest < 0)
                return false;
            if (dest == sourceAbsoluteIndex)
                return true;
            if (!boundInventory.IsMainSlotUnlocked(dest))
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

        private void OnInvPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;

            invPointerSlot = index;
            invPointerDown = (Vector2)evt.position;
            invLastPanelPos = invPointerDown;
            invDragActive = false;
            invCapturedPointerId = evt.pointerId;
            slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnInvPointerMove(PointerMoveEvent evt)
        {
            if (invPointerSlot < 0)
                return;

            invLastPanelPos = (Vector2)evt.position;
            if (invDragActive)
            {
                PositionInvDragGhost(invLastPanelPos);
                return;
            }

            if ((evt.pressedButtons & 1) == 0)
                return;

            Vector2 delta = invLastPanelPos - invPointerDown;
            if (delta.sqrMagnitude < InvDragThresholdPx * InvDragThresholdPx)
                return;

            BeginInvDrag(invPointerSlot, invLastPanelPos);
        }

        private void OnInvPointerUp(PointerUpEvent evt)
        {
            int sourceSlot = invPointerSlot;
            Vector2 panelPos = (Vector2)evt.position;
            int button = evt.button;
            bool dragging = invDragActive;
            ReleaseInvPointer();

            if (dragging)
            {
                CompleteInvDrag(panelPos);
                return;
            }

            if (sourceSlot >= 0)
                HandleInvClick(sourceSlot, button);
        }

        private void OnInvPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (invPointerSlot < 0)
                return;

            bool dragging = invDragActive;
            Vector2 panelPos = invLastPanelPos;
            ReleaseInvPointer();
            if (dragging)
                CompleteInvDrag(panelPos);
        }

        private void ReleaseInvPointer()
        {
            int id = invCapturedPointerId;
            int slotIndex = invPointerSlot;
            invCapturedPointerId = -1;
            invPointerSlot = -1;
            invDragActive = false;
            if (id >= 0 && slotIndex >= 0 && slotIndex < invSlots.Count)
            {
                VisualElement slot = invSlots[slotIndex];
                if (slot != null && slot.HasPointerCapture(id))
                    slot.ReleasePointer(id);
            }
        }

        private void BeginInvDrag(int slotIndex, Vector2 panelPos)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;
            if (slotIndex < 0 || slotIndex >= boundInventory.slots.Count)
                return;
            if (!boundInventory.IsMainSlotUnlocked(slotIndex))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[slotIndex];
            if (data == null || data.IsEmpty || data.item == null || data.item.icon == null)
                return;

            invDragActive = true;
            invDragSource = slotIndex;
            ClearInvDragGhost();

            invDragGhost = new VisualElement();
            invDragGhost.name = "dmg-inv-drag-ghost";
            invDragGhost.pickingMode = PickingMode.Ignore;
            invDragGhost.style.position = Position.Absolute;
            invDragGhost.style.width = 48f;
            invDragGhost.style.height = 48f;
            invDragGhost.style.backgroundImage = new StyleBackground(Background.FromSprite(data.item.icon));
            invDragGhost.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            invDragGhost.style.opacity = 0.75f;
            VisualElement ghostParent = root != null ? root : inventoryBody;
            ghostParent?.Add(invDragGhost);
            PositionInvDragGhost(panelPos);

            if (slotIndex < invIcons.Count && invIcons[slotIndex] != null)
                invIcons[slotIndex].style.opacity = 0.35f;
        }

        private void PositionInvDragGhost(Vector2 panelPos)
        {
            if (invDragGhost == null)
                return;

            VisualElement parent = invDragGhost.parent != null ? invDragGhost.parent : root;
            Vector2 local = panelPos;
            if (parent != null)
                local = parent.WorldToLocal(panelPos);

            float width = invDragGhost.resolvedStyle.width;
            float height = invDragGhost.resolvedStyle.height;
            if (width <= 0f)
                width = 48f;
            if (height <= 0f)
                height = 48f;

            invDragGhost.style.left = local.x - width * 0.5f;
            invDragGhost.style.top = local.y - height * 0.5f;
        }

        private void ClearInvDragGhost()
        {
            if (invDragGhost != null)
            {
                invDragGhost.RemoveFromHierarchy();
                invDragGhost = null;
            }

            for (int i = 0; i < invIcons.Count; i++)
            {
                if (invIcons[i] != null)
                    invIcons[i].style.opacity = 1f;
            }
        }

        private void CompleteInvDrag(Vector2 panelPos)
        {
            int source = invDragSource;
            ClearInvDragGhost();
            invDragSource = -1;
            RefreshInventory();
            if (boundInventory == null || source < 0)
                return;

            Vector2 screenPos = CurrentPointerScreenPosition();
            int dest = FindInventorySlotAtPanel(panelPos);
            if (dest >= 0)
            {
                if (dest != source && boundInventory.IsMainSlotUnlocked(dest))
                {
                    InventorySystem.InventorySlot from = boundInventory.slots[source];
                    if (from != null && !from.IsEmpty && from.item != null
                        && boundInventory.CanAcceptItemAt(dest, from.item, showLevelToast: true))
                        boundInventory.MoveOrMergeSlots(source, dest);
                }

                return;
            }

            if (DMUiToolkitHud.TryDropOnSlot(screenPos, source))
                return;
            if (DMUiToolkitHud.IsPointerOverHotbarOrTools(screenPos))
                return;

            if (menuRoot != null && menuRoot.worldBound.Contains(panelPos))
                return;

            boundItemActions ??= boundInventory.GetComponent<InventoryItemActions>();
            if (boundItemActions != null)
                boundItemActions.TryDrop(source);
            else
                boundInventory.DropItemAt(source);
        }

        private void HandleInvClick(int slotIndex, int button)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null || slotIndex < 0 || slotIndex >= boundInventory.slots.Count)
                return;
            if (!boundInventory.IsMainSlotUnlocked(slotIndex))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[slotIndex];
            if (data == null || data.IsEmpty || data.item == null)
                return;

            Vector2 screenPos = CurrentPointerScreenPosition();
            if (button == 1)
            {
                EnsureInventoryContextMenu();
                boundItemActions ??= boundInventory.GetComponent<InventoryItemActions>();
                DMUiToolkitContext.TryShow(slotIndex, screenPos, boundItemActions);
                return;
            }

            if (button != 0)
                return;

            boundItemActions ??= boundInventory.GetComponent<InventoryItemActions>();
            if (boundItemActions != null)
                boundItemActions.TryUse(slotIndex);
            else
                boundInventory.UseItemAt(slotIndex);
        }


        private void OnInvPointerEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null || index < 0 || index >= boundInventory.slots.Count)
                return;
            if (!boundInventory.IsMainSlotUnlocked(index))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[index];
            if (data == null || data.IsEmpty || data.item == null)
                return;

            DMUiToolkitWorldMenus.TryShowItemTooltip(data.item, data.amount, CurrentPointerScreenPosition());
        }

        private void OnInvPointerLeave(PointerLeaveEvent evt)
        {
            DMUiToolkitWorldMenus.HideItemTooltip();
        }

        private int FindInventorySlotAtPanel(Vector2 panelPos)
        {
            for (int i = 0; i < invSlots.Count; i++)
            {
                VisualElement slot = invSlots[i];
                if (slot == null || slot.resolvedStyle.display == DisplayStyle.None)
                    continue;
                if (slot.worldBound.Contains(panelPos))
                    return i;
            }

            return -1;
        }

        private Vector2 ScreenToMenuPanel(Vector2 screenPosition)
        {
            IPanel panel = root != null ? root.panel : null;
            if (panel == null && document != null && document.rootVisualElement != null)
                panel = document.rootVisualElement.panel;
            if (panel == null)
                return screenPosition;
            return RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        }

        private static Vector2 CurrentPointerScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
            return Vector2.zero;
        }
    }
}
