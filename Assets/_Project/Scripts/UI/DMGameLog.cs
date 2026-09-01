using System.Collections.Generic;
using UnityEngine;

namespace Project.UI
{
    public enum DMGameLogKind
    {
        Popup,
        Pickup,
        Prompt,
        Dialogue,
        Radio,
        Other
    }

    public readonly struct DMGameLogEntry
    {
        public readonly float Time;
        public readonly string Text;
        public readonly DMGameLogKind Kind;

        public DMGameLogEntry(float time, string text, DMGameLogKind kind)
        {
            Time = time;
            Text = text;
            Kind = kind;
        }
    }

    public static class DMGameLog
    {
        public const int MaxEntries = 250;
        private const float DedupWindowSeconds = 0.25f;

        private static readonly List<DMGameLogEntry> entries = new List<DMGameLogEntry>(64);
        private static string lastText;
        private static float lastTime = float.NegativeInfinity;

        public static event System.Action Changed;

        public static IReadOnlyList<DMGameLogEntry> Entries => entries;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            entries.Clear();
            Changed = null;
            lastText = null;
            lastTime = float.NegativeInfinity;
        }

        public static void Add(string text, DMGameLogKind kind = DMGameLogKind.Other)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.Trim();
            float now = Time.unscaledTime;
            if (lastText != null
                && string.Equals(lastText, trimmed, System.StringComparison.Ordinal)
                && now - lastTime < DedupWindowSeconds)
                return;

            lastText = trimmed;
            lastTime = now;
            entries.Add(new DMGameLogEntry(now, trimmed, kind));
            while (entries.Count > MaxEntries)
                entries.RemoveAt(0);

            Changed?.Invoke();
        }

        public static DMGameLogKind KindFromPopupText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DMGameLogKind.Popup;

            string t = text.Trim();
            if (t.Length >= 2 && t[0] == '+' && char.IsDigit(t[1]))
                return DMGameLogKind.Pickup;
            if (t.IndexOf("Inventory full", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMGameLogKind.Pickup;
            return DMGameLogKind.Popup;
        }
    }
}
