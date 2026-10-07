using System;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Loot;
using Project.Map;
using Project.Progression;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// Dark Matter loot table / grant logic for world caches.
    /// Pair with <see cref="DMItemCollection"/> on a child trigger for open animation + E interaction.
    /// Keeps ScannableTarget + OutlineController on the cache root.
    /// Per-item looting goes through <see cref="IDMLootContainer"/> (stable entry ids) and the shared
    /// <see cref="DMLootGrant"/> path: items only (D6), main grid (D8), no ammo auto-load (D7),
    /// level-gated items granted and shown locked (D3, D22).
    /// </summary>
    [DisallowMultipleComponent]
    public class DmEvents : MonoBehaviour, IDMLootContainer
    {
        public const int MaxLootSlots = 25;

        [Serializable]
        public class LootSlot
        {
            public ItemData item;
            [Min(1)] public int amount = 1;
        }

        [Header("Identity")]
        [SerializeField] private string cacheDisplayName = "IO Ancient Cache";

        [Header("Loot Table (up to 25)")]
        [SerializeField] private LootSlot[] lootSlots = new LootSlot[MaxLootSlots];

        [Header("Scanner")]
        [SerializeField] private bool visibleToScanner = true;
        [SerializeField] private string scanLabel = "IO Ancient Cache";
        [SerializeField] private Color scanColor = DarkMatterGenesisUiPalette.Gold;

        [Header("Lifecycle")]
        [Tooltip("When empty, only stop interaction — keep the opened chest mesh in the world.")]
        [SerializeField] private bool keepVisualWhenEmpty = true;

        private readonly DMLootEntryList remainingLoot = new DMLootEntryList();
        private UIManager uiManager;
        private bool initialized;
        private bool emptied;
        private ScannableTarget scannableTarget;

        public string CacheDisplayName =>
            string.IsNullOrWhiteSpace(cacheDisplayName) ? "IO Ancient Cache" : cacheDisplayName;

        public bool HasRemainingLoot => !remainingLoot.IsEmpty;

        /// <summary>
        /// Set on window close: a loot action this visit hit a full inventory and items remain.
        /// Phase 4 turns this into the 30-minute hold (D4, D19). Read it from <see cref="LootWindowClosed"/>.
        /// </summary>
        public bool FullInventoryHoldRequested { get; private set; }

        /// <summary>Raised after every loot window close (D4 hold, lid close and timers hook here).</summary>
        public event Action<DmEvents> LootWindowClosed;

        // IDMLootContainer
        public string LootDisplayName => CacheDisplayName;
        public IReadOnlyList<DMLootEntry> Entries => remainingLoot.Entries;
        public bool IsEmpty => remainingLoot.IsEmpty;
        public bool HasProtectedEntries => remainingLoot.HasProtectedEntries;
        public Vector3 LootWorldPosition => transform.position;

        private bool authoredVisibleToScanner = true;
        private bool authoredCaptured;
        private bool savedStateApplied;

        private void Awake()
        {
            CaptureAuthored();
            EnsureScannerAndOutline();
            // A save / New Game restore that ran before Awake (9.2) wins over the authored slots.
            if (!savedStateApplied)
                BuildRuntimeLootFromSlots();
            initialized = true;
        }

        private void CaptureAuthored()
        {
            if (authoredCaptured)
                return;
            authoredCaptured = true;
            authoredVisibleToScanner = visibleToScanner;
        }

        /// <summary>New Game / fresh load: authored loot back, scanner as authored (loot plan 9.3).</summary>
        public void ResetToAuthoredLoot()
        {
            CaptureAuthored();
            savedStateApplied = true;
            FullInventoryHoldRequested = false;
            BuildRuntimeLootFromSlots();
            initialized = true;
            SetVisibleToScanner(authoredVisibleToScanner);
        }

        /// <summary>Remaining entries from a v24 save (9.2). Unknown item ids are skipped.</summary>
        public void ApplySavedEntries(LootChestEntrySave[] entries)
        {
            CaptureAuthored();
            savedStateApplied = true;
            FullInventoryHoldRequested = false;
            remainingLoot.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Length && remainingLoot.Count < MaxLootSlots; i++)
                {
                    LootChestEntrySave entry = entries[i];
                    if (entry == null || entry.amount <= 0 || string.IsNullOrEmpty(entry.itemId))
                        continue;
                    ItemData item = ItemRegistry.Resolve(entry.itemId);
                    if (item != null)
                        remainingLoot.Add(item, entry.amount);
                }
            }

            emptied = !HasRemainingLoot;
            initialized = true;
            SetVisibleToScanner(authoredVisibleToScanner && !emptied);
        }

        /// <summary>Remaining entries for the save (items only, D6).</summary>
        public LootChestEntrySave[] BuildEntrySave()
        {
            IReadOnlyList<DMLootEntry> entries = remainingLoot.Entries;
            List<LootChestEntrySave> list = new List<LootChestEntrySave>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                DMLootEntry entry = entries[i];
                if (entry == null || entry.IsEmpty)
                    continue;
                list.Add(new LootChestEntrySave { itemId = entry.item.name, amount = entry.amount });
            }

            return list.ToArray();
        }

        private void OnValidate()
        {
            if (lootSlots != null && lootSlots.Length > MaxLootSlots)
                Array.Resize(ref lootSlots, MaxLootSlots);

            if (string.IsNullOrWhiteSpace(cacheDisplayName))
                cacheDisplayName = "IO Ancient Cache";

            WarnOnNonItemLoot();

            // Keep scanner toggle in sync while editing.
            if (scannableTarget == null)
                scannableTarget = GetComponent<ScannableTarget>();
            scannableTarget?.SetHiddenFromScanner(!visibleToScanner);
        }

        /// <summary>Designer / runtime helper to replace loot contents (clamped to <see cref="MaxLootSlots"/>).</summary>
        public void ConfigureLoot(IReadOnlyList<LootSlot> slots, string displayName = null)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                cacheDisplayName = displayName;

            lootSlots = new LootSlot[MaxLootSlots];
            int count = slots != null ? Mathf.Min(slots.Count, MaxLootSlots) : 0;
            for (int i = 0; i < count; i++)
                lootSlots[i] = slots[i];

            BuildRuntimeLootFromSlots();
        }

        private void BuildRuntimeLootFromSlots()
        {
            remainingLoot.Clear();
            emptied = false;

            if (lootSlots == null)
                return;

            int count = Mathf.Min(lootSlots.Length, MaxLootSlots);
            for (int i = 0; i < count; i++)
            {
                LootSlot slot = lootSlots[i];
                if (slot == null || slot.item == null || slot.amount <= 0)
                    continue;

                remainingLoot.Add(slot.item, Mathf.Max(1, slot.amount));
            }
        }

        /// <summary>
        /// Item-only validator (D6): chests never hold Aether Credits. AC-infused items are flagged so
        /// the designer swaps them for plain items. Full id/slot validation is the phase 5 editor tool.
        /// </summary>
        private void WarnOnNonItemLoot()
        {
            if (lootSlots == null)
                return;

            for (int i = 0; i < lootSlots.Length; i++)
            {
                LootSlot slot = lootSlots[i];
                if (slot == null || slot.item == null || !slot.item.isAcInfused)
                    continue;

                Debug.LogWarning(
                    $"[DmEvents] '{name}' loot slot {i} ({slot.item.itemName}) carries Aether Credits. " +
                    "Chests hold items only (loot plan D6).",
                    this);
            }
        }

        private void EnsureScannerAndOutline()
        {
            scannableTarget = GetComponent<ScannableTarget>();
            if (scannableTarget == null)
                scannableTarget = gameObject.AddComponent<ScannableTarget>();

            scannableTarget.Configure(
                scanLabel,
                scanColor,
                ScannerTargetCategory.Loot,
                categoryOverride: true,
                lineOfSight: true,
                visibleToScanner: visibleToScanner);

            if (GetComponent<OutlineController>() == null)
                gameObject.AddComponent<OutlineController>();

            OutlineController outline = GetComponent<OutlineController>();
            if (outline != null)
                outline.scannerOnlyOutline = true;

            MapMarker marker = GetComponent<MapMarker>();
            if (marker == null)
                marker = gameObject.AddComponent<MapMarker>();
            marker.ConfigureScannedPoi(scanLabel, scanColor);
        }

        public void SetVisibleToScanner(bool visible)
        {
            visibleToScanner = visible;
            if (scannableTarget == null)
                scannableTarget = GetComponent<ScannableTarget>();
            scannableTarget?.SetHiddenFromScanner(!visible);
        }

        /// <summary>Story chests (and storage) keep their emptied visual instead of hiding (loot plan 7.5).</summary>
        public void SetKeepVisualWhenEmpty(bool keep)
        {
            keepVisualWhenEmpty = keep;
        }

        /// <summary>Called by <see cref="DMItemCollection"/> after the open delay. True when the loot window now shows this chest.</summary>
        public bool OpenLootDialogFromCollection()
        {
            if (!initialized || emptied || !HasRemainingLoot)
                return false;

            if (EnemyLootDialogUI.IsDialogOpen)
                return false;

            EnemyLootDialogUI.Show(this);
            return EnemyLootDialogUI.IsShowing(this);
        }

        /// <summary>Grants one entry by its stable id (loot window rows, D4 partial rules).</summary>
        public DMLootGrantResult TryLootEntry(int entryId)
        {
            DMLootGrantResult result = remainingLoot.TryLootEntry(entryId);
            RefreshEmptyState();
            return result;
        }

        /// <summary>Takes whatever fits in list order; the rest stays in the cache (D4).</summary>
        public DMLootGrantResult TryLootAll()
        {
            DMLootGrantResult result = remainingLoot.TryLootAll();
            RefreshEmptyState();
            return result;
        }

        public void NotifyWindowClosed()
        {
            FullInventoryHoldRequested = remainingLoot.FullInventoryRefusedThisVisit && HasRemainingLoot;
            remainingLoot.ResetVisit();
            LootWindowClosed?.Invoke(this);

            // Emptied while the window was open: hide only now, so the lid / listeners finish first.
            if (emptied && !keepVisualWhenEmpty)
                gameObject.SetActive(false);
        }

        private void RefreshEmptyState()
        {
            if (HasRemainingLoot)
                return;

            emptied = true;
            ResolveUiManager()?.HideInteractionPrompt();

            // Hide from scanners once emptied.
            SetVisibleToScanner(false);

            // While the loot window shows this chest, NotifyWindowClosed hides it after the window closes.
            if (!keepVisualWhenEmpty && !EnemyLootDialogUI.IsShowing(this))
                gameObject.SetActive(false);
        }

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.83f, 0.63f, 0.09f, 0.2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.4f, new Vector3(1.2f, 0.9f, 1f));
        }
#endif
    }
}
