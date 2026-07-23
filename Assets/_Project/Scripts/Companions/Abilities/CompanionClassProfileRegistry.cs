using System.Collections.Generic;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Resolves per-class loadout profiles from Resources/CompanionClassProfiles.
    /// </summary>
    public static class CompanionClassProfileRegistry
    {
        private const string ResourceFolder = "CompanionClassProfiles";

        private static readonly Dictionary<SkilledPioneerClass, CompanionClassProfile> Cache =
            new Dictionary<SkilledPioneerClass, CompanionClassProfile>();

        public static CompanionClassProfile GetProfile(SkilledPioneerClass pioneerClass)
        {
            if (Cache.TryGetValue(pioneerClass, out CompanionClassProfile cached) && cached != null)
                return cached;

            CompanionClassProfile[] profiles = Resources.LoadAll<CompanionClassProfile>(ResourceFolder);
            for (int i = 0; i < profiles.Length; i++)
            {
                CompanionClassProfile profile = profiles[i];
                if (profile == null)
                    continue;

                Cache[profile.pioneerClass] = profile;
            }

            if (Cache.TryGetValue(pioneerClass, out cached))
                return cached;

            return null;
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
