using System;
using System.Collections.Generic;

namespace Project.PPT
{
    public static class PptKeywordLog
    {
        private static readonly HashSet<string> KnownIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> SourceById = new Dictionary<string, string>(StringComparer.Ordinal);

        public static event Action<string, string> KeywordLogged;

        public static bool IsKnown(string pptId)
        {
            return !string.IsNullOrWhiteSpace(pptId) && KnownIds.Contains(pptId);
        }

        public static bool TryGetSource(string pptId, out string source)
        {
            return SourceById.TryGetValue(pptId, out source);
        }

        public static IReadOnlyCollection<string> GetKnownIds()
        {
            return KnownIds;
        }

        public static bool Log(string pptId, string source)
        {
            if (string.IsNullOrWhiteSpace(pptId))
                return false;

            if (!KnownIds.Add(pptId))
                return false;

            SourceById[pptId] = source ?? string.Empty;
            KeywordLogged?.Invoke(pptId, source);
            return true;
        }

        public static void LogMany(string[] pptIds, string source)
        {
            if (pptIds == null)
                return;

            for (int i = 0; i < pptIds.Length; i++)
                Log(pptIds[i], source);
        }

        public static string[] BuildSave()
        {
            if (KnownIds.Count == 0)
                return Array.Empty<string>();

            var ids = new string[KnownIds.Count];
            KnownIds.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        public static void ApplySave(string[] keywordIds)
        {
            KnownIds.Clear();
            SourceById.Clear();

            if (keywordIds == null)
                return;

            for (int i = 0; i < keywordIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(keywordIds[i]))
                    KnownIds.Add(keywordIds[i]);
            }
        }

        public static void Clear()
        {
            KnownIds.Clear();
            SourceById.Clear();
        }
    }
}
