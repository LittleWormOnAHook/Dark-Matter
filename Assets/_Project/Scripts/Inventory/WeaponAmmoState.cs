using System;
using System.Collections.Generic;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.Inventory
{
    /// <summary>
    /// Tracks loaded ammo per weapon hotbar slot and credits ammo pickups to compatible weapons.
    /// </summary>
    public class WeaponAmmoState : MonoBehaviour
    {
        [Serializable]
        private class SlotAmmo
        {
            public int loaded;
            public AmmoType loadedType = AmmoType.Gunpowder;
            /// <summary>Actual ammo ItemData asset currently loaded, so VFX/status-effect data can be
            /// resolved per specific ammo variant rather than just the shared enum type.</summary>
            public ItemData loadedItem;
        }

        private readonly Dictionary<int, SlotAmmo> slotAmmo = new Dictionary<int, SlotAmmo>(4);
        private readonly Dictionary<int, ItemData> slotWeaponIdentity = new Dictionary<int, ItemData>(4);
        private EquipmentController equipment;
        private InventorySystem inventory;

        public event Action OnAmmoChanged;

        private const string StandardAmmoItemName = "Standard";
        private static ItemData cachedStandardAmmo;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            inventory = GetComponent<InventorySystem>();
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        public int GetLoadedAmmo(int weaponHotbarSlot)
        {
            return slotAmmo.TryGetValue(weaponHotbarSlot, out SlotAmmo entry) ? entry.loaded : 0;
        }

        public AmmoType GetLoadedAmmoType(int weaponHotbarSlot)
        {
            return slotAmmo.TryGetValue(weaponHotbarSlot, out SlotAmmo entry)
                ? entry.loadedType
                : AmmoType.Gunpowder;
        }

        /// <summary>The actual ammo ItemData asset currently loaded in this weapon slot, or null if unset/empty.</summary>
        public ItemData GetLoadedAmmoItem(int weaponHotbarSlot)
        {
            return slotAmmo.TryGetValue(weaponHotbarSlot, out SlotAmmo entry) ? entry.loadedItem : null;
        }

        public int GetActiveLoadedAmmo()
        {
            if (equipment == null)
                return 0;

            return GetLoadedAmmo(equipment.ActiveWeaponHotbarSlot);
        }

        /// <summary>
        /// Reserve ammo count for a specific weapon hotbar slot: only counts inventory stacks
        /// matching that slot's currently loaded ammo type (not just any type the weapon could
        /// accept), so the HUD doesn't advertise reserve rounds that won't actually auto-refill.
        /// </summary>
        public int GetReserveAmmoCount(int weaponHotbarSlot)
        {
            if (IsInfiniteAmmoForSlot(weaponHotbarSlot))
                return int.MaxValue;

            if (inventory == null)
                return 0;

            AmmoType loadedType = GetLoadedAmmoType(weaponHotbarSlot);
            ItemData loadedItem = GetLoadedAmmoItem(weaponHotbarSlot);
            int reserve = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (slot.item.ammoType != loadedType)
                    continue;

                if (!AmmoItemsCompatibleForReserve(loadedItem, slot.item))
                    continue;

                reserve += slot.amount;
            }

            return reserve;
        }

        private static bool AmmoItemsCompatibleForReserve(ItemData loadedItem, ItemData candidate)
        {
            if (candidate == null)
                return false;

            // Keep continuous Laser Tool cells separate from pulse Laser Pistol Ammo.
            if (loadedItem != null && loadedItem.ammoType == AmmoType.Laser)
                return candidate.isContinuousLaser == loadedItem.isContinuousLaser;

            if (candidate.ammoType == AmmoType.Laser && candidate.isContinuousLaser)
                return loadedItem != null && loadedItem.isContinuousLaser;

            return true;
        }

        /// <summary>
        /// Consumes one round from the active weapon's magazine. Deliberately does NOT auto-refill
        /// from reserve when the magazine is already empty — an empty magazine should pause fire and
        /// visibly reload (see PioneerInvectorAmmoBridge.TryProcessShotAmmo/TryStartReloadIfEmpty),
        /// not silently top itself up mid-shot. Actual reserve refilling happens once, at reload
        /// completion, via EnsureWeaponInitialized.
        /// </summary>
        public bool TryConsumeActiveRound()
        {
            if (equipment == null)
                return false;

            int slot = ResolveActiveWeaponHotbarSlot();
            ItemData weapon = equipment.GetHotbarItem(slot);
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            SlotAmmo entry = GetOrCreateSlot(slot, weapon);
            if (IsInfiniteAmmoForSlot(slot))
            {
                if (entry.loaded <= 0)
                    entry.loaded = Mathf.Max(1, weapon.magazineSize);

                NotifyChanged();
                return true;
            }

            if (entry.loaded <= 0)
                return false;

            entry.loaded--;
            NotifyChanged();
            return true;
        }

        public void CreditAmmoPickup(ItemData ammoItem, int amount)
        {
            if (ammoItem == null || !ammoItem.CountsAsAmmo || amount <= 0 || equipment == null)
                return;

            // Continuous Laser Tool: unlock infinite mining power. Consume the whole pickup —
            // never leave leftover cells in the backpack.
            if (ammoItem.isContinuousLaser)
            {
                bool credited = false;
                equipment.ForEachWeaponHotbarSlot(hotbarSlot =>
                {
                    ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                    if (weapon == null || !weapon.isMiningTool || !weapon.AcceptsAmmoType(ammoItem.ammoType))
                        return;

                    SlotAmmo entry = GetOrCreateSlot(hotbarSlot, weapon);
                    entry.loadedType = ammoItem.ammoType;
                    entry.loadedItem = ammoItem;
                    entry.loaded = Mathf.Max(1, weapon.magazineSize);
                    credited = true;
                });

                // No mining tool on a weapon hotbar yet — keep the stack so the player can
                // right-click Equip Ammo once the tool is equipped.
                if (!credited && inventory != null)
                    inventory.AddItem(ammoItem, amount, autoCreditAmmoToWeapons: false);

                NotifyChanged();
                return;
            }

            int remaining = amount;
            equipment.ForEachWeaponHotbarSlot(hotbarSlot =>
            {
                if (remaining <= 0)
                    return;

                ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                if (weapon == null || !weapon.AcceptsAmmoType(ammoItem.ammoType))
                    return;

                // Continuous cells handled above. Pulse Laser / other ammo never auto-fills mining tools.
                if (weapon.isMiningTool)
                    return;

                SlotAmmo entry = GetOrCreateSlot(hotbarSlot, weapon);
                // Only auto-credit a pickup into the weapon if it matches the ammo type already
                // loaded (or defaulted) for this slot. A different type never silently swaps in —
                // the player must explicitly "Equip Ammo To" it via the inventory right-click menu.
                if (entry.loadedType != ammoItem.ammoType)
                    return;

                entry.loadedType = ammoItem.ammoType;
                entry.loadedItem = ammoItem;
                int space = Mathf.Max(0, weapon.magazineSize - entry.loaded);
                int add = Mathf.Min(space, remaining);
                entry.loaded += add;
                remaining -= add;
            });

            if (remaining > 0 && inventory != null)
                inventory.AddItem(ammoItem, remaining, autoCreditAmmoToWeapons: false);

            NotifyChanged();
        }

        /// <summary>
        /// Explicit player-driven equip: loads ammo from a specific inventory stack into a specific
        /// weapon's hotbar slot, used by the inventory right-click "Equip Ammo To" menu. Unlike the
        /// passive auto-credit path, this deliberately swaps ammo type if a different one is already
        /// loaded — any remaining old rounds are returned to the inventory first.
        /// </summary>
        public bool TryEquipAmmoToWeaponSlot(int weaponHotbarSlot, int inventorySlotIndex)
        {
            if (inventory == null || equipment == null)
                return false;

            if (inventorySlotIndex < 0 || inventorySlotIndex >= inventory.slots.Count)
                return false;

            InventorySystem.InventorySlot invSlot = inventory.slots[inventorySlotIndex];
            if (invSlot == null || invSlot.IsEmpty || invSlot.item == null || !invSlot.item.CountsAsAmmo)
                return false;

            ItemData weapon = equipment.GetHotbarItem(weaponHotbarSlot);
            if (weapon == null || !weapon.IsRangedWeapon || !weapon.AcceptsAmmoType(invSlot.item.ammoType))
                return false;

            // Continuous Laser Tool only equips to mining tools; pulse Laser never equips to mining.
            if (weapon.isMiningTool)
            {
                if (!invSlot.item.isContinuousLaser)
                    return false;
            }
            else if (invSlot.item.isContinuousLaser)
            {
                return false;
            }

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (entry.loaded > 0 && entry.loadedType != invSlot.item.ammoType)
                ReturnLoadedAmmoToInventory(entry);

            entry.loadedType = invSlot.item.ammoType;
            entry.loadedItem = invSlot.item;

            // Continuous Laser Tool unlocks infinite mining power — consume the whole stack.
            if (weapon.isMiningTool && invSlot.item.isContinuousLaser)
            {
                entry.loaded = Mathf.Max(1, weapon.magazineSize);
                inventory.RemoveItemAt(inventorySlotIndex, invSlot.amount);
                NotifyChanged();
                return true;
            }

            int space = Mathf.Max(0, weapon.magazineSize - entry.loaded);
            int take = Mathf.Min(space, invSlot.amount);
            if (take > 0)
            {
                entry.loaded += take;
                inventory.RemoveItemAt(inventorySlotIndex, take);
            }

            NotifyChanged();
            return true;
        }

        /// <summary>Lists hotbar slots holding a ranged weapon that can accept the given ammo type, for the "Equip Ammo To" menu.</summary>
        public List<int> GetEligibleWeaponHotbarSlots(ItemData ammoItem)
        {
            List<int> eligible = new List<int>(EquipmentController.WeaponSlotCount);
            if (ammoItem == null || equipment == null)
                return eligible;

            equipment.ForEachWeaponHotbarSlot(hotbarSlot =>
            {
                ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                if (weapon == null || !weapon.IsRangedWeapon || !weapon.AcceptsAmmoType(ammoItem.ammoType))
                    return;

                if (weapon.isMiningTool)
                {
                    if (!ammoItem.isContinuousLaser)
                        return;
                }
                else if (ammoItem.isContinuousLaser)
                {
                    return;
                }

                eligible.Add(hotbarSlot);
            });

            return eligible;
        }

        private void ReturnLoadedAmmoToInventory(SlotAmmo entry)
        {
            if (entry.loaded <= 0)
                return;

            if (entry.loadedItem != null && inventory != null)
                inventory.AddItem(entry.loadedItem, entry.loaded, autoCreditAmmoToWeapons: false);

            entry.loaded = 0;
            entry.loadedItem = null;
        }

        public void EnsureWeaponInitialized(int weaponHotbarSlot, ItemData weapon)
        {
            if (weapon == null || !weapon.IsRangedWeapon)
                return;

            bool isFreshWeaponInSlot = !slotWeaponIdentity.TryGetValue(weaponHotbarSlot, out ItemData previousWeapon)
                || previousWeapon != weapon;

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (isFreshWeaponInSlot)
            {
                slotWeaponIdentity[weaponHotbarSlot] = weapon;
                ApplyFreshWeaponAmmo(weapon, entry);
                if (entry.loaded <= 0)
                    TryRefillFromInventory(weapon, entry);
                NotifyChanged();
                return;
            }

            if (entry.loaded > 0)
                return;

            if (TryRefillFromInventory(weapon, entry))
                NotifyChanged();
        }

        /// <summary>
        /// First equip/pickup for a weapon slot: either Empty 0/0, or a random Standard mag load
        /// when <see cref="ItemData.grantRandomStartingAmmo"/> is enabled.
        /// </summary>
        private void ApplyFreshWeaponAmmo(ItemData weapon, SlotAmmo entry)
        {
            ItemData defaultAmmo = ResolveDefaultAmmoItemForWeapon(weapon);
            entry.loadedItem = defaultAmmo;
            entry.loadedType = defaultAmmo != null ? defaultAmmo.ammoType : AmmoType.Gunpowder;
            entry.loaded = 0;

            if (!weapon.grantRandomStartingAmmo)
                return;

            int min = Mathf.Max(0, Mathf.Min(weapon.startingAmmoMin, weapon.startingAmmoMax));
            int max = Mathf.Max(0, Mathf.Max(weapon.startingAmmoMin, weapon.startingAmmoMax));
            if (max <= 0)
                return;

            int granted = UnityEngine.Random.Range(min, max + 1);
            entry.loaded = Mathf.Clamp(granted, 0, Mathf.Max(1, weapon.magazineSize));
        }

        private SlotAmmo GetOrCreateSlot(int hotbarSlot, ItemData weapon)
        {
            if (!slotAmmo.TryGetValue(hotbarSlot, out SlotAmmo entry))
            {
                ItemData defaultAmmo = ResolveDefaultAmmoItemForWeapon(weapon);
                entry = new SlotAmmo
                {
                    loaded = 0,
                    loadedType = defaultAmmo != null ? defaultAmmo.ammoType : AmmoType.Gunpowder,
                    loadedItem = defaultAmmo
                };
                slotAmmo[hotbarSlot] = entry;
            }
            else if (entry.loadedItem == null)
            {
                ItemData defaultAmmo = ResolveDefaultAmmoItemForWeapon(weapon);
                entry.loadedItem = defaultAmmo;
                entry.loadedType = defaultAmmo != null ? defaultAmmo.ammoType : AmmoType.Gunpowder;
            }

            return entry;
        }

        private static ItemData ResolveDefaultAmmoItemForWeapon(ItemData weapon)
        {
            if (weapon != null && weapon.isMiningTool)
            {
                if (weapon.defaultAmmoItem != null && weapon.defaultAmmoItem.CountsAsAmmo)
                    return weapon.defaultAmmoItem;

                ItemData[] all = ItemRegistry.GetAllItems();
                for (int i = 0; i < all.Length; i++)
                {
                    ItemData item = all[i];
                    if (item != null && item.CountsAsAmmo && item.isContinuousLaser && item.ammoType == AmmoType.Laser)
                        return item;
                }

                for (int i = 0; i < all.Length; i++)
                {
                    ItemData item = all[i];
                    if (item != null && item.CountsAsAmmo &&
                        (string.Equals(item.itemName, "Laser Tool", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.name, "Laser Tool", StringComparison.OrdinalIgnoreCase)))
                        return item;
                }
            }

            return ResolveStandardAmmoItem(weapon);
        }

        /// <summary>
        /// Player weapons always prefer Standard ammo for defaults. Falls back to the weapon's
        /// defaultAmmoItem, then any Gunpowder ammo in the registry.
        /// </summary>
        public static ItemData ResolveStandardAmmoItem(ItemData weapon = null)
        {
            if (cachedStandardAmmo != null)
                return cachedStandardAmmo;

            ItemData[] all = ItemRegistry.GetAllItems();
            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item == null || !item.CountsAsAmmo)
                    continue;

                if (string.Equals(item.itemName, StandardAmmoItemName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.name, StandardAmmoItemName, StringComparison.OrdinalIgnoreCase))
                {
                    cachedStandardAmmo = item;
                    return cachedStandardAmmo;
                }
            }

            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item != null && item.CountsAsAmmo && item.ammoType == AmmoType.Gunpowder)
                {
                    cachedStandardAmmo = item;
                    return cachedStandardAmmo;
                }
            }

            if (weapon != null && weapon.defaultAmmoItem != null && weapon.defaultAmmoItem.CountsAsAmmo)
                return weapon.defaultAmmoItem;

            return null;
        }

        /// <summary>
        /// Auto-refill on empty: only pulls inventory ammo matching the type already loaded (or
        /// defaulted) for this slot. It deliberately does NOT fall back to a different compatible
        /// type — switching ammo types is an explicit "Equip Ammo To" action.
        /// </summary>
        private bool TryRefillFromInventory(ItemData weapon, SlotAmmo entry)
        {
            if (inventory == null || weapon == null)
                return false;

            if (IsInfiniteAmmoType(entry.loadedType, entry.loadedItem))
            {
                entry.loaded = Mathf.Max(entry.loaded, weapon.magazineSize);
                return entry.loaded > 0;
            }

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                int needed = Mathf.Max(0, weapon.magazineSize - entry.loaded);
                if (needed <= 0)
                    break;

                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (slot.item.ammoType != entry.loadedType)
                    continue;

                if (!AmmoItemsCompatibleForReserve(entry.loadedItem, slot.item))
                    continue;

                int take = Mathf.Min(needed, slot.amount);
                entry.loadedType = slot.item.ammoType;
                entry.loadedItem = slot.item;
                entry.loaded += take;
                inventory.RemoveItemAt(i, take);
            }

            return entry.loaded > 0;
        }

        private void HandleInventoryChanged()
        {
            if (equipment == null)
                return;

            equipment.ForEachWeaponHotbarSlot(slot =>
            {
                ItemData weapon = equipment.GetHotbarItem(slot);
                if (weapon != null && weapon.IsRangedWeapon)
                {
                    EnsureWeaponInitialized(slot, weapon);
                    return;
                }

                slotWeaponIdentity.Remove(slot);
            });
        }

        private int ResolveActiveWeaponHotbarSlot()
        {
            if (equipment == null)
                return 0;

            if (equipment.IsWeaponHotbarSlot(equipment.SelectedHotbarSlot))
                return equipment.SelectedHotbarSlot;

            return equipment.ActiveWeaponHotbarSlot;
        }

        private void NotifyChanged()
        {
            OnAmmoChanged?.Invoke();
        }

        /// <summary>
        /// Player magazines are finite — reserve comes from inventory only.
        /// Companions/enemies do not use this path; they keep Invector isInfinityAmmo separately.
        /// </summary>
        public static bool IsInfiniteAmmoType(AmmoType ammoType, ItemData ammoItem = null)
        {
            // Continuous Laser Tool cells power mining tools indefinitely once loaded.
            return ammoItem != null && ammoItem.isContinuousLaser;
        }

        public bool IsInfiniteAmmoForSlot(int weaponHotbarSlot)
        {
            // Require at least one loaded cell so mining stays empty until Laser Tool is picked up / equipped.
            if (!slotAmmo.TryGetValue(weaponHotbarSlot, out SlotAmmo entry) || entry.loaded <= 0)
                return false;

            return IsInfiniteAmmoType(entry.loadedType, entry.loadedItem);
        }
    }
}
