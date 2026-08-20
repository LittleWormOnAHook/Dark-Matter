using System;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using Project.Interaction;
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
    /// </summary>
    [DisallowMultipleComponent]
    public class DmEvents : MonoBehaviour, IEnemyLootProvider
    {
        public const int MaxLootSlots = 10;

        [Serializable]
        public class LootSlot
        {
            public ItemData item;
            [Min(1)] public int amount = 1;
        }

        [Header("Identity")]
        [SerializeField] private string cacheDisplayName = "IO Ancient Cache";

        [Header("Loot Table (up to 10)")]
        [SerializeField] private LootSlot[] lootSlots = new LootSlot[MaxLootSlots];

        [Header("Scanner")]
        [SerializeField] private bool visibleToScanner = true;
        [SerializeField] private string scanLabel = "IO Ancient Cache";
        [SerializeField] private Color scanColor = DarkMatterGenesisUiPalette.Gold;

        [Header("Lifecycle")]
        [Tooltip("When empty, only stop interaction — keep the opened chest mesh in the world.")]
        [SerializeField] private bool keepVisualWhenEmpty = true;

        private readonly List<QuestRewardDefinition> remainingLoot = new List<QuestRewardDefinition>(MaxLootSlots);
        private UIManager uiManager;
        private bool initialized;
        private bool emptied;
        private ScannableTarget scannableTarget;

        public string CacheDisplayName =>
            string.IsNullOrWhiteSpace(cacheDisplayName) ? "IO Ancient Cache" : cacheDisplayName;

        public bool HasRemainingLoot => remainingLoot.Count > 0;

        private void Awake()
        {
            EnsureScannerAndOutline();
            BuildRuntimeLootFromSlots();
            initialized = true;
        }

        private void OnValidate()
        {
            if (lootSlots != null && lootSlots.Length > MaxLootSlots)
                Array.Resize(ref lootSlots, MaxLootSlots);

            if (string.IsNullOrWhiteSpace(cacheDisplayName))
                cacheDisplayName = "IO Ancient Cache";

            // Keep scanner toggle in sync while editing.
            if (scannableTarget == null)
                scannableTarget = GetComponent<ScannableTarget>();
            scannableTarget?.SetHiddenFromScanner(!visibleToScanner);
        }

        /// <summary>Designer / runtime helper to replace loot contents (clamped to 10).</summary>
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

                remainingLoot.Add(new QuestRewardDefinition
                {
                    type = QuestRewardType.Item,
                    item = slot.item,
                    amount = Mathf.Max(1, slot.amount)
                });
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

        /// <summary>Called by <see cref="DMItemCollection"/> after the open animation delay.</summary>
        public void OpenLootDialogFromCollection()
        {
            if (!initialized || emptied || !HasRemainingLoot)
                return;

            if (EnemyLootDialogUI.IsDialogOpen)
                return;

            EnemyLootDialogUI.Show(this, CacheDisplayName, BuildLootSummary());
        }

        public bool TryLootNextEntry()
        {
            if (!HasRemainingLoot)
                return false;

            QuestRewardDefinition entry = remainingLoot[0];
            if (TryGrantLootEntry(entry))
                remainingLoot.RemoveAt(0);

            RefreshEmptyState();
            return true;
        }

        public bool TryLootAll()
        {
            if (!HasRemainingLoot)
                return false;

            bool anyLeftUnlooted = false;
            for (int i = remainingLoot.Count - 1; i >= 0; i--)
            {
                if (TryGrantLootEntry(remainingLoot[i]))
                    remainingLoot.RemoveAt(i);
                else
                    anyLeftUnlooted = true;
            }

            if (anyLeftUnlooted)
                PickupToastUI.ShowInventoryFull();

            RefreshEmptyState();
            return true;
        }

        public string BuildLootSummary()
        {
            if (!HasRemainingLoot)
                return "Cache is empty.";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine($"{CacheDisplayName} contains:");
            for (int i = 0; i < remainingLoot.Count; i++)
            {
                QuestRewardDefinition entry = remainingLoot[i];
                if (entry == null)
                    continue;

                string line = QuestRewardFormatter.FormatLootLine(entry);
                if (!string.IsNullOrEmpty(line))
                    builder.AppendLine(line);
            }

            builder.AppendLine();
            builder.Append("Press E to loot · Shift+E or Loot All for everything");
            return builder.ToString().TrimEnd();
        }

        private void RefreshEmptyState()
        {
            if (HasRemainingLoot)
                return;

            emptied = true;
            ResolveUiManager()?.HideInteractionPrompt();

            // Hide from scanners once emptied.
            SetVisibleToScanner(false);

            if (!keepVisualWhenEmpty)
                gameObject.SetActive(false);
        }

        /// <returns>True when the entry was fully granted and can be removed from remaining loot.</returns>
        private bool TryGrantLootEntry(QuestRewardDefinition entry)
        {
            if (entry == null)
                return true;

            if (entry.type == QuestRewardType.Item && entry.item != null
                && !LevelUnlockUtility.PassesPickupGate(entry.item, showToast: true))
                return false;

            int requested = Mathf.Max(0, entry.amount);
            int granted = QuestRewardGranter.GrantReward(entry, CacheDisplayName);

            if (entry.type == QuestRewardType.Item && entry.item != null)
            {
                if (granted > 0)
                    PickupToastUI.Show($"+{granted} {entry.item.itemName}");

                if (granted >= requested)
                    return true;

                entry.amount = Mathf.Max(0, requested - granted);
                return false;
            }

            if (entry.type == QuestRewardType.Pi && granted > 0)
                PickupToastUI.Show($"+{granted} AC");

            return granted > 0 || requested <= 0;
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
