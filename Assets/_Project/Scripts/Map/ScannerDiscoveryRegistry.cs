using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Tracks world markers/items unlocked by scanner sweeps for map + compass icons.
    /// </summary>
    public static class ScannerDiscoveryRegistry
    {
        private static readonly HashSet<string> DiscoveredIds = new HashSet<string>(StringComparer.Ordinal);

        public static event Action Changed;

        public static bool IsDiscovered(string discoveryId)
        {
            return !string.IsNullOrWhiteSpace(discoveryId) && DiscoveredIds.Contains(discoveryId);
        }

        /// <returns>True when this id was newly discovered.</returns>
        public static bool Discover(string discoveryId)
        {
            if (string.IsNullOrWhiteSpace(discoveryId))
                return false;

            if (!DiscoveredIds.Add(discoveryId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public static string[] BuildSave()
        {
            if (DiscoveredIds.Count == 0)
                return Array.Empty<string>();

            var ids = new string[DiscoveredIds.Count];
            DiscoveredIds.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        public static void ApplySave(string[] discoveryIds)
        {
            DiscoveredIds.Clear();
            if (discoveryIds != null)
            {
                for (int i = 0; i < discoveryIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(discoveryIds[i]))
                        DiscoveredIds.Add(discoveryIds[i]);
                }
            }

            Changed?.Invoke();
        }

        public static void Clear()
        {
            if (DiscoveredIds.Count == 0)
                return;

            DiscoveredIds.Clear();
            Changed?.Invoke();
        }

        internal static void ResetStaticState()
        {
            DiscoveredIds.Clear();
            Changed = null;
        }
    }
}
