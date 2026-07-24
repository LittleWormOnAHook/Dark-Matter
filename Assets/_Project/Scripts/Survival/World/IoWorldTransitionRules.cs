namespace Project.Survival.World
{
    /// <summary>
    /// Surface ↔ underground transition rules — biome plan §2.5–2.7 (July 2026).
    /// </summary>
    public static class IoWorldTransitionRules
    {
        public const float MinUndergroundEntryPackRadiusMeters = 10f;
        public const float MaxUndergroundEntryPackRadiusMeters = 20f;
        public const float DefaultUndergroundEntryPackRadiusMeters = 15f;

        public static bool IsFootOnlySurfaceRegion(IoSurfaceRegionId region)
        {
            return region == IoSurfaceRegionId.LavaCalderas
                || region == IoSurfaceRegionId.PolarRadiationFlats
                || region == IoSurfaceRegionId.PrecursorRuinBelt;
        }

        public static bool IsStoryBranchBeforeCalderas(IoSurfaceRegionId region)
        {
            return region == IoSurfaceRegionId.PolarRadiationFlats;
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

    /// <summary>How a subsurface space connects to the main map.</summary>
    public enum IoUndergroundAccessKind
    {
        /// <summary>Walk-in geometry on main map — no teleport.</summary>
        SeamlessWalkIn = 0,

        /// <summary>Breach with 10–20 m pack zone and load/teleport.</summary>
        InstancedBreach = 1,

        /// <summary>Nested load from inside another underground scene.</summary>
        NestedInstance = 2
    }
}
