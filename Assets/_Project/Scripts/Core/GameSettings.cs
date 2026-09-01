using Project.Audio;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Core
{
    public static class GameSettings
    {
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string SfxVolumeKey = "settings.sfxVolume";
        private const string PostProcessingKey = "settings.postProcessing";
        private const string BloomKey = "settings.pp.bloom";
        private const string MotionBlurKey = "settings.pp.motionBlur";
        private const string DepthOfFieldKey = "settings.pp.dof";
        private const string AmbientOcclusionKey = "settings.pp.ao";
        private const string ColorGradingKey = "settings.pp.colorGrading";
        private const string VignetteKey = "settings.pp.vignette";
        private const string FullscreenKey = "settings.fullscreen";
        private const string VSyncKey = "settings.vsync";
        private const string QualityKey = "settings.quality";
        private const string ResolutionIndexKey = "settings.resolutionIndex";
        private const string ResolutionWidthKey = "settings.resolutionWidth";
        private const string ResolutionHeightKey = "settings.resolutionHeight";
        private const string MinimapEnabledKey = "settings.mapSystemEnabled";
        private const string RayTracingKey = "settings.rayTracing";
        private const string FogKey = "settings.pp.fog";
        private const string BloomIntensityKey = "settings.pp.bloomIntensity";
        private const string DlssEnabledKey = "settings.dlss.enabled";
        private const string DlssQualityKey = "settings.dlss.quality";
        private const string UiScaleKey = "settings.uiScale";
        private const string TargetFrameRateKey = "settings.targetFrameRate";
        private const string SaveExistsKey = "save.exists";

        public const float UiScaleMin = 0.4f;
        public const float UiScaleMax = 1.25f;

        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static float UiScale { get; private set; } = 1f;
        public static bool PostProcessingEnabled { get; private set; } = true;
        public static bool BloomEnabled { get; private set; } = true;
        public static bool MotionBlurEnabled { get; private set; }
        public static bool DepthOfFieldEnabled { get; private set; }
        public static bool AmbientOcclusionEnabled { get; private set; } = true;
        public static bool ColorGradingEnabled { get; private set; } = true;
        public static bool VignetteEnabled { get; private set; } = true;
        public static bool MinimapEnabled { get; private set; } = true;
        public static bool Fullscreen { get; private set; } = true;
        public static bool VSync { get; private set; } = true;
        public static bool RayTracingEnabled { get; private set; }
        public static bool FogEnabled { get; private set; } = true;
        public static float BloomIntensity { get; private set; }
        public static bool DlssEnabled { get; private set; }
        public static int DlssQualityIndex { get; private set; } = 2;
        public static int TargetFrameRate { get; private set; } = -1;
        public static int QualityLevel { get; private set; }

        public static bool HasSaveFile => GameSaveSystem.HasAnySaveFile;

        /// <summary>Re-reads every setting from PlayerPrefs and applies them (used after settings scene reload).</summary>
        public static void ReloadFromPlayerPrefs()
        {
            Load();
        }

        public static void ApplyAll()
        {
            ApplyQualityLevel(QualityLevel, persistPreference: false);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            ApplyTargetFrameRate();
            ApplyAudio();
            ApplyDisplay();
            SetResolutionIndex(GetCurrentResolutionIndex());
            PlatformGraphicsBootstrap.ApplyAfterSettingsLoad();
            Project.UI.UiScaleApplier.ApplyFromSettings();
            DlssSettingsApplier.ApplyFromGameSettings();
            PostProcessingController.EnsureExists();
            PostProcessingController.Instance?.RebuildRuntimeProfile();
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.85f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            UiScale = Mathf.Clamp(PlayerPrefs.GetFloat(UiScaleKey, 1f), UiScaleMin, UiScaleMax);
            PostProcessingEnabled = PlayerPrefs.GetInt(PostProcessingKey, 1) == 1;
            BloomEnabled = PlayerPrefs.GetInt(BloomKey, 1) == 1;
            MotionBlurEnabled = PlayerPrefs.GetInt(MotionBlurKey, 0) == 1;
            DepthOfFieldEnabled = PlayerPrefs.GetInt(DepthOfFieldKey, 0) == 1;
            AmbientOcclusionEnabled = PlayerPrefs.GetInt(AmbientOcclusionKey, 1) == 1;
            ColorGradingEnabled = PlayerPrefs.GetInt(ColorGradingKey, 1) == 1;
            VignetteEnabled = PlayerPrefs.GetInt(VignetteKey, 1) == 1;
            MinimapEnabled = PlayerPrefs.GetInt(MinimapEnabledKey, 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            VSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            RayTracingEnabled = PlayerPrefs.GetInt(RayTracingKey, 0) == 1;
            FogEnabled = PlayerPrefs.GetInt(FogKey, 1) == 1;
            BloomIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat(BloomIntensityKey, 0f));
            DlssEnabled = PlayerPrefs.GetInt(DlssEnabledKey, 0) == 1;
            DlssQualityIndex = PlayerPrefs.GetInt(DlssQualityKey, 2);
            TargetFrameRate = PlayerPrefs.GetInt(TargetFrameRateKey, -1);

            int quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            quality = Mathf.Clamp(quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualityLevel = quality;
            ApplyQualityLevel(quality, persistPreference: false);

            QualitySettings.vSyncCount = VSync ? 1 : 0;
            ApplyTargetFrameRate();
            ApplyAudio();
            ApplyDisplay();
            SetResolutionIndex(GetCurrentResolutionIndex());
            PlatformGraphicsBootstrap.ApplyAfterSettingsLoad();
            Project.UI.UiScaleApplier.ApplyFromSettings();
            DlssSettingsApplier.ApplyFromGameSettings();
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.SetFloat(UiScaleKey, UiScale);
            PlayerPrefs.SetInt(PostProcessingKey, PostProcessingEnabled ? 1 : 0);
            PlayerPrefs.SetInt(BloomKey, BloomEnabled ? 1 : 0);
            PlayerPrefs.SetInt(MotionBlurKey, MotionBlurEnabled ? 1 : 0);
            PlayerPrefs.SetInt(DepthOfFieldKey, DepthOfFieldEnabled ? 1 : 0);
            PlayerPrefs.SetInt(AmbientOcclusionKey, AmbientOcclusionEnabled ? 1 : 0);
            PlayerPrefs.SetInt(ColorGradingKey, ColorGradingEnabled ? 1 : 0);
            PlayerPrefs.SetInt(VignetteKey, VignetteEnabled ? 1 : 0);
            PlayerPrefs.SetInt(MinimapEnabledKey, MinimapEnabled ? 1 : 0);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey, VSync ? 1 : 0);
            PlayerPrefs.SetInt(RayTracingKey, RayTracingEnabled ? 1 : 0);
            PlayerPrefs.SetInt(FogKey, FogEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(BloomIntensityKey, BloomIntensity);
            PlayerPrefs.SetInt(DlssEnabledKey, DlssEnabled ? 1 : 0);
            PlayerPrefs.SetInt(DlssQualityKey, DlssQualityIndex);
            PlayerPrefs.SetInt(TargetFrameRateKey, TargetFrameRate);
            PlayerPrefs.SetInt(QualityKey, QualitySettings.GetQualityLevel());
            PlayerPrefs.SetInt(ResolutionIndexKey, GetCurrentResolutionIndex());
            PlayerPrefs.Save();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            ApplyAudio();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            GameAudioManager.Instance?.RefreshVolumes();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            GameAudioManager.Instance?.RefreshVolumes();
        }

        public static void SetUiScale(float value)
        {
            UiScale = Mathf.Clamp(value, UiScaleMin, UiScaleMax);
            Project.UI.UiScaleApplier.ApplyFromSettings();
        }

        public static void SetPostProcessingEnabled(bool enabled)
        {
            PostProcessingEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetBloomEnabled(bool enabled)
        {
            BloomEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetBloomIntensity(float normalizedBoost)
        {
            BloomIntensity = Mathf.Clamp01(normalizedBoost);
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetFogEnabled(bool enabled)
        {
            FogEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetTargetFrameRate(int frameRate)
        {
            TargetFrameRate = frameRate;
            ApplyTargetFrameRate();
        }

        public static void SetDlssEnabled(bool enabled)
        {
            DlssEnabled = enabled;
            DlssSettingsApplier.ApplyFromGameSettings();
        }

        public static void SetDlssQualityIndex(int enumValue)
        {
            DlssQualityIndex = enumValue;
            DlssSettingsApplier.ApplyFromGameSettings();
        }

        public static void SetDlssQualityDropdownIndex(int dropdownIndex)
        {
            DlssQualityIndex = DlssSettingsApplier.GetQualityEnumFromDropdownIndex(dropdownIndex);
            DlssSettingsApplier.ApplyFromGameSettings();
        }

        /// <summary>Preview-only quality change from the settings panel (no PlayerPrefs write).</summary>
        public static void PreviewQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            ApplyEffectPresetForQualityTier(QualityLevel, applyRuntime: true);
        }

        public static void ApplyEffectPresetForQualityTier(int level, bool applyRuntime)
        {
            // Lightweight tier presets so Overall Quality updates post-FX toggles before Save.
            bool performance = level <= 0;
            bool balanced = level == 1;
            bool qualityPlus = level >= 2;
            bool ultra = level >= 4;

            PostProcessingEnabled = !performance;
            BloomEnabled = !performance;
            FogEnabled = qualityPlus;
            MotionBlurEnabled = ultra;
            DepthOfFieldEnabled = ultra;
            AmbientOcclusionEnabled = qualityPlus;
            ColorGradingEnabled = !performance;
            VignetteEnabled = qualityPlus || balanced;
            RayTracingEnabled = ultra;
            if (performance)
                TargetFrameRate = 60;
            else if (ultra)
                TargetFrameRate = -1;

            if (applyRuntime)
            {
                ApplyTargetFrameRate();
                PostProcessingController.Instance?.ApplyFromSettings();
            }
        }

        public static void SetMotionBlurEnabled(bool enabled)
        {
            MotionBlurEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetDepthOfFieldEnabled(bool enabled)
        {
            DepthOfFieldEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetAmbientOcclusionEnabled(bool enabled)
        {
            AmbientOcclusionEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetColorGradingEnabled(bool enabled)
        {
            ColorGradingEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static void SetVignetteEnabled(bool enabled)
        {
            VignetteEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static bool IsPostEffectEnabled(string componentTypeName)
        {
            if (string.IsNullOrEmpty(componentTypeName))
                return true;

            return componentTypeName switch
            {
                "Bloom" => BloomEnabled,
                "Fog" => FogEnabled,
                "MotionBlur" => MotionBlurEnabled,
                "DepthOfField" => DepthOfFieldEnabled,
                "AmbientOcclusion" => AmbientOcclusionEnabled,
                "ScreenSpaceAmbientOcclusion" => AmbientOcclusionEnabled,
                "ColorAdjustments" => ColorGradingEnabled,
                "LiftGammaGain" => ColorGradingEnabled,
                "SplitToning" => ColorGradingEnabled,
                "Tonemapping" => ColorGradingEnabled,
                "WhiteBalance" => ColorGradingEnabled,
                "Vignette" => VignetteEnabled,
                "RayTracingSettings" => RayTracingEnabled,
                _ => true,
            };
        }

        public static void SetMinimapEnabled(bool enabled)
        {
            MinimapEnabled = enabled;
        }

        public static void SetFullscreen(bool enabled)
        {
            Fullscreen = enabled;
            ApplyDisplay();
            SetResolutionIndex(GetCurrentResolutionIndex());
        }

        public static void SetVSync(bool enabled)
        {
            VSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        public static void SetQualityLevel(int level)
        {
            SetQualityLevel(level, persistPreference: true, applyTierEffectPresets: false);
        }

        public static void SetQualityLevel(int level, bool persistPreference, bool applyTierEffectPresets)
        {
            level = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (applyTierEffectPresets)
                ApplyEffectPresetForQualityTier(level, applyRuntime: false);
            ApplyQualityLevel(level, persistPreference);
        }

        public static void SetRayTracingEnabled(bool enabled)
        {
            RayTracingEnabled = enabled;
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        public static string GetGraphicsAdvisorySummary()
        {
            return GraphicsCapabilityAdvisor.EvaluateCurrentSettings().Summary;
        }

        public static void SetResolutionIndex(int index)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, resolutions.Length - 1);
            Resolution resolution = resolutions[index];
            PlayerPrefs.SetInt(ResolutionIndexKey, index);
            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
            // Borderless fullscreen is safer for UI Toolkit / panel scaling than exclusive mode.
            FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(resolution.width, resolution.height, mode);
        }

        public static int GetCurrentResolutionIndex()
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return 0;

            int savedW = PlayerPrefs.GetInt(ResolutionWidthKey, 0);
            int savedH = PlayerPrefs.GetInt(ResolutionHeightKey, 0);
            if (savedW > 0 && savedH > 0)
            {
                for (int i = resolutions.Length - 1; i >= 0; i--)
                {
                    if (resolutions[i].width == savedW && resolutions[i].height == savedH)
                        return i;
                }
            }

            int saved = PlayerPrefs.GetInt(ResolutionIndexKey, -1);
            if (saved >= 0 && saved < resolutions.Length)
                return saved;

            for (int i = resolutions.Length - 1; i >= 0; i--)
            {
                if (resolutions[i].width == Screen.width &&
                    resolutions[i].height == Screen.height)
                    return i;
            }

            for (int i = resolutions.Length - 1; i >= 0; i--)
            {
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                    return i;
            }

            return resolutions.Length - 1;
        }

        /// <summary>
        /// Unique width×height choices for settings UI (highest refresh rate kept per size).
        /// </summary>
        public static void BuildUniqueResolutionChoices(
            System.Collections.Generic.List<string> labels,
            System.Collections.Generic.List<int> sourceIndices)
        {
            labels?.Clear();
            sourceIndices?.Clear();
            if (labels == null || sourceIndices == null)
                return;

            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return;

            var bestBySize = new System.Collections.Generic.Dictionary<long, int>(resolutions.Length);
            for (int i = 0; i < resolutions.Length; i++)
            {
                Resolution r = resolutions[i];
                long key = ((long)r.width << 32) | (uint)r.height;
                if (!bestBySize.TryGetValue(key, out int existing)
                    || r.refreshRateRatio.value >= resolutions[existing].refreshRateRatio.value)
                    bestBySize[key] = i;
            }

            var ordered = new System.Collections.Generic.List<int>(bestBySize.Values);
            ordered.Sort((a, b) =>
            {
                int area = (resolutions[a].width * resolutions[a].height)
                    .CompareTo(resolutions[b].width * resolutions[b].height);
                return area != 0 ? area : a.CompareTo(b);
            });

            for (int i = 0; i < ordered.Count; i++)
            {
                int src = ordered[i];
                Resolution r = resolutions[src];
                labels.Add($"{r.width} x {r.height}");
                sourceIndices.Add(src);
            }
        }

        public static int FindUniqueResolutionChoiceIndex(
            System.Collections.Generic.IReadOnlyList<int> sourceIndices,
            int rawResolutionIndex)
        {
            if (sourceIndices == null || sourceIndices.Count == 0)
                return 0;

            for (int i = 0; i < sourceIndices.Count; i++)
            {
                if (sourceIndices[i] == rawResolutionIndex)
                    return i;
            }

            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return 0;

            rawResolutionIndex = Mathf.Clamp(rawResolutionIndex, 0, resolutions.Length - 1);
            int w = resolutions[rawResolutionIndex].width;
            int h = resolutions[rawResolutionIndex].height;
            for (int i = 0; i < sourceIndices.Count; i++)
            {
                Resolution r = resolutions[sourceIndices[i]];
                if (r.width == w && r.height == h)
                    return i;
            }

            return Mathf.Clamp(sourceIndices.Count - 1, 0, sourceIndices.Count - 1);
        }

        public static void MarkSaveExists(bool exists)
        {
            PlayerPrefs.SetInt(SaveExistsKey, exists ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ApplyQualityLevel(int level, bool persistPreference)
        {
            level = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualityLevel = level;
            QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);

            RenderPipelineAsset pipeline = QualitySettings.renderPipeline;
            if (pipeline != null)
                GraphicsSettings.defaultRenderPipeline = pipeline;

            PlatformGraphicsBootstrap.ApplyTierOverrides(level);
            PostProcessingController.Instance?.ApplyFromSettings();

            if (persistPreference)
                PlayerPrefs.SetInt(QualityKey, level);
        }

        private static void ApplyTargetFrameRate()
        {
            Application.targetFrameRate = TargetFrameRate;
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
        }

        private static void ApplyDisplay()
        {
            // Prefer borderless fullscreen — exclusive mode + resolution flips often blank UITK panels.
            FullScreenMode desired = Fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            if (Screen.fullScreenMode == desired)
                return;

            Screen.fullScreenMode = desired;
        }
    }
}
