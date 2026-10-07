using System;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Loot
{
    /// <summary>Outcome of one loot grant (loot plan section 7.7).</summary>
    public enum DMLootGrantResult
    {
        /// <summary>Unknown entry id, empty container, or nothing to grant.</summary>
        None = 0,
        Granted = 1,
        Partial = 2,
        InventoryFull = 3
    }

    /// <summary>
    /// One loot row. Items only (D6). <see cref="entryId"/> is stable for the container's lifetime,
    /// so a click on a stale row never grants the wrong entry after the list shifts.
    /// </summary>
    [Serializable]
    public sealed class DMLootEntry
    {
        public int entryId;
        public ItemData item;
        public int amount;

        public bool IsEmpty => item == null || amount <= 0;

        /// <summary>Quest or unique entries keep a chest from expiring (D2).</summary>
        public bool IsProtected =>
            item != null && (item.itemType == ItemType.Quest || item.rarity == ItemRarity.Unique);
    }

    /// <summary>
    /// Shared loot grant path for chests and enemy loot bags (replaces the two TryGrantLootEntry copies).
    /// - Items land in the main inventory grid only, never auto-slotted into weapon or tool slots (D8).
    /// - Ammo never loads into a magazine: matching Hot Cross stack, then main grid stack, then a free
    ///   main grid slot (D7). Level-locked ammo skips the Hot Cross step (D21).
    /// - No pickup-level check: level-locked items are granted and shown locked (D3, D22).
    /// - Item-only. Enemy bag AC goes through <see cref="GrantAetherCredits"/> with the roster's card only.
    /// </summary>
    public static class DMLootGrant
    {
        /// <summary>
        /// Grants as much of <paramref name="entry"/> as fits and reduces <c>entry.amount</c> in place.
        /// Toasts "+X Item" or "+X Item (Y left)". Never shows the inventory-full toast; callers do that
        /// once per action.
        /// </summary>
        public static DMLootGrantResult TryGrant(DMLootEntry entry, out int granted)
        {
            granted = 0;
            if (entry == null || entry.IsEmpty)
                return DMLootGrantResult.None;

            InventorySystem inventory = ResolvePlayerInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[DMLootGrant] No player inventory found; loot left in place.");
                return DMLootGrantResult.InventoryFull;
            }

            granted = AddToMainGrid(inventory, entry.item, entry.amount);
            if (granted > 0)
            {
                entry.amount = Mathf.Max(0, entry.amount - granted);
                PickupToastUI.Show(entry.amount > 0
                    ? $"+{granted} {entry.item.itemName} ({entry.amount} left)"
                    : $"+{granted} {entry.item.itemName}");
            }

            if (entry.amount <= 0)
                return DMLootGrantResult.Granted;

            return granted > 0 ? DMLootGrantResult.Partial : DMLootGrantResult.InventoryFull;
        }

        /// <summary>Chest / bag rule: main grid only; ammo uses the Hot Cross-first ammo rule.</summary>
        public static int AddToMainGrid(InventorySystem inventory, ItemData item, int amount)
        {
            if (inventory == null || item == null || amount <= 0)
                return 0;

            if (EquipmentController.IsAmmoItem(item))
                return AddAmmo(inventory, item, amount);

            return inventory.AddItemToMainInventory(item, amount);
        }

        /// <summary>
        /// World pickups and quest rewards: same ammo rule (never auto-loaded, D7); other items keep
        /// the normal pickup routing (unlocked weapons/tools may still fill empty slots; level-locked
        /// items are refused there by the D21 placement gate and land in the main grid).
        /// </summary>
        public static int AddPickupItem(InventorySystem inventory, ItemData item, int amount)
        {
            if (inventory == null || item == null || amount <= 0)
                return 0;

            if (EquipmentController.IsAmmoItem(item))
                return AddAmmo(inventory, item, amount);

            return inventory.AddItem(item, amount, autoCreditAmmoToWeapons: false);
        }

        /// <summary>
        /// Ammo order (D7): existing matching stack in a Hot Cross utility slot (5-0), then a matching
        /// main grid stack, then a free unlocked main grid slot. Magazines are never touched.
        /// </summary>
        public static int AddAmmo(InventorySystem inventory, ItemData ammo, int amount)
        {
            if (inventory == null || ammo == null || amount <= 0)
                return 0;

            int remaining = amount;
            int maxStack = Mathf.Max(1, ammo.maxStack);
            int hotCrossAdded = 0;

            EquipmentController equipment = inventory.GetComponent<EquipmentController>();
            if (equipment != null && !LevelUnlockUtility.IsLevelLocked(ammo))
            {
                for (int local = 0; local < inventory.hotbarSize && remaining > 0; local++)
                {
                    if (!equipment.IsUtilityHotbarSlot(local))
                        continue;

                    int absolute = inventory.HotbarStartIndex + local;
                    if (absolute < 0 || absolute >= inventory.slots.Count)
                        continue;

                    InventorySystem.InventorySlot slot = inventory.slots[absolute];
                    if (slot == null || slot.IsEmpty || slot.item != ammo || slot.amount >= maxStack)
                        continue;
                    if (!inventory.CanAcceptItemAt(absolute, ammo))
                        continue;

                    int add = Mathf.Min(remaining, maxStack - slot.amount);
                    slot.amount += add;
                    remaining -= add;
                    hotCrossAdded += add;
                }
            }

            int mainAdded = remaining > 0 ? inventory.AddItemToMainInventory(ammo, remaining) : 0;

            // AddItemToMainInventory raises OnInventoryChanged itself when it adds anything.
            if (hotCrossAdded > 0 && mainAdded <= 0)
                inventory.NotifyInventoryChanged();

            return hotCrossAdded + mainAdded;
        }

        /// <summary>
        /// Enemy bag AC only (chests never hold AC, D6). One feedback path: the roster's AC reward card,
        /// no extra "+N AC" toast.
        /// </summary>
        public static int GrantAetherCredits(int amount, string source)
        {
            if (amount <= 0)
                return 0;

            return QuestRewardGranter.GrantReward(
                new QuestRewardDefinition { type = QuestRewardType.Pi, amount = amount },
                string.IsNullOrWhiteSpace(source) ? "Loot" : source);
        }

        public static InventorySystem ResolvePlayerInventory()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                InventorySystem onPlayer = player.GetComponent<InventorySystem>();
                if (onPlayer != null)
                    return onPlayer;
            }

            return PlayerLocator.FindLiveInventory();
        }
    }
}
