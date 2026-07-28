using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.UI;
using Project.Vehicles;
using UnityEngine;

namespace Project.Inventory
{
    public class InventoryItemActions : MonoBehaviour
    {
        /// <summary>One weapon a right-clicked ammo stack can be equipped to, for the "Equip Ammo To" flyout.</summary>
        public readonly struct AmmoEquipOption
        {
            public readonly int WeaponHotbarSlot;
            public readonly string WeaponLabel;

            public AmmoEquipOption(int weaponHotbarSlot, string weaponLabel)
            {
                WeaponHotbarSlot = weaponHotbarSlot;
                WeaponLabel = weaponLabel;
            }
        }

        private InventorySystem inventory;
        private EquipmentController equipment;
        private WeaponAmmoState ammoState;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            equipment = GetComponent<EquipmentController>();
            ammoState = GetComponent<WeaponAmmoState>();
        }

        /// <summary>Installs an Increase Storage Module from inventory, unlocking the next row.</summary>
        public bool TryInstallStorageModule(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !item.IsInventoryStorageModule)
                return false;

            if (!inventory.CanUnlockNextStorageRow())
            {
                PickupToastUI.Show("Inventory storage is fully expanded.");
                return false;
            }

            if (!inventory.TryUnlockNextStorageRow())
                return false;

            inventory.RemoveItemAt(slotIndex, 1);
            PickupToastUI.Show("Storage expanded — new inventory row unlocked.");
            GameAudioManager.Instance?.PlayItemUse();
            return true;
        }

        public bool CanInstallStorageModule(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            return item != null && item.IsInventoryStorageModule && inventory.CanUnlockNextStorageRow();
        }

        public bool TryUse(int slotIndex)
        {
            if (CanInstallStorageModule(slotIndex))
                return TryInstallStorageModule(slotIndex);

            if (inventory == null || !inventory.UseItemAt(slotIndex))
                return false;

            GameAudioManager.Instance?.PlayItemUse();
            return true;
        }

        public bool TryEquip(int slotIndex)
        {
            if (equipment == null || inventory == null)
                return false;

            InventorySystem.InventorySlot slot = inventory.slots[slotIndex];
            if (slot.IsEmpty || slot.item == null || !slot.item.IsEquippable)
                return false;

            bool isOpticsTool = slot.item.IsOpticsTool;

            if (!equipment.TryEquipItemFromSlot(slotIndex))
                return false;

            if (isOpticsTool)
                UiInputGuard.BlockOpticsActivationForFrames();

            GameAudioManager.Instance?.PlayItemEquip();
            return true;
        }

        public bool TryUnequip(int slotIndex)
        {
            if (equipment == null || !equipment.TryUnequipFromSlot(slotIndex))
                return false;

            GameAudioManager.Instance?.PlayItemUnequip();
            return true;
        }

        public bool TrySplit(int slotIndex)
        {
            if (inventory == null || !inventory.SplitStackAt(slotIndex))
                return false;

            GameAudioManager.Instance?.PlayItemSplit();
            return true;
        }

        public bool TryDrop(int slotIndex)
        {
            if (inventory == null || !inventory.DropItemAt(slotIndex))
                return false;

            GameAudioManager.Instance?.PlayItemDrop();
            return true;
        }

        /// <summary>Consumes one Plasma Fuel to top up a stored (not-yet-deployed) vehicle item's
        /// remembered fuel level. Right-click action for the Hovercraft inventory item.</summary>
        public bool TryRefuelVehicle(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !item.IsVehicle)
                return false;

            ItemData plasmaFuel = ItemRegistry.Resolve("Plasma Fuel");
            if (plasmaFuel == null || inventory.CountItem(plasmaFuel) <= 0)
                return false;

            inventory.RemoveItem(plasmaFuel, 1);
            HovercraftStorageState.AddStoredFuel(HovercraftStorageState.FuelPerPlasmaCell, HovercraftStorageState.DefaultMaxFuel);
            GameAudioManager.Instance?.PlayItemUse();
            return true;
        }

        public bool CanRefuelVehicle(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !item.IsVehicle)
                return false;

            if (HovercraftStorageState.StoredFuel >= HovercraftStorageState.DefaultMaxFuel - 0.01f)
                return false;

            ItemData plasmaFuel = ItemRegistry.Resolve("Plasma Fuel");
            return plasmaFuel != null && inventory.CountItem(plasmaFuel) > 0;
        }

        /// <summary>Spawns the stored vehicle near the player and removes the item from the inventory.
        /// Right-click action for the Hovercraft inventory item.</summary>
        public bool TryDeployVehicle(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !item.IsVehicle)
                return false;

            Transform playerTransform = PlayerLocator.FindPlayerObject()?.transform;
            bool deployed = HovercraftDeploymentUtility.TryDeploy(inventory, item, playerTransform, out string message);
            if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);

            if (deployed)
                GameAudioManager.Instance?.PlayItemEquip();

            return deployed;
        }

        public bool CanDeployVehicle(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            return item != null && item.IsVehicle && item.deployedPrefab != null;
        }

        /// <summary>Right-click action on Plasma Fuel: consume one cell into the drawn/equipped mining tool charge tank.</summary>
        public bool TryRefillMiningTool(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !IsPlasmaFuelItem(item))
                return false;

            if (ammoState == null || equipment == null)
            {
                PickupToastUI.Show("No mining tool equipped.");
                return false;
            }

            if (!TryResolveMiningToolHotbarSlot(out int miningSlot, out ItemData miningTool))
            {
                PickupToastUI.Show("No mining tool equipped.");
                return false;
            }

            if (ammoState.GetMiningChargePercent(miningSlot) >= WeaponAmmoState.MiningChargeCapacity)
            {
                PickupToastUI.Show("Mining tool charge is full.");
                return false;
            }

            if (inventory.CountItem(item) <= 0)
            {
                PickupToastUI.Show("No Plasma Fuel.");
                return false;
            }

            if (!ammoState.TryReloadMiningWithPlasmaFuel(miningSlot))
            {
                PickupToastUI.Show("Could not refill mining tool.");
                return false;
            }

            int charge = ammoState.GetMiningChargePercent(miningSlot);
            string toolName = miningTool != null ? miningTool.itemName : "Mining tool";
            PickupToastUI.Show($"{toolName} recharged — {charge}%");
            GameAudioManager.Instance?.PlayItemUse();
            return true;
        }

        public bool CanRefillMiningTool(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            return item != null && IsPlasmaFuelItem(item);
        }

        private bool TryResolveMiningToolHotbarSlot(out int hotbarSlot, out ItemData miningTool)
        {
            hotbarSlot = -1;
            miningTool = null;
            if (equipment == null)
                return false;

            ItemData drawn = equipment.DrawnWeaponItem;
            if (drawn != null && drawn.isMiningTool && equipment.IsWeaponHotbarSlot(equipment.ActiveWeaponHotbarSlot))
            {
                hotbarSlot = equipment.ActiveWeaponHotbarSlot;
                miningTool = drawn;
                return true;
            }

            int foundSlot = -1;
            ItemData foundTool = null;
            equipment.ForEachWeaponHotbarSlot(slot =>
            {
                if (foundSlot >= 0)
                    return;

                ItemData weapon = equipment.GetHotbarItem(slot);
                if (weapon != null && weapon.isMiningTool)
                {
                    foundSlot = slot;
                    foundTool = weapon;
                }
            });

            if (foundSlot < 0)
                return false;

            hotbarSlot = foundSlot;
            miningTool = foundTool;
            return true;
        }

        private static bool IsPlasmaFuelItem(ItemData item)
        {
            if (item == null)
                return false;

            ItemData plasma = ItemRegistry.Resolve("Plasma Fuel");
            if (plasma != null && (item == plasma || item.itemName == plasma.itemName))
                return true;

            return string.Equals(item.itemName, "Plasma Fuel", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.name, "Plasma Fuel", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool CanUse(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null)
                return false;

            if (item.IsInventoryStorageModule)
                return inventory.CanUnlockNextStorageRow();

            return item.IsConsumable;
        }

        public bool CanEquip(int slotIndex)
        {
            if (inventory == null || equipment == null)
                return false;

            ItemData item = inventory.GetItemAt(slotIndex);
            if (item == null || !item.IsEquippable)
                return false;

            if (inventory.IsToolbarIndex(slotIndex))
                return true;

            if (!inventory.IsHotbarIndex(slotIndex))
                return true;

            int hotbarIndex = slotIndex - inventory.inventorySize;
            return !equipment.IsWeaponHotbarSlot(hotbarIndex) ||
                   hotbarIndex != equipment.SelectedHotbarSlot ||
                   !equipment.HasActiveMeleeWeapon();
        }

        public bool CanUnequip(int slotIndex)
        {
            if (inventory == null || (!inventory.IsHotbarIndex(slotIndex) && !inventory.IsToolbarIndex(slotIndex)))
                return false;

            if (inventory.slots[slotIndex].IsEmpty)
                return false;

            return HasEmptyMainInventorySlot();
        }

        public bool CanSplit(int slotIndex)
        {
            if (inventory == null || slotIndex < 0 || slotIndex >= inventory.slots.Count)
                return false;

            InventorySystem.InventorySlot slot = inventory.slots[slotIndex];
            return !slot.IsEmpty && slot.amount > 1 && HasEmptySlot();
        }

        public bool CanDrop(int slotIndex)
        {
            return inventory != null && inventory.GetItemAt(slotIndex) != null;
        }

        public bool CanEquipAmmo(int slotIndex)
        {
            return GetAmmoEquipOptions(slotIndex).Count > 0;
        }

        /// <summary>Eligible ranged weapons (currently in a hotbar slot) this ammo stack could be loaded into.</summary>
        public List<AmmoEquipOption> GetAmmoEquipOptions(int slotIndex)
        {
            List<AmmoEquipOption> options = new List<AmmoEquipOption>();

            ItemData item = inventory?.GetItemAt(slotIndex);
            if (item == null || !item.CountsAsAmmo || ammoState == null || equipment == null)
                return options;

            List<int> hotbarSlots = ammoState.GetEligibleWeaponHotbarSlots(item);
            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                int hotbarSlot = hotbarSlots[i];
                ItemData weapon = equipment.GetHotbarItem(hotbarSlot);
                string label = weapon != null ? weapon.itemName : $"Slot {hotbarSlot + 1}";
                options.Add(new AmmoEquipOption(hotbarSlot, label));
            }

            return options;
        }

        public bool TryEquipAmmoToWeapon(int ammoSlotIndex, int weaponHotbarSlot)
        {
            if (ammoState == null || !ammoState.TryEquipAmmoToWeaponSlot(weaponHotbarSlot, ammoSlotIndex))
                return false;

            GameAudioManager.Instance?.PlayItemEquip();
            return true;
        }

        private bool HasEmptyMainInventorySlot()
        {
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.unlockedMainSlots; i++)
            {
                if (inventory.slots[i].IsEmpty)
                    return true;
            }

            return false;
        }

        private bool HasEmptySlot()
        {
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                if (inventory.slots[i].IsEmpty && inventory.IsMainSlotUnlocked(i))
                    return true;
            }

            return false;
        }
    }
}
