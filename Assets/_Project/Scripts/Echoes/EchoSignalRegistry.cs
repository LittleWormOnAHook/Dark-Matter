using System;
using System.Collections.Generic;
using Project.Pioneers;
using UnityEngine;

namespace Project.Echoes
{
    [Serializable]
    public struct EchoSignalSummary
    {
        public string EntityId;
        public string DisplayName;
        public string ClassLabel;
        public EchoDisposition Disposition;
        public float SignalStrength;
        public Vector3 WorldPosition;
    }

    /// <summary>
    /// Static registry of active echo signal summaries for UI and companion sense.
    /// </summary>
    public static class EchoSignalRegistry
    {
        private static readonly List<EchoSignalSummary> Active = new List<EchoSignalSummary>();

        public static event Action OnSignalsChanged;

        public static IReadOnlyList<EchoSignalSummary> ActiveSignals => Active;

        public static void Register(EchoSignalSummary summary)
        {
            if (string.IsNullOrWhiteSpace(summary.EntityId))
                return;

            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i].EntityId != summary.EntityId)
                    continue;

                Active[i] = summary;
                OnSignalsChanged?.Invoke();
                return;
            }

            Active.Add(summary);
            OnSignalsChanged?.Invoke();
        }

        public static void Unregister(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return;

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].EntityId != entityId)
                    continue;

                Active.RemoveAt(i);
                OnSignalsChanged?.Invoke();
                return;
            }
        }

        public static void Clear()
        {
            if (Active.Count == 0)
                return;

            Active.Clear();
            OnSignalsChanged?.Invoke();
        }

        /// <summary>
        /// No-op placeholder hook for journal UI refresh when no world signals exist yet.
        /// </summary>
        public static void EnsureDefaultPlaceholder()
        {
        }

        public static IReadOnlyList<string> GetActiveSignalSummaries()
        {
            List<string> summaries = new List<string>();
            for (int i = 0; i < Active.Count; i++)
            {
                EchoSignalSummary signal = Active[i];
                string disposition = PioneerTraitUtility.GetDispositionLabel(signal.Disposition);
                int strengthPercent = Mathf.RoundToInt(Mathf.Clamp01(signal.SignalStrength) * 100f);
                summaries.Add($"{signal.DisplayName} ({signal.ClassLabel}) — {disposition} · {strengthPercent}%");
            }

            if (summaries.Count == 0)
                summaries.Add("No echo signals detected nearby.");

            return summaries;
        }
    }
}
