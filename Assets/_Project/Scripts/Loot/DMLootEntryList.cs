using System.Collections.Generic;
using Project.Data;
using Project.UI;
using UnityEngine;

namespace Project.Loot
{
    /// <summary>
    /// Remaining loot for one container (chest or enemy bag) with stable entry ids and the shared
    /// per-entry / Loot All rules (loot plan 7.2, 7.7, D4). Items only (D6).
    /// </summary>
    public sealed class DMLootEntryList
    {
        public const int MaxEntries = 25;

        private readonly List<DMLootEntry> entries = new List<DMLootEntry>(MaxEntries);
        private int nextEntryId = 1;

        public IReadOnlyList<DMLootEntry> Entries => entries;
        public int Count => entries.Count;
        public bool IsEmpty => entries.Count == 0;

        /// <summary>
        /// A Loot or Loot All during the current window visit could not take everything because the
        /// inventory was full. Drives the 30-minute hold on close (D4, D19; timers are phase 4).
        /// </summary>
        public bool FullInventoryRefusedThisVisit { get; private set; }

        public bool HasProtectedEntries
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].IsProtected)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Sum of remaining amounts (the "Inventory full - N left" banner).</summary>
        public int TotalAmount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                        total += Mathf.Max(0, entries[i].amount);
                }

                return total;
            }
        }

        /// <summary>Clears entries. Ids keep counting up so an old row id can never match a new entry.</summary>
        public void Clear()
        {
            entries.Clear();
            FullInventoryRefusedThisVisit = false;
        }

        public DMLootEntry Add(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || entries.Count >= MaxEntries)
                return null;

            DMLootEntry entry = new DMLootEntry
            {
                entryId = nextEntryId++,
                item = item,
                amount = amount
            };
            entries.Add(entry);
            return entry;
        }

        public DMLootEntry Find(int entryId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].entryId == entryId)
                    return entries[i];
            }

            return null;
        }

        /// <summary>Grants one entry by stable id. Partial grants keep the rest in the row.</summary>
        public DMLootGrantResult TryLootEntry(int entryId)
        {
            DMLootEntry entry = Find(entryId);
            if (entry == null)
                return DMLootGrantResult.None;

            DMLootGrantResult result = DMLootGrant.TryGrant(entry, out _);
            if (entry.IsEmpty)
                entries.Remove(entry);

            if (result == DMLootGrantResult.Partial || result == DMLootGrantResult.InventoryFull)
            {
                FullInventoryRefusedThisVisit = true;
                PickupToastUI.ShowInventoryFull();
            }

            return result;
        }

        /// <summary>
        /// Loot All (D4): takes whatever fits in list order, leaves the rest, one inventory-full toast.
        /// Returns Granted when emptied, Partial when some was taken, InventoryFull when nothing fit.
        /// </summary>
        public DMLootGrantResult TryLootAll()
        {
            if (entries.Count == 0)
                return DMLootGrantResult.None;

            bool tookAny = false;
            bool refused = false;
            int index = 0;
            while (index < entries.Count)
            {
                DMLootEntry entry = entries[index];
                if (entry == null || entry.IsEmpty)
                {
                    entries.RemoveAt(index);
                    continue;
                }

                DMLootGrant.TryGrant(entry, out int granted);
                if (granted > 0)
                    tookAny = true;

                if (entry.IsEmpty)
                {
                    entries.RemoveAt(index);
                    continue;
                }

                refused = true;
                index++;
            }

            if (!refused)
                return DMLootGrantResult.Granted;

            FullInventoryRefusedThisVisit = true;
            PickupToastUI.ShowInventoryFull();
            return tookAny ? DMLootGrantResult.Partial : DMLootGrantResult.InventoryFull;
        }

        /// <summary>Call after the window-closed hook has read <see cref="FullInventoryRefusedThisVisit"/>.</summary>
        public void ResetVisit()
        {
            FullInventoryRefusedThisVisit = false;
        }
    }
}
