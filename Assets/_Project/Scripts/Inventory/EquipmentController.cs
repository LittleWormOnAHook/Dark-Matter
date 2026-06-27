using System;
using Project.Data;
using UnityEngine;

namespace Project.Inventory
{
    [RequireComponent(typeof(InventorySystem))]
    public class EquipmentController : MonoBehaviour
    {
        [Header("Hotbar Selection")]
        [SerializeField] private int selectedHotbarSlot = 0;

        [Header("Weapon Slots")]
        [Tooltip("Hotbar index for weapon slot 1 (keyboard 1).")]
        [SerializeField] private int primaryWeaponHotbarSlot = 0;
        [Tooltip("Hotbar index for weapon slot 2 (keyboard 2).")]
        [SerializeField] private int secondaryWeaponHotbarSlot = 1;
        [Tooltip("Hotbar index for weapon slot 3 (keyboard 3).")]
        [SerializeField] private int tertiaryWeaponHotbarSlot = 2;
        [Tooltip("Hotbar index for weapon slot 4 (keyboard 4).")]
        [SerializeField] private int quaternaryWeaponHotbarSlot = 3;

        public const int WeaponSlotCount = 4;

        [Header("Toolbar")]
        [Tooltip("Toolbar index for the scanner (keyboard N).")]
        [SerializeField] private int scannerToolbarSlot = 0;
        [Tooltip("Toolbar index for binoculars (keyboard B).")]
        [SerializeField] private int binocularsToolbarSlot = 1;

        private InventorySystem inventory;
        private int activeWeaponSlot;
        private int selectedToolbarSlot = -1;

        public event Action<int> OnSelectedHotbarChanged;
        public event Action OnToolbarSelectionChanged;

        public int SelectedHotbarSlot => selectedHotbarSlot;
        public int SelectedToolbarSlot => selectedToolbarSlot;
        public bool IsToolbarActive => selectedToolbarSlot >= 0;
        public int ActiveWeaponSlot => activeWeaponSlot;
        public int PrimaryWeaponHotbarSlot => primaryWeaponHotbarSlot;
        public int SecondaryWeaponHotbarSlot => secondaryWeaponHotbarSlot;
        public int TertiaryWeaponHotbarSlot => tertiaryWeaponHotbarSlot;
        public int QuaternaryWeaponHotbarSlot => quaternaryWeaponHotbarSlot;
        public int ScannerToolbarSlot => scannerToolbarSlot;
        public int BinocularsToolbarSlot => binocularsToolbarSlot;
        public int ActiveWeaponHotbarSlot => GetWeaponHotbarSlot(activeWeaponSlot);
        public int InactiveWeaponHotbarSlot => GetWeaponHotbarSlot((activeWeaponSlot + 1) % WeaponSlotCount);
        public int SelectedSlotIndex => inventory != null ? inventory.inventorySize + selectedHotbarSlot : -1;
        public bool IsWeaponDrawn { get; private set; } = true;

        public ItemData EquippedItem => GetHotbarItem(ActiveWeaponHotbarSlot);

        public ItemData SelectedHotbarItem => GetHotbarItem(selectedHotbarSlot);

        public ItemData ActiveToolItem => GetToolbarItem(selectedToolbarSlot);

        public ItemData SecondaryWeaponItem => GetHotbarItem(InactiveWeaponHotbarSlot);

        public int GetWeaponHotbarSlot(int weaponSlotIndex)
        {
            weaponSlotIndex = Mathf.Clamp(weaponSlotIndex, 0, WeaponSlotCount - 1);
            return weaponSlotIndex switch
            {
                0 => primaryWeaponHotbarSlot,
                1 => secondaryWeaponHotbarSlot,
                2 => tertiaryWeaponHotbarSlot,
                3 => quaternaryWeaponHotbarSlot,
                _ => primaryWeaponHotbarSlot
            };
        }

        public int GetWeaponSlotIndexForHotbar(int hotbarSlot)
        {
            if (hotbarSlot == primaryWeaponHotbarSlot)
                return 0;
            if (hotbarSlot == secondaryWeaponHotbarSlot)
                return 1;
            if (hotbarSlot == tertiaryWeaponHotbarSlot)
                return 2;
            if (hotbarSlot == quaternaryWeaponHotbarSlot)
                return 3;
            return -1;
        }

        public void ForEachWeaponHotbarSlot(Action<int> visit)
        {
            if (visit == null)
                return;

            visit(primaryWeaponHotbarSlot);
            visit(secondaryWeaponHotbarSlot);
            visit(tertiaryWeaponHotbarSlot);
            visit(quaternaryWeaponHotbarSlot);
        }

        public int FindFirstEmptyWeaponHotbarSlot()
        {
            int result = -1;
            ForEachWeaponHotbarSlot(hotbarIndex =>
            {
                if (result >= 0)
                    return;

                if (GetHotbarItem(hotbarIndex) == null)
                    result = hotbarIndex;
            });
            return result;
        }

        public int FindFirstEmptyToolbarSlot(ItemData toolItem)
        {
            if (inventory == null || toolItem == null || toolItem.itemType != ItemType.Tool)
                return -1;

            int preferred = toolItem.toolType == ToolType.Scanner ? scannerToolbarSlot : binocularsToolbarSlot;
            preferred = Mathf.Clamp(preferred, 0, inventory.toolbarSize - 1);
            if (GetToolbarItem(preferred) == null)
                return preferred;

            for (int i = 0; i < inventory.toolbarSize; i++)
            {
                if (GetToolbarItem(i) == null)
                    return i;
            }

            return -1;
        }

        public bool HasOpticsToolSelected()
        {
            ItemData item = ActiveToolItem;
            return item != null && item.IsOpticsTool;
        }

        public bool HasEquippedItemOfType(ItemType itemType)
        {
            ItemData item = EquippedItem;
            return item != null && item.itemType == itemType;
        }

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
            selectedHotbarSlot = Mathf.Clamp(selectedHotbarSlot, 0, Mathf.Max(0, inventory.hotbarSize - 1));
            activeWeaponSlot = 0;
            selectedHotbarSlot = ActiveWeaponHotbarSlot;
            selectedToolbarSlot = -1;
        }

        private void Start()
        {
            RelocateMisplacedHotbarWeapons();
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

        public void SwitchActiveWeapon()
        {
            ClearToolbarSelection();
            activeWeaponSlot = (activeWeaponSlot + 1) % WeaponSlotCount;
            selectedHotbarSlot = ActiveWeaponHotbarSlot;
            IsWeaponDrawn = HasEquippableInHotbarSlot(ActiveWeaponHotbarSlot);
            NotifySelectionChanged();
        }

        public void SelectWeaponSlot(int weaponSlotIndex)
        {
            weaponSlotIndex = Mathf.Clamp(weaponSlotIndex, 0, WeaponSlotCount - 1);
            int hotbarIndex = GetWeaponHotbarSlot(weaponSlotIndex);

            ClearToolbarSelection();

            if (activeWeaponSlot == weaponSlotIndex)
            {
                if (HasEquippableInHotbarSlot(hotbarIndex))
                    IsWeaponDrawn = !IsWeaponDrawn;
            }
            else
            {
                activeWeaponSlot = weaponSlotIndex;
                IsWeaponDrawn = HasEquippableInHotbarSlot(hotbarIndex);
            }

            selectedHotbarSlot = hotbarIndex;
            NotifySelectionChanged();
        }

        public void SelectHotbarSlot(int hotbarSlot)
        {
            if (inventory == null || inventory.hotbarSize <= 0)
                return;

            int clamped = Mathf.Clamp(hotbarSlot, 0, inventory.hotbarSize - 1);

            if (IsWeaponHotbarSlot(clamped))
            {
                SelectWeaponSlot(GetWeaponSlotIndexForHotbar(clamped));
                return;
            }

            ClearToolbarSelection();
            selectedHotbarSlot = clamped;
            NotifySelectionChanged();
        }

        public void SelectToolbarSlot(int toolbarSlot, bool allowToggleOff = true)
        {
            if (inventory == null || inventory.toolbarSize <= 0)
                return;

            int clamped = Mathf.Clamp(toolbarSlot, 0, inventory.toolbarSize - 1);
            ItemData item = GetToolbarItem(clamped);
            if (item == null)
            {
                ClearToolbarSelection();
                NotifySelectionChanged();
                return;
            }

            if (allowToggleOff && selectedToolbarSlot == clamped)
                ClearToolbarSelection();
            else
                selectedToolbarSlot = clamped;

            NotifySelectionChanged();
        }

        public bool TryEnsureToolbarTool(ToolType toolType, out int toolbarSlot)
        {
            toolbarSlot = -1;
            if (inventory == null)
                return false;

            toolbarSlot = toolType == ToolType.Scanner ? scannerToolbarSlot : binocularsToolbarSlot;
            toolbarSlot = Mathf.Clamp(toolbarSlot, 0, inventory.toolbarSize - 1);

            ItemData inSlot = GetToolbarItem(toolbarSlot);
            if (inSlot != null && inSlot.toolType == toolType)
            {
                selectedToolbarSlot = toolbarSlot;
                NotifySelectionChanged();
                return true;
            }

            int sourceAbsolute = FindInventorySlotWithTool(toolType);
            if (sourceAbsolute < 0)
                return false;

            ItemData item = inventory.slots[sourceAbsolute].item;
            TryEquipToolFromSlot(sourceAbsolute, item, selectAfterMove: true);
            inSlot = GetToolbarItem(toolbarSlot);
            return inSlot != null && inSlot.toolType == toolType;
        }

        private int FindInventorySlotWithTool(ToolType toolType)
        {
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                if (slot.item.itemType == ItemType.Tool && slot.item.toolType == toolType)
                    return i;
            }

            return -1;
        }

        public bool SelectInventorySlot(int slotIndex)
        {
            if (inventory == null)
                return false;

            if (inventory.IsToolbarIndex(slotIndex))
            {
                SelectToolbarSlot(inventory.ToToolbarSlotIndex(slotIndex));
                return true;
            }

            int hotbarStart = inventory.inventorySize;
            int hotbarEnd = hotbarStart + inventory.hotbarSize;
            if (slotIndex < hotbarStart || slotIndex >= hotbarEnd)
                return false;

            SelectHotbarSlot(slotIndex - hotbarStart);
            return true;
        }

        public bool IsWeaponHotbarSlot(int hotbarSlot)
        {
            return GetWeaponSlotIndexForHotbar(hotbarSlot) >= 0;
        }

        public bool IsUtilityHotbarSlot(int hotbarSlot)
        {
            return hotbarSlot >= 0
                && hotbarSlot < inventory.hotbarSize
                && !IsWeaponHotbarSlot(hotbarSlot);
        }

        public static bool IsMeleeWeaponItem(ItemData item)
        {
            return item != null && item.itemType == ItemType.MeleeWeapon;
        }

        public bool CanPlaceItemInHotbarSlot(int hotbarSlot, ItemData item)
        {
            if (inventory == null || item == null || hotbarSlot < 0 || hotbarSlot >= inventory.hotbarSize)
                return false;

            if (IsWeaponHotbarSlot(hotbarSlot))
                return IsMeleeWeaponItem(item);

            return !IsMeleeWeaponItem(item);
        }

        public bool CanPlaceItemAt(int absoluteSlotIndex, ItemData item)
        {
            if (inventory == null || item == null)
                return false;

            if (inventory.IsToolbarIndex(absoluteSlotIndex))
                return item.itemType == ItemType.Tool;

            if (!inventory.IsHotbarIndex(absoluteSlotIndex))
                return true;

            int hotbarIndex = absoluteSlotIndex - inventory.inventorySize;
            return CanPlaceItemInHotbarSlot(hotbarIndex, item);
        }

        public bool IsToolbarSlotIndex(int toolbarSlot)
        {
            return toolbarSlot >= 0 && toolbarSlot < inventory.toolbarSize;
        }

        public bool IsActiveWeaponHotbarIndex(int absoluteSlotIndex)
        {
            if (inventory == null)
                return false;

            int hotbarIndex = absoluteSlotIndex - inventory.inventorySize;
            return hotbarIndex == ActiveWeaponHotbarSlot;
        }

        public bool IsSelectedToolbarAbsoluteIndex(int absoluteSlotIndex)
        {
            if (inventory == null || selectedToolbarSlot < 0)
                return false;

            return absoluteSlotIndex == inventory.ToolbarStartIndex + selectedToolbarSlot;
        }

        public ItemData GetHotbarItem(int hotbarSlot)
        {
            if (inventory == null)
                return null;

            int index = inventory.inventorySize + hotbarSlot;
            if (index < 0 || index >= inventory.slots.Count)
                return null;

            InventorySystem.InventorySlot slot = inventory.slots[index];
            return slot == null || slot.IsEmpty ? null : slot.item;
        }

        public ItemData GetToolbarItem(int toolbarSlot)
        {
            if (inventory == null || !IsToolbarSlotIndex(toolbarSlot))
                return null;

            int index = inventory.ToolbarStartIndex + toolbarSlot;
            if (index < 0 || index >= inventory.slots.Count)
                return null;

            InventorySystem.InventorySlot slot = inventory.slots[index];
            return slot == null || slot.IsEmpty ? null : slot.item;
        }

        private void HandleInventoryChanged()
        {
            if (selectedToolbarSlot >= 0 && GetToolbarItem(selectedToolbarSlot) == null)
                selectedToolbarSlot = -1;

            if (!HasEquippableInHotbarSlot(ActiveWeaponHotbarSlot))
                IsWeaponDrawn = false;

            NotifySelectionChanged();
        }

        private bool HasEquippableInHotbarSlot(int hotbarSlot)
        {
            ItemData item = GetHotbarItem(hotbarSlot);
            return item != null && item.IsEquippable;
        }

        public void ApplySaveState(int hotbarSlot, int weaponSlot, bool drawn, int toolbarSlot = -1)
        {
            if (inventory == null)
                return;

            selectedHotbarSlot = Mathf.Clamp(hotbarSlot, 0, Mathf.Max(0, inventory.hotbarSize - 1));
            activeWeaponSlot = Mathf.Clamp(weaponSlot, 0, WeaponSlotCount - 1);
            IsWeaponDrawn = drawn;
            selectedToolbarSlot = toolbarSlot >= 0 && GetToolbarItem(toolbarSlot) != null
                ? Mathf.Clamp(toolbarSlot, 0, inventory.toolbarSize - 1)
                : -1;

            NotifySelectionChanged();
        }

        public bool TryEquipItemFromSlot(int absoluteSlotIndex)
        {
            if (inventory == null)
                return false;

            if (absoluteSlotIndex < 0 || absoluteSlotIndex >= inventory.slots.Count)
                return false;

            InventorySystem.InventorySlot sourceSlot = inventory.slots[absoluteSlotIndex];
            if (sourceSlot.IsEmpty || sourceSlot.item == null || !sourceSlot.item.IsEquippable)
                return false;

            if (sourceSlot.item.itemType == ItemType.Tool)
                return TryEquipToolFromSlot(absoluteSlotIndex, sourceSlot.item, selectAfterMove: false);

            int targetHotbar = ResolveEquipHotbarSlot(absoluteSlotIndex);
            int targetAbsolute = inventory.inventorySize + targetHotbar;

            if (targetAbsolute != absoluteSlotIndex)
                inventory.MoveOrMergeSlots(absoluteSlotIndex, targetAbsolute);

            SelectHotbarSlot(targetHotbar);
            IsWeaponDrawn = HasEquippableInHotbarSlot(targetHotbar);
            NotifySelectionChanged();
            return true;
        }

        public bool TryUnequipFromSlot(int absoluteSlotIndex)
        {
            if (inventory == null || (!inventory.IsHotbarIndex(absoluteSlotIndex) && !inventory.IsToolbarIndex(absoluteSlotIndex)))
                return false;

            if (inventory.slots[absoluteSlotIndex].IsEmpty)
                return false;

            int emptyInventorySlot = FindFirstEmptyMainInventorySlot();
            if (emptyInventorySlot < 0)
                return false;

            inventory.SwapSlots(absoluteSlotIndex, emptyInventorySlot);

            if (inventory.IsHotbarIndex(absoluteSlotIndex))
            {
                int hotbarIndex = absoluteSlotIndex - inventory.inventorySize;
                if (hotbarIndex == ActiveWeaponHotbarSlot)
                    IsWeaponDrawn = false;
            }
            else if (inventory.IsToolbarIndex(absoluteSlotIndex) &&
                     inventory.ToToolbarSlotIndex(absoluteSlotIndex) == selectedToolbarSlot)
            {
                selectedToolbarSlot = -1;
            }

            NotifySelectionChanged();
            return true;
        }

        private bool TryEquipToolFromSlot(int absoluteSlotIndex, ItemData item, bool selectAfterMove = false)
        {
            int targetToolbarSlot = ResolveEquipToolbarSlot(item, absoluteSlotIndex);
            int targetAbsolute = inventory.ToolbarStartIndex + targetToolbarSlot;

            if (targetAbsolute != absoluteSlotIndex)
                inventory.MoveOrMergeSlots(absoluteSlotIndex, targetAbsolute);

            if (selectAfterMove)
                SelectToolbarSlot(targetToolbarSlot, allowToggleOff: false);

            return true;
        }

        private int ResolveEquipToolbarSlot(ItemData item, int sourceAbsolute)
        {
            int preferred = item.toolType == ToolType.Scanner ? scannerToolbarSlot : binocularsToolbarSlot;
            preferred = Mathf.Clamp(preferred, 0, inventory.toolbarSize - 1);

            int preferredAbsolute = inventory.ToolbarStartIndex + preferred;
            if (sourceAbsolute == preferredAbsolute)
                return preferred;

            if (inventory.slots[preferredAbsolute].IsEmpty)
                return preferred;

            for (int i = 0; i < inventory.toolbarSize; i++)
            {
                int absolute = inventory.ToolbarStartIndex + i;
                if (inventory.slots[absolute].IsEmpty)
                    return i;
            }

            return preferred;
        }

        private int ResolveEquipHotbarSlot(int sourceAbsolute)
        {
            int hotbarStart = inventory.inventorySize;
            int bestMatch = -1;

            ForEachWeaponHotbarSlot(hotbarIndex =>
            {
                int absolute = hotbarStart + hotbarIndex;
                if (sourceAbsolute == absolute)
                    bestMatch = hotbarIndex;
            });

            if (bestMatch >= 0)
                return bestMatch;

            int firstEmpty = -1;
            ForEachWeaponHotbarSlot(hotbarIndex =>
            {
                if (firstEmpty >= 0)
                    return;

                if (inventory.slots[hotbarStart + hotbarIndex].IsEmpty)
                    firstEmpty = hotbarIndex;
            });

            if (firstEmpty >= 0)
                return firstEmpty;

            return ActiveWeaponHotbarSlot;
        }

        private int FindFirstEmptyMainInventorySlot()
        {
            for (int i = 0; i < inventory.inventorySize; i++)
            {
                if (inventory.slots[i].IsEmpty)
                    return i;
            }

            return -1;
        }

        private void RelocateMisplacedHotbarWeapons()
        {
            if (inventory == null)
                return;

            int hotbarStart = inventory.inventorySize;
            for (int hotbarIndex = 0; hotbarIndex < inventory.hotbarSize; hotbarIndex++)
            {
                if (IsWeaponHotbarSlot(hotbarIndex))
                    continue;

                int absoluteIndex = hotbarStart + hotbarIndex;
                InventorySystem.InventorySlot slot = inventory.slots[absoluteIndex];
                if (slot.IsEmpty || !IsMeleeWeaponItem(slot.item))
                    continue;

                int targetHotbar = ResolveEquipHotbarSlot(absoluteIndex);
                int targetAbsolute = hotbarStart + targetHotbar;
                if (targetAbsolute == absoluteIndex)
                    continue;

                if (inventory.slots[targetAbsolute].IsEmpty)
                {
                    inventory.SwapSlots(absoluteIndex, targetAbsolute);
                    continue;
                }

                int emptyMainSlot = FindFirstEmptyMainInventorySlot();
                if (emptyMainSlot >= 0)
                    inventory.SwapSlots(absoluteIndex, emptyMainSlot);
            }
        }

        private void ClearToolbarSelection()
        {
            if (selectedToolbarSlot < 0)
                return;

            selectedToolbarSlot = -1;
        }

        private void NotifySelectionChanged()
        {
            OnSelectedHotbarChanged?.Invoke(SelectedSlotIndex);
            OnToolbarSelectionChanged?.Invoke();
        }
    }
}
