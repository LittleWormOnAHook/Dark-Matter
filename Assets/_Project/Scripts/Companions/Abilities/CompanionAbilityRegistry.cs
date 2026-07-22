using System.Collections.Generic;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Resolves authored companion ability assets from Resources/CompanionAbilities.
    /// </summary>
    public static class CompanionAbilityRegistry
    {
        private const string ResourceFolder = "CompanionAbilities";

        private static readonly Dictionary<string, CompanionAbilityData> Cache =
            new Dictionary<string, CompanionAbilityData>(System.StringComparer.OrdinalIgnoreCase);

        public static CompanionAbilityData Find(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return null;

            if (Cache.TryGetValue(abilityId, out CompanionAbilityData cached) && cached != null)
                return cached;

            EnsureLoaded();
            Cache.TryGetValue(abilityId, out cached);
            return cached;
        }

        private static void EnsureLoaded()
        {
            if (Cache.Count > 0)
                return;

            CompanionAbilityData[] abilities = Resources.LoadAll<CompanionAbilityData>(ResourceFolder);
            for (int i = 0; i < abilities.Length; i++)
            {
                CompanionAbilityData ability = abilities[i];
                if (ability == null || string.IsNullOrWhiteSpace(ability.abilityId))
                    continue;

                Cache[ability.abilityId] = ability;
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
