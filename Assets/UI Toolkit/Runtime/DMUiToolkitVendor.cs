using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using Project.Storage;
using Project.Vendor;
using Project.World.Clock;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DefaultExecutionOrder(-365)]
    [DisallowMultipleComponent]
    public class DMUiToolkitVendor : MonoBehaviour
    {
        private enum FocusSide
        {
            None,
            Vendor,
            Player
        }

        private static DMUiToolkitVendor instance;

        private UIDocument document;
        private VisualElement root;
        private Label titleLabel;
        private Label clockLabel;
        private Label playerAcLabel;
        private Label vendorAcLabel;
        private ScrollView stockGrid;
        private ScrollView invGrid;
        private VisualElement qtyRow;
        private SliderInt qtySlider;
        private Label qtyValue;
        private Label qtyPrice;
        private Label statusLabel;
        private VisualElement ctxRoot;
        private VisualElement ctxPanel;
        private Button ctxAction;
        private Button closeButton;
        private bool bound;
        private bool open;
        private DMVendorNpc npc;
        private InventorySystem inventory;
        private InventoryItemActions itemActions;
        private FocusSide focusSide;
        private int focusIndex = -1;
        private float holdAStarted = -1f;
        private bool ctxOpen;
        private Vector2 ctxAnchorScreen;
        private const float DragThresholdPx = 8f;
        private bool dragActive;
        private FocusSide dragSide;
        private int dragIndex = -1;
        private int capturedPointerId = -1;
        private Vector2 pointerDown;
        private Vector2 lastPanelPos;
        private VisualElement dragCaptureSlot;
        private VisualElement dragGhost;

        private readonly List<VisualElement> stockSlots = new List<VisualElement>();
        private readonly List<VisualElement> invSlots = new List<VisualElement>();
        private readonly Dictionary<ItemData, int> crateOwnedScratch = new Dictionary<ItemData, int>();
        private readonly List<VendorHeldStack> extraOfferScratch = new List<VendorHeldStack>(8);
        private bool stockGridStyled;
        private bool invGridStyled;
        private int lastHeaderPlayerAc = int.MinValue;
        private int lastHeaderVendorAc = int.MinValue;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitVendor EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.VendorName,
                DMUiToolkitOverlayDocument.VendorUxml,
                DMUiToolkitOverlayDocument.VendorUss,
                DMUiToolkitOverlayDocument.VendorSort);
            if (doc == null)
                return null;

            DMUiToolkitVendor host = doc.GetComponent<DMUiToolkitVendor>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitVendor>();
            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(DMVendorNpc vendor)
        {
            if (vendor == null || vendor.Profile == null)
                return false;

            DMUiToolkitVendor host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal(vendor);
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
            if (DMUiToolkitWorldMenus.TryHideVendorTradeCard())
                return true;
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
            DMIoClock.OnPainted += RefreshClock;
            DMVendorRuntime.StatesChanged += RefreshAll;
        }

        private void OnDisable()
        {
            DMIoClock.OnPainted -= RefreshClock;
            DMVendorRuntime.StatesChanged -= RefreshAll;
            ApplyOverlaySession(false);
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!open)
                return;

            Gamepad pad = Gamepad.current;
            if (pad != null && focusIndex >= 0)
            {
                if (pad.buttonSouth.isPressed)
                {
                    if (holdAStarted < 0f)
                        holdAStarted = Time.unscaledTime;
                    else if (Time.unscaledTime - holdAStarted >= 0.45f
                        && !ctxOpen
                        && !DMUiToolkitWorldMenus.IsVendorTradeCardOpen)
                        ShowTradeCard();
                }
                else
                {
                    holdAStarted = -1f;
                }

                if (pad.buttonEast.wasPressedThisFrame)
                {
                    if (ctxOpen)
                        HideContext();
                    else
                        HideInternal();
                }
            }
        }

        private void BindTree()
        {
            if (bound)
                return;
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("vendor-root");
            titleLabel = tree.Q<Label>("vendor-title");
            clockLabel = tree.Q<Label>("vendor-clock");
            playerAcLabel = tree.Q<Label>("vendor-player-ac");
            vendorAcLabel = tree.Q<Label>("vendor-npc-ac");
            stockGrid = tree.Q<ScrollView>("vendor-stock-grid");
            invGrid = tree.Q<ScrollView>("vendor-inv-grid");
            qtyRow = tree.Q<VisualElement>("vendor-qty-row");
            qtySlider = tree.Q<SliderInt>("vendor-qty");
            qtyValue = tree.Q<Label>("vendor-qty-value");
            qtyPrice = tree.Q<Label>("vendor-qty-price");
            statusLabel = tree.Q<Label>("vendor-status");
            ctxRoot = tree.Q<VisualElement>("vendor-ctx-root");
            ctxPanel = tree.Q<VisualElement>("vendor-ctx-panel");
            ctxAction = tree.Q<Button>("vendor-ctx-action");
            closeButton = tree.Q<Button>("vendor-close");

            if (closeButton != null)
                closeButton.clicked += HideInternal;
            if (qtySlider != null)
                qtySlider.RegisterValueChangedCallback(OnQtyChanged);
            if (ctxAction != null)
                ctxAction.clicked += () => ConfirmFocused(1);
            if (root != null)
                root.RegisterCallback<PointerDownEvent>(OnRootPointerDown);

            bound = root != null;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            HideContext();
        }

        private void ShowInternal(DMVendorNpc vendor)
        {
            BindTree();
            npc = vendor;
            inventory = FindAnyObjectByType<InventorySystem>();
            itemActions = inventory != null
                ? inventory.GetComponent<InventoryItemActions>()
                : FindAnyObjectByType<InventoryItemActions>();
            open = true;
            focusSide = FocusSide.None;
            focusIndex = -1;
            DMVendorRuntime.GetOrCreate(vendor.Profile);
            if (titleLabel != null)
                titleLabel.text = vendor.DisplayName.ToUpperInvariant();
            RefreshClock();
            RefreshAll();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            ApplyOverlaySession(true);
        }

        private void HideInternal()
        {
            open = false;
            CancelDrag();
            HideContext();
            DMUiToolkitWorldMenus.TryHideVendorTradeCard();
            DMUiToolkitWorldMenus.HideItemTooltip();
            DMUiToolkitOverlayDocument.SetShown(root, false);
            ApplyOverlaySession(false);
        }

        private static void ApplyOverlaySession(bool overlayOpen)
        {
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonVendorShop, overlayOpen);
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

        private void RefreshClock()
        {
            if (clockLabel != null)
                clockLabel.text = DMIoClock.DisplayText;
        }

        private bool refreshQueued;

        private void RefreshAll()
        {
            if (!open || npc == null || npc.Profile == null || refreshQueued)
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
            if (!open || npc == null || npc.Profile == null)
                return;

            RefreshHeader();
            RebuildVendorSlots();
            RebuildPlayerSlots();
            RefreshQtyRow();
        }

        private void RefreshHeader()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            int playerAc = Mathf.RoundToInt(roster != null ? roster.AetherCredits : 0f);
            if (playerAcLabel != null && playerAc != lastHeaderPlayerAc)
            {
                lastHeaderPlayerAc = playerAc;
                playerAcLabel.text = "Your AC " + playerAc;
            }

            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(npc.Profile);
            int purse = state != null ? state.CurrentAc : 0;
            if (vendorAcLabel != null && purse != lastHeaderVendorAc)
            {
                lastHeaderVendorAc = purse;
                vendorAcLabel.text = "Vendor AC " + purse;
                vendorAcLabel.style.color = purse <= 0
                    ? DarkMatterGenesisUiPalette.DeepMagenta
                    : DarkMatterGenesisUiPalette.Gold;
            }
        }

        private void RebuildVendorSlots()
        {
            if (stockGrid == null || npc == null)
                return;

            DMVendorProfile profile = npc.Profile;
            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            DMVendorListing[] listings = profile != null && profile.catalog != null
                ? profile.catalog.listings
                : null;
            int catalog = listings != null ? listings.Length : 0;
            extraOfferScratch.Clear();
            state?.CollectExtraOffers(profile != null ? profile.catalog : null, extraOfferScratch);

            int visible = 0;
            for (int i = 0; i < catalog; i++)
            {
                DMVendorListing listing = listings[i];
                bool placeholder = listing != null && listing.armorPlaceholder;
                int stock = listing != null && !placeholder && state != null ? state.GetStock(i) : 0;
                if (placeholder || stock > 0)
                    visible++;
            }

            visible += extraOfferScratch.Count;
            EnsureSlots(stockGrid.contentContainer, stockSlots, visible, FocusSide.Vendor, ref stockGridStyled);

            int slotIndex = 0;
            for (int i = 0; i < catalog && slotIndex < visible; i++)
            {
                DMVendorListing listing = listings[i];
                bool placeholder = listing != null && listing.armorPlaceholder;
                ItemData item = listing != null ? listing.item : null;
                int stock = listing != null && !placeholder && state != null ? state.GetStock(i) : 0;
                if (!placeholder && stock <= 0)
                    continue;

                SlotVisual visual = ResolveSlotVisual(stockSlots[slotIndex]);
                if (visual != null)
                    visual.OfferIndex = i;
                bool blocked = placeholder || (item != null && !item.ResolveCanBuy());
                PaintSlot(
                    stockSlots[slotIndex],
                    item,
                    stock,
                    blocked,
                    focusSide == FocusSide.Vendor && focusIndex == slotIndex,
                    item != null || placeholder);
                if (placeholder && visual != null && visual.Amount != null)
                {
                    string label = listing.ResolveDisplayName();
                    if (visual.PaintedAmountText != label)
                    {
                        visual.PaintedAmountText = label;
                        visual.Amount.text = label;
                    }
                }

                slotIndex++;
            }

            for (int e = 0; e < extraOfferScratch.Count && slotIndex < visible; e++)
            {
                VendorHeldStack extra = extraOfferScratch[e];
                ItemData item = extra != null ? extra.item : null;
                int stock = extra != null ? extra.amount : 0;
                SlotVisual visual = ResolveSlotVisual(stockSlots[slotIndex]);
                if (visual != null)
                    visual.OfferIndex = catalog + e;
                PaintSlot(
                    stockSlots[slotIndex],
                    item,
                    stock,
                    item != null && (stock <= 0 || !item.ResolveCanBuy()),
                    focusSide == FocusSide.Vendor && focusIndex == slotIndex,
                    item != null);
                slotIndex++;
            }

            if (focusSide == FocusSide.Vendor && focusIndex >= slotIndex)
                focusIndex = -1;
        }

        private void RebuildPlayerSlots()
        {
            if (invGrid == null || inventory == null)
                return;

            int unlocked = Mathf.Max(0, inventory.unlockedMainSlots);
            EnsureSlots(invGrid.contentContainer, invSlots, unlocked, FocusSide.Player, ref invGridStyled);

            bool needOwned = false;
            for (int i = 0; i < unlocked && !needOwned; i++)
            {
                InventorySystem.InventorySlot probe = i < inventory.slots.Count ? inventory.slots[i] : null;
                if (probe != null && !probe.IsEmpty && probe.item != null && probe.item.cannotSellLastCopy)
                    needOwned = true;
            }

            if (needOwned)
                DMStorageCrateRuntime.FillItemCounts(crateOwnedScratch);
            else
                crateOwnedScratch.Clear();

            for (int i = 0; i < unlocked; i++)
            {
                InventorySystem.InventorySlot slot = i < inventory.slots.Count ? inventory.slots[i] : null;
                ItemData item = slot != null && !slot.IsEmpty ? slot.item : null;
                int amount = slot != null ? slot.amount : 0;
                int owned = -1;
                if (item != null && item.cannotSellLastCopy)
                {
                    crateOwnedScratch.TryGetValue(item, out int crateOwned);
                    owned = inventory.CountItem(item) + crateOwned;
                }

                bool blocked = item == null
                    || !DMVendorService.CanPlayerSellItem(npc.Profile, item, inventory, owned);
                PaintSlot(invSlots[i], item, amount, item != null && blocked, focusSide == FocusSide.Player && focusIndex == i, false);
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

                int captured = index;
                FocusSide capturedSide = side;
                slot.RegisterCallback<PointerEnterEvent>(_ => HoverSlot(capturedSide, captured));
                slot.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    if (!DMUiToolkitWorldMenus.IsVendorTradeCardOpen)
                        DMUiToolkitWorldMenus.HideItemTooltip();
                });
                slot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointer(evt, capturedSide, captured));
                slot.RegisterCallback<PointerMoveEvent>(OnSlotPointerMove);
                slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
                slot.RegisterCallback<PointerCaptureOutEvent>(OnSlotPointerCaptureOut);
                slot.userData = new SlotVisual
                {
                    Root = slot,
                    Icon = icon,
                    Amount = amount,
                    Block = block,
                    Side = capturedSide,
                    Index = captured
                };
                host.Add(slot);
                slots.Add(slot);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].userData is SlotVisual visual)
                {
                    visual.Side = side;
                    visual.Index = i;
                }

                DMUiToolkitOverlayDocument.SetShown(slots[i], i < count);
            }
        }

        private static SlotVisual ResolveSlotVisual(VisualElement slot)
        {
            return slot != null ? slot.userData as SlotVisual : null;
        }

        private static void PaintSlot(
            VisualElement slot,
            ItemData item,
            int amount,
            bool blocked,
            bool focused,
            bool showStockAlways)
        {
            if (slot == null)
                return;

            SlotVisual visual = ResolveSlotVisual(slot);
            string amountText = item != null && (showStockAlways || amount > 1) ? amount.ToString() : string.Empty;
            bool showBlock = blocked && item != null;
            if (visual != null
                && visual.PaintedItem == item
                && visual.PaintedAmount == amount
                && visual.PaintedBlocked == showBlock
                && visual.PaintedFocus == focused
                && visual.PaintedAmountText == amountText)
                return;

            VisualElement icon = visual != null ? visual.Icon : slot.Q(className: "dmg-vendor-icon");
            Label amountLabel = visual != null ? visual.Amount : slot.Q<Label>(className: "dmg-vendor-amount");
            VisualElement block = visual != null ? visual.Block : slot.Q(className: "dmg-vendor-block");
            if (visual == null || visual.PaintedItem != item)
            {
                if (item != null)
                    DMUiToolkitStyle.TrySetItemIcon(icon, item, ScaleMode.ScaleToFit);
                else
                    DMUiToolkitStyle.ClearBackgroundImage(icon);
            }

            if (amountLabel != null && (visual == null || visual.PaintedAmountText != amountText))
                amountLabel.text = amountText;

            DMUiToolkitOverlayDocument.SetShown(block, showBlock);
            slot.EnableInClassList("dmg-vendor-slot--focus", focused);
            if (visual != null)
            {
                visual.PaintedItem = item;
                visual.PaintedAmount = amount;
                visual.PaintedBlocked = showBlock;
                visual.PaintedFocus = focused;
                visual.PaintedAmountText = amountText;
            }
        }

        private void OnSlotPointer(PointerDownEvent evt, FocusSide side, int index)
        {
            evt.StopPropagation();
            focusSide = side;
            focusIndex = index;
            HideContext();

            if (evt.button == 1)
            {
                CancelDrag();
                RefreshAll();
                ShowSlotContext(evt.position);
                return;
            }

            if (evt.button != 0)
                return;

            if (side != FocusSide.Player || ResolveItem(side, index) == null)
                return;

            dragCaptureSlot = evt.currentTarget as VisualElement;
            dragSide = side;
            dragIndex = index;
            dragActive = false;
            capturedPointerId = evt.pointerId;
            pointerDown = evt.position;
            lastPanelPos = pointerDown;
            dragCaptureSlot?.CapturePointer(evt.pointerId);
        }

        private void HoverSlot(FocusSide side, int index)
        {
            if (DMUiToolkitWorldMenus.IsVendorTradeCardOpen)
                return;
            ItemData item = ResolveItem(side, index);
            int amount = ResolveAmount(side, index);
            if (item == null)
                return;
            Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            DMUiToolkitWorldMenus.TryShowItemTooltip(item, amount, pointer);
        }

        private void FocusSlot(FocusSide side, int index)
        {
            focusSide = side;
            focusIndex = index;
            HideContext();
            RefreshAll();
        }

        private bool IsFocusedBlocked()
        {
            if (focusSide == FocusSide.Vendor)
            {
                int offerIndex = VendorOfferIndex(focusIndex);
                ItemData offer = DMVendorService.ResolveOffer(npc.Profile, offerIndex, out DMVendorListing listing, out _);
                if (listing != null && listing.armorPlaceholder)
                    return true;
                return offer == null
                    || DMVendorService.GetOfferStock(npc.Profile, offerIndex) <= 0
                    || !offer.ResolveCanBuy();
            }

            if (focusSide == FocusSide.Player)
            {
                ItemData item = ResolveItem(FocusSide.Player, focusIndex);
                return item == null
                    || !DMVendorService.CanPlayerSellItem(npc.Profile, item, inventory);
            }

            return true;
        }

        private void RefreshQtyRow()
        {
            if (qtyRow != null)
                DMUiToolkitOverlayDocument.SetShown(qtyRow, false);
        }

        private void OnQtyChanged(ChangeEvent<int> evt)
        {
            RefreshQtyLabels();
        }

        private void RefreshQtyLabels()
        {
            int qty = qtySlider != null ? Mathf.Max(1, qtySlider.value) : 1;
            if (qtyValue != null)
                qtyValue.text = qty.ToString();
            if (qtyPrice != null)
                qtyPrice.text = focusSide == FocusSide.Vendor
                    ? "Buy " + ResolveFocusedBuy(qty) + " AC"
                    : "Sell " + ResolveFocusedSell(qty) + " AC";
        }

        private int ResolveFocusedMaxQty()
        {
            if (focusSide == FocusSide.Vendor)
                return Mathf.Max(1, DMVendorService.GetOfferStock(npc.Profile, VendorOfferIndex(focusIndex)));

            ItemData item = ResolveItem(FocusSide.Player, focusIndex);
            int amount = ResolveAmount(FocusSide.Player, focusIndex);
            return Mathf.Max(1, amount);
        }

        private int ResolveFocusedAffordableQty()
        {
            if (focusSide == FocusSide.Vendor)
            {
                int offerIndex = VendorOfferIndex(focusIndex);
                ItemData item = DMVendorService.ResolveOffer(npc.Profile, offerIndex, out DMVendorListing listing, out _);
                int stock = DMVendorService.GetOfferStock(npc.Profile, offerIndex);
                return DMVendorService.MaxAffordableBuyQty(npc.Profile, listing, item, stock);
            }

            ItemData sellItem = ResolveItem(FocusSide.Player, focusIndex);
            int amount = ResolveAmount(FocusSide.Player, focusIndex);
            return DMVendorService.MaxAffordableSellQty(npc.Profile, sellItem, amount);
        }

        private int ResolveFocusedBuy(int qty)
        {
            ItemData item = DMVendorService.ResolveOffer(npc.Profile, VendorOfferIndex(focusIndex), out DMVendorListing listing, out _);
            return DMVendorService.ResolveListingBuy(npc.Profile, listing, item, qty);
        }

        private int ResolveFocusedSell(int qty)
        {
            return DMVendorService.ResolveListingSell(npc.Profile, ResolveItem(FocusSide.Player, focusIndex), qty);
        }

        private int VendorOfferIndex(int visualIndex)
        {
            if (visualIndex < 0 || visualIndex >= stockSlots.Count)
                return -1;
            SlotVisual visual = ResolveSlotVisual(stockSlots[visualIndex]);
            return visual != null ? visual.OfferIndex : -1;
        }

        private ItemData ResolveItem(FocusSide side, int index)
        {
            if (side == FocusSide.Vendor)
                return DMVendorService.ResolveOffer(npc != null ? npc.Profile : null, VendorOfferIndex(index), out _, out _);

            if (inventory == null || index < 0 || index >= inventory.slots.Count)
                return null;
            InventorySystem.InventorySlot slot = inventory.slots[index];
            return slot != null && !slot.IsEmpty ? slot.item : null;
        }

        private int ResolveAmount(FocusSide side, int index)
        {
            if (side == FocusSide.Vendor)
                return DMVendorService.GetOfferStock(npc != null ? npc.Profile : null, VendorOfferIndex(index));

            if (inventory == null || index < 0 || index >= inventory.slots.Count)
                return 0;
            return inventory.slots[index].amount;
        }

        private void ShowTradeCard()
        {
            HideContext();
            if (IsFocusedBlocked())
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            ItemData item = ResolveItem(focusSide, focusIndex);
            int amount = ResolveAmount(focusSide, focusIndex);
            if (item == null)
                return;

            Vector2 screen = ctxAnchorScreen;
            if (screen.sqrMagnitude < 4f && Mouse.current != null)
                screen = Mouse.current.position.ReadValue();
            int max = ResolveFocusedMaxQty();
            int afford = ResolveFocusedAffordableQty();
            int startQty = afford > 0 ? Mathf.Clamp(afford, 1, max) : 1;
            DMUiToolkitWorldMenus.TryShowVendorTradeCard(
                item,
                amount,
                screen,
                new DMVendorTradeCard
                {
                    ActionLabel = focusSide == FocusSide.Vendor ? "Buy" : "Sell",
                    MinQty = 1,
                    MaxQty = max,
                    DefaultQty = startQty,
                    FormatPrice = qty => focusSide == FocusSide.Vendor
                        ? "Buy " + ResolveFocusedBuy(qty) + " AC"
                        : "Sell " + ResolveFocusedSell(qty) + " AC",
                    Confirm = ConfirmFocused
                });
        }

        private void ShowContext()
        {
            ShowSlotContext(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        private void ShowSlotContext(Vector2 panelPosition)
        {
            if (ctxPanel == null)
                return;

            ItemData item = ResolveItem(focusSide, focusIndex);
            if (item == null)
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            HideContext();
            DMUiToolkitWorldMenus.TryHideVendorTradeCard();
            RememberContextAnchor(panelPosition);
            ctxPanel.Clear();

            if (focusSide == FocusSide.Player)
            {
                AddContextButton("Split", itemActions != null && itemActions.CanSplit(focusIndex),
                    () =>
                    {
                        if (itemActions == null || !itemActions.TrySplit(focusIndex))
                            GameAudioManager.Instance?.PlayUiDeny();
                        RefreshAll();
                    });
                AddContextButton("Drop", itemActions != null && itemActions.CanDrop(focusIndex),
                    () =>
                    {
                        if (itemActions == null || !itemActions.TryDrop(focusIndex))
                            GameAudioManager.Instance?.PlayUiDeny();
                        RefreshAll();
                    });
                AddContextButton("Combine", itemActions != null && itemActions.CanCombine(focusIndex),
                    () =>
                    {
                        if (itemActions == null || !itemActions.TryCombine(focusIndex))
                            GameAudioManager.Instance?.PlayUiDeny();
                        RefreshAll();
                    });
            }

            bool tradeReady = !IsFocusedBlocked();
            AddContextButton(focusSide == FocusSide.Vendor ? "Buy" : "Sell", tradeReady, ShowTradeCard);

            if (ctxPanel.childCount == 0)
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            ctxOpen = true;
            if (ctxRoot != null)
            {
                ctxRoot.pickingMode = PickingMode.Position;
                ctxRoot.BringToFront();
            }

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
                HideContext();
                action?.Invoke();
            };
            ctxPanel.Add(button);
        }

        private void RememberContextAnchor(Vector2 panelPosition)
        {
            if (ctxPanel != null && ctxPanel.panel != null)
                ctxAnchorScreen = DMUiToolkitOverlayDocument.PanelPositionToScreen(ctxPanel.panel, panelPosition);
            else if (Mouse.current != null)
                ctxAnchorScreen = Mouse.current.position.ReadValue();
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
            if (ctxRoot != null)
                ctxRoot.pickingMode = PickingMode.Ignore;
            DMUiToolkitOverlayDocument.SetShown(ctxRoot, false);
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (ctxOpen && ctxPanel != null && !ctxPanel.worldBound.Contains(evt.position))
                HideContext();
        }

        private void ConfirmFocused(int qty)
        {
            HideContext();
            if (npc == null || inventory == null || IsFocusedBlocked())
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            qty = Mathf.Max(1, qty);
            bool ok;
            string message;
            if (focusSide == FocusSide.Vendor)
                ok = DMVendorService.TryBuy(npc.Profile, VendorOfferIndex(focusIndex), qty, inventory, out message);
            else
                ok = DMVendorService.TrySellFromSlot(npc.Profile, focusIndex, qty, inventory, out message);

            if (!ok)
            {
                GameAudioManager.Instance?.PlayUiDeny();
                if (message == DMVendorService.NotEnoughCreditMessage)
                {
                    if (statusLabel != null)
                        statusLabel.text = DMVendorService.NotEnoughCreditMessage;
                    DMUiToolkitHud.ShowPopup(DMVendorService.NotEnoughCreditMessage);
                    return;
                }

                if (statusLabel != null)
                    statusLabel.text = message;
                DMUiToolkitWorldMenus.TryHideVendorTradeCard();
                return;
            }

            DMUiToolkitWorldMenus.TryHideVendorTradeCard();
            if (statusLabel != null)
                statusLabel.text = message;
            GameAudioManager.Instance?.PlayInventoryItemClick();
            DMUiToolkitHud.ShowPopup(message);
            RefreshAll();
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
            if (dragSide != FocusSide.Player)
                return;

            ItemData item = ResolveItem(dragSide, dragIndex);
            if (item == null)
                return;

            dragActive = true;
            DMUiToolkitWorldMenus.HideItemTooltip();
            if (invGrid != null)
                invGrid.pickingMode = PickingMode.Ignore;
            ClearDragGhost();

            dragGhost = new VisualElement { name = "dmg-vendor-drag-ghost", pickingMode = PickingMode.Ignore };
            dragGhost.style.position = Position.Absolute;
            dragGhost.style.width = 64f;
            dragGhost.style.height = 64f;
            dragGhost.style.opacity = 0.75f;
            DMUiToolkitStyle.TrySetItemIcon(dragGhost, item);
            root?.Add(dragGhost);
            dragGhost.BringToFront();
            PositionDragGhost(panelPos);

            SlotVisual visual = dragCaptureSlot != null ? dragCaptureSlot.userData as SlotVisual : null;
            if (visual != null && visual.Icon != null)
                visual.Icon.style.opacity = 0.35f;
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

            for (int i = 0; i < invSlots.Count; i++)
            {
                SlotVisual visual = ResolveSlotVisual(invSlots[i]);
                if (visual != null && visual.Icon != null)
                    visual.Icon.style.opacity = 1f;
            }
        }

        private void ReleasePointer()
        {
            VisualElement slot = dragCaptureSlot;
            int id = capturedPointerId;
            dragCaptureSlot = null;
            capturedPointerId = -1;
            dragActive = false;
            if (invGrid != null)
                invGrid.pickingMode = PickingMode.Position;

            if (slot != null && id >= 0 && slot.HasPointerCapture(id))
                slot.ReleasePointer(id);
        }

        private void CancelDrag()
        {
            ClearDragGhost();
            ReleasePointer();
            dragIndex = -1;
        }

        private void CompleteDrag(Vector2 panelPos)
        {
            int sourceIndex = dragIndex;
            ClearDragGhost();
            dragIndex = -1;
            if (sourceIndex < 0 || inventory == null)
                return;

            if (!TryFindPlayerSlotAt(panelPos, out int destIndex) || destIndex == sourceIndex)
                return;

            ItemData source = ResolveItem(FocusSide.Player, sourceIndex);
            ItemData dest = ResolveItem(FocusSide.Player, destIndex);
            if (source == null || dest == null || source != dest)
            {
                GameAudioManager.Instance?.PlayUiDeny();
                return;
            }

            inventory.MoveOrMergeSlots(sourceIndex, destIndex);
            GameAudioManager.Instance?.PlayInventoryItemClick();
            RefreshAll();
        }

        private bool TryFindPlayerSlotAt(Vector2 panelPos, out int index)
        {
            index = -1;
            for (int i = 0; i < invSlots.Count; i++)
            {
                VisualElement slot = invSlots[i];
                if (slot == null || slot.resolvedStyle.display == DisplayStyle.None)
                    continue;
                if (slot.worldBound.Contains(panelPos))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private sealed class SlotVisual
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Amount;
            public VisualElement Block;
            public FocusSide Side;
            public int Index;
            public int OfferIndex;
            public ItemData PaintedItem;
            public int PaintedAmount = int.MinValue;
            public bool PaintedBlocked;
            public bool PaintedFocus;
            public string PaintedAmountText;
        }
    }
}
