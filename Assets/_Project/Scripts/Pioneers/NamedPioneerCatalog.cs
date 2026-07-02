using System.Collections.Generic;
using UnityEngine;

namespace Project.Pioneers
{
    public static class NamedPioneerCatalog
    {
        private static NamedPioneerDefinition[] cachedDefinitions;
        private static bool cacheLoaded;

        public static IReadOnlyList<NamedPioneerDefinition> GetAllDefinitions()
        {
            EnsureLoaded();
            return cachedDefinitions ?? System.Array.Empty<NamedPioneerDefinition>();
        }

        public static NamedPioneerDefinition FindById(string pioneerId)
        {
            if (string.IsNullOrWhiteSpace(pioneerId))
                return null;

            EnsureLoaded();
            if (cachedDefinitions == null)
                return null;

            for (int i = 0; i < cachedDefinitions.Length; i++)
            {
                NamedPioneerDefinition definition = cachedDefinitions[i];
                if (definition != null && definition.ResolvedId == pioneerId)
                    return definition;
            }

            return null;
        }

        public static NamedPioneerDefinition FindByDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            EnsureLoaded();
            if (cachedDefinitions == null)
                return null;

            for (int i = 0; i < cachedDefinitions.Length; i++)
            {
                NamedPioneerDefinition definition = cachedDefinitions[i];
                if (definition != null && definition.displayName == displayName)
                    return definition;
            }

            return null;
        }

        private static void EnsureLoaded()
        {
            if (cacheLoaded)
                return;

            cacheLoaded = true;
            cachedDefinitions = Resources.LoadAll<NamedPioneerDefinition>("Pioneers");
        }

        public static void ReloadCache()
        {
            cacheLoaded = false;
            cachedDefinitions = null;
            EnsureLoaded();
        }
    }
}
