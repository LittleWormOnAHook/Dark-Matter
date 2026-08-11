#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class GameBuildInfoGenerator
    {
        private const string OutputPath = "Assets/_Project/Resources/GameBuildInfo.txt";

        static GameBuildInfoGenerator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                WriteBuildInfo(incrementBuildNumber: false);
        }

        [MenuItem("Dark Matter/Write Build Info")]
        public static void WriteBuildInfoMenu()
        {
            WriteBuildInfo(incrementBuildNumber: true);
        }

        public static void WriteBuildInfo(bool incrementBuildNumber)
        {
            int buildNumber = ReadBuildNumber();
            if (incrementBuildNumber)
                buildNumber++;

            string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string content = $"buildDateUtc={utc}\nbuildNumber={buildNumber}\n";

            string directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(OutputPath, content);
            AssetDatabase.ImportAsset(OutputPath);
        }

        private static int ReadBuildNumber()
        {
            if (!File.Exists(OutputPath))
                return 1;

            string[] lines = File.ReadAllLines(OutputPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("buildNumber=", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (int.TryParse(lines[i].Substring("buildNumber=".Length), out int parsed))
                    return parsed;
            }

            return 1;
        }
    }

    public class GameBuildInfoPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            GameBuildInfoGenerator.WriteBuildInfo(incrementBuildNumber: true);
        }
    }
}
#endif
