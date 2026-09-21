using System;
using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Storage
{
    public static class DMStorageCrateRuntime
    {
        private static readonly Dictionary<string, DMStorageCrateState> States =
            new Dictionary<string, DMStorageCrateState>(StringComparer.Ordinal);

        public static event Action StatesChanged;

        public static DMStorageCrateState GetOrCreate(string crateId, int slotCount)
        {
            if (string.IsNullOrWhiteSpace(crateId))
                crateId = "camp_storage_01";

            if (!States.TryGetValue(crateId, out DMStorageCrateState state) || state == null)
            {
                state = new DMStorageCrateState { CrateId = crateId };
                States[crateId] = state;
            }

            state.EnsureSlotCount(Mathf.Max(1, slotCount));
            return state;
        }

        public static int CountItem(ItemData item)
        {
            if (item == null)
                return 0;

            int total = 0;
            foreach (DMStorageCrateState state in States.Values)
            {
                if (state == null)
                    continue;
                total += state.CountItem(item);
            }

            return total;
        }

        public static void FillItemCounts(Dictionary<ItemData, int> dest)
        {
            if (dest == null)
                return;

            dest.Clear();
            foreach (DMStorageCrateState state in States.Values)
            {
                if (state == null)
                    continue;
                state.AddItemCounts(dest);
            }
        }

        public static StorageCrateSave[] BuildSave()
        {
            var list = new List<StorageCrateSave>(States.Count);
            foreach (KeyValuePair<string, DMStorageCrateState> pair in States)
            {
                if (pair.Value == null)
                    continue;
                list.Add(pair.Value.ToSave());
            }

            return list.ToArray();
        }

        public static void ApplySave(StorageCrateSave[] saves)
        {
            States.Clear();
            if (saves == null)
            {
                NotifyChanged();
                return;
            }

            for (int i = 0; i < saves.Length; i++)
            {
                StorageCrateSave save = saves[i];
                if (save == null || string.IsNullOrWhiteSpace(save.crateId))
                    continue;

                var state = new DMStorageCrateState { CrateId = save.crateId };
                state.ApplySave(save);
                States[save.crateId] = state;
            }

            NotifyChanged();
        }

        public static void ResetAll()
        {
            States.Clear();
            NotifyChanged();
        }

        public static void NotifyChanged()
        {
            StatesChanged?.Invoke();
        }
    }

    public sealed class DMStorageCrateState
    {
        public string CrateId;
        public readonly List<CrateSlot> Slots = new List<CrateSlot>();

        public void EnsureSlotCount(int count)
        {
            while (Slots.Count < count)
                Slots.Add(new CrateSlot());
            if (Slots.Count > count)
                Slots.RemoveRange(count, Slots.Count - count);
        }

        public int CountItem(ItemData item)
        {
            int total = 0;
            for (int i = 0; i < Slots.Count; i++)
            {
                CrateSlot slot = Slots[i];
                if (slot != null && slot.item == item)
                    total += slot.amount;
            }

            return total;
        }

        public void AddItemCounts(Dictionary<ItemData, int> dest)
        {
            if (dest == null)
                return;

            for (int i = 0; i < Slots.Count; i++)
            {
                CrateSlot slot = Slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                dest.TryGetValue(slot.item, out int current);
                dest[slot.item] = current + slot.amount;
            }
        }

        public bool CanAccept(ItemData item)
        {
            return item != null
                && item.itemType != ItemType.Quest
                && item.itemType != ItemType.Vehicle
                && item.itemType != ItemType.WorldDeployable;
        }

        public int AddItem(ItemData item, int amount)
        {
            if (!CanAccept(item) || amount <= 0)
                return 0;

            int remaining = amount;
            int maxStack = Mathf.Max(1, item.maxStack);

            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                CrateSlot slot = Slots[i];
                if (slot.item != item || slot.amount >= maxStack)
                    continue;
                int canAdd = Mathf.Min(remaining, maxStack - slot.amount);
                slot.amount += canAdd;
                remaining -= canAdd;
            }

            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                CrateSlot slot = Slots[i];
                if (!slot.IsEmpty)
                    continue;
                int canAdd = Mathf.Min(remaining, maxStack);
                slot.item = item;
                slot.amount = canAdd;
                remaining -= canAdd;
            }

            int added = amount - remaining;
            if (added > 0)
                DMStorageCrateRuntime.NotifyChanged();
            return added;
        }

        public bool CanSplit(int index)
        {
            if (index < 0 || index >= Slots.Count)
                return false;

            CrateSlot slot = Slots[index];
            if (slot == null || slot.IsEmpty || slot.amount <= 1)
                return false;

            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i] != null && Slots[i].IsEmpty)
                    return true;
            }

            return false;
        }

        public bool MoveOrMerge(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return true;
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= Slots.Count || toIndex >= Slots.Count)
                return false;

            CrateSlot from = Slots[fromIndex];
            CrateSlot to = Slots[toIndex];
            if (from == null || from.IsEmpty)
                return false;

            if (to == null)
            {
                to = new CrateSlot();
                Slots[toIndex] = to;
            }

            if (to.IsEmpty)
            {
                to.item = from.item;
                to.amount = from.amount;
                from.item = null;
                from.amount = 0;
                DMStorageCrateRuntime.NotifyChanged();
                return true;
            }

            if (from.item == to.item)
            {
                int maxStack = Mathf.Max(1, from.item.maxStack);
                int canAdd = Mathf.Min(from.amount, maxStack - to.amount);
                if (canAdd <= 0)
                    return false;

                to.amount += canAdd;
                from.amount -= canAdd;
                if (from.amount <= 0)
                {
                    from.item = null;
                    from.amount = 0;
                }

                DMStorageCrateRuntime.NotifyChanged();
                return true;
            }

            ItemData swapItem = to.item;
            int swapAmount = to.amount;
            to.item = from.item;
            to.amount = from.amount;
            from.item = swapItem;
            from.amount = swapAmount;
            DMStorageCrateRuntime.NotifyChanged();
            return true;
        }

        public bool SplitAt(int index)
        {
            if (!CanSplit(index))
                return false;

            CrateSlot source = Slots[index];
            int emptyIndex = -1;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i] != null && Slots[i].IsEmpty)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex < 0)
                return false;

            int splitAmount = source.amount / 2;
            source.amount -= splitAmount;
            Slots[emptyIndex].item = source.item;
            Slots[emptyIndex].amount = splitAmount;
            DMStorageCrateRuntime.NotifyChanged();
            return true;
        }

        public bool RemoveAt(int index, int amount)
        {
            if (index < 0 || index >= Slots.Count || amount <= 0)
                return false;

            CrateSlot slot = Slots[index];
            if (slot.IsEmpty || slot.amount < amount)
                return false;

            slot.amount -= amount;
            if (slot.amount <= 0)
            {
                slot.item = null;
                slot.amount = 0;
            }

            DMStorageCrateRuntime.NotifyChanged();
            return true;
        }

        public StorageCrateSave ToSave()
        {
            var slots = new StorageCrateSlotSave[Slots.Count];
            for (int i = 0; i < Slots.Count; i++)
            {
                CrateSlot slot = Slots[i];
                slots[i] = new StorageCrateSlotSave
                {
                    itemId = slot != null && slot.item != null ? slot.item.name : string.Empty,
                    amount = slot != null ? slot.amount : 0
                };
            }

            return new StorageCrateSave
            {
                crateId = CrateId,
                slots = slots
            };
        }

        public void ApplySave(StorageCrateSave save)
        {
            Slots.Clear();
            if (save == null || save.slots == null)
                return;

            for (int i = 0; i < save.slots.Length; i++)
            {
                StorageCrateSlotSave slotSave = save.slots[i];
                var slot = new CrateSlot();
                if (slotSave != null && slotSave.amount > 0 && !string.IsNullOrEmpty(slotSave.itemId))
                {
                    slot.item = ItemRegistry.Resolve(slotSave.itemId);
                    slot.amount = slot.item != null ? slotSave.amount : 0;
                }

                Slots.Add(slot);
            }
        }
    }

    public sealed class CrateSlot
    {
        public ItemData item;
        public int amount;
        public bool IsEmpty => item == null || amount <= 0;
    }
}
