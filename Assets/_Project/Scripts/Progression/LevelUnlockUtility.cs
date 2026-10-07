using Project.Data;
using Project.UI;
using UnityEngine;

namespace Project.Progression
{
    public static class LevelUnlockUtility
    {
        /// <summary>0 or 1 means unrestricted.</summary>
        public static bool IsGateActive(int requiredLevel) => requiredLevel > 1;

        public static bool CanAccess(int playerLevel, int requiredLevel)
        {
            if (!IsGateActive(requiredLevel))
                return true;
            return playerLevel >= requiredLevel;
        }

        public static bool CanAccess(PlayerProgressionManager progression, int requiredLevel) =>
            CanAccess(progression != null ? progression.Level : 1, requiredLevel);

        public static int GetPlayerLevel()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null ? Mathf.Max(1, progression.Level) : 1;
        }

        /// <summary>Player-facing toast / popup copy for level gates.</summary>
        public static string FormatLevelRequiredMessage(int requiredLevel) =>
            $"Level {requiredLevel} Required";

        public static void ShowLevelRequiredToast(int requiredLevel)
        {
            if (!IsGateActive(requiredLevel))
                return;

            ShowRequireLevelPopup(requiredLevel);
        }

        /// <summary>Center popup: title "Require Level" + required level number.</summary>
        public static void ShowRequireLevelPopup(int requiredLevel)
        {
            if (!IsGateActive(requiredLevel))
                return;

            DMIRequireLevelPopupUI.Show(requiredLevel);
        }

        /// <summary>
        /// Equip/draw/select: authored equip gate, or use gate when equip is inactive
        /// so a single filled field still blocks both actions.
        /// </summary>
        public static int GetEffectiveEquipRequiredLevel(ItemData item)
        {
            if (item == null)
                return 1;
            if (IsGateActive(item.requiredLevelToEquip))
                return item.requiredLevelToEquip;
            if (IsGateActive(item.requiredLevelToUse))
                return item.requiredLevelToUse;
            return 1;
        }

        /// <summary>
        /// Consume/install/deploy/throw: authored use gate, or equip gate when use is inactive.
        /// </summary>
        public static int GetEffectiveUseRequiredLevel(ItemData item)
        {
            if (item == null)
                return 1;
            if (IsGateActive(item.requiredLevelToUse))
                return item.requiredLevelToUse;
            if (IsGateActive(item.requiredLevelToEquip))
                return item.requiredLevelToEquip;
            return 1;
        }

        public static bool PassesEquipGate(ItemData item, bool showToast = false) =>
            PassesGate(GetEffectiveEquipRequiredLevel(item), showToast);

        public static bool PassesUseGate(ItemData item, bool showToast = false) =>
            PassesGate(GetEffectiveUseRequiredLevel(item), showToast);

        public static bool PassesCraftGate(ItemData outputItem, bool showToast = false) =>
            PassesGate(outputItem?.requiredLevelToCraft ?? 0, showToast);

        public static bool PassesPickupGate(ItemData item, bool showToast = false) =>
            PassesGate(item?.requiredLevelToPickup ?? 0, showToast);

        /// <summary>
        /// Level lock (loot plan D3/D18/D21): the higher of the item's effective equip and use gates.
        /// Pickup and craft gates are not part of the lock. 1 means unlocked for everyone.
        /// </summary>
        public static int GetLevelLockRequiredLevel(ItemData item)
        {
            if (item == null)
                return 1;
            return Mathf.Max(GetEffectiveEquipRequiredLevel(item), GetEffectiveUseRequiredLevel(item));
        }

        /// <summary>True while the player is below the item's equip or use gate (vendor blocked-overlay look).</summary>
        public static bool IsLevelLocked(ItemData item) => IsLevelLocked(item, GetPlayerLevel());

        public static bool IsLevelLocked(ItemData item, int playerLevel)
        {
            if (item == null)
                return false;
            int required = GetLevelLockRequiredLevel(item);
            return IsGateActive(required) && !CanAccess(playerLevel, required);
        }

        /// <summary>
        /// Hotbar, toolbar and Hot Cross placement gate (D21): refuses every level-locked item kind,
        /// not only equippables. Shows the Require Level popup when asked (once per failed drag).
        /// </summary>
        public static bool PassesLevelLock(ItemData item, bool showToast = false)
        {
            if (!IsLevelLocked(item))
                return true;

            if (showToast)
                ShowLevelRequiredToast(GetLevelLockRequiredLevel(item));

            return false;
        }

        /// <summary>Cheap per-frame level read for UI repaint checks (no scene search).</summary>
        public static int CachedPlayerLevel
        {
            get
            {
                PlayerProgressionManager progression = PlayerProgressionManager.Instance;
                return progression != null ? Mathf.Max(1, progression.Level) : 1;
            }
        }

        /// <summary>Effective craft level = max(recipe gate, output item craft gate). Inactive gates ignored.</summary>
        public static int GetEffectiveCraftRequiredLevel(int recipeRequiredLevel, ItemData outputItem)
        {
            int recipeGate = IsGateActive(recipeRequiredLevel) ? recipeRequiredLevel : 1;
            int itemGate = outputItem != null && IsGateActive(outputItem.requiredLevelToCraft)
                ? outputItem.requiredLevelToCraft
                : 1;
            return Mathf.Max(recipeGate, itemGate);
        }

        private static bool PassesGate(int requiredLevel, bool showToast)
        {
            if (!IsGateActive(requiredLevel))
                return true;

            if (CanAccess(GetPlayerLevel(), requiredLevel))
                return true;

            if (showToast)
                ShowLevelRequiredToast(requiredLevel);

            return false;
        }
    }
}
