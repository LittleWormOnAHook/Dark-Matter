using System.Collections.Generic;
using UnityEngine;

namespace Project.Environment.Doors
{
    /// <summary>
    /// Resolves <see cref="DMSlidingDoorProfile"/> assets from Resources/Environment/SlidingDoors by profileId.
    /// </summary>
    public static class DMSlidingDoorProfileRegistry
    {
        public const string ResourceFolder = "Environment/SlidingDoors";
        public const string DefaultProfileId = "scifi_big_horizontal";

        private static readonly Dictionary<string, DMSlidingDoorProfile> ById =
            new Dictionary<string, DMSlidingDoorProfile>(System.StringComparer.OrdinalIgnoreCase);

        private static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            ById.Clear();
            loaded = false;
        }

        public static DMSlidingDoorProfile Resolve(string profileId)
        {
            EnsureLoaded();

            if (!string.IsNullOrWhiteSpace(profileId)
                && ById.TryGetValue(profileId.Trim(), out DMSlidingDoorProfile found)
                && found != null)
                return found;

            if (ById.TryGetValue(DefaultProfileId, out found) && found != null)
                return found;

            foreach (KeyValuePair<string, DMSlidingDoorProfile> pair in ById)
            {
                if (pair.Value != null)
                    return pair.Value;
            }

            return null;
        }

        public static void ClearCache()
        {
            ById.Clear();
            loaded = false;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;
            DMSlidingDoorProfile[] profiles = Resources.LoadAll<DMSlidingDoorProfile>(ResourceFolder);
            for (int i = 0; i < profiles.Length; i++)
            {
                DMSlidingDoorProfile profile = profiles[i];
                if (profile == null || string.IsNullOrWhiteSpace(profile.profileId))
                    continue;

                ById[profile.profileId.Trim()] = profile;
            }
        }
    }
}
