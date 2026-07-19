using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Applies PC / console quality defaults on boot and tier-specific LOD/post overrides.
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
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                ForceEditorPlayModePcProfile();
                return;
            }
#endif

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
            bool lowTier = qualityLevel == PlatformGraphicsProfile.LowQualityIndex;

            QualitySettings.maximumLODLevel = lowTier ? 1 : 0;
            QualitySettings.lodBias = lowTier ? 1.5f : 2f;
            QualitySettings.shadowDistance = lowTier ? 30f : 40f;

            PostProcessingController.Instance?.ApplyFromSettings();
        }

#if UNITY_EDITOR
        public static void ForceEditorPlayModePcProfile()
        {
            int pcLevel = Mathf.Clamp(
                PlatformGraphicsProfile.PcQualityIndex,
                0,
                Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(pcLevel, applyExpensiveChanges: true);
            ApplyTierOverrides(pcLevel);
        }
#endif
    }
}
