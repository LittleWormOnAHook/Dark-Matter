using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Grenades must live on a dedicated hotbar slot (Hot Cross lower-right). Inventory-only stacks cannot be thrown.
    /// </summary>
    public static class DMIGrenadeHotbarUtility
    {
        public const int DefaultHotbarLocalIndex = 9;

        public static int ResolveHotbarLocalIndex()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            int idx = profile != null ? profile.grenadeHotbarLocalIndex : DefaultHotbarLocalIndex;
            return Mathf.Clamp(idx, 0, 9);
        }

        public static int ToAbsoluteSlot(InventorySystem inventory, int localHotbarIndex)
        {
            if (inventory == null)
                return -1;
            return inventory.HotbarStartIndex + localHotbarIndex;
        }

        public static bool IsConfiguredGrenadeItem(ItemData candidate, ItemData configuredGrenade)
        {
            if (candidate == null || configuredGrenade == null)
                return false;

            if (ReferenceEquals(candidate, configuredGrenade))
                return true;

            if (!string.IsNullOrEmpty(configuredGrenade.StableItemId)
                && configuredGrenade.StableItemId == candidate.StableItemId)
                return true;

            return string.Equals(candidate.itemName, configuredGrenade.itemName, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(candidate.name, configuredGrenade.name, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetHotbarStack(InventorySystem inventory, ItemData grenadeItem, out int amount, out int absoluteSlot)
        {
            return TryGetHotbarStackAtLocal(
                inventory,
                ResolveHotbarLocalIndex(),
                grenadeItem,
                out amount,
                out absoluteSlot);
        }

        public static bool TryGetHotbarStackAtLocal(
            InventorySystem inventory,
            int localHotbarIndex,
            ItemData grenadeItem,
            out int amount,
            out int absoluteSlot)
        {
            amount = 0;
            absoluteSlot = -1;
            if (inventory == null || grenadeItem == null)
                return false;

            int local = Mathf.Clamp(localHotbarIndex, 0, 9);
            absoluteSlot = ToAbsoluteSlot(inventory, local);
            if (absoluteSlot < 0 || absoluteSlot >= inventory.slots.Count)
                return false;

            InventorySystem.InventorySlot slot = inventory.slots[absoluteSlot];
            if (slot == null || slot.IsEmpty || slot.item == null)
                return false;

            if (!IsConfiguredGrenadeItem(slot.item, grenadeItem))
                return false;

            amount = slot.amount;
            return amount > 0;
        }

        public static bool TryConsumeOneFromHotbar(InventorySystem inventory, ItemData grenadeItem, int localHotbarIndex = -1)
        {
            int local = localHotbarIndex >= 0 ? localHotbarIndex : ResolveHotbarLocalIndex();
            if (!TryGetHotbarStackAtLocal(inventory, local, grenadeItem, out _, out int absoluteSlot))
                return false;

            return inventory.RemoveItemAt(absoluteSlot, 1);
        }
    }
}
