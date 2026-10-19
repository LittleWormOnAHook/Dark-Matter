using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Applies PC / macOS / console quality defaults on boot and tier-specific LOD/post overrides.
    /// </summary>
    public static class PlatformGraphicsBootstrap
    {
        private const string QualityKey = "settings.quality";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnPlayBoot()
        {
            if (!Application.isPlaying)
                return;

#if UNITY_EDITOR
            ForceEditorPlayModePcProfile();
#else
            ApplyTierOverrides(QualitySettings.GetQualityLevel());
#endif
        }

        public static void ApplyAfterSettingsLoad()
        {
            // perf-1018b: the Editor no longer forces the PC tier here (that overrode the player's saved
            // tier on every settings load / Apply). Editor Play now behaves like a build: saved tier wins.
            if (!PlayerPrefs.HasKey(QualityKey))
            {
                int defaultLevel = PlatformGraphicsProfile.DefaultQualityIndex;
                defaultLevel = Mathf.Clamp(defaultLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                QualitySettings.SetQualityLevel(defaultLevel, applyExpensiveChanges: true);
            }

            ApplyTierOverrides(QualitySettings.GetQualityLevel());
        }

        public static void ApplyTierOverrides(int qualityLevel)
        {
            qualityLevel = Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

            switch (qualityLevel)
            {
                case PlatformGraphicsProfile.PerformanceTierIndex:
                    QualitySettings.maximumLODLevel = 2;
                    QualitySettings.lodBias = 1.2f;
                    QualitySettings.shadowDistance = 25f;
                    break;
                case PlatformGraphicsProfile.BalancedTierIndex:
                    QualitySettings.maximumLODLevel = 1;
                    QualitySettings.lodBias = 1.5f;
                    QualitySettings.shadowDistance = 30f;
                    break;
                case PlatformGraphicsProfile.QualityTierIndex:
                    QualitySettings.maximumLODLevel = 0;
                    QualitySettings.lodBias = 1.8f;
                    QualitySettings.shadowDistance = 35f;
                    break;
                case PlatformGraphicsProfile.UltraTierIndex:
                    QualitySettings.maximumLODLevel = 0;
                    QualitySettings.lodBias = 2f;
                    QualitySettings.shadowDistance = 50f;
                    break;
                default:
                    QualitySettings.maximumLODLevel = 0;
                    QualitySettings.lodBias = 2f;
                    QualitySettings.shadowDistance = 40f;
                    break;
            }

            PostProcessingController.Instance?.ApplyFromSettings();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor Play boot: apply the player's saved tier (PlayerPrefs settings.quality); the PC tier is only
        /// the fallback when nothing is saved. perf-1018b (was: always forced the PC/High tier).
        /// </summary>
        public static void ForceEditorPlayModePcProfile()
        {
            int level = PlayerPrefs.HasKey(QualityKey)
                ? PlayerPrefs.GetInt(QualityKey)
                : PlatformGraphicsProfile.PcQualityIndex;
            level = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
            ApplyTierOverrides(level);
        }
#endif
    }
}
