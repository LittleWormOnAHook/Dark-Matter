using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// PC / console graphics defaults and humanoid performance budgets.
    /// WebGL / browser is out of scope — do not reintroduce platform forks for it.
    /// </summary>
    public static class PlatformGraphicsProfile
    {
        public const int LowQualityIndex = 0;
        public const int PcQualityIndex = 1;

        /// <summary>Default quality tier for PC and console builds.</summary>
        public static int DefaultQualityIndex => PcQualityIndex;

        public static float HumanoidFullDetailDistance => 32f;
        public static float HumanoidCullDistance => 64f;
        public static float HumanoidCheckInterval => 0.25f;

        public static int DefaultMaxZoneHumanoids => 10;
        public static float DefaultZoneActivationRadius => 50f;
        public static float DefaultZoneDespawnRadius => 70f;
    }
}
