using Project.Audio;
using Project.Core;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Shared apply/capture for Settings UI (uGUI or UITK).
    /// </summary>
    public static class GameSettingsUiBridge
    {
        private static readonly int[] FrameRateOptions = { -1, 30, 60, 120, 144 };

        public struct Snapshot
        {
            public float MasterVolume;
            public float MusicVolume;
            public float SfxVolume;
            public float UiScale;
            public bool PostProcessingEnabled;
            public bool BloomEnabled;
            public float BloomIntensity;
            public bool FogEnabled;
            public bool MotionBlurEnabled;
            public bool DepthOfFieldEnabled;
            public bool AmbientOcclusionEnabled;
            public bool ColorGradingEnabled;
            public bool VignetteEnabled;
            public bool MinimapEnabled;
            public bool Fullscreen;
            public bool VSync;
            public bool RayTracingEnabled;
            public bool DlssEnabled;
            public int DlssQualityDropdownIndex;
            public int TargetFrameRate;
            public int QualityLevel;
            public int ResolutionIndex;
        }

        public static Snapshot CaptureCurrent()
        {
            return new Snapshot
            {
                MasterVolume = GameSettings.MasterVolume,
                MusicVolume = GameSettings.MusicVolume,
                SfxVolume = GameSettings.SfxVolume,
                UiScale = GameSettings.UiScale,
                PostProcessingEnabled = GameSettings.PostProcessingEnabled,
                BloomEnabled = GameSettings.BloomEnabled,
                BloomIntensity = GameSettings.BloomIntensity,
                FogEnabled = GameSettings.FogEnabled,
                MotionBlurEnabled = GameSettings.MotionBlurEnabled,
                DepthOfFieldEnabled = GameSettings.DepthOfFieldEnabled,
                AmbientOcclusionEnabled = GameSettings.AmbientOcclusionEnabled,
                ColorGradingEnabled = GameSettings.ColorGradingEnabled,
                VignetteEnabled = GameSettings.VignetteEnabled,
                MinimapEnabled = GameSettings.MinimapEnabled,
                Fullscreen = GameSettings.Fullscreen,
                VSync = GameSettings.VSync,
                RayTracingEnabled = GameSettings.RayTracingEnabled,
                DlssEnabled = GameSettings.DlssEnabled,
                DlssQualityDropdownIndex = DlssSettingsApplier.GetQualityDropdownIndex(GameSettings.DlssQualityIndex),
                TargetFrameRate = GameSettings.TargetFrameRate,
                QualityLevel = GameSettings.QualityLevel,
                ResolutionIndex = GameSettings.GetCurrentResolutionIndex()
            };
        }

        /// <returns>True when a full scene reload was started (caller should not restore menus).</returns>
        public static bool ApplySnapshot(Snapshot snap, bool reloadSceneAfterApply)
        {
            Snapshot before = CaptureCurrent();

            GameSettings.SetMasterVolume(snap.MasterVolume);
            GameSettings.SetMusicVolume(snap.MusicVolume);
            GameSettings.SetSfxVolume(snap.SfxVolume);
            GameSettings.SetUiScale(snap.UiScale);
            GameSettings.SetPostProcessingEnabled(snap.PostProcessingEnabled);
            GameSettings.SetBloomEnabled(snap.BloomEnabled);
            GameSettings.SetBloomIntensity(snap.BloomIntensity);
            GameSettings.SetFogEnabled(snap.FogEnabled);
            GameSettings.SetMotionBlurEnabled(snap.MotionBlurEnabled);
            GameSettings.SetDepthOfFieldEnabled(snap.DepthOfFieldEnabled);
            GameSettings.SetAmbientOcclusionEnabled(snap.AmbientOcclusionEnabled);
            GameSettings.SetColorGradingEnabled(snap.ColorGradingEnabled);
            GameSettings.SetVignetteEnabled(snap.VignetteEnabled);
            GameSettings.SetMinimapEnabled(snap.MinimapEnabled);
            MapUI.ApplyMinimapEnabled(snap.MinimapEnabled);
            GameSettings.SetFullscreen(snap.Fullscreen);
            GameSettings.SetVSync(snap.VSync);
            GameSettings.SetRayTracingEnabled(snap.RayTracingEnabled);
            GameSettings.SetTargetFrameRate(snap.TargetFrameRate);
            GameSettings.SetQualityLevel(snap.QualityLevel, persistPreference: true, applyTierEffectPresets: false);
            GameSettings.SetResolutionIndex(snap.ResolutionIndex);
            GameSettings.SetDlssEnabled(snap.DlssEnabled);
            GameSettings.SetDlssQualityDropdownIndex(snap.DlssQualityDropdownIndex);

            GameSettings.Save();
            GameSettings.ApplyAll();
            PostProcessingController.EnsureExists();
            PostProcessingController.Instance?.RebuildRuntimeProfile();
            PostProcessingController.Instance?.ApplyFromSettings();
            GameAudioManager.Instance?.RefreshVolumes();
            UiScaleApplier.ApplyFromSettings();

            bool needsPipelineReload = before.QualityLevel != snap.QualityLevel
                || before.RayTracingEnabled != snap.RayTracingEnabled
                || before.DlssEnabled != snap.DlssEnabled
                || before.DlssQualityDropdownIndex != snap.DlssQualityDropdownIndex;

            // Resolution / audio / PP toggles apply live. Full scene reload only when the
            // render pipeline must reinitialize — forced reload was wiping UITK after resolution changes.
            if (reloadSceneAfterApply && needsPipelineReload)
            {
                SettingsSceneReloader.ReloadAfterApply();
                return true;
            }

            return false;
        }

        public static int FrameRateOptionCount => FrameRateOptions.Length;

        public static int FrameRateOptionAt(int index)
        {
            index = Mathf.Clamp(index, 0, FrameRateOptions.Length - 1);
            return FrameRateOptions[index];
        }

        public static int FrameRateDropdownIndex(int frameRate)
        {
            for (int i = 0; i < FrameRateOptions.Length; i++)
            {
                if (FrameRateOptions[i] == frameRate)
                    return i;
            }

            return 0;
        }

        public static string[] FrameRateLabels { get; } = { "Unlimited", "30 FPS", "60 FPS", "120 FPS", "144 FPS" };
    }
}
