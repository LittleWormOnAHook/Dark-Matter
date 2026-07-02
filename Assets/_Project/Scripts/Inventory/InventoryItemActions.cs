using Project.Audio;
using Project.Data;
using Project.UI;
using UnityEngine;

namespace Project.Inventory
{
    public class InventoryItemActions : MonoBehaviour
    {
        private InventorySystem inventory;
        private EquipmentController equipment;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            equipment = GetComponent<EquipmentController>();
        }

        public bool TryUse(int slotIndex)
        {
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

        public bool CanUse(int slotIndex)
        {
            ItemData item = inventory?.GetItemAt(slotIndex);
            return item != null && item.IsConsumable;
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

        private bool HasEmptyMainInventorySlot()
        {
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.inventorySize; i++)
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
                if (inventory.slots[i].IsEmpty)
                    return true;
            }

            return false;
        }
    }
}
