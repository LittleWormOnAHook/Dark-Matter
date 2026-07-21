using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Remembers the fuel/shield/health of the last hovercraft "Stored" into the inventory, so
    /// "Deploy" can restore roughly where it left off instead of always spawning at full stats.
    /// Simple in-memory record (mirrored into GameSaveData for persistence) since only one hovercraft
    /// item can exist at a time (ItemData.maxStack = 1 on the Hovercraft item).
    /// </summary>
    public static class HovercraftStorageState
    {
        /// <summary>Must match HovercraftFuelSystem's default maxFuel — used when no live component is
        /// available to ask (e.g. refueling a stored, not-yet-deployed craft from the inventory).</summary>
        public const float DefaultMaxFuel = 1000f;
        public const float FuelPerPlasmaCell = 250f;

        public static bool HasStoredCraft { get; private set; }
        public static float StoredFuel { get; private set; } = DefaultMaxFuel;
        public static float StoredShield { get; private set; } = 60f;
        public static float StoredHealth { get; private set; } = 120f;

        public static void SetStoredState(float fuel, float shield, float health)
        {
            HasStoredCraft = true;
            StoredFuel = Mathf.Max(0f, fuel);
            StoredShield = Mathf.Max(0f, shield);
            StoredHealth = Mathf.Max(0f, health);
        }

        public static void ClearStored()
        {
            HasStoredCraft = false;
        }

        public static void AddStoredFuel(float amount, float maxFuel)
        {
            StoredFuel = Mathf.Clamp(StoredFuel + amount, 0f, maxFuel);
        }

        /// <summary>Restores a previously-saved state (used by save/load) without marking a craft as stored.</summary>
        public static void RestoreFromSave(bool hasStoredCraft, float fuel, float shield, float health)
        {
            HasStoredCraft = hasStoredCraft;
            StoredFuel = fuel;
            StoredShield = shield;
            StoredHealth = health;
        }
    }
}
