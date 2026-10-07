using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Events;
using Project.Interaction;
using Project.Loot;
using Project.Player;
using Project.Survival;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK per-item loot window (loot plan section 8) for world caches and enemy bags.
    /// One row per entry (stable entry id), per-row Loot, Loot All, full pause while open (D5),
    /// level-lock overlay on locked items (D18), KBM and controller input (D15 for the loot window).
    /// Every close path calls <see cref="IDMLootContainer.NotifyWindowClosed"/> (8.6).
    /// </summary>
    [DefaultExecutionOrder(-365)]
    [DisallowMultipleComponent]
    public class DMUiToolkitLoot : MonoBehaviour
    {
        private const float RangeCheckInterval = 0.25f;
        private const string RowSelectedClass = "dmg-loot-row--selected";

        private sealed class RowRefs
        {
            public VisualElement Row;
            public VisualElement IconHolder;
            public VisualElement Icon;
            public Label Name;
            public Label Amount;
            public int EntryId = -1;
        }

        private static DMUiToolkitLoot instance;
        private static int closedFrame = -1;

        private UIDocument document;
        private VisualElement root;
        private Label titleLabel;
        private Label hintLabel;
        private Label controlsLabel;
        private ScrollView list;
        private Button lootAllButton;
        private Button closeButton;
        private bool bound;
        private bool open;

        private IDMLootContainer container;
        private Object containerObject;
        private bool containerIsUnityObject;
        private int openedFrame = -1;
        private int selectedEntryId = -1;
        private bool fullBannerShown;
        private bool refreshQueued;
        private float nextRangeCheckTime;
        private bool padSouthHeldSinceOpen;
        private int padSubmitBlockedUntilFrame = -1;
        private SurvivalStats survival;

        private readonly List<RowRefs> rows = new List<RowRefs>();

        public static bool IsOpen => instance != null && instance.open;

        /// <summary>True while open and on the frame it closed, so the closing press cannot reopen a container.</summary>
        public static bool IsOpenOrClosingThisFrame => IsOpen || closedFrame == Time.frameCount;

        /// <summary>Gamepad navigation root while open (<see cref="DMUiJournalGamepadNav"/>).</summary>
        public static VisualElement NavigationRoot => IsOpen ? instance.root : null;

        /// <summary>True while the window is open on <paramref name="lootContainer"/>.</summary>
        public static bool IsShowing(IDMLootContainer lootContainer) =>
            lootContainer != null && IsOpen && ReferenceEquals(instance.container, lootContainer);

        public static DMUiToolkitLoot EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.LootName,
                DMUiToolkitOverlayDocument.LootUxml,
                DMUiToolkitOverlayDocument.LootUss,
                DMUiToolkitOverlayDocument.LootSort);
            if (doc == null)
                return null;

            DMUiToolkitLoot host = doc.GetComponent<DMUiToolkitLoot>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitLoot>();
            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(IDMLootContainer lootContainer)
        {
            if (lootContainer == null || lootContainer.IsEmpty)
                return false;

            DMUiToolkitLoot host = EnsureHost();
            if (host == null || !host.bound)
                return false;

            host.ShowInternal(lootContainer);
            return true;
        }

        public static bool TryHide()
        {
            if (!IsOpen)
                return false;

            instance.HideInternal();
            return true;
        }

        /// <summary>Esc / gamepad B: closes the window (one layer).</summary>
        public static bool TryHandleBack() => TryHide();

        private void OnEnable()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
            BindTree();
        }

        private void OnDisable()
        {
            if (open && GameplayInputRecovery.IsTearingDown)
            {
                // Exit Play / quit: drop the session without container callbacks or scene lookups.
                open = false;
                container = null;
                containerObject = null;
                containerIsUnityObject = false;
                GameplayMenuTime.SetPause(GameplayMenuTime.ReasonLootDialog, false);
            }
            else if (open)
            {
                HideInternal();
            }
            if (instance == this)
                instance = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            VisualElement liveRoot = tree.Q<VisualElement>("loot-root");
            if (bound && liveRoot != null && liveRoot == root)
                return;

            // Tree reloaded (or first bind): drop row refs that pointed at the old tree.
            rows.Clear();
            root = liveRoot;
            if (root == null)
            {
                bound = false;
                return;
            }

            titleLabel = root.Q<Label>("loot-title");
            hintLabel = root.Q<Label>("loot-hint");
            controlsLabel = root.Q<Label>("loot-controls");
            list = root.Q<ScrollView>("loot-list");
            lootAllButton = root.Q<Button>("loot-all");
            closeButton = root.Q<Button>("loot-close");

            if (lootAllButton != null)
                lootAllButton.clicked += OnLootAllClicked;
            if (closeButton != null)
                closeButton.clicked += () => HideInternal();

            // Close X is not a Button so first gamepad focus lands on the top row, not on the header.
            VisualElement closeX = root.Q<VisualElement>("loot-close-x");
            closeX?.RegisterCallback<ClickEvent>(_ => HideInternal());

            VisualElement veil = root.Q<VisualElement>("loot-veil");
            veil?.RegisterCallback<PointerDownEvent>(_ => HideInternal());

            bound = list != null;
            DMUiToolkitOverlayDocument.SetShown(root, open);
        }

        private void ShowInternal(IDMLootContainer lootContainer)
        {
            BindTree();
            if (!bound)
                return;

            if (open && !ReferenceEquals(container, lootContainer))
                HideInternal();

            container = lootContainer;
            containerObject = lootContainer as Object;
            containerIsUnityObject = containerObject != null;
            open = true;
            openedFrame = Time.frameCount;
            selectedEntryId = -1;
            fullBannerShown = false;
            nextRangeCheckTime = Time.unscaledTime + RangeCheckInterval;
            padSouthHeldSinceOpen = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
            padSubmitBlockedUntilFrame = -1;
            survival = PlayerLocator.FindOnLivePlayer<SurvivalStats>();

            if (titleLabel != null)
            {
                string displayName = lootContainer.LootDisplayName;
                titleLabel.text = string.IsNullOrWhiteSpace(displayName) ? "Loot" : displayName;
            }

            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            ApplyOverlaySession(true);
            RefreshNow();
            if (list != null)
                list.scrollOffset = Vector2.zero;
            DMUiJournalGamepadNav.NotifyMenuOpened(root);
        }

        private void HideInternal()
        {
            if (!open)
                return;

            open = false;
            closedFrame = Time.frameCount;
            IDMLootContainer closing = ContainerAlive ? container : null;
            container = null;
            containerObject = null;
            containerIsUnityObject = false;
            refreshQueued = false;
            selectedEntryId = -1;
            fullBannerShown = false;
            survival = null;

            DMUiToolkitWorldMenus.HideItemTooltip();
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            ApplyOverlaySession(false);

            // 8.6: lid close, timers and the full-inventory hold hang off this callback.
            closing?.NotifyWindowClosed();
        }

        private bool ContainerAlive =>
            container != null && (!containerIsUnityObject || containerObject != null);

        /// <summary>Full pause (D5), like the storage crate, plus the player loot-dialog input gate.</summary>
        private static void ApplyOverlaySession(bool overlayOpen)
        {
            if (!overlayOpen && GameplayInputRecovery.IsTearingDown)
            {
                GameplayMenuTime.SetPause(GameplayMenuTime.ReasonLootDialog, false);
                return;
            }

            PlayerController player = PlayerLocator.FindPlayerController();
            if (overlayOpen)
            {
                player?.SetLootDialogOpen(true);
                GameplayMenuTime.SetPause(GameplayMenuTime.ReasonLootDialog, true);
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                return;
            }

            player?.SetLootDialogOpen(false);
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonLootDialog, false);
        }

        private void Update()
        {
            if (!open)
                return;

            if (!ContainerAlive || container.IsEmpty)
            {
                HideInternal();
                return;
            }

            if (UiEscapeGate.TryConsumeEscape() || IsPlayerDead())
            {
                HideInternal();
                return;
            }

            if (Time.unscaledTime >= nextRangeCheckTime)
            {
                nextRangeCheckTime = Time.unscaledTime + RangeCheckInterval;
                if (IsBeyondCloseRange())
                {
                    HideInternal();
                    return;
                }
            }

            TickGamepad();
            if (open)
                TickKeyboard();
        }

        private void TickGamepad()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null)
                return;

            // The A press that opened the window must not loot on release.
            if (padSouthHeldSinceOpen && !pad.buttonSouth.isPressed)
            {
                padSouthHeldSinceOpen = false;
                padSubmitBlockedUntilFrame = Time.frameCount + 1;
            }

            if (Time.frameCount != openedFrame && pad.buttonNorth.wasPressedThisFrame)
                LootAll();
        }

        private void TickKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.frameCount == openedFrame || !keyboard.eKey.wasPressedThisFrame)
                return;

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                LootAll();
            else
                LootEntry(ResolveTargetEntryId());
        }

        private bool PadSubmitBlocked =>
            padSouthHeldSinceOpen || Time.frameCount <= padSubmitBlockedUntilFrame;

        private bool IsPlayerDead()
        {
            if (DMUiToolkitDeath.IsOpen)
                return true;
            return survival != null && survival.IsDead;
        }

        private bool IsBeyondCloseRange()
        {
            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return false;

            float range = DMLootChestProfile.ResolveCloseRangeMeters();
            return (playerPosition - container.LootWorldPosition).sqrMagnitude > range * range;
        }

        /// <summary>E target: focused row, then the clicked (selected) row, then the top row.</summary>
        private int ResolveTargetEntryId()
        {
            VisualElement focused = root?.panel?.focusController?.focusedElement as VisualElement;
            RowRefs focusedRow = FindRowFor(focused);
            if (focusedRow != null && focusedRow.EntryId >= 0)
                return focusedRow.EntryId;

            if (selectedEntryId >= 0 && FindEntry(selectedEntryId) != null)
                return selectedEntryId;

            IReadOnlyList<DMLootEntry> entries = container.Entries;
            return entries.Count > 0 ? entries[0].entryId : -1;
        }

        private void LootEntry(int entryId)
        {
            if (!open || !ContainerAlive || entryId < 0)
                return;

            AfterLoot(container.TryLootEntry(entryId));
        }

        private void LootAll()
        {
            if (!open || !ContainerAlive)
                return;

            AfterLoot(container.TryLootAll());
        }

        private void AfterLoot(DMLootGrantResult result)
        {
            if (!open)
                return;

            // Last item taken (row or Loot All): close. Loot All never closes with leftovers (D4).
            if (!ContainerAlive || container.IsEmpty)
            {
                HideInternal();
                return;
            }

            if (result == DMLootGrantResult.Partial || result == DMLootGrantResult.InventoryFull)
                fullBannerShown = true;

            RequestRefresh();
        }

        private void OnLootAllClicked()
        {
            if (PadSubmitBlocked)
                return;
            LootAll();
        }

        private void RequestRefresh()
        {
            if (!open || refreshQueued)
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
            if (open)
                RefreshNow();
        }

        private void RefreshNow()
        {
            if (!open || !ContainerAlive || list == null)
                return;

            IReadOnlyList<DMLootEntry> entries = container.Entries;
            int count = Mathf.Min(entries.Count, DMLootEntryList.MaxEntries);
            EnsureRows(count);
            int playerLevel = Project.Progression.LevelUnlockUtility.CachedPlayerLevel;

            bool selectedStillPresent = false;
            int remainingTotal = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                RowRefs refs = rows[i];
                DMLootEntry entry = i < count ? entries[i] : null;
                if (entry == null || entry.IsEmpty)
                {
                    refs.EntryId = -1;
                    DMUiToolkitOverlayDocument.SetShown(refs.Row, false);
                    continue;
                }

                PaintRow(refs, entry, playerLevel);
                remainingTotal += entry.amount;
                if (entry.entryId == selectedEntryId)
                    selectedStillPresent = true;
            }

            if (!selectedStillPresent)
                selectedEntryId = -1;
            ApplySelectionClasses();

            lootAllButton?.SetEnabled(count > 0);

            if (hintLabel != null)
            {
                hintLabel.text = fullBannerShown ? $"Inventory full - {remainingTotal} left" : string.Empty;
                DMUiToolkitOverlayDocument.SetShown(hintLabel, fullBannerShown);
            }

            if (controlsLabel != null)
            {
                controlsLabel.text = DMInputSchemeRouter.IsGamepadScheme
                    ? "A  Loot     Y  Loot All     B  Close"
                    : "E  Loot     Shift+E  Loot All     Esc  Close";
            }
        }

        private static void PaintRow(RowRefs refs, DMLootEntry entry, int playerLevel)
        {
            refs.EntryId = entry.entryId;
            ItemData item = entry.item;

            if (DMUiToolkitStyle.TrySetItemIcon(refs.Icon, item))
                DMUiToolkitOverlayDocument.SetShown(refs.Icon, true);
            else
            {
                DMUiToolkitStyle.ClearBackgroundImage(refs.Icon);
                DMUiToolkitOverlayDocument.SetShown(refs.Icon, false);
            }

            if (refs.Name != null)
                refs.Name.text = item.itemName;
            if (refs.Amount != null)
                refs.Amount.text = entry.amount > 1 ? $"x{entry.amount}" : string.Empty;

            // D3/D18: lootable at any level; the icon shows the vendor blocked overlay while locked.
            DMUiLevelLockOverlay.Apply(refs.IconHolder, item, playerLevel);
            DMUiToolkitOverlayDocument.SetShown(refs.Row, true);
        }

        private void EnsureRows(int count)
        {
            while (rows.Count < count)
                rows.Add(CreateRow(rows.Count));
        }

        private RowRefs CreateRow(int index)
        {
            RowRefs refs = new RowRefs();

            VisualElement row = new VisualElement { name = "loot-row-" + index };
            row.AddToClassList("dmg-list-row");
            row.AddToClassList("dmg-loot-row");
            row.pickingMode = PickingMode.Position;
            row.focusable = true;
            row.userData = refs;

            VisualElement iconHolder = new VisualElement { name = "loot-row-icon-holder" };
            iconHolder.AddToClassList("dmg-loot-icon-holder");
            iconHolder.pickingMode = PickingMode.Ignore;
            VisualElement icon = new VisualElement { name = "loot-row-icon" };
            icon.AddToClassList("dmg-loot-icon");
            icon.pickingMode = PickingMode.Ignore;
            iconHolder.Add(icon);
            row.Add(iconHolder);

            Label nameLabel = new Label { name = "loot-row-name" };
            nameLabel.AddToClassList("dmg-loot-name");
            nameLabel.pickingMode = PickingMode.Ignore;
            row.Add(nameLabel);

            Label amountLabel = new Label { name = "loot-row-amount" };
            amountLabel.AddToClassList("dmg-loot-amount");
            amountLabel.pickingMode = PickingMode.Ignore;
            row.Add(amountLabel);

            Button lootButton = new Button { name = "loot-row-btn", text = "Loot" };
            lootButton.AddToClassList("dmg-loot-btn");
            lootButton.AddToClassList("dmg-loot-row-btn");
            lootButton.clicked += () =>
            {
                if (!PadSubmitBlocked)
                    LootEntry(refs.EntryId);
            };
            row.Add(lootButton);

            refs.Row = row;
            refs.IconHolder = iconHolder;
            refs.Icon = icon;
            refs.Name = nameLabel;
            refs.Amount = amountLabel;

            row.RegisterCallback<ClickEvent>(OnRowClicked);
            row.RegisterCallback<FocusInEvent>(OnRowFocusIn);
            row.RegisterCallback<PointerEnterEvent>(OnRowPointerEnter);
            row.RegisterCallback<PointerLeaveEvent>(OnRowPointerLeave);

            DMUiToolkitOverlayDocument.SetShown(row, false);
            list.Add(row);
            return refs;
        }

        private void OnRowClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement row || row.userData is not RowRefs refs || refs.EntryId < 0)
                return;

            // Clicks on the per-row Loot button bubble here; the button already handled them.
            if (IsInsideButton(evt.target as VisualElement, row))
                return;

            // clickCount 0 = synthesized submit (gamepad A / Enter via DMUiJournalGamepadNav): loot the row.
            if (evt.clickCount == 0)
            {
                if (!PadSubmitBlocked)
                    LootEntry(refs.EntryId);
                return;
            }

            if (evt.clickCount >= 2)
            {
                LootEntry(refs.EntryId);
                return;
            }

            selectedEntryId = refs.EntryId;
            ApplySelectionClasses();
        }

        private void OnRowFocusIn(FocusInEvent evt)
        {
            if (evt.currentTarget is VisualElement row && row.userData is RowRefs refs && refs.EntryId >= 0)
            {
                selectedEntryId = refs.EntryId;
                ApplySelectionClasses();
            }
        }

        private void OnRowPointerEnter(PointerEnterEvent evt)
        {
            if (!open || evt.currentTarget is not VisualElement row || row.userData is not RowRefs refs)
                return;

            DMLootEntry entry = FindEntry(refs.EntryId);
            if (entry == null || entry.IsEmpty)
            {
                DMUiToolkitWorldMenus.HideItemTooltip();
                return;
            }

            Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            DMUiToolkitWorldMenus.TryShowItemTooltip(entry.item, entry.amount, pointer);
        }

        private static void OnRowPointerLeave(PointerLeaveEvent evt)
        {
            DMUiToolkitWorldMenus.HideItemTooltip();
        }

        private void ApplySelectionClasses()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                RowRefs refs = rows[i];
                refs.Row?.EnableInClassList(RowSelectedClass, refs.EntryId >= 0 && refs.EntryId == selectedEntryId);
            }
        }

        private DMLootEntry FindEntry(int entryId)
        {
            if (!ContainerAlive || entryId < 0)
                return null;

            IReadOnlyList<DMLootEntry> entries = container.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].entryId == entryId)
                    return entries[i];
            }

            return null;
        }

        private RowRefs FindRowFor(VisualElement element)
        {
            while (element != null && element != root)
            {
                if (element.userData is RowRefs refs)
                    return refs;
                element = element.parent;
            }

            return null;
        }

        private static bool IsInsideButton(VisualElement element, VisualElement stopAt)
        {
            while (element != null && element != stopAt)
            {
                if (element is Button)
                    return true;
                element = element.parent;
            }

            return false;
        }
    }
}
