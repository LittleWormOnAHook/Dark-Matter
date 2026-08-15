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
            $"Require Level {requiredLevel}";

        public static void ShowLevelRequiredToast(int requiredLevel)
        {
            if (!IsGateActive(requiredLevel))
                return;

            PickupToastUI.Show(FormatLevelRequiredMessage(requiredLevel));
        }

        /// <summary>Center popup: title "Require Level" + required level number.</summary>
        public static void ShowRequireLevelPopup(int requiredLevel)
        {
            if (!IsGateActive(requiredLevel))
                return;

            DMIRequireLevelPopupUI.Show(requiredLevel);
        }

        public static bool PassesEquipGate(ItemData item, bool showToast = false) =>
            PassesGate(item?.requiredLevelToEquip ?? 0, showToast);

        public static bool PassesUseGate(ItemData item, bool showToast = false) =>
            PassesGate(item?.requiredLevelToUse ?? 0, showToast);

        public static bool PassesCraftGate(ItemData outputItem, bool showToast = false) =>
            PassesGate(outputItem?.requiredLevelToCraft ?? 0, showToast);

        public static bool PassesPickupGate(ItemData item, bool showToast = false) =>
            PassesGate(item?.requiredLevelToPickup ?? 0, showToast);

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
