using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Audio.Voice
{
    [Serializable]
    public sealed class DMVoiceLine
    {
        public string id = "line_01";
        [TextArea(1, 4)] public string subtitle = "";
        [Tooltip("Optional quest stage tag, e.g. QM.3.")] public string stageTag = "";
        [Tooltip("Per-line seed. 0 = use the actor seed. Set it when a line's voice drifts.")] public int seed;
        [Tooltip("Hash of text + actor settings at last bake.")] public string bakedHash = "";
        public AudioClip clip;

        private static readonly System.Text.RegularExpressions.Regex PauseTag =
            new System.Text.RegularExpressions.Regex(@"\s*\[pause(?:\s+[0-9]*\.?[0-9]+)?\]\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Subtitle with [pause] / [pause 0.8] tags removed (for on-screen display).</summary>
        private static readonly System.Text.RegularExpressions.Regex SentenceEnd =
            new System.Text.RegularExpressions.Regex(@"(\.{2,}|\u2026)|([.!?]+)(?=\s|$)");
        private static readonly HashSet<string> Abbrev = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "mr", "mrs", "ms", "dr", "st", "jr", "sr", "vs", "etc", "lt", "sgt", "capt", "col", "gen", "prof", "no" };

        /// <summary>Split text into sentences on . ! ? and ... (abbreviations kept; fragments under 3 words merged).</summary>
        public static List<string> SplitSentences(string text)
        {
            var raw = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return raw;
            int pos = 0;
            foreach (System.Text.RegularExpressions.Match m in SentenceEnd.Matches(text))
            {
                if (m.Groups[2].Success && m.Value == ".")
                {
                    int ws = m.Index > 0 ? text.LastIndexOfAny(new[] { ' ', '\n', '\t' }, m.Index - 1) : -1;
                    string word = text.Substring(ws + 1, m.Index - ws - 1);
                    if (Abbrev.Contains(word)) continue;
                }
                string sentence = text.Substring(pos, m.Index + m.Length - pos).Trim();
                if (sentence.Length > 0) raw.Add(sentence);
                pos = m.Index + m.Length;
            }
            if (pos < text.Length && text.Substring(pos).Trim().Length > 0) raw.Add(text.Substring(pos).Trim());
            var merged = new List<string>();
            foreach (string r in raw)
            {
                int words = r.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (words < 3 && merged.Count > 0) merged[merged.Count - 1] += " " + r;
                else merged.Add(r);
            }
            if (merged.Count > 1 && merged[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length < 3)
            { merged[1] = merged[0] + " " + merged[1]; merged.RemoveAt(0); }
            return merged;
        }

        public string SubtitleClean => string.IsNullOrEmpty(subtitle) ? "" : PauseTag.Replace(subtitle, " ").Trim();
    }

    /// <summary>Lines for one voice actor. Baked in Genesis Studio > Audio > Voices.</summary>
    [CreateAssetMenu(menuName = "Project/Audio/Voice Line Set", fileName = "DMVoiceLineSet")]
    public sealed class DMVoiceLineSet : ScriptableObject
    {
        public DMVoiceActor actor;
        public List<DMVoiceLine> lines = new List<DMVoiceLine>();

        /// <summary>
        /// 2026-11-03n. Stable key for play-once save flags ("setKey:lineId"). Asset name
        /// (e.g. "Kade_Lines") is unique per line set and identical in the editor and in a build,
        /// so a flag written in Play mode still matches after a build.
        /// </summary>
        public string SetKey => string.IsNullOrEmpty(name) ? string.Empty : name;

        public static string ComputeHash(DMVoiceActor actor, DMVoiceLine line)
        {
            string key = (line != null ? line.seed + "|" + line.subtitle : "") + "#" + (actor != null ? actor.SettingsKey() : "");
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                byte[] h = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
                return BitConverter.ToString(h, 0, 8).Replace("-", "").ToLowerInvariant();
            }
        }

        public bool IsDirty(DMVoiceLine line) => line.clip == null || line.bakedHash != ComputeHash(actor, line);
    }
}
