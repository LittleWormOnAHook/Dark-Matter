using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using Project.Progression;
using Project.Survival;
using UnityEngine;

namespace Project.Core
{
    public static class DMDevCommandState
    {
        public static bool GodMode { get; set; }
        public static bool InfiniteStamina { get; set; }
        public static bool InfiniteEnergy { get; set; }
        public static bool InfiniteOxygen { get; set; }
        public static bool GamePaused { get; set; }
        public static bool UnlockCursor { get; set; }

        public static bool BlocksDamage => GodMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            ClearTransient();
            GodMode = false;
            InfiniteStamina = false;
            InfiniteEnergy = false;
            InfiniteOxygen = false;
        }

        /// <summary>
        /// Drop pause / free-cursor leftovers. Domain Reload off keeps these statics across
        /// Settings Apply scene reloads and would leave the pointer unlocked in gameplay.
        /// </summary>
        public static void ClearTransient()
        {
            GamePaused = false;
            UnlockCursor = false;
        }

        public static void Tick(SurvivalStats stats)
        {
            if (stats == null || stats.IsDead)
                return;

            if ((GodMode || InfiniteEnergy) && stats.CurrentEnergy < stats.maxEnergy - 0.05f)
                stats.DevSetEnergy(stats.maxEnergy);
            if ((GodMode || InfiniteStamina) && stats.CurrentStamina < stats.maxStamina - 0.05f)
                stats.DevSetStamina(stats.maxStamina);
            if ((GodMode || InfiniteOxygen) && stats.CurrentOxygen < stats.maxOxygen - 0.05f)
                stats.DevSetOxygen(stats.maxOxygen);
            if (GodMode && stats.CurrentHealth < stats.maxHealth - 0.05f)
                stats.DevSetHealth(stats.maxHealth);
            if (GodMode && (Mathf.Abs(stats.CurrentThermalStress) > 0.05f
                || stats.CurrentRadiation > 0.05f
                || stats.CurrentSulfur > 0.05f
                || stats.CurrentVolcano > 0.05f))
                stats.DevClearExposure();
        }

        public static string RefillVitals()
        {
            SurvivalStats stats = FindStats();
            if (stats == null)
                return "No SurvivalStats.";

            stats.DevSetHealth(stats.maxHealth);
            stats.DevSetEnergy(stats.maxEnergy);
            stats.DevSetStamina(stats.maxStamina);
            stats.DevSetOxygen(stats.maxOxygen);
            stats.DevClearExposure();
            return "Vitals refilled.";
        }

        public static string SetLevel(int level)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null)
                return "No progression.";

            progression.DevSetLevel(level);
            return $"Level set to {progression.Level}.";
        }

        public static string AddSkillPoints(int amount)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null)
                return "No progression.";

            progression.DevAddSkillPoints(amount);
            return $"Skill points now {progression.UnspentSkillPoints}.";
        }

        public static string AddCredits(int amount)
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return "No roster.";

            roster.AddAetherCredits(amount, "DevPanel");
            return $"AC now {Mathf.RoundToInt(roster.AetherCredits)}.";
        }

        public static string AddItem(string itemId, int amount)
        {
            ItemData item = ResolveItem(itemId);
            if (item == null)
                return $"Unknown item '{itemId}'.";

            InventorySystem inventory = FindInventory();
            if (inventory == null)
                return "No inventory.";

            int added = inventory.AddItem(item, Mathf.Max(1, amount));
            return added > 0 ? $"Added {added}x {item.itemName}." : $"Could not add {item.itemName}.";
        }

        public static string RemoveItem(string itemId, int amount)
        {
            ItemData item = ResolveItem(itemId);
            if (item == null)
                return $"Unknown item '{itemId}'.";

            InventorySystem inventory = FindInventory();
            if (inventory == null)
                return "No inventory.";

            bool removed = inventory.RemoveItem(item, Mathf.Max(1, amount));
            return removed ? $"Removed {amount}x {item.itemName}." : $"{item.itemName} not in inventory.";
        }

        public static string ClearInventory()
        {
            InventorySystem inventory = FindInventory();
            if (inventory == null)
                return "No inventory.";

            int cleared = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                if (inventory.slots[i] == null || inventory.slots[i].IsEmpty)
                    continue;

                int amount = inventory.slots[i].amount;
                if (inventory.RemoveItemAt(i, amount))
                    cleared += amount;
            }

            return $"Cleared {cleared} stacked items.";
        }

        public static string DespawnNearbyPickups(float radius = 12f)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return "No player.";

            Project.Interaction.ItemPickup[] pickups =
                UnityEngine.Object.FindObjectsByType<Project.Interaction.ItemPickup>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int removed = 0;
            Vector3 origin = player.transform.position;
            float sqr = radius * radius;
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] == null)
                    continue;
                if ((pickups[i].transform.position - origin).sqrMagnitude > sqr)
                    continue;

                UnityEngine.Object.Destroy(pickups[i].gameObject);
                removed++;
            }

            return $"Despawned {removed} nearby pickups.";
        }

        public static ItemData ResolveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            ItemData resolved = ItemRegistry.Resolve(itemId.Trim());
            if (resolved != null)
                return resolved;

            ItemData[] all = ItemRegistry.GetAllItems();
            string needle = itemId.Trim();
            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item == null)
                    continue;
                if (string.Equals(item.name, needle, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.itemName, needle, System.StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(item.itemName)
                        && item.itemName.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0))
                    return item;
            }

            return null;
        }

        private static SurvivalStats FindStats()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            return player != null ? player.GetComponent<SurvivalStats>() : null;
        }

        private static InventorySystem FindInventory()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            return player != null ? player.GetComponent<InventorySystem>() : null;
        }
    }
}
