using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.UI
{
    /// <summary>
    /// 2026-11-04b. Holds direct references to every UI Toolkit asset loaded at runtime (UXML, USS, TSS, PanelSettings,
    /// and a few sprites), so a player build includes them and can find them. Those assets do not live in a
    /// Resources folder, so the old <c>Resources.Load(fileName)</c> path returned null in builds and left the game
    /// with no main menu. This registry asset is at Assets/UI Toolkit/Resources/DMUiRuntimeAssetRegistry.asset.
    /// Keys are bare file names without extension, the same strings Path.GetFileNameWithoutExtension returns.
    /// </summary>
    public sealed class DMUiRuntimeAssetRegistry : ScriptableObject
    {
        public const string ResourcePath = "DMUiRuntimeAssetRegistry";

        [Serializable]
        public struct Entry
        {
            public string key;
            public Object asset;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private static DMUiRuntimeAssetRegistry cached;
        private static bool loadAttempted;

        internal static DMUiRuntimeAssetRegistry Load()
        {
            if (cached == null && !loadAttempted)
            {
                loadAttempted = true;
                cached = Resources.Load<DMUiRuntimeAssetRegistry>(ResourcePath);
            }

            return cached;
        }

        internal static T Resolve<T>(string key) where T : Object
        {
            if (string.IsNullOrEmpty(key))
                return null;

            DMUiRuntimeAssetRegistry registry = Load();
            if (registry == null || registry.entries == null)
                return null;

            for (int i = 0; i < registry.entries.Count; i++)
            {
                Entry entry = registry.entries[i];
                if (!string.Equals(entry.key, key, StringComparison.Ordinal))
                    continue;

                if (entry.asset is T typed)
                    return typed;
            }

            return null;
        }

        /// <summary>Last-resort: targeted Resources paths only (never LoadAll("") — that walks every Resources asset and logs missing-script noise).</summary>
        internal static T ResolveByName<T>(string fileName) where T : Object
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            T direct = Resources.Load<T>(fileName);
            if (direct != null)
                return direct;

            ReadOnlySpan<string> roots = UiResourceSearchRoots;
            for (int r = 0; r < roots.Length; r++)
            {
                string path = string.IsNullOrEmpty(roots[r]) ? fileName : roots[r] + "/" + fileName;
                T hit = Resources.Load<T>(path);
                if (hit != null)
                    return hit;
            }

            return null;
        }

        private static readonly string[] UiResourceSearchRoots =
        {
            "UI",
            "UI Toolkit",
            "UITK",
            "Themes"
        };

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
            loadAttempted = false;
        }
#endif
    }
}
