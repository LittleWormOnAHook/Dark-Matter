using System;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>Voice line priority. Higher wins; FIFO inside one priority band.</summary>
    public enum DMVoicePriority
    {
        Bark = 10,
        Npc = 40,
        Radio = 60,
        Story = 80,
        Cinematic = 100
    }

    /// <summary>How one line is routed through the audio stack.</summary>
    public enum DMVoiceOutputMode
    {
        /// <summary>2D, dry (Kade's journal / inner voice).</summary>
        Narration = 0,
        /// <summary>2D with comm clicks in/out.</summary>
        Radio = 1,
        /// <summary>3D on a world transform (NPC / speaker).</summary>
        World = 2
    }

    /// <summary>
    /// Per-call overrides for <see cref="DMVoiceManager.Play(DMVoiceLineRef, DMVoicePlayOptions)"/>.
    /// Null is allowed everywhere and means the defaults below.
    /// </summary>
    [Serializable]
    public sealed class DMVoicePlayOptions
    {
        public DMVoicePriority priority = DMVoicePriority.Story;
        public DMVoiceOutputMode output = DMVoiceOutputMode.Narration;
        [Tooltip("Stop whatever is playing, even at equal or higher priority.")]
        public bool interrupt;
        [Tooltip("Speaker transform for World mode. Followed while the line plays.")]
        public Transform worldTarget;
        [Range(0f, 2f)] public float volumeScale = 1f;

        public static DMVoicePlayOptions Narration(DMVoicePriority priority = DMVoicePriority.Story)
        {
            return new DMVoicePlayOptions { priority = priority, output = DMVoiceOutputMode.Narration };
        }

        public static DMVoicePlayOptions Radio(DMVoicePriority priority = DMVoicePriority.Radio)
        {
            return new DMVoicePlayOptions { priority = priority, output = DMVoiceOutputMode.Radio };
        }

        public static DMVoicePlayOptions World(Transform target, DMVoicePriority priority = DMVoicePriority.Npc)
        {
            return new DMVoicePlayOptions { priority = priority, output = DMVoiceOutputMode.World, worldTarget = target };
        }
    }
}
