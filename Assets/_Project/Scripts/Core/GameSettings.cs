using Project.Audio;
using UnityEngine;

namespace Project.Core
{
    public static class GameSettings
    {
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string SfxVolumeKey = "settings.sfxVolume";
        private const string PostProcessingKey = "settings.postProcessing";
        private const string FullscreenKey = "settings.fullscreen";
        private const string VSyncKey = "settings.vsync";
        private const string QualityKey = "settings.quality";
        private const string ResolutionIndexKey = "settings.resolutionIndex";
        private const string MapSystemEnabledKey = "settings.mapSystemEnabled";
        private const string SaveExistsKey = "save.exists";

        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static bool PostProcessingEnabled { get; private set; } = true;
        public static bool MapSystemEnabled { get; private set; } = true;
        public static bool Fullscreen { get; private set; } = true;
        public static bool VSync { get; private set; } = true;

        public static bool HasSaveFile => GameSaveSystem.HasAnySaveFile;

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.85f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            PostProcessingEnabled = PlayerPrefs.GetInt(PostProcessingKey, 1) == 1;
            MapSystemEnabled = PlayerPrefs.GetInt(MapSystemEnabledKey, 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            VSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

            int quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            quality = Mathf.Clamp(quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(quality, applyExpensiveChanges: true);

            QualitySettings.vSyncCount = VSync ? 1 : 0;
            ApplyAudio();
            ApplyDisplay();
            SetResolutionIndex(GetCurrentResolutionIndex());
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.SetInt(PostProcessingKey, PostProcessingEnabled ? 1 : 0);
            PlayerPrefs.SetInt(MapSystemEnabledKey, MapSystemEnabled ? 1 : 0);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey, VSync ? 1 : 0);
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

        public static void SetPostProcessingEnabled(bool enabled)
        {
            PostProcessingEnabled = enabled;
        }

        public static void SetMapSystemEnabled(bool enabled)
        {
            MapSystemEnabled = enabled;
        }

        public static void SetFullscreen(bool enabled)
        {
            Fullscreen = enabled;
            ApplyDisplay();
        }

        public static void SetVSync(bool enabled)
        {
            VSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        public static void SetQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        }

        public static void SetResolutionIndex(int index)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, resolutions.Length - 1);
            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Fullscreen);
        }

        public static int GetCurrentResolutionIndex()
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return 0;

            int saved = PlayerPrefs.GetInt(ResolutionIndexKey, -1);
            if (saved >= 0 && saved < resolutions.Length)
                return saved;

            for (int i = resolutions.Length - 1; i >= 0; i--)
            {
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                    return i;
            }

            return resolutions.Length - 1;
        }

        public static void MarkSaveExists(bool exists)
        {
            PlayerPrefs.SetInt(SaveExistsKey, exists ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
        }

        private static void ApplyDisplay()
        {
            if (Screen.fullScreen == Fullscreen)
                return;

            Screen.fullScreen = Fullscreen;
        }
    }
}
