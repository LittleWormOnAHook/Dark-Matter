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

        public int GetActiveLoadedAmmo()
        {
            if (equipment == null)
                return 0;

            return GetLoadedAmmo(equipment.ActiveWeaponHotbarSlot);
        }

        public int GetReserveAmmoCount(ItemData weapon)
        {
            if (weapon == null || inventory == null)
                return 0;

            int reserve = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (!weapon.AcceptsAmmoType(slot.item.ammoType))
                    continue;

                reserve += slot.amount;
            }

            return reserve;
        }

        public bool TryConsumeActiveRound()
        {
            if (equipment == null)
                return false;

            int slot = ResolveActiveWeaponHotbarSlot();
            ItemData weapon = equipment.GetHotbarItem(slot);
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            SlotAmmo entry = GetOrCreateSlot(slot, weapon);
            if (entry.loaded > 0)
            {
                entry.loaded--;
                NotifyChanged();
                return true;
            }

            if (!TryRefillFromInventory(weapon, entry))
                return false;

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
                if (entry.loadedType != ammoItem.ammoType && entry.loaded > 0)
                    return;

                entry.loadedType = ammoItem.ammoType;
                int space = Mathf.Max(0, weapon.magazineSize - entry.loaded);
                int add = Mathf.Min(space, remaining);
                entry.loaded += add;
                remaining -= add;
            });

            if (remaining > 0 && inventory != null)
                inventory.AddItem(ammoItem, remaining, autoCreditAmmoToWeapons: false);

            NotifyChanged();
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

        private bool TryRefillFromInventory(ItemData weapon, SlotAmmo entry)
        {
            if (inventory == null || weapon == null)
                return false;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (!weapon.AcceptsAmmoType(slot.item.ammoType))
                    continue;

                int needed = Mathf.Max(0, weapon.magazineSize - entry.loaded);
                if (needed <= 0)
                    return true;

                int take = Mathf.Min(needed, slot.amount);
                entry.loadedType = slot.item.ammoType;
                entry.loaded += take;
                inventory.RemoveItemAt(i, take);
                return entry.loaded > 0;
            }

            return entry.loaded > 0;
        }

        private SlotAmmo GetOrCreateSlot(int hotbarSlot, ItemData weapon)
        {
            if (!slotAmmo.TryGetValue(hotbarSlot, out SlotAmmo entry))
            {
                entry = new SlotAmmo
                {
                    loaded = 0,
                    loadedType = weapon.defaultAmmoType
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
    }
}
