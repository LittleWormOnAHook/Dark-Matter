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

        // stamp: ammo-conserve 1006. Magazines are never "parked" in memory any more: swapping ammo
        // type unloads the unspent rounds back into real inventory / Hot Cross stacks (see
        // ReturnRoundsToInventory), so rounds can never become invisible or be destroyed.
        [Serializable]
        private class SlotAmmo
        {
            public int loaded;
            public AmmoType loadedType = AmmoType.Gunpowder;
            /// <summary>Actual ammo ItemData asset currently loaded, so VFX/status-effect data can be
            /// resolved per specific ammo variant rather than just the shared enum type.</summary>
            public ItemData loadedItem;
            /// <summary>Absolute inventory slot the loaded rounds were last taken from (-1 = unknown).
            /// Unloading prefers this slot so ammo goes back where the player had it.</summary>
            public int sourceSlotIndex = -1;
        }

        private readonly Dictionary<int, SlotAmmo> slotAmmo = new Dictionary<int, SlotAmmo>(4);
        private readonly Dictionary<int, ItemData> slotWeaponIdentity = new Dictionary<int, ItemData>(4);
        private readonly HashSet<ItemData> startingAmmoGrantedWeapons = new HashSet<ItemData>();
        private bool handlingInventoryChange;
        private bool pendingInventoryNotify;
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

        /// <summary>Absolute inventory slot the loaded rounds were last taken from (-1 = unknown).
        /// Used by the X ammo cycle to know where the loaded type sits in the Hot Cross / inventory order.</summary>
        public int GetLoadedAmmoSourceSlot(int weaponHotbarSlot)
        {
            return slotAmmo.TryGetValue(weaponHotbarSlot, out SlotAmmo entry) ? entry.sourceSlotIndex : -1;
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
            return GetMagazineCapacity(weapon, null);
        }

        public static int GetMagazineCapacity(ItemData weapon, ItemData ammo)
        {
            if (weapon != null && weapon.isMiningTool)
                return MiningChargeCapacity;

            return Mathf.Max(1, DMRangedAmmoStats.ResolveMagazineSize(weapon, ammo));
        }

        /// <summary>
        /// True for Hot Cross utility slots (keys 5-0). Reload and ammo loading only ever pull
        /// rounds from these slots, never from the inventory grid. stamp: reload-hotcross-only 1006
        /// </summary>
        public bool IsHotCrossUtilityIndex(int absoluteSlotIndex)
        {
            if (inventory == null || equipment == null || !inventory.IsHotbarIndex(absoluteSlotIndex))
                return false;

            return equipment.IsUtilityHotbarSlot(absoluteSlotIndex - inventory.HotbarStartIndex);
        }

        /// <summary>Name of the ammo loaded (or last loaded) in this weapon slot, for toasts.</summary>
        public string GetLoadedAmmoDisplayName(int weaponHotbarSlot)
        {
            ItemData item = GetLoadedAmmoItem(weaponHotbarSlot);
            if (item != null && !string.IsNullOrWhiteSpace(item.itemName))
                return item.itemName;

            AmmoType type = GetLoadedAmmoType(weaponHotbarSlot);
            return type == AmmoType.Gunpowder ? "Standard" : type.ToString();
        }

        /// <summary>
        /// Reserve ammo count for a specific weapon hotbar slot: only counts Hot Cross utility stacks
        /// (5-0) matching that slot's currently loaded ammo type. Inventory-grid ammo is not reserve:
        /// R reload never pulls from the grid. Mining tools report Plasma Fuel count instead.
        /// </summary>
        public int GetReserveAmmoCount(int weaponHotbarSlot)
        {
            if (inventory == null)
                return 0;

            ItemData weapon = equipment != null ? equipment.GetHotbarItem(weaponHotbarSlot) : null;
            if (weapon != null && weapon.isMiningTool)
                return CountPlasmaFuelInInventory();

            // Never use ItemData.ammoType on a weapon — Survival Rifle leftover is Plasma (1).
            // Empty mag still counts the type that was just loaded. Switching types is Equip Ammo To.
            AmmoType loadedType = GetLoadedAmmoType(weaponHotbarSlot);
            ItemData loadedItem = GetLoadedAmmoItem(weaponHotbarSlot);
            if (loadedItem == null)
                loadedItem = FindFirstInventoryAmmo(loadedType);

            int reserve = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                if (!IsHotCrossUtilityIndex(i))
                    continue;

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
                // Partial mag keeps its type — swap only via explicit equip / empty-mag pickup.
                if (entry.loaded > 0 && entry.loadedType != ammoItem.ammoType)
                    return;

                if (entry.loaded <= 0 && entry.loadedType != ammoItem.ammoType)
                {
                    if (!weapon.AcceptsAmmoType(ammoItem.ammoType))
                        return;

                    entry.loadedType = ammoItem.ammoType;
                    entry.loadedItem = ammoItem;
                }

                entry.loadedType = ammoItem.ammoType;
                entry.loadedItem = ammoItem;
                int space = Mathf.Max(0, GetMagazineCapacity(weapon, ammoItem) - entry.loaded);
                int add = Mathf.Min(space, remaining);
                entry.loaded += add;
                remaining -= add;
            });

            if (remaining > 0 && inventory != null)
                inventory.AddItem(ammoItem, remaining, autoCreditAmmoToWeapons: false);

            NotifyChanged();
        }

        /// <summary>
        /// Pulls matching reserve ammo from inventory up to magazine capacity (reload finish, etc.).
        /// Does not run on first weapon assign — mags stay empty until pickup, explicit equip, or reload.
        /// </summary>
        public bool RefillMagazineFromInventory(int weaponHotbarSlot, ItemData weapon)
        {
            if (weapon == null || !weapon.IsRangedWeapon || weapon.isMiningTool)
                return false;

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            int capacity = GetMagazineCapacity(weapon, entry.loadedItem);
            if (entry.loaded >= capacity)
                return false;

            int before = entry.loaded;
            TryRefillFromInventory(weapon, entry);
            if (entry.loaded <= before)
                return false;

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Explicit player-driven equip: loads ammo from a specific inventory stack into a specific
        /// weapon's hotbar slot (X cycle, Hot Cross hold, inventory "Equip Ammo To"). Only what fits the
        /// magazine is taken; the rest stays in the stack. Swapping to a different ammo first unloads the
        /// current magazine back into the inventory (origin slot, same stack, free Hot Cross slot, free
        /// inventory slot, else world drop). If the rounds have nowhere to go the swap is refused and
        /// they stay in the magazine. Ammo is never destroyed. stamp: ammo-conserve 1006
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

            ItemData ammo = invSlot.item;
            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);
            int capacity = GetMagazineCapacity(weapon, ammo);

            // Same ammo (or an empty magazine): top up with only what fits; the rest stays in the slot.
            if (entry.loaded <= 0 || IsSameLoadedAmmo(entry, ammo))
            {
                int current = Mathf.Max(0, entry.loaded);
                int take = Mathf.Min(Mathf.Max(0, capacity - current), invSlot.amount);
                if (take <= 0)
                    return false;

                entry.loadedType = ammo.ammoType;
                entry.loadedItem = ammo;
                entry.loaded = current + take;
                entry.sourceSlotIndex = inventorySlotIndex;
                inventory.RemoveItemAt(inventorySlotIndex, take);
                NotifyChanged();
                return true;
            }

            // Different ammo: unload the current magazine into the inventory first, then load the new type.
            ItemData oldAmmo = ResolveLoadedItemForReturn(entry);
            if (oldAmmo == null)
            {
                Project.UI.PickupToastUI.Show("Cannot unload the current magazine");
                return false;
            }

            int takeNew = Mathf.Min(capacity, invSlot.amount);
            if (takeNew <= 0)
                return false;

            int leftover = ReturnRoundsToInventory(oldAmmo, entry.loaded, entry.sourceSlotIndex);

            // Inventory full but the new stack is fully loaded: the old rounds take its slot (a clean swap).
            bool swapIntoSource = leftover > 0
                && takeNew >= invSlot.amount
                && leftover <= Mathf.Max(1, oldAmmo.maxStack)
                && inventory.CanAcceptItemAt(inventorySlotIndex, oldAmmo);

            if (leftover > 0 && !swapIntoSource && Application.isPlaying && inventory.TrySpawnWorldDrop(oldAmmo, leftover))
            {
                Project.UI.PickupToastUI.Show($"Inventory full: dropped {leftover} {AmmoDisplayName(oldAmmo)}");
                leftover = 0;
            }

            if (leftover > 0 && !swapIntoSource)
            {
                // Nowhere to put them: keep the remaining old rounds in the magazine and refuse the swap.
                entry.loaded = leftover;
                inventory.NotifyChanged();
                NotifyChanged();
                Project.UI.PickupToastUI.Show($"No room to unload {AmmoDisplayName(oldAmmo)} ({leftover} kept in magazine)");
                return false;
            }

            invSlot.amount -= takeNew;
            if (invSlot.amount <= 0)
            {
                invSlot.item = null;
                invSlot.amount = 0;
            }

            if (swapIntoSource)
            {
                invSlot.item = oldAmmo;
                invSlot.amount = leftover;
            }

            entry.loadedType = ammo.ammoType;
            entry.loadedItem = ammo;
            entry.loaded = takeNew;
            entry.sourceSlotIndex = inventorySlotIndex;
            inventory.NotifyChanged();
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

        private static bool IsSameLoadedAmmo(SlotAmmo entry, ItemData ammo)
        {
            if (entry == null || ammo == null)
                return false;

            if (entry.loadedItem != null)
                return entry.loadedItem == ammo;

            return entry.loadedType == ammo.ammoType && !ammo.isContinuousLaser;
        }

        private static string AmmoDisplayName(ItemData item)
        {
            if (item == null)
                return "ammo";

            return !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : item.name;
        }

        /// <summary>The ammo item the magazine's rounds should go back into the inventory as.</summary>
        private ItemData ResolveLoadedItemForReturn(SlotAmmo entry)
        {
            if (entry == null)
                return null;

            if (entry.loadedItem != null)
                return entry.loadedItem;

            ItemData sameType = FindFirstInventoryAmmo(entry.loadedType);
            if (sameType != null)
                return sameType;

            ItemData[] all = ItemRegistry.GetAllItems();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item != null && item.CountsAsAmmo && !item.isContinuousLaser && item.ammoType == entry.loadedType)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Puts unloaded rounds back as real stacks: (1) the slot they came from if it is empty or still
        /// holds that ammo, (2) any existing stack of that ammo, (3) the first free Hot Cross utility
        /// slot (5-0) so X can see it, (4) any free unlocked inventory slot. Mutates slots without
        /// raising OnInventoryChanged; callers notify once. Returns rounds that did not fit.
        /// </summary>
        private int ReturnRoundsToInventory(ItemData item, int amount, int preferredIndex)
        {
            if (amount <= 0)
                return 0;

            if (inventory == null || item == null)
                return amount;

            int remaining = amount;
            int maxStack = Mathf.Max(1, item.maxStack);
            int count = inventory.slots.Count;

            if (preferredIndex >= 0 && preferredIndex < count)
                remaining = AddRoundsToSlot(preferredIndex, item, remaining, maxStack, requireEmpty: false);

            for (int i = 0; i < count && remaining > 0; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot != null && !slot.IsEmpty && slot.item == item)
                    remaining = AddRoundsToSlot(i, item, remaining, maxStack, requireEmpty: false);
            }

            if (equipment != null)
            {
                for (int local = 0; local < inventory.hotbarSize && remaining > 0; local++)
                {
                    if (!equipment.IsUtilityHotbarSlot(local))
                        continue;

                    int absolute = inventory.HotbarStartIndex + local;
                    if (absolute < count)
                        remaining = AddRoundsToSlot(absolute, item, remaining, maxStack, requireEmpty: true);
                }
            }

            for (int i = 0; i < inventory.inventorySize && i < count && remaining > 0; i++)
                remaining = AddRoundsToSlot(i, item, remaining, maxStack, requireEmpty: true);

            return remaining;
        }

        private int AddRoundsToSlot(int index, ItemData item, int remaining, int maxStack, bool requireEmpty)
        {
            if (remaining <= 0)
                return 0;

            InventorySystem.InventorySlot slot = inventory.slots[index];
            if (slot == null)
                return remaining;

            bool empty = slot.IsEmpty;
            if (requireEmpty && !empty)
                return remaining;
            if (!empty && slot.item != item)
                return remaining;
            if (!inventory.CanAcceptItemAt(index, item))
                return remaining;

            int current = empty ? 0 : slot.amount;
            int add = Mathf.Min(remaining, maxStack - current);
            if (add <= 0)
                return remaining;

            slot.item = item;
            slot.amount = current + add;
            return remaining - add;
        }

        /// <summary>
        /// A ranged weapon left this hotbar slot (moved, unequipped or replaced): its unspent rounds go
        /// back into the inventory instead of being wiped by the next weapon's fresh magazine.
        /// </summary>
        private bool ReturnMagazineOfRemovedWeapon(int hotbarSlot, ItemData previousWeapon)
        {
            if (previousWeapon == null || previousWeapon.isMiningTool)
                return false;

            if (!slotAmmo.TryGetValue(hotbarSlot, out SlotAmmo entry) || entry.loaded <= 0)
                return false;

            ItemData ammo = ResolveLoadedItemForReturn(entry);
            if (ammo == null)
                return false;

            int leftover = ReturnRoundsToInventory(ammo, entry.loaded, entry.sourceSlotIndex);
            if (leftover > 0 && Application.isPlaying && inventory.TrySpawnWorldDrop(ammo, leftover))
            {
                Project.UI.PickupToastUI.Show($"Inventory full: dropped {leftover} {AmmoDisplayName(ammo)}");
                leftover = 0;
            }

            if (leftover > 0)
                Debug.LogWarning($"[WeaponAmmoState] No room to unload {leftover} {AmmoDisplayName(ammo)} from removed weapon {previousWeapon.name}.");

            entry.loaded = 0;
            entry.sourceSlotIndex = -1;
            NotifyChanged();
            return true;
        }

        private void QueueInventoryNotify()
        {
            if (inventory == null)
                return;

            if (handlingInventoryChange)
            {
                pendingInventoryNotify = true;
                return;
            }

            inventory.NotifyChanged();
        }

        public void EnsureWeaponInitialized(int weaponHotbarSlot, ItemData weapon)
        {
            if (weapon == null || !weapon.IsRangedWeapon)
                return;

            bool hadIdentity = slotWeaponIdentity.TryGetValue(weaponHotbarSlot, out ItemData previousWeapon);
            bool isFreshWeaponInSlot = !hadIdentity || previousWeapon != weapon;

            if (isFreshWeaponInSlot)
            {
                bool returnedRounds = hadIdentity && ReturnMagazineOfRemovedWeapon(weaponHotbarSlot, previousWeapon);
                SlotAmmo freshEntry = GetOrCreateSlot(weaponHotbarSlot, weapon);
                slotWeaponIdentity[weaponHotbarSlot] = weapon;
                ApplyFreshWeaponAmmo(weapon, freshEntry);
                NotifyChanged();
                if (returnedRounds)
                    QueueInventoryNotify();
                return;
            }

            SlotAmmo entry = GetOrCreateSlot(weaponHotbarSlot, weapon);

            if (entry.loaded > 0)
                return;

            RefillMagazineFromInventory(weaponHotbarSlot, weapon);
        }

        /// <summary>
        /// First equip/pickup for a weapon slot: either Empty 0/0, or a random Standard mag load
        /// when <see cref="ItemData.grantRandomStartingAmmo"/> is enabled.
        /// Mining tools always start at 0% charge (Plasma Fuel reload required).
        /// </summary>
        private void ApplyFreshWeaponAmmo(ItemData weapon, SlotAmmo entry)
        {
            entry.sourceSlotIndex = -1;

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

            // Unloading now returns rounds to the inventory, so only grant the random starting mag once
            // per weapon item per session (moving a weapon between slots must not mint ammo).
            if (!startingAmmoGrantedWeapons.Add(weapon))
                return;

            int capacity = GetMagazineCapacity(weapon, defaultAmmo);
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
                // Keep the type already on this slot. Do not snap back to Standard on empty mag.
                ItemData sameType = FindFirstInventoryAmmo(entry.loadedType);
                if (sameType != null)
                    entry.loadedItem = sameType;
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

            int capacity = GetMagazineCapacity(weapon, entry.loadedItem);
            int before = entry.loaded;

            if (entry.loadedItem == null)
            {
                ItemData sameType = FindFirstInventoryAmmo(entry.loadedType);
                if (sameType != null)
                    entry.loadedItem = sameType;
            }

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                int needed = Mathf.Max(0, capacity - entry.loaded);
                if (needed <= 0)
                    break;

                // Reload refills only from Hot Cross utility slots (5-0), never the inventory grid.
                if (!IsHotCrossUtilityIndex(i))
                    continue;

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
                entry.sourceSlotIndex = i;
                inventory.RemoveItemAt(i, take);
            }

            return entry.loaded > before;
        }

        private ItemData FindFirstInventoryAmmo(AmmoType type)
        {
            if (inventory == null)
                return null;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;
                if (slot.item.ammoType != type)
                    continue;
                return slot.item;
            }

            return null;
        }

        private void HandleInventoryChanged()
        {
            if (equipment == null || handlingInventoryChange)
                return;

            handlingInventoryChange = true;
            try
            {
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

                    // Ranged weapon removed from this slot: unload its magazine into the inventory first.
                    if (slotWeaponIdentity.TryGetValue(slot, out ItemData removedWeapon)
                        && ReturnMagazineOfRemovedWeapon(slot, removedWeapon))
                    {
                        pendingInventoryNotify = true;
                    }

                    slotWeaponIdentity.Remove(slot);
                });
            }
            finally
            {
                handlingInventoryChange = false;
            }

            if (pendingInventoryNotify)
            {
                pendingInventoryNotify = false;
                inventory?.NotifyChanged();
            }
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
