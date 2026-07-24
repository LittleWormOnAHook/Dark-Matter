namespace Project.Survival.World
{
    /// <summary>
    /// Surface ↔ underground transition rules — GDD biome plan §2.5–2.6 (July 2026).
    /// </summary>
    public static class IoWorldTransitionRules
    {
        /// <summary>Default radius to auto-pack deployed vehicles before breach entry.</summary>
        public const float DefaultUndergroundEntryPackRadiusMeters = 20f;

        public static bool IsFootOnlySurfaceRegion(IoSurfaceRegionId region)
        {
            return region == IoSurfaceRegionId.LavaCalderas
                || region == IoSurfaceRegionId.PolarRadiationFlats
                || region == IoSurfaceRegionId.PrecursorRuinBelt;
        }
    }

    /// <summary>Surface biome regions on the full-scale main map (B1–B7).</summary>
    public enum IoSurfaceRegionId
    {
        None = 0,
        SulfurPlains = 1,
        GeyserFields = 2,
        AshFlatsAndRidges = 3,
        LavaCalderas = 4,
        PolarRadiationFlats = 5,
        BasaltHighlands = 6,
        PrecursorRuinBelt = 7
    }
}
