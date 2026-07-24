namespace Project.Survival.World
{
    /// <summary>
    /// Echo signal scheduling policy — biome plan §2.8 (July 2026).
    /// ExperienceDirector owns spawn timing; biomes are weights only.
    /// </summary>
    public static class IoEchoSignalDirectorPolicy
    {
        public const int EarlyGameActiveSignalCap = 2;
        public const int MidGameActiveSignalCap = 4;
        public const int LateGameActiveSignalCap = 5;

        /// <summary>Minutes after successful rescue before same-tier signal can respawn.</summary>
        public const float RescueCooldownMinutes = 20f;
    }
}
