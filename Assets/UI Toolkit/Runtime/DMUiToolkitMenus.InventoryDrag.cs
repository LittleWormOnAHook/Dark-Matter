using Project.Audio;
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
        private VisualElement inventoryScroll;
        private VisualElement invDragCaptureSlot;
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

            inventoryScroll = tree.Q<VisualElement>("inventory-scroll");
            if (inventoryBody == null)
                inventoryBody = tree.Q<VisualElement>("inventory-body");
        }

        private void RefreshInventoryStorage()
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            boundItemActions ??= EnsureBoundItemActions();

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

        private void AttachInventorySlotDrag(VisualElement slot, int index)
        {
            if (slot == null)
                return;

            slot.userData = index;
            slot.pickingMode = PickingMode.Position;
            slot.UnregisterCallback<PointerDownEvent>(OnInvPointerDown);
            slot.UnregisterCallback<ContextClickEvent>(OnInvContextClick);
            slot.UnregisterCallback<PointerMoveEvent>(OnInvPointerMove);
            slot.UnregisterCallback<PointerUpEvent>(OnInvPointerUp);
            slot.UnregisterCallback<PointerCaptureOutEvent>(OnInvPointerCaptureOut);
            slot.UnregisterCallback<PointerEnterEvent>(OnInvPointerEnter);
            slot.UnregisterCallback<PointerLeaveEvent>(OnInvPointerLeave);
            slot.RegisterCallback<PointerDownEvent>(OnInvPointerDown);
            slot.RegisterCallback<ContextClickEvent>(OnInvContextClick);
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
                dest = FindInventoryHotbarSlotAtPanel(panelPos);
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

        private void OnInvContextClick(ContextClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;

            evt.StopImmediatePropagation();
            // ContextClickEvent.mousePosition is already panel space (same as PointerEvent.position).
            HandleInvClick(index, 1, evt.mousePosition);
        }

        private void OnInvPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;

            DMUiToolkitWorldMenus.HideItemTooltip();

            if (evt.button == 1)
            {
                HandleInvClick(index, 1, evt.position);
                evt.StopImmediatePropagation();
                return;
            }

            HideInventoryContextMenu();
            DMUiToolkitContext.Hide();

            if (evt.button != 0)
                return;

            invPointerSlot = index;
            invDragCaptureSlot = slot;
            invPointerDown = (Vector2)evt.position;
            invLastPanelPos = invPointerDown;
            invDragActive = false;
            invCapturedPointerId = evt.pointerId;
            slot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnInvPointerMove(PointerMoveEvent evt)
        {
            if (invPointerSlot < 0 || evt.pointerId != invCapturedPointerId)
                return;

            invLastPanelPos = (Vector2)evt.position;
            if (invDragActive)
            {
                PositionInvDragGhost(invLastPanelPos);
                evt.StopPropagation();
                return;
            }

            if ((evt.pressedButtons & 1) == 0)
                return;

            Vector2 delta = invLastPanelPos - invPointerDown;
            if (delta.sqrMagnitude < InvDragThresholdPx * InvDragThresholdPx)
                return;

            BeginInvDrag(invPointerSlot, invLastPanelPos);
            evt.StopPropagation();
        }

        private void OnInvPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != invCapturedPointerId)
                return;

            int sourceSlot = invPointerSlot;
            Vector2 panelPos = (Vector2)evt.position;
            int button = evt.button;
            bool dragging = invDragActive;
            ReleaseInvPointer();

            if (dragging)
            {
                CompleteInvDrag(panelPos);
                evt.StopPropagation();
                return;
            }

            if (sourceSlot >= 0 && button == 0)
                HandleInvClick(sourceSlot, button, panelPos);

            evt.StopPropagation();
        }

        private void OnInvPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (invPointerSlot < 0 || evt.pointerId != invCapturedPointerId)
                return;

            bool dragging = invDragActive;
            Vector2 panelPos = invLastPanelPos;
            ReleaseInvPointer();
            if (dragging)
                CompleteInvDrag(panelPos);
        }

        private void ReleaseInvPointer()
        {
            VisualElement slot = invDragCaptureSlot;
            int id = invCapturedPointerId;
            invDragCaptureSlot = null;
            invCapturedPointerId = -1;
            invPointerSlot = -1;
            invDragActive = false;
            SetInventoryScrollEnabled(true);

            if (slot != null && id >= 0 && slot.HasPointerCapture(id))
                slot.ReleasePointer(id);
        }

        private void SetInventoryScrollEnabled(bool enabled)
        {
            if (inventoryScroll == null)
                return;

            inventoryScroll.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
        }

        private void BeginInvDrag(int slotIndex, Vector2 panelPos)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;
            if (slotIndex < 0 || slotIndex >= boundInventory.slots.Count)
                return;
            if (slotIndex < boundInventory.inventorySize && !boundInventory.IsMainSlotUnlocked(slotIndex))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[slotIndex];
            if (data == null || data.IsEmpty || data.item == null)
                return;

            invDragActive = true;
            invDragSource = slotIndex;
            DMUiToolkitWorldMenus.HideItemTooltip();
            SetInventoryScrollEnabled(false);
            ClearInvDragGhost();

            invDragGhost = new VisualElement();
            invDragGhost.name = "dmg-inv-drag-ghost";
            invDragGhost.pickingMode = PickingMode.Ignore;
            invDragGhost.style.position = Position.Absolute;
            invDragGhost.style.width = 48f;
            invDragGhost.style.height = 48f;
            if (data.item.icon != null)
                DMUiToolkitStyle.TrySetSpriteBackground(invDragGhost, data.item.icon, ScaleMode.ScaleToFit);
            invDragGhost.style.opacity = 0.75f;
            VisualElement ghostParent = menuRoot != null ? menuRoot : inventoryBody;
            ghostParent?.Add(invDragGhost);
            invDragGhost.BringToFront();
            PositionInvDragGhost(panelPos);

            if (slotIndex < invIcons.Count && invIcons[slotIndex] != null)
                invIcons[slotIndex].style.opacity = 0.35f;
            else
                DimHotbarDragSource(slotIndex, 0.35f);
        }

        private void DimHotbarDragSource(int absoluteIndex, float opacity)
        {
            for (int i = 0; i < invHotbarSlots.Count; i++)
            {
                VisualElement slot = invHotbarSlots[i];
                if (slot?.userData is int index && index == absoluteIndex && i < invHotbarIcons.Count && invHotbarIcons[i] != null)
                    invHotbarIcons[i].style.opacity = opacity;
            }
        }

        private void RestoreHotbarDragOpacity()
        {
            for (int i = 0; i < invHotbarIcons.Count; i++)
            {
                if (invHotbarIcons[i] != null)
                    invHotbarIcons[i].style.opacity = 1f;
            }
        }

        private void PositionInvDragGhost(Vector2 panelPos)
        {
            if (invDragGhost == null)
                return;

            VisualElement parent = invDragGhost.parent != null ? invDragGhost.parent : menuRoot;
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

            RestoreHotbarDragOpacity();
        }

        private void CompleteInvDrag(Vector2 panelPos)
        {
            int source = invDragSource;
            ClearInvDragGhost();
            invDragSource = -1;
            if (boundInventory == null || source < 0)
                return;

            boundInventory ??= FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;

            int destMain = FindInventorySlotAtPanel(panelPos);
            if (destMain >= 0)
            {
                if (destMain != source && boundInventory.IsMainSlotUnlocked(destMain))
                {
                    InventorySystem.InventorySlot from = boundInventory.slots[source];
                    if (from != null && !from.IsEmpty && from.item != null
                        && boundInventory.CanAcceptItemAt(destMain, from.item, showLevelToast: true))
                        boundInventory.MoveOrMergeSlots(source, destMain);
                }

                RefreshInventory();
                return;
            }

            int destHotbar = FindInventoryHotbarSlotAtPanel(panelPos);
            if (destHotbar >= 0)
            {
                if (destHotbar != source)
                {
                    InventorySystem.InventorySlot from = boundInventory.slots[source];
                    if (from != null && !from.IsEmpty && from.item != null
                        && boundInventory.CanAcceptItemAt(destHotbar, from.item, showLevelToast: true))
                        boundInventory.MoveOrMergeSlots(source, destHotbar);
                }

                RefreshInventory();
                return;
            }

            Vector2 screenPos = CurrentPointerScreenPosition();
            if (DMUiToolkitHud.TryDropOnSlot(screenPos, source))
            {
                RefreshInventory();
                return;
            }

            if (!IsPointerOverInventoryBody(panelPos))
            {
                boundItemActions ??= boundInventory.GetComponent<InventoryItemActions>();
                if (boundItemActions != null)
                    boundItemActions.TryDrop(source);
                else
                    boundInventory.DropItemAt(source);
            }

            RefreshInventory();
        }

        private bool IsPointerOverInventoryBody(Vector2 panelPos)
        {
            if (inventoryBody != null && inventoryBody.worldBound.Contains(panelPos))
                return true;
            if (menuRoot != null && menuRoot.worldBound.Contains(panelPos))
                return true;
            return false;
        }

        private InventoryItemActions EnsureBoundItemActions()
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return null;

            // Legacy InventoryUI AddComponent'd this at runtime — UITK must do the same or
            // right-click context menus silently no-op (Player often has InventorySystem only).
            boundItemActions = boundInventory.GetComponent<InventoryItemActions>();
            if (boundItemActions == null)
                boundItemActions = boundInventory.gameObject.AddComponent<InventoryItemActions>();
            return boundItemActions;
        }

        private void HandleInvClick(int slotIndex, int button, Vector2 pointerPanelPosition)
        {
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null || slotIndex < 0 || slotIndex >= boundInventory.slots.Count)
                return;
            if (slotIndex < boundInventory.inventorySize && !boundInventory.IsMainSlotUnlocked(slotIndex))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[slotIndex];
            if (data == null || data.IsEmpty || data.item == null)
                return;

            if (button == 1)
            {
                // Same-frame ContextClick + PointerDown must not hide/show twice.
                if (invContextOpen && invContextSlot == slotIndex && invContextOpenedFrame == Time.frameCount)
                    return;

                if (EnsureBoundItemActions() == null)
                    return;

                GameAudioManager.Instance?.PlayInventoryItemClick();
                // Journal inventory shares the HUD panel — use PointerEvent panel coords, not Mouse.screen
                // (Game view letterboxing breaks ScreenToPanel and parks the menu off-screen).
                ShowInventoryContextMenu(slotIndex, pointerPanelPosition);
                return;
            }

            if (button != 0)
                return;

            if (EnsureBoundItemActions() != null)
                boundItemActions.TryUse(slotIndex);
            else
                boundInventory.UseItemAt(slotIndex);
        }

        private void OnInvPointerEnter(PointerEnterEvent evt)
        {
            if (invDragActive)
                return;
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;
            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null || index < 0 || index >= boundInventory.slots.Count)
                return;
            if (index < boundInventory.inventorySize && !boundInventory.IsMainSlotUnlocked(index))
                return;

            InventorySystem.InventorySlot data = boundInventory.slots[index];
            if (data == null || data.IsEmpty || data.item == null)
                return;

            DMUiToolkitWorldMenus.TryShowItemTooltip(data.item, data.amount, Vector2.zero, centerOnScreen: true);
        }

        private void OnInvPointerLeave(PointerLeaveEvent evt)
        {
            if (!invDragActive)
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
