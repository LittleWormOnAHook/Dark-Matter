using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.AI
{
    [Serializable]
    public class SurfaceEncounterSpawnEntry
    {
        [Tooltip("Optional filter. Any = matches all anchors.")]
        public SurfaceThreatKind threatKind = SurfaceThreatKind.Any;

        [Tooltip("Spawn-ready combat enemy or DMI creature prefab.")]
        public GameObject prefab;

        [Min(0)]
        public int weight = 1;

        [Tooltip("When set, overrides the prefab definition behavior preset at spawn.")]
        public EnemyBehaviorPreset behaviorPreset = EnemyBehaviorPreset.Custom;
    }

    /// <summary>
    /// Weighted random pool for expedition and full-map surface threats.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SurfaceEncounterTable",
        menuName = "Dark Matter Genesis/Encounters/Surface Encounter Table")]
    public class SurfaceEncounterTable : ScriptableObject
    {
        [SerializeField] private SurfaceEncounterSpawnEntry[] entries = Array.Empty<SurfaceEncounterSpawnEntry>();

        public IReadOnlyList<SurfaceEncounterSpawnEntry> Entries => entries;

        public bool TryPickRandom(
            SurfaceThreatKind preferredKind,
            out SurfaceEncounterSpawnEntry picked,
            System.Random rng = null)
        {
            picked = null;
            if (entries == null || entries.Length == 0)
                return false;

            int totalWeight = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                SurfaceEncounterSpawnEntry entry = entries[i];
                if (!IsValidEntry(entry))
                    continue;

                if (!MatchesKind(entry.threatKind, preferredKind))
                    continue;

                totalWeight += Mathf.Max(1, entry.weight);
            }

            if (totalWeight <= 0)
                return false;

            int roll = Roll(rng, totalWeight);
            for (int i = 0; i < entries.Length; i++)
            {
                SurfaceEncounterSpawnEntry entry = entries[i];
                if (!IsValidEntry(entry))
                    continue;

                if (!MatchesKind(entry.threatKind, preferredKind))
                    continue;

                int weight = Mathf.Max(1, entry.weight);
                roll -= weight;
                if (roll < 0)
                {
                    picked = entry;
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidEntry(SurfaceEncounterSpawnEntry entry)
        {
            return entry != null && entry.prefab != null && entry.weight > 0;
        }

        private static bool MatchesKind(SurfaceThreatKind entryKind, SurfaceThreatKind preferredKind)
        {
            if (preferredKind == SurfaceThreatKind.Any || entryKind == SurfaceThreatKind.Any)
                return true;

            return entryKind == preferredKind;
        }

        private static int Roll(System.Random rng, int totalWeight)
        {
            if (rng != null)
                return rng.Next(0, totalWeight);

            return UnityEngine.Random.Range(0, totalWeight);
        }
    }
}
