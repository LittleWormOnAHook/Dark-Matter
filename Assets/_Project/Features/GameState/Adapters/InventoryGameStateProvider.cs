using System.Collections.Generic;
using Project.Features.GameState;
using Project.Inventory;
using UnityEngine;

namespace Project.Features.GameState.Adapters
{
    public sealed class InventoryGameStateProvider : IGameStateProvider
    {
        public string DomainId => "inventory";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            InventorySystem inventory = Object.FindAnyObjectByType<InventorySystem>();
            if (inventory == null || inventory.slots == null)
            {
                builder.Inventory = InventorySnapshot.Empty;
                return;
            }

            int occupied = 0;
            int totalStacks = 0;
            var distinct = new HashSet<string>();
            var top = new List<string>(3);

            int limit = Mathf.Min(inventory.unlockedMainSlots, inventory.slots.Count);
            for (int i = 0; i < limit; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                occupied++;
                totalStacks += Mathf.Max(0, slot.amount);
                string label = slot.item.itemName;
                if (!string.IsNullOrEmpty(label))
                    distinct.Add(label);
                if (top.Count < 3)
                    top.Add(label + " x" + slot.amount);
            }

            builder.Inventory = new InventorySnapshot(
                inventorySize: inventory.unlockedMainSlots,
                occupiedSlots: occupied,
                distinctItemCount: distinct.Count,
                totalStackCount: totalStacks,
                topItemLabels: top.ToArray());
        }
    }
}
