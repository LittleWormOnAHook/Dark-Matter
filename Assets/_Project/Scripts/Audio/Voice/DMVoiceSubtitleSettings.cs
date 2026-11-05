using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// Presentation-only subtitle prefs (font size / hold time). The on/off switch is the shared
    /// Game Options value <see cref="Project.Core.GameSettings.SubtitlesEnabled"/>, so the main menu
    /// and the Kade Player Systems profile can never disagree with it.
    /// </summary>
    public static class DMVoiceSubtitleSettings
    {
        private const string FontSizeKey = "settings.voice.subtitleFontSize";
        private const string HoldSecondsKey = "settings.voice.subtitleHoldSeconds";

        public const float FontSizeMin = 14f;
        public const float FontSizeMax = 40f;
        public const float HoldMin = 0f;
        public const float HoldMax = 4f;

        /// <summary>Point size of the subtitle body. Default 24.</summary>
        public static float FontSize
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(FontSizeKey, 24f), FontSizeMin, FontSizeMax);
            set
            {
                PlayerPrefs.SetFloat(FontSizeKey, Mathf.Clamp(value, FontSizeMin, FontSizeMax));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Seconds the subtitle stays up after the line ends. Default 0.5.</summary>
        public static float HoldSeconds
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(HoldSecondsKey, 0.5f), HoldMin, HoldMax);
            set
            {
                PlayerPrefs.SetFloat(HoldSecondsKey, Mathf.Clamp(value, HoldMin, HoldMax));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Cheap re-read (Genesis Studio / settings reload). Values are read on demand anyway.</summary>
        public static void Reload() { }
    }
}
