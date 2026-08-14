using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Compares active graphics settings to documented guidance in System_Requirements.md.
    /// Returns advisory strings only — never blocks settings.
    /// </summary>
    public static class GraphicsCapabilityAdvisor
    {
        public struct AdvisoryResult
        {
            public bool HasWarnings;
            public string Summary;
            public IReadOnlyList<string> Messages;
        }

        public static AdvisoryResult EvaluateCurrentSettings()
        {
            List<string> messages = new List<string>(8);
            int quality = QualitySettings.GetQualityLevel();
            int systemMemoryMb = SystemInfo.systemMemorySize;
            int vramMb = SystemInfo.graphicsMemorySize;

            if (quality >= PlatformGraphicsProfile.UltraTierIndex)
                messages.Add("Ultra quality targets high-end GPUs. Performance may be poor on older hardware.");

            if (GameSettings.RayTracingEnabled)
            {
                messages.Add("Ray tracing is enabled. Lower FPS or longer load times are possible on minimum-spec hardware.");
                if (vramMb > 0 && vramMb < 6144)
                    messages.Add("Detected VRAM is below 6 GB guidance for Low RT.");
            }

            if (quality >= PlatformGraphicsProfile.HighTierIndex && systemMemoryMb > 0 && systemMemoryMb < 8192)
                messages.Add("System RAM is below 8 GB minimum guidance for High quality.");

            if (quality >= PlatformGraphicsProfile.QualityTierIndex && vramMb > 0 && vramMb < 6144)
                messages.Add("Detected VRAM is below 6 GB guidance for Quality tier.");

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                messages.Add("No graphics device detected.");

            string summary = messages.Count == 0
                ? string.Empty
                : BuildSummary(messages);

            return new AdvisoryResult
            {
                HasWarnings = messages.Count > 0,
                Summary = summary,
                Messages = messages,
            };
        }

        public static string BuildSummary(IReadOnlyList<string> messages)
        {
            if (messages == null || messages.Count == 0)
                return string.Empty;

            if (messages.Count == 1)
                return messages[0];

            StringBuilder builder = new StringBuilder(messages[0]);
            for (int i = 1; i < messages.Count; i++)
            {
                builder.Append('\n');
                builder.Append(messages[i]);
            }

            return builder.ToString();
        }
    }
}
