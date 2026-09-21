using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Storage;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DefaultExecutionOrder(-365)]
    [DisallowMultipleComponent]
    public class DMUiToolkitCrate : MonoBehaviour
    {
        private enum FocusSide
        {
            Player,
            Crate
        }

        private static DMUiToolkitCrate instance;

        private UIDocument document;
        private VisualElement root;
        private ScrollView playerGrid;
        private ScrollView crateGrid;
        private Label statusLabel;
        private Button closeButton;
        private VisualElement ctxRoot;
        private VisualElement ctxPanel;
        private bool bound;
        private bool open;
        private bool ctxOpen;
        private DMStorageCrate crate;
        private InventorySystem inventory;
        private InventoryItemActions itemActions;
        private FocusSide ctxSide;
        private int ctxIndex = -1;

        private const float DragThresholdPx = 8f;
        private VisualElement dragCaptureSlot;
        private bool dragActive;
        private FocusSide dragSide;
        private int dragIndex = -1;
        private int capturedPointerId = -1;
        private Vector2 pointerDown;
        private Vector2 lastPanelPos;
        private VisualElement dragGhost;

        private readonly List<VisualElement> playerSlots = new List<VisualElement>();
        private readonly List<VisualElement> crateSlots = new List<VisualElement>();

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitCrate EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.CrateName,
                DMUiToolkitOverlayDocument.CrateUxml,
                DMUiToolkitOverlayDocument.CrateUss,
                DMUiToolkitOverlayDocument.CrateSort);
            if (doc == null)
                return null;

            DMUiToolkitCrate host = doc.GetComponent<DMUiToolkitCrate>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitCrate>();
            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(DMStorageCrate storageCrate)
        {
            if (storageCrate == null)
                return false;

            DMUiToolkitCrate host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal(storageCrate);
            return true;
        }

        public static bool TryHide()
        {
            if (instance == null || !instance.open)
                return false;
            instance.HideInternal();
            return true;
        }

        public static bool TryHandleBack()
        {
            if (instance == null || !instance.open)
                return false;
            if (instance.ctxOpen)
            {
                instance.HideContext();
                return true;
            }

            instance.HideInternal();
            return true;
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            DMStorageCrateRuntime.StatesChanged += RefreshAll;
        }

        private void OnDisable()
        {
            DMStorageCrateRuntime.StatesChanged -= RefreshAll;
            ApplyOverlaySession(false);
            if (instance == this)
                instance = null;
        }

        private void BindTree()
        {
            if (bound || document == null)
                return;

            root = document.rootVisualElement;
            if (root == null)
                return;

            playerGrid = root.Q<ScrollView>("crate-player-grid");
            crateGrid = root.Q<ScrollView>("crate-stash-grid");
            statusLabel = root.Q<Label>("crate-status");
            closeButton = root.Q<Button>("crate-close");
            ctxRoot = root.Q("crate-ctx-root");
            ctxPanel = root.Q("crate-ctx-panel");
            if (closeButton != null)
                closeButton.clicked += () => TryHide();
            VisualElement veil = root.Q("crate-veil");
            if (veil != null)
            {
                veil.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (dragActive)
                        return;
                    TryHide();
                });
            }
            if (root != null)
                root.RegisterCallback<PointerDownEvent>(OnRootPointerDown);
            bound = true;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            HideContext();
        }

        private void ShowInternal(DMStorageCrate storageCrate)
        {
            BindTree();
            crate = storageCrate;
            inventory = Object.FindAnyObjectByType<InventorySystem>();
            itemActions = inventory != null
                ? inventory.GetComponent<InventoryItemActions>()
                : Object.FindAnyObjectByType<InventoryItemActions>();
            open = true;
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            ApplyOverlaySession(true);
            RefreshAll();
        }

        private void HideInternal()
        {
            if (crate != null)
                crate.NotifyClosed();
            open = false;
            crate = null;
            CancelDrag();
            HideContext();
            DMUiToolkitWorldMenus.HideItemTooltip();
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            if (statusLabel != null)
                statusLabel.text = string.Empty;
            ApplyOverlaySession(false);
        }

        private static void ApplyOverlaySession(bool overlayOpen)
        {
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonStorageCrate, overlayOpen);
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null)
            {
                player.SetGameplayPaused(overlayOpen);
                if (overlayOpen)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;
                }

                player.ApplyCursorState();
            }
        }

        private bool refreshQueued;
        private bool playerGridStyled;
        private bool crateGridStyled;

        private void RefreshAll()
        {
            if (!open || crate == null || refreshQueued)
                return;

            refreshQueued = true;
            if (root != null)
                root.schedule.Execute(FlushRefresh).ExecuteLater(0);
            else
                FlushRefresh();
        }

        private void FlushRefresh()
        {
            refreshQueued = false;
            if (!open || crate == null)
                return;

            RebuildPlayerSlots();
            RebuildCrateSlots();
        }

        private void RebuildPlayerSlots()
        {
            if (playerGrid == null || inventory == null)
                return;

            int unlocked = Mathf.Max(0, inventory.unlockedMainSlots);
            EnsureSlots(playerGrid.contentContainer, playerSlots, unlocked, FocusSide.Player, ref playerGridStyled);
            for (int i = 0; i < unlocked; i++)
            {
                InventorySystem.InventorySlot slot = i < inventory.slots.Count ? inventory.slots[i] : null;
                ItemData item = slot != null && !slot.IsEmpty ? slot.item : null;
                int amount = slot != null ? slot.amount : 0;
                bool blocked = item != null && !CanStore(item);
                PaintSlot(playerSlots[i], item, amount, blocked);
            }
        }

        private void RebuildCrateSlots()
        {
            if (crateGrid == null)
                return;

            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            int count = state != null ? state.Slots.Count : crate.SlotCount;
            EnsureSlots(crateGrid.contentContainer, crateSlots, count, FocusSide.Crate, ref crateGridStyled);
            for (int i = 0; i < count; i++)
            {
                CrateSlot slot = state != null && i < state.Slots.Count ? state.Slots[i] : null;
                ItemData item = slot != null && !slot.IsEmpty ? slot.item : null;
                int amount = slot != null ? slot.amount : 0;
                PaintSlot(crateSlots[i], item, amount, false);
            }
        }

        private void EnsureSlots(
            VisualElement host,
            List<VisualElement> slots,
            int count,
            FocusSide side,
            ref bool styled)
        {
            if (host == null)
                return;

            if (!styled)
            {
                host.style.flexDirection = FlexDirection.Row;
                host.style.flexWrap = Wrap.Wrap;
                host.style.paddingTop = 12;
                host.style.paddingLeft = 8;
                host.style.paddingRight = 8;
                host.style.paddingBottom = 8;
                styled = true;
            }

            while (slots.Count < count)
            {
                int index = slots.Count;
                VisualElement slot = new VisualElement();
                slot.AddToClassList("dmg-vendor-slot");
                slot.style.position = Position.Relative;
                slot.pickingMode = PickingMode.Position;

                VisualElement icon = new VisualElement();
                icon.AddToClassList("dmg-vendor-icon");
                icon.pickingMode = PickingMode.Ignore;
                slot.Add(icon);

                Label amount = new Label();
                amount.AddToClassList("dmg-vendor-amount");
                amount.pickingMode = PickingMode.Ignore;
                slot.Add(amount);

                VisualElement block = new VisualElement();
                block.AddToClassList("dmg-vendor-block");
                block.pickingMode = PickingMode.Ignore;
                slot.Add(block);

                slot.userData = new SlotKey
                {
                    Side = side,
                    Index = index,
                    Icon = icon,
                    Amount = amount,
                    Block = block
                };
                slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
                slot.RegisterCallback<PointerMoveEvent>(OnSlotPointerMove);
                slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
                slot.RegisterCallback<PointerCaptureOutEvent>(OnSlotPointerCaptureOut);
                host.Add(slot);
                slots.Add(slot);
            }

            for (int i = 0; i < slots.Count; i++)
                DMUiToolkitOverlayDocument.SetShown(slots[i], i < count);
        }

        private static void PaintSlot(VisualElement slot, ItemData item, int amount, bool blocked)
        {
            if (slot == null)
                return;

            SlotKey key = slot.userData as SlotKey;
            VisualElement icon = key != null ? key.Icon : slot.Q(className: "dmg-vendor-icon");
            Label amountLabel = key != null ? key.Amount : slot.Q<Label>(className: "dmg-vendor-amount");
            VisualElement block = key != null ? key.Block : slot.Q(className: "dmg-vendor-block");
            if (item != null)
                DMUiToolkitStyle.TrySetItemIcon(icon, item, ScaleMode.ScaleToFit);
            else
                DMUiToolkitStyle.ClearBackgroundImage(icon);

            if (amountLabel != null)
                amountLabel.text = item != null && amount > 1 ? amount.ToString() : string.Empty;

            DMUiToolkitOverlayDocument.SetShown(block, blocked);
        }

        private void OnSlotPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not SlotKey key)
                return;

            evt.StopPropagation();
            if (crate == null || inventory == null)
                return;

            if (evt.button == 1)
            {
                CancelDrag();
                ShowContext(key.Side, key.Index, evt.position);
                return;
            }

            if (evt.button != 0)
                return;

            HideContext();
            if (ResolveItem(key.Side, key.Index) == null)
                return;

            dragCaptureSlot = slot;
            dragSide = key.Side;
            dragIndex = key.Index;
            dragActive = false;
            capturedPointerId = evt.pointerId;
            pointerDown = evt.position;
            lastPanelPos = pointerDown;
            slot.CapturePointer(evt.pointerId);
        }

        private void OnSlotPointerMove(PointerMoveEvent evt)
        {
            if (dragIndex < 0 || evt.pointerId != capturedPointerId)
                return;

            lastPanelPos = evt.position;
            if (dragActive)
            {
                PositionDragGhost(lastPanelPos);
                evt.StopPropagation();
                return;
            }

            if ((evt.pressedButtons & 1) == 0)
                return;

            Vector2 delta = lastPanelPos - pointerDown;
            if (delta.sqrMagnitude < DragThresholdPx * DragThresholdPx)
                return;

            BeginDrag(lastPanelPos);
            evt.StopPropagation();
        }

        private void OnSlotPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != capturedPointerId)
                return;

            bool dragging = dragActive;
            Vector2 panelPos = evt.position;
            ReleasePointer();
            if (dragging)
                CompleteDrag(panelPos);
            evt.StopPropagation();
        }

        private void OnSlotPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (dragIndex < 0 || evt.pointerId != capturedPointerId)
                return;

            bool dragging = dragActive;
            Vector2 panelPos = lastPanelPos;
            ReleasePointer();
            if (dragging)
                CompleteDrag(panelPos);
        }

        private void BeginDrag(Vector2 panelPos)
        {
            ItemData item = ResolveItem(dragSide, dragIndex);
            if (item == null)
                return;

            dragActive = true;
            DMUiToolkitWorldMenus.HideItemTooltip();
            SetGridsScrollEnabled(false);
            ClearDragGhost();

            dragGhost = new VisualElement();
            dragGhost.name = "dmg-crate-drag-ghost";
            dragGhost.pickingMode = PickingMode.Ignore;
            dragGhost.style.position = Position.Absolute;
            dragGhost.style.width = 64f;
            dragGhost.style.height = 64f;
            dragGhost.style.opacity = 0.75f;
            DMUiToolkitStyle.TrySetItemIcon(dragGhost, item);
            root?.Add(dragGhost);
            dragGhost.BringToFront();
            PositionDragGhost(panelPos);

            VisualElement source = ResolveSlotElement(dragSide, dragIndex);
            VisualElement icon = source != null ? source.Q(className: "dmg-vendor-icon") : null;
            if (icon != null)
                icon.style.opacity = 0.35f;
        }

        private void PositionDragGhost(Vector2 panelPos)
        {
            if (dragGhost == null)
                return;

            VisualElement parent = dragGhost.parent != null ? dragGhost.parent : root;
            Vector2 local = parent != null ? parent.WorldToLocal(panelPos) : panelPos;
            float width = dragGhost.resolvedStyle.width > 0f ? dragGhost.resolvedStyle.width : 64f;
            float height = dragGhost.resolvedStyle.height > 0f ? dragGhost.resolvedStyle.height : 64f;
            dragGhost.style.left = local.x - width * 0.5f;
            dragGhost.style.top = local.y - height * 0.5f;
        }

        private void ClearDragGhost()
        {
            if (dragGhost != null)
            {
                dragGhost.RemoveFromHierarchy();
                dragGhost = null;
            }

            RestoreSlotIconOpacity(playerSlots);
            RestoreSlotIconOpacity(crateSlots);
        }

        private static void RestoreSlotIconOpacity(List<VisualElement> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                VisualElement icon = slots[i] != null ? slots[i].Q(className: "dmg-vendor-icon") : null;
                if (icon != null)
                    icon.style.opacity = 1f;
            }
        }

        private void ReleasePointer()
        {
            VisualElement slot = dragCaptureSlot;
            int id = capturedPointerId;
            dragCaptureSlot = null;
            capturedPointerId = -1;
            dragActive = false;
            SetGridsScrollEnabled(true);

            if (slot != null && id >= 0 && slot.HasPointerCapture(id))
                slot.ReleasePointer(id);
        }

        private void CancelDrag()
        {
            ClearDragGhost();
            ReleasePointer();
            dragIndex = -1;
        }

        private void SetGridsScrollEnabled(bool enabled)
        {
            PickingMode mode = enabled ? PickingMode.Position : PickingMode.Ignore;
            if (playerGrid != null)
                playerGrid.pickingMode = mode;
            if (crateGrid != null)
                crateGrid.pickingMode = mode;
        }

        private void CompleteDrag(Vector2 panelPos)
        {
            FocusSide sourceSide = dragSide;
            int sourceIndex = dragIndex;
            ClearDragGhost();
            dragIndex = -1;
            if (sourceIndex < 0 || crate == null || inventory == null)
                return;

            if (TryFindSlotAt(panelPos, out FocusSide destSide, out int destIndex))
            {
                DropOnSlot(sourceSide, sourceIndex, destSide, destIndex);
                return;
            }

            if (sourceSide == FocusSide.Player && IsOverGrid(crateGrid, panelPos))
                TransferPlayerToCrate(sourceIndex);
            else if (sourceSide == FocusSide.Crate && IsOverGrid(playerGrid, panelPos))
                TransferCrateToPlayer(sourceIndex);
        }

        private void DropOnSlot(FocusSide sourceSide, int sourceIndex, FocusSide destSide, int destIndex)
        {
            if (sourceSide == destSide && sourceIndex == destIndex)
                return;

            if (sourceSide == destSide)
            {
                if (sourceSide == FocusSide.Player)
                {
                    if (inventory.IsMainSlotUnlocked(destIndex))
                        inventory.MoveOrMergeSlots(sourceIndex, destIndex);
                    RefreshAll();
                    return;
                }

                DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
                if (state != null && !state.MoveOrMerge(sourceIndex, destIndex))
                    GameAudioManager.Instance?.PlayUiDeny();
                RefreshAll();
                return;
            }

            if (sourceSide == FocusSide.Player)
                TransferPlayerToCrateAt(sourceIndex, destIndex);
            else
                TransferCrateToPlayerAt(sourceIndex, destIndex);
        }

        private bool TryFindSlotAt(Vector2 panelPos, out FocusSide side, out int index)
        {
            if (TryFindSlotIn(playerSlots, panelPos, out index))
            {
                side = FocusSide.Player;
                return true;
            }

            if (TryFindSlotIn(crateSlots, panelPos, out index))
            {
                side = FocusSide.Crate;
                return true;
            }

            side = FocusSide.Player;
            index = -1;
            return false;
        }

        private static bool TryFindSlotIn(List<VisualElement> slots, Vector2 panelPos, out int index)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                VisualElement slot = slots[i];
                if (slot == null || slot.resolvedStyle.display == DisplayStyle.None)
                    continue;
                if (slot.worldBound.Contains(panelPos))
                {
                    index = slot.userData is SlotKey key ? key.Index : i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static bool IsOverGrid(ScrollView grid, Vector2 panelPos)
        {
            return grid != null && grid.worldBound.Contains(panelPos);
        }

        private VisualElement ResolveSlotElement(FocusSide side, int index)
        {
            List<VisualElement> slots = side == FocusSide.Player ? playerSlots : crateSlots;
            return index >= 0 && index < slots.Count ? slots[index] : null;
        }

        private void TransferSlot(FocusSide side, int index)
        {
            if (side == FocusSide.Player)
                TransferPlayerToCrate(index);
            else
                TransferCrateToPlayer(index);
        }

        private void ShowContext(FocusSide side, int index, Vector2 panelPosition)
        {
            if (ctxPanel == null || ctxRoot == null)
                return;

            ItemData item = ResolveItem(side, index);
            if (item == null)
                return;

            ctxSide = side;
            ctxIndex = index;
            ctxPanel.Clear();

            bool canSplit = side == FocusSide.Player
                ? itemActions != null && itemActions.CanSplit(index)
                : CanSplitCrate(index);
            bool canDrop = true;
            bool canTransfer = side != FocusSide.Player || CanStore(item);

            AddContextButton("Split", canSplit, SplitFocused);
            AddContextButton("Drop", canDrop, DropFocused);
            AddContextButton("Transfer", canTransfer, () => TransferSlot(ctxSide, ctxIndex));

            if (ctxPanel.childCount == 0)
                return;

            ctxOpen = true;
            ctxRoot.pickingMode = PickingMode.Position;
            ctxRoot.BringToFront();
            DMUiToolkitOverlayDocument.SetShown(ctxRoot, true);
            PlaceContext(panelPosition);
            ctxPanel.schedule.Execute(() =>
            {
                if (!ctxOpen || ctxPanel == null)
                    return;
                PlaceContext(panelPosition);
            }).ExecuteLater(1);
        }

        private void AddContextButton(string label, bool visible, System.Action action)
        {
            if (!visible || ctxPanel == null)
                return;

            Button button = DMUiToolkitOverlayDocument.MakeMenuButton(label, label);
            button.AddToClassList("dmg-vendor-ctx-btn");
            button.clicked += () =>
            {
                action?.Invoke();
                HideContext();
            };
            ctxPanel.Add(button);
        }

        private void PlaceContext(Vector2 panelPosition)
        {
            if (ctxPanel == null)
                return;

            ctxPanel.BringToFront();
            DMUiToolkitOverlayDocument.PositionContextMenuAtPanel(ctxPanel, panelPosition, ctxRoot ?? root);
        }

        private void HideContext()
        {
            ctxOpen = false;
            ctxIndex = -1;
            if (ctxRoot != null)
                ctxRoot.pickingMode = PickingMode.Ignore;
            DMUiToolkitOverlayDocument.SetShown(ctxRoot, false);
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (dragActive)
                return;
            if (ctxOpen && ctxPanel != null && !ctxPanel.worldBound.Contains(evt.position))
                HideContext();
        }

        private ItemData ResolveItem(FocusSide side, int index)
        {
            if (side == FocusSide.Player)
            {
                if (inventory == null || index < 0 || index >= inventory.slots.Count)
                    return null;
                InventorySystem.InventorySlot playerSlot = inventory.slots[index];
                return playerSlot != null && !playerSlot.IsEmpty ? playerSlot.item : null;
            }

            DMStorageCrateState state = crate != null
                ? DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount)
                : null;
            if (state == null || index < 0 || index >= state.Slots.Count)
                return null;
            CrateSlot crateSlot = state.Slots[index];
            return crateSlot != null && !crateSlot.IsEmpty ? crateSlot.item : null;
        }

        private bool CanSplitCrate(int index)
        {
            if (crate == null)
                return false;
            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            return state != null && state.CanSplit(index);
        }

        private void SplitFocused()
        {
            bool ok = ctxSide == FocusSide.Player
                ? itemActions != null && itemActions.TrySplit(ctxIndex)
                : SplitCrate(ctxIndex);
            if (!ok)
                GameAudioManager.Instance?.PlayUiDeny();
            else if (ctxSide == FocusSide.Crate)
                GameAudioManager.Instance?.PlayItemSplit();
            RefreshAll();
        }

        private bool SplitCrate(int index)
        {
            if (crate == null)
                return false;
            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            return state != null && state.SplitAt(index);
        }

        private void DropFocused()
        {
            if (ctxSide == FocusSide.Player)
            {
                bool ok = itemActions != null && itemActions.TryDrop(ctxIndex);
                if (!ok)
                    GameAudioManager.Instance?.PlayUiDeny();
                RefreshAll();
                return;
            }

            DMStorageCrateState state = crate != null
                ? DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount)
                : null;
            if (state == null || ctxIndex < 0 || ctxIndex >= state.Slots.Count)
                return;

            CrateSlot slot = state.Slots[ctxIndex];
            if (slot == null || slot.IsEmpty)
                return;

            ItemData item = slot.item;
            int amount = slot.amount;
            if (inventory == null || !inventory.TrySpawnWorldDrop(item, amount))
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            state.RemoveAt(ctxIndex, amount);
            GameAudioManager.Instance?.PlayItemDrop();
            SetStatus("Dropped " + item.itemName + ".");
            RefreshAll();
        }

        private void TransferPlayerToCrateAt(int playerIndex, int crateIndex)
        {
            if (inventory == null || crate == null || playerIndex < 0 || playerIndex >= inventory.slots.Count)
                return;

            InventorySystem.InventorySlot playerSlot = inventory.slots[playerIndex];
            if (playerSlot == null || playerSlot.IsEmpty || !CanStore(playerSlot.item))
            {
                SetStatus("Cannot store that.");
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            if (state == null || crateIndex < 0 || crateIndex >= state.Slots.Count)
            {
                TransferPlayerToCrate(playerIndex);
                return;
            }

            CrateSlot dest = state.Slots[crateIndex] ?? new CrateSlot();
            state.Slots[crateIndex] = dest;
            ItemData sourceItem = playerSlot.item;
            int sourceAmount = playerSlot.amount;

            if (dest.IsEmpty || dest.item == sourceItem)
            {
                int maxStack = Mathf.Max(1, sourceItem.maxStack);
                int canAdd = dest.IsEmpty ? Mathf.Min(sourceAmount, maxStack) : Mathf.Min(sourceAmount, maxStack - dest.amount);
                if (canAdd <= 0)
                {
                    GameAudioManager.Instance?.PlayUiDeny();
                    return;
                }

                dest.item = sourceItem;
                dest.amount = dest.IsEmpty ? canAdd : dest.amount + canAdd;
                inventory.RemoveItemAt(playerIndex, canAdd);
                DMStorageCrateRuntime.NotifyChanged();
                GameAudioManager.Instance?.PlayInventoryItemClick();
                SetStatus("Stored " + sourceItem.itemName + ".");
                RefreshAll();
                return;
            }

            ItemData swapItem = dest.item;
            int swapAmount = dest.amount;
            dest.item = sourceItem;
            dest.amount = sourceAmount;
            playerSlot.item = swapItem;
            playerSlot.amount = swapAmount;
            inventory.NotifyChanged();
            DMStorageCrateRuntime.NotifyChanged();
            GameAudioManager.Instance?.PlayInventoryItemClick();
            SetStatus("Swapped " + sourceItem.itemName + ".");
            RefreshAll();
        }

        private void TransferCrateToPlayerAt(int crateIndex, int playerIndex)
        {
            if (inventory == null || crate == null)
                return;

            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            if (state == null || crateIndex < 0 || crateIndex >= state.Slots.Count)
                return;

            CrateSlot source = state.Slots[crateIndex];
            if (source == null || source.IsEmpty)
                return;

            if (playerIndex < 0 || playerIndex >= inventory.slots.Count || !inventory.IsMainSlotUnlocked(playerIndex))
            {
                TransferCrateToPlayer(crateIndex);
                return;
            }

            InventorySystem.InventorySlot dest = inventory.slots[playerIndex];
            if (dest == null)
            {
                TransferCrateToPlayer(crateIndex);
                return;
            }

            ItemData sourceItem = source.item;
            int sourceAmount = source.amount;
            if (dest.IsEmpty || dest.item == sourceItem)
            {
                int maxStack = Mathf.Max(1, sourceItem.maxStack);
                int canAdd = dest.IsEmpty ? Mathf.Min(sourceAmount, maxStack) : Mathf.Min(sourceAmount, maxStack - dest.amount);
                if (canAdd <= 0)
                {
                    GameAudioManager.Instance?.PlayUiDeny();
                    return;
                }

                dest.item = sourceItem;
                dest.amount = dest.IsEmpty ? canAdd : dest.amount + canAdd;
                state.RemoveAt(crateIndex, canAdd);
                inventory.NotifyChanged();
                GameAudioManager.Instance?.PlayInventoryItemClick();
                SetStatus("Took " + sourceItem.itemName + ".");
                RefreshAll();
                return;
            }

            ItemData swapItem = dest.item;
            int swapAmount = dest.amount;
            if (!CanStore(swapItem))
            {
                SetStatus("Cannot store that.");
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            dest.item = sourceItem;
            dest.amount = sourceAmount;
            source.item = swapItem;
            source.amount = swapAmount;
            inventory.NotifyChanged();
            DMStorageCrateRuntime.NotifyChanged();
            GameAudioManager.Instance?.PlayInventoryItemClick();
            SetStatus("Swapped " + sourceItem.itemName + ".");
            RefreshAll();
        }

        private void TransferPlayerToCrate(int index)
        {
            if (index < 0 || index >= inventory.slots.Count)
                return;

            InventorySystem.InventorySlot slot = inventory.slots[index];
            if (slot == null || slot.IsEmpty)
                return;

            ItemData item = slot.item;
            int amount = slot.amount;
            if (!CanStore(item))
            {
                SetStatus("Cannot store that.");
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            int added = state.AddItem(item, amount);
            if (added <= 0)
            {
                SetStatus("Crate is full.");
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            inventory.RemoveItemAt(index, added);
            GameAudioManager.Instance?.PlayInventoryItemClick();
            SetStatus("Stored " + item.itemName + ".");
            RefreshAll();
        }

        private void TransferCrateToPlayer(int index)
        {
            DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
            if (state == null || index < 0 || index >= state.Slots.Count)
                return;

            CrateSlot slot = state.Slots[index];
            if (slot == null || slot.IsEmpty)
                return;

            ItemData item = slot.item;
            int amount = slot.amount;
            int added = inventory.AddItemToMainInventory(item, amount);
            if (added <= 0)
            {
                SetStatus("Inventory is full.");
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            state.RemoveAt(index, added);
            GameAudioManager.Instance?.PlayInventoryItemClick();
            SetStatus("Took " + item.itemName + ".");
            RefreshAll();
        }

        private static bool CanStore(ItemData item)
        {
            return item != null
                && item.itemType != ItemType.Quest
                && item.itemType != ItemType.Vehicle
                && item.itemType != ItemType.WorldDeployable;
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.text = text ?? string.Empty;
        }

        private sealed class SlotKey
        {
            public FocusSide Side;
            public int Index;
            public VisualElement Icon;
            public Label Amount;
            public VisualElement Block;
        }
    }
}
