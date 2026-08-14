#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Diagnostics
{
    /// <summary>
    /// Counts and exports Unity Console errors/warnings (Editor log buffer).
    /// Menu: Tools → Dark Matter: Genesis → Diagnostics → Audit Console.
    /// </summary>
    public static class ConsoleAuditUtility
    {
        private const string MenuRoot = "Tools/Dark Matter: Genesis/Diagnostics/Audit Console";

        [MenuItem(MenuRoot, false, 1)]
        public static void AuditAndLogSummary()
        {
            ConsoleAuditResult result = Collect();
            Debug.Log(result.ToReport());
            EditorUtility.DisplayDialog(
                "Console Audit",
                $"Errors: {result.ErrorCount}\nWarnings: {result.WarningCount}\nLogs: {result.LogCount}",
                "OK");
        }

        [MenuItem(MenuRoot + " (Copy Report)", false, 2)]
        public static void AuditAndCopyReport()
        {
            ConsoleAuditResult result = Collect();
            EditorGUIUtility.systemCopyBuffer = result.ToReport();
            Debug.Log("[ConsoleAudit] Report copied to clipboard.\n" + result.ToReport());
        }

        [MenuItem("Tools/Dark Matter: Genesis/Diagnostics/Audit Resources Paths", false, 11)]
        public static void AuditResourcesPaths()
        {
            string report = ResourcesPathAudit.BuildReport();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Resources Path Audit", report, "OK");
        }

        [MenuItem("Tools/Dark Matter: Genesis/Diagnostics/Audit Resources Paths (Copy)", false, 12)]
        public static void AuditResourcesPathsCopy()
        {
            string report = ResourcesPathAudit.BuildReport();
            EditorGUIUtility.systemCopyBuffer = report;
            Debug.Log("[ResourcesPathAudit] Report copied to clipboard.\n" + report);
        }

        public static ConsoleAuditResult Collect()
        {
            var result = new ConsoleAuditResult();
            if (!TryReadLogEntries(out int errorCount, out int warningCount, out int logCount, out List<ConsoleEntry> entries))
            {
                result.Note = "LogEntries API unavailable in this Unity version.";
                return result;
            }

            result.ErrorCount = errorCount;
            result.WarningCount = warningCount;
            result.LogCount = logCount;

            var errorBuckets = new Dictionary<string, int>(StringComparer.Ordinal);
            var warningBuckets = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (ConsoleEntry entry in entries)
            {
                string key = NormalizeMessage(entry.Message);
                if (entry.Mode == LogType.Error || entry.Mode == LogType.Exception || entry.Mode == LogType.Assert)
                {
                    result.Errors.Add(entry);
                    errorBuckets[key] = errorBuckets.TryGetValue(key, out int c) ? c + 1 : 1;
                }
                else if (entry.Mode == LogType.Warning)
                {
                    result.Warnings.Add(entry);
                    warningBuckets[key] = warningBuckets.TryGetValue(key, out int c) ? c + 1 : 1;
                }
            }

            result.TopErrors = SortBuckets(errorBuckets, 12);
            result.TopWarnings = SortBuckets(warningBuckets, 12);
            return result;
        }

        private static List<KeyValuePair<string, int>> SortBuckets(Dictionary<string, int> buckets, int max)
        {
            var list = new List<KeyValuePair<string, int>>(buckets);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (list.Count > max)
                list.RemoveRange(max, list.Count - max);
            return list;
        }

        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "(empty)";

            int nl = message.IndexOf('\n');
            string firstLine = nl >= 0 ? message.Substring(0, nl) : message;
            if (firstLine.Length > 160)
                firstLine = firstLine.Substring(0, 160) + "...";
            return firstLine.Trim();
        }

        private static bool TryReadLogEntries(
            out int errorCount,
            out int warningCount,
            out int logCount,
            out List<ConsoleEntry> entries)
        {
            errorCount = warningCount = logCount = 0;
            entries = new List<ConsoleEntry>();

            Type logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType == null)
                return false;

            MethodInfo getCounts = logEntriesType.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public);
            MethodInfo getEntryInternal = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
            MethodInfo startGetting = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
            MethodInfo endGetting = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

            if (getCounts == null || getEntryInternal == null || startGetting == null || endGetting == null)
                return false;

            object[] countArgs = { 0, 0, 0 };
            getCounts.Invoke(null, countArgs);
            errorCount = (int)countArgs[0];
            warningCount = (int)countArgs[1];
            logCount = (int)countArgs[2];

            startGetting.Invoke(null, null);

            Type entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
            if (entryType == null)
            {
                endGetting.Invoke(null, null);
                return false;
            }

            object entry = Activator.CreateInstance(entryType);
            int total = errorCount + warningCount + logCount;
            for (int i = 0; i < total; i++)
            {
                object[] getArgs = { i, entry };
                bool ok = (bool)getEntryInternal.Invoke(null, getArgs);
                if (!ok)
                    continue;

                string message = entryType.GetField("message", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry) as string;
                string file = entryType.GetField("file", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry) as string;
                int line = (int)(entryType.GetField("line", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry) ?? 0);
                int modeInt = (int)(entryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry) ?? 0);
                var mode = (LogType)modeInt;

                entries.Add(new ConsoleEntry(mode, message, file, line));
            }

            endGetting.Invoke(null, null);
            return true;
        }

        public sealed class ConsoleEntry
        {
            public LogType Mode { get; }
            public string Message { get; }
            public string File { get; }
            public int Line { get; }

            public ConsoleEntry(LogType mode, string message, string file, int line)
            {
                Mode = mode;
                Message = message ?? string.Empty;
                File = file ?? string.Empty;
                Line = line;
            }
        }

        public sealed class ConsoleAuditResult
        {
            public int ErrorCount;
            public int WarningCount;
            public int LogCount;
            public string Note;
            public readonly List<ConsoleEntry> Errors = new List<ConsoleEntry>();
            public readonly List<ConsoleEntry> Warnings = new List<ConsoleEntry>();
            public List<KeyValuePair<string, int>> TopErrors = new List<KeyValuePair<string, int>>();
            public List<KeyValuePair<string, int>> TopWarnings = new List<KeyValuePair<string, int>>();

            public string ToReport()
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Unity Console Audit ===");
                sb.AppendLine($"Errors:   {ErrorCount}");
                sb.AppendLine($"Warnings: {WarningCount}");
                sb.AppendLine($"Logs:     {LogCount}");
                if (!string.IsNullOrEmpty(Note))
                    sb.AppendLine(Note);

                AppendBucketSection(sb, "Top error patterns", TopErrors);
                AppendBucketSection(sb, "Top warning patterns", TopWarnings);

                AppendSampleSection(sb, "Sample errors (up to 8)", Errors, LogType.Error);
                AppendSampleSection(sb, "Sample warnings (up to 8)", Warnings, LogType.Warning);

                return sb.ToString();
            }

            private static void AppendBucketSection(StringBuilder sb, string title, List<KeyValuePair<string, int>> buckets)
            {
                if (buckets == null || buckets.Count == 0)
                    return;

                sb.AppendLine();
                sb.AppendLine(title + ":");
                for (int i = 0; i < buckets.Count; i++)
                    sb.AppendLine($"  [{buckets[i].Value}x] {buckets[i].Key}");
            }

            private static void AppendSampleSection(StringBuilder sb, string title, List<ConsoleEntry> list, LogType type)
            {
                if (list == null || list.Count == 0)
                    return;

                sb.AppendLine();
                sb.AppendLine(title + ":");
                int shown = 0;
                for (int i = 0; i < list.Count && shown < 8; i++)
                {
                    ConsoleEntry e = list[i];
                    if (type == LogType.Error && e.Mode != LogType.Error && e.Mode != LogType.Exception && e.Mode != LogType.Assert)
                        continue;
                    if (type == LogType.Warning && e.Mode != LogType.Warning)
                        continue;

                    sb.AppendLine($"  - {NormalizeMessage(e.Message)}");
                    if (!string.IsNullOrEmpty(e.File))
                        sb.AppendLine($"    at {e.File}:{e.Line}");
                    shown++;
                }
            }
        }
    }

    /// <summary>
    /// Static scan of Resources.Load paths referenced in _Project scripts.
    /// </summary>
    public static class ResourcesPathAudit
    {
        private const string ResourcesRoot = "Assets/_Project/Resources";

        public static string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Resources Path Audit ===");

            HashSet<string> paths = CollectLoadPaths();
            int missing = 0;
            foreach (string path in paths)
            {
                if (PathExists(path))
                    continue;

                missing++;
                sb.AppendLine($"  MISSING: {path}");
            }

            sb.Insert(0, $"Paths scanned: {paths.Count} | Missing: {missing}\n");
            if (missing == 0)
                sb.AppendLine("All referenced Resources paths resolve on disk.");
            return sb.ToString();
        }

        private static HashSet<string> CollectLoadPaths()
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            string scriptsRoot = "Assets/_Project/Scripts";
            if (!System.IO.Directory.Exists(scriptsRoot))
                return paths;

            foreach (string file in System.IO.Directory.GetFiles(scriptsRoot, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string text = System.IO.File.ReadAllText(file);
                foreach (System.Text.RegularExpressions.Match match in
                         System.Text.RegularExpressions.Regex.Matches(text, @"Resources\.Load(?:All)?<[^>]+>\(""([^""]+)""\)"))
                    paths.Add(match.Groups[1].Value);
            }

            return paths;
        }

        private static bool PathExists(string resourcePath)
        {
            string[] extensions = { ".asset", ".prefab", ".mat", ".png", ".jpg", ".wav", ".mp3", ".json", ".txt", ".shader" };
            foreach (string ext in extensions)
            {
                if (System.IO.File.Exists($"{ResourcesRoot}/{resourcePath}{ext}"))
                    return true;
            }

            string folder = $"{ResourcesRoot}/{resourcePath}";
            return System.IO.Directory.Exists(folder) &&
                   System.IO.Directory.EnumerateFileSystemEntries(folder).GetEnumerator().MoveNext();
        }
    }
}
#endif
