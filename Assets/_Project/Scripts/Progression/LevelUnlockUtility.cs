using UnityEngine;

namespace Project.Progression
{
    public static class LevelUnlockUtility
    {
        public static bool CanAccess(int playerLevel, int requiredLevel) =>
            playerLevel >= Mathf.Max(1, requiredLevel);

        public static bool CanAccess(PlayerProgressionManager progression, int requiredLevel) =>
            progression != null && CanAccess(progression.Level, requiredLevel);
    }
}
