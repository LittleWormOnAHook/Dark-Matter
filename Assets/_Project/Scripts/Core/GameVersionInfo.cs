using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Reads Player Settings version plus build metadata from Resources/GameBuildInfo.txt.
    /// </summary>
    public static class GameVersionInfo
    {
        private const string ResourcePath = "GameBuildInfo";

        private static bool loaded;
        private static string buildDateUtc = string.Empty;
        private static string buildNumber = string.Empty;

        public static string Version => Application.version;

        public static string BuildNumber
        {
            get
            {
                EnsureLoaded();
                return buildNumber;
            }
        }

        public static string ShortFooterLabel
        {
            get
            {
                EnsureLoaded();
                string datePart = FormatBuildDateShort();
                return string.IsNullOrEmpty(datePart)
                    ? $"v{Version}"
                    : $"v{Version} · {datePart}";
            }
        }

        public static string AboutTitle => "Dark Matter: Genesis 2160";

        public static string AboutBody
        {
            get
            {
                EnsureLoaded();

                StringBuilder lines = new StringBuilder();
                lines.AppendLine($"Version {Version}");

                if (!string.IsNullOrWhiteSpace(buildNumber))
                    lines.AppendLine($"Build {buildNumber}");

                string fullDate = FormatBuildDateLong();
                if (!string.IsNullOrEmpty(fullDate))
                    lines.AppendLine($"Built {fullDate}");

                lines.Append($"Unity {Application.unityVersion}");
                return lines.ToString();
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            ParseBuildInfo(asset.text);
        }

        private static void ParseBuildInfo(string text)
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                int separator = lines[i].IndexOf('=');
                if (separator <= 0)
                    continue;

                string key = lines[i].Substring(0, separator).Trim();
                string value = lines[i].Substring(separator + 1).Trim();

                if (key.Equals("buildDateUtc", StringComparison.OrdinalIgnoreCase))
                    buildDateUtc = value;
                else if (key.Equals("buildNumber", StringComparison.OrdinalIgnoreCase))
                    buildNumber = value;
            }
        }

        private static string FormatBuildDateShort()
        {
            if (!TryParseBuildDate(out DateTime utc))
                return string.Empty;

            return utc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatBuildDateLong()
        {
            if (!TryParseBuildDate(out DateTime utc))
                return string.Empty;

            return utc.ToString("MMM d, yyyy HH:mm", CultureInfo.InvariantCulture) + " UTC";
        }

        private static bool TryParseBuildDate(out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(buildDateUtc))
                return false;

            return DateTime.TryParse(
                buildDateUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out utc);
        }
    }
}
