using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.UI;
using UnityEngine;

namespace Project.Survival
{
    /// <summary>
    /// Med packs, food, fuel, and other survival restores must sit on the hotbar before use (Hot Cross lower-left, V tap/hold).
    /// Hold B is reserved for stone build mode (see GameplayKeyboardShortcuts.HandleBinocularsKey).
    /// </summary>
    public static class DMISurvivalHotbarUtility
    {
        public const int DefaultHotbarLocalIndex = 8;

        /// <summary>
        /// Hot Cross BL / V: any inventory item that restores player stats or refuels powered gear (not weapons, ammo, or grenades).
        /// </summary>
        public static bool IsHotCrossSurvivalItem(ItemData item)
        {
            if (item == null)
                return false;

            if (item.IsWeapon || item.IsEquippable || item.itemType == ItemType.Tool)
                return false;

            if (item.IsAmmo || item.CountsAsAmmo)
                return false;

            if (item.itemType == ItemType.Quest
                || item.itemType == ItemType.Vehicle
                || item.itemType == ItemType.WorldDeployable)
                return false;

            if (item.IsInventoryStorageModule || item.IsBuildingTerrainBootstrap)
                return false;

            if (item.IsDeployableShelter || item.IsWalkerDrill)
                return false;

            if (DMIGrenadeThrowBridge.GrenadesEnabled
                && DMIGrenadeHotbarUtility.IsConfiguredGrenadeItem(item, ResolveGrenadeItemForCompare()))
                return false;

            if (HasSurvivalStatRestore(item))
                return true;

            if (IsPlasmaFuelItem(item))
                return true;

            return false;
        }

        /// <summary>Alias for <see cref="IsHotCrossSurvivalItem"/> (legacy call sites).</summary>
        public static bool IsSurvivalConsumable(ItemData item) => IsHotCrossSurvivalItem(item);

        public static bool HasSurvivalStatRestore(ItemData item)
        {
            if (item == null)
                return false;

            return item.healthRestore > 0f
                || item.energyRestore > 0f
                || item.staminaRestore > 0f
                || item.oxygenRestore > 0f;
        }

        public static bool IsPlasmaFuelItem(ItemData item)
        {
            if (item == null)
                return false;

            ItemData plasma = ItemRegistry.Resolve("Plasma Fuel");
            if (plasma != null && (item == plasma || item.itemName == plasma.itemName))
                return true;

            return string.Equals(item.itemName, "Plasma Fuel", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.name, "Plasma Fuel", System.StringComparison.OrdinalIgnoreCase);
        }

        public static int ResolveHotbarLocalIndex()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            int idx = profile != null ? profile.survivalHotbarLocalIndex : DefaultHotbarLocalIndex;
            return Mathf.Clamp(idx, 0, 9);
        }

        public static int ToAbsoluteSlot(InventorySystem inventory, int localHotbarIndex)
        {
            if (inventory == null)
                return -1;
            return inventory.HotbarStartIndex + localHotbarIndex;
        }

        public static bool IsBlockedGrenadeSlot(int localHotbarIndex)
        {
            return localHotbarIndex == DMIGrenadeHotbarUtility.ResolveHotbarLocalIndex();
        }

        public static bool TryGetHotbarSurvivalAtLocal(
            InventorySystem inventory,
            int localHotbarIndex,
            out ItemData item,
            out int amount)
        {
            item = null;
            amount = 0;
            if (inventory == null || IsBlockedGrenadeSlot(localHotbarIndex))
                return false;

            int absolute = ToAbsoluteSlot(inventory, localHotbarIndex);
            if (absolute < 0 || absolute >= inventory.slots.Count)
                return false;

            InventorySystem.InventorySlot slot = inventory.slots[absolute];
            if (slot == null || slot.IsEmpty || slot.item == null)
                return false;

            if (!IsHotCrossSurvivalItem(slot.item))
                return false;

            if (DMIGrenadeThrowBridge.GrenadesEnabled
                && DMIGrenadeHotbarUtility.IsConfiguredGrenadeItem(slot.item, ResolveGrenadeItemForCompare()))
                return false;

            item = slot.item;
            amount = slot.amount;
            return amount > 0;
        }

        public static bool CanUseSurvivalFromSlot(InventorySystem inventory, int absoluteSlotIndex, bool showToast)
        {
            if (inventory == null || absoluteSlotIndex < 0 || absoluteSlotIndex >= inventory.slots.Count)
                return false;

            ItemData item = inventory.GetItemAt(absoluteSlotIndex);
            if (!IsHotCrossSurvivalItem(item))
                return true;

            if (inventory.IsHotbarIndex(absoluteSlotIndex))
                return true;

            if (showToast)
                PickupToastUI.Show("Assign survival items to the hotbar (lower-left Hot Cross).");

            return false;
        }

        private static ItemData ResolveGrenadeItemForCompare()
        {
            DMIGrenadeThrowBridge bridge = PlayerLocator.FindOnLivePlayer<DMIGrenadeThrowBridge>();
            if (bridge != null && bridge.GrenadeItem != null)
                return bridge.GrenadeItem;

            return ItemRegistry.Resolve("Frag Grenade") ?? ItemRegistry.Resolve("DM_Frag_Grenade");
        }
    }
}
