using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// PC / macOS / console graphics defaults and humanoid performance budgets.
    /// Five quality tiers map to Genesis HDRP pipeline assets.
    /// </summary>
    public static class PlatformGraphicsProfile
    {
        public const int PerformanceTierIndex = 0;
        public const int BalancedTierIndex = 1;
        public const int QualityTierIndex = 2;
        public const int HighTierIndex = 3;
        public const int UltraTierIndex = 4;

        /// <summary>Legacy alias for bootstrap and older call sites.</summary>
        public const int LowQualityIndex = PerformanceTierIndex;

        /// <summary>Legacy alias for editor play-mode PC profile.</summary>
        public const int PcQualityIndex = HighTierIndex;

        /// <summary>Default quality tier for PC, macOS, and console builds.</summary>
        public static int DefaultQualityIndex => HighTierIndex;

        public static int TierCount => 5;

        public static float HumanoidFullDetailDistance =>
            GetQualityLevel() <= BalancedTierIndex ? 28f : 32f;

        public static float HumanoidCullDistance =>
            GetQualityLevel() <= BalancedTierIndex ? 48f : 64f;

        public static float HumanoidCheckInterval =>
            GetQualityLevel() <= PerformanceTierIndex ? 0.35f : 0.25f;

        public static int DefaultMaxZoneHumanoids =>
            GetQualityLevel() <= BalancedTierIndex ? 8 : 10;

        public static float DefaultZoneActivationRadius => 50f;
        public static float DefaultZoneDespawnRadius => 70f;

        private static int GetQualityLevel()
        {
            int maxIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
            return Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, maxIndex);
        }
    }
}
