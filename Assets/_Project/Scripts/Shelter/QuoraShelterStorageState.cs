using UnityEngine;

namespace Project.Shelter
{
    /// <summary>
    /// Remembers remaining deploy lifetime when a Quora Shelter is stored back into inventory.
    /// Timer pauses while stored; restored on the next deploy.
    /// </summary>
    public static class QuoraShelterStorageState
    {
        public const float DefaultLifetimeSeconds = 600f;

        public static bool HasStoredLifetime { get; private set; }
        public static float StoredRemainingSeconds { get; private set; } = DefaultLifetimeSeconds;

        public static void SetStoredLifetime(float remainingSeconds)
        {
            HasStoredLifetime = true;
            StoredRemainingSeconds = Mathf.Clamp(remainingSeconds, 0f, DefaultLifetimeSeconds);
        }

        public static float ConsumeStoredLifetimeOrDefault()
        {
            float remaining = HasStoredLifetime ? StoredRemainingSeconds : DefaultLifetimeSeconds;
            ClearStored();
            return remaining;
        }

        public static void ClearStored()
        {
            HasStoredLifetime = false;
            StoredRemainingSeconds = DefaultLifetimeSeconds;
        }

        public static void RestoreFromSave(bool hasStoredLifetime, float remainingSeconds)
        {
            HasStoredLifetime = hasStoredLifetime;
            StoredRemainingSeconds = Mathf.Clamp(remainingSeconds, 0f, DefaultLifetimeSeconds);
        }
    }
}
