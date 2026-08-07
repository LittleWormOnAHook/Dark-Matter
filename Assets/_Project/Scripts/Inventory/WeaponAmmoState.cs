using System;
using System.Collections.Generic;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.Inventory
{
    /// <summary>
    /// Tracks loaded ammo per weapon hotbar slot and credits ammo pickups to compatible weapons.
    /// Mining tools use a 0–100% charge tank refilled by Plasma Fuel (not discrete ammo magazines).
    /// </summary>
    public class WeaponAmmoState : MonoBehaviour
    {
        public const int MiningChargeCapacity = 100;
        private const string PlasmaFuelItemName = "Plasma Fuel";
        private const string StandardAmmoItemName = "Standard";

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

        private static ItemData cachedStandardAmmo;
        private static ItemData cachedPlasmaFuel;

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

        /// <summary>Mining charge as 0–100 integer percent for the given weapon hotbar slot.</summary>
        public int GetMiningChargePercent(int weaponHotbarSlot)
        {
            return Mathf.Clamp(GetLoadedAmmo(weaponHotbarSlot), 0, MiningChargeCapacity);
        }

        /// <summary>Active mining tool charge as 0–1.</summary>
        public float GetActiveMiningCharge01()
        {
            if (equipment == null)
                return 0f;

            return GetMiningChargePercent(equipment.ActiveWeaponHotbarSlot) / (float)MiningChargeCapacity;
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
        /// Magazine / charge capacity. Mining tools always use a 0–100% tank.
        /// </summary>
        public static int GetMagazineCapacity(ItemData weapon)
        {
            if (weapon != null && weapon.isMiningTool)
                return MiningChargeCapacity;

            return weapon != null ? Mathf.Max(1, weapon.magazineSize) : 1;
        }

        /// <summary>
        /// Reserve ammo count for a specific weapon hotbar slot: only counts inventory stacks
        /// matching that slot's currently loaded ammo type (not just any type the weapon could
        /// accept), so the HUD doesn't advertise reserve rounds that won't actually auto-refill.
        /// Mining tools report Plasma Fuel count instead.
        /// </summary>
        public int GetReserveAmmoCount(int weaponHotbarSlot)
        {
            if (inventory == null)
                return 0;

            ItemData weapon = equipment != null ? equipment.GetHotbarItem(weaponHotbarSlot) : null;
            if (weapon != null && weapon.isMiningTool)
                return CountPlasmaFuelInInventory();

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

        public int CountPlasmaFuelInInventory()
        {
            ItemData plasma = ResolvePlasmaFuelItem();
            return plasma != null && inventory != null ? inventory.CountItem(plasma) : 0;
        }

        private static bool AmmoItemsCompatibleForReserve(ItemData loadedItem, ItemData candidate)
        {
            if (candidate == null)
                return false;

            // Keep continuous laser cells separate from pulse Laser Pistol Ammo.
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
            if (entry.loaded <= 0)
                return false;

            entry.loaded--;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Consumes one Plasma Fuel from inventory and restores mining charge % on the given slot.
        /// Returns true if any charge was added.
        /// </summary>
        public bool TryReloadMiningWithPlasmaFuel(int weaponHotbarSlot)
        {
            if (equipment == null || inventory == null)
                return false;

            ItemData weapon = equipment.GetHotbarItem(weaponHotbarSlot);
            if (weapon == null || !weapon.isMiningTool)
                return false;

            ItemData plasma = ResolvePlasmaFuelItem();
            if (plasma == null)
                return false;

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (entry.loaded >= MiningChargeCapacity)
                return false;

            if (inventory.CountItem(plasma) <= 0)
                return false;

            float refill = weapon.miningChargePerPlasmaFuel > 0f
                ? weapon.miningChargePerPlasmaFuel
                : 50f;

            // One reload press consumes one Plasma Fuel for a substantial % refill.
            inventory.RemoveItem(plasma, 1);
            entry.loaded = Mathf.Clamp(
                entry.loaded + Mathf.RoundToInt(refill),
                0,
                MiningChargeCapacity);
            entry.loadedItem = null;
            NotifyChanged();
            return true;
        }

        public void CreditAmmoPickup(ItemData ammoItem, int amount)
        {
            if (ammoItem == null || !ammoItem.CountsAsAmmo || amount <= 0 || equipment == null)
                return;

            // Continuous laser cells are inventory ammo only. Mining charge uses Plasma Fuel.
            if (ammoItem.isContinuousLaser)
            {
                if (inventory != null)
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

                // Mining tools never auto-fill from discrete ammo — Plasma Fuel reload only.
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
                int space = Mathf.Max(0, GetMagazineCapacity(weapon) - entry.loaded);
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

            // Mining tools are Plasma Fuel powered — discrete ammo cannot be equipped into them.
            if (weapon.isMiningTool || invSlot.item.isContinuousLaser)
                return false;

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            if (entry.loaded > 0 && entry.loadedType != invSlot.item.ammoType)
                ReturnLoadedAmmoToInventory(entry);

            entry.loadedType = invSlot.item.ammoType;
            entry.loadedItem = invSlot.item;

            int space = Mathf.Max(0, GetMagazineCapacity(weapon) - entry.loaded);
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
            if (ammoItem == null || equipment == null || ammoItem.isContinuousLaser)
                return eligible;

            equipment.ForEachWeaponHotbarSlot(hotbarSlot =>
            {
                ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                if (weapon == null || !weapon.IsRangedWeapon || weapon.isMiningTool)
                    return;

                if (!weapon.AcceptsAmmoType(ammoItem.ammoType))
                    return;

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
        /// Mining tools always start at 0% charge (Plasma Fuel reload required).
        /// </summary>
        private void ApplyFreshWeaponAmmo(ItemData weapon, SlotAmmo entry)
        {
            if (weapon != null && weapon.isMiningTool)
            {
                entry.loadedItem = null;
                entry.loadedType = AmmoType.Laser;
                entry.loaded = 0;
                return;
            }

            ItemData defaultAmmo = ResolveDefaultAmmoItemForWeapon(weapon);
            entry.loadedItem = defaultAmmo;
            entry.loadedType = defaultAmmo != null ? defaultAmmo.ammoType : AmmoType.Gunpowder;
            entry.loaded = 0;

            if (!weapon.grantRandomStartingAmmo)
                return;

            int capacity = GetMagazineCapacity(weapon);
            int min = Mathf.Max(0, Mathf.Min(weapon.startingAmmoMin, weapon.startingAmmoMax));
            int max = Mathf.Max(0, Mathf.Max(weapon.startingAmmoMin, weapon.startingAmmoMax));
            if (max <= 0)
                return;

            int granted = UnityEngine.Random.Range(min, max + 1);
            entry.loaded = Mathf.Clamp(granted, 0, capacity);
        }

        private SlotAmmo GetOrCreateSlot(int hotbarSlot, ItemData weapon)
        {
            if (!slotAmmo.TryGetValue(hotbarSlot, out SlotAmmo entry))
            {
                if (weapon != null && weapon.isMiningTool)
                {
                    entry = new SlotAmmo
                    {
                        loaded = 0,
                        loadedType = AmmoType.Laser,
                        loadedItem = null
                    };
                }
                else
                {
                    ItemData defaultAmmo = ResolveDefaultAmmoItemForWeapon(weapon);
                    entry = new SlotAmmo
                    {
                        loaded = 0,
                        loadedType = defaultAmmo != null ? defaultAmmo.ammoType : AmmoType.Gunpowder,
                        loadedItem = defaultAmmo
                    };
                }

                slotAmmo[hotbarSlot] = entry;
            }
            else if (entry.loadedItem == null && (weapon == null || !weapon.isMiningTool))
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
                return null;

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

        public static ItemData ResolvePlasmaFuelItem()
        {
            if (cachedPlasmaFuel != null)
                return cachedPlasmaFuel;

            cachedPlasmaFuel = ItemRegistry.Resolve(PlasmaFuelItemName);
            if (cachedPlasmaFuel != null)
                return cachedPlasmaFuel;

            ItemData[] all = ItemRegistry.GetAllItems();
            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item == null)
                    continue;

                if (string.Equals(item.itemName, PlasmaFuelItemName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.name, PlasmaFuelItemName, StringComparison.OrdinalIgnoreCase))
                {
                    cachedPlasmaFuel = item;
                    return cachedPlasmaFuel;
                }
            }

            return null;
        }

        /// <summary>
        /// Auto-refill on empty: only pulls inventory ammo matching the type already loaded (or
        /// defaulted) for this slot. It deliberately does NOT fall back to a different compatible
        /// type — switching ammo types is an explicit "Equip Ammo To" action.
        /// Mining tools refill from Plasma Fuel instead.
        /// </summary>
        private bool TryRefillFromInventory(ItemData weapon, SlotAmmo entry)
        {
            if (inventory == null || weapon == null)
                return false;

            if (weapon.isMiningTool)
            {
                // Plasma Fuel is only consumed on explicit reload (R) — never auto-siphoned
                // when inventory changes or the tool is first equipped empty.
                return false;
            }

            int capacity = GetMagazineCapacity(weapon);
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                int needed = Mathf.Max(0, capacity - entry.loaded);
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
                    // Only init freshly assigned weapons. Do NOT silently refill an empty magazine
                    // whenever any item is picked up — that races with reload audio and skips R.
                    // Empty→full refill belongs to EnsureWeaponInitialized after a real reload finish,
                    // or CreditAmmoPickup for world ammo stacks.
                    bool isFreshWeaponInSlot = !slotWeaponIdentity.TryGetValue(slot, out ItemData previousWeapon)
                        || previousWeapon != weapon;
                    if (isFreshWeaponInSlot)
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
        /// Mining tools are never infinite; they drain a 0–100% Plasma Fuel charge tank.
        /// </summary>
        public static bool IsInfiniteAmmoType(AmmoType ammoType, ItemData ammoItem = null)
        {
            return false;
        }

        public bool IsInfiniteAmmoForSlot(int weaponHotbarSlot)
        {
            return false;
        }
    }
}
