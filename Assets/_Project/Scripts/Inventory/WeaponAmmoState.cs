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
        private EquipmentController equipment;
        private InventorySystem inventory;

        public event Action OnAmmoChanged;

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
            int reserve = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (slot.item.ammoType != loadedType)
                    continue;

                reserve += slot.amount;
            }

            return reserve;
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

            int remaining = amount;
            equipment.ForEachWeaponHotbarSlot(hotbarSlot =>
            {
                if (remaining <= 0)
                    return;

                ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                if (weapon == null || !weapon.AcceptsAmmoType(ammoItem.ammoType))
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

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (entry.loaded > 0 && entry.loadedType != invSlot.item.ammoType)
                ReturnLoadedAmmoToInventory(entry);

            entry.loadedType = invSlot.item.ammoType;
            entry.loadedItem = invSlot.item;

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
                if (weapon != null && weapon.IsRangedWeapon && weapon.AcceptsAmmoType(ammoItem.ammoType))
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

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (entry.loaded > 0)
                return;

            TryRefillFromInventory(weapon, entry);
        }

        /// <summary>
        /// Auto-refill on empty: only pulls inventory ammo matching the type already loaded (or
        /// defaulted) for this slot. It deliberately does NOT fall back to a different compatible
        /// type — running dry on Plasma should leave the weapon empty, not silently swap the player
        /// onto whatever Gunpowder happens to be sitting in their inventory. Switching ammo types is
        /// an explicit "Equip Ammo To" action (TryEquipAmmoToWeaponSlot), never an automatic one.
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

            // Walk every matching-type stack (not just the first one found) so a full magazine
            // refill isn't short-changed just because the player's reserve happens to be split
            // across multiple inventory slots.
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

                int take = Mathf.Min(needed, slot.amount);
                entry.loadedType = slot.item.ammoType;
                entry.loadedItem = slot.item;
                entry.loaded += take;
                inventory.RemoveItemAt(i, take);
            }

            return entry.loaded > 0;
        }

        private SlotAmmo GetOrCreateSlot(int hotbarSlot, ItemData weapon)
        {
            if (!slotAmmo.TryGetValue(hotbarSlot, out SlotAmmo entry))
            {
                // Seed from the weapon's own defaultAmmoItem (if set) so "default ammo" resolves a
                // real ItemData from the start and flows through the exact same projectile/VFX/audio
                // path as any explicitly equipped ammo, rather than relying on ammoItem == null
                // falling back to (easy to leave unset) weapon-level VFX fields.
                entry = new SlotAmmo
                {
                    loaded = 0,
                    loadedType = weapon.defaultAmmoItem != null ? weapon.defaultAmmoItem.ammoType : weapon.defaultAmmoType,
                    loadedItem = weapon.defaultAmmoItem
                };
                slotAmmo[hotbarSlot] = entry;
            }

            return entry;
        }

        private void HandleInventoryChanged()
        {
            if (equipment == null)
                return;

            equipment.ForEachWeaponHotbarSlot(slot =>
            {
                ItemData weapon = equipment.GetHotbarItem(slot);
                if (weapon != null && weapon.IsRangedWeapon)
                    EnsureWeaponInitialized(slot, weapon);
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
            return false;
        }

        public bool IsInfiniteAmmoForSlot(int weaponHotbarSlot)
        {
            return IsInfiniteAmmoType(GetLoadedAmmoType(weaponHotbarSlot), GetLoadedAmmoItem(weaponHotbarSlot));
        }
    }
}
