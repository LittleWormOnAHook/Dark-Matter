#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Project.Pioneers;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Companions
{
    /// <summary>
    /// Keeps CompanionCatalogRegistry.asset (Assets/_Project/Resources) in sync with whatever
    /// NamedPioneerDefinition "Echo" data assets exist under Assets/_Project/Data/Companions. The
    /// registry itself must live in a Resources folder so NamedPioneerCatalog can find it via
    /// Resources.Load at runtime; the individual companion .asset files stay out of Resources so
    /// designers can freely organize/sub-folder them under Data/Companions.
    /// </summary>
    public static class CompanionCatalogRegistryUtility
    {
        public const string DataFolder = "Assets/_Project/Data/Companions";
        public const string RegistryPath = "Assets/_Project/Resources/CompanionCatalogRegistry.asset";

        public static CompanionCatalogRegistry LoadOrCreateRegistry()
        {
            CompanionCatalogRegistry registry = AssetDatabase.LoadAssetAtPath<CompanionCatalogRegistry>(RegistryPath);
            if (registry != null)
                return registry;

            EnsureFolder("Assets/_Project/Resources");
            registry = ScriptableObject.CreateInstance<CompanionCatalogRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            return registry;
        }

        /// <summary>
        /// Scans DataFolder for every NamedPioneerDefinition asset and makes sure the registry
        /// references all of them (adds missing ones, keeps existing order, drops nulls/duplicates).
        /// Returns how many new entries were added.
        /// </summary>
        public static int SyncRegistryWithDataFolder()
        {
            CompanionCatalogRegistry registry = LoadOrCreateRegistry();
            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty companionsProp = serialized.FindProperty("companions");

            List<NamedPioneerDefinition> current = new List<NamedPioneerDefinition>();
            for (int i = 0; i < companionsProp.arraySize; i++)
            {
                NamedPioneerDefinition existing =
                    companionsProp.GetArrayElementAtIndex(i).objectReferenceValue as NamedPioneerDefinition;
                if (existing != null && !current.Contains(existing))
                    current.Add(existing);
            }

            int added = 0;
            if (AssetDatabase.IsValidFolder(DataFolder))
            {
                string[] guids = AssetDatabase.FindAssets("t:NamedPioneerDefinition", new[] { DataFolder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    NamedPioneerDefinition definition = AssetDatabase.LoadAssetAtPath<NamedPioneerDefinition>(path);
                    if (definition != null && !current.Contains(definition))
                    {
                        current.Add(definition);
                        added++;
                    }
                }
            }

            companionsProp.arraySize = current.Count;
            for (int i = 0; i < current.Count; i++)
                companionsProp.GetArrayElementAtIndex(i).objectReferenceValue = current[i];

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            NamedPioneerCatalog.ReloadCache();
            return added;
        }

        /// <summary>All NamedPioneerDefinition assets found under DataFolder, regardless of whether
        /// they're currently in the registry — used by the Companion Prefab Tool window to show
        /// what's registered vs. orphaned.</summary>
        public static List<NamedPioneerDefinition> FindAllDataAssets()
        {
            List<NamedPioneerDefinition> results = new List<NamedPioneerDefinition>();
            if (!AssetDatabase.IsValidFolder(DataFolder))
                return results;

            string[] guids = AssetDatabase.FindAssets("t:NamedPioneerDefinition", new[] { DataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NamedPioneerDefinition definition = AssetDatabase.LoadAssetAtPath<NamedPioneerDefinition>(path);
                if (definition != null)
                    results.Add(definition);
            }

            return results;
        }

        /// <summary>
        /// Auto-suggests the next sequential display name for a given origin — "Echo 1", "Echo 2",
        /// etc — instead of always handing back the same generic stub name (which would immediately
        /// collide on displayName the second time someone clicks Create). Scans existing data assets
        /// of the same origin for the highest "{prefix} N" already used.
        /// </summary>
        public static string GetNextSequentialDisplayName(CompanionOrigin origin)
        {
            string prefix = origin switch
            {
                CompanionOrigin.Expedition => "Expedition Pioneer",
                CompanionOrigin.SupportShip => "Support Ship Pioneer",
                CompanionOrigin.Other => "Unique Character",
                _ => "Echo"
            };

            int highest = 0;
            string pattern = $@"^{Regex.Escape(prefix)}\s+(\d+)$";
            List<NamedPioneerDefinition> existing = FindAllDataAssets();
            for (int i = 0; i < existing.Count; i++)
            {
                NamedPioneerDefinition definition = existing[i];
                if (definition == null || definition.origin != origin || string.IsNullOrWhiteSpace(definition.displayName))
                    continue;

                Match match = Regex.Match(definition.displayName.Trim(), pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                    highest = Mathf.Max(highest, number);
            }

            return $"{prefix} {highest + 1}";
        }

        /// <summary>
        /// Deletes a companion data asset and every artifact the Companion Prefab Tool baked from it —
        /// the Companion/Echo/Recruit prefabs (whichever exist) and its entry in the catalog registry.
        /// Returns a short summary of what was removed, for a confirmation dialog.
        /// </summary>
        public static string DeleteCompanionAndArtifacts(NamedPioneerDefinition definition)
        {
            if (definition == null)
                return "Nothing to delete.";

            string displayName = definition.displayName;
            string safeName = CompanionPrefabGenerator.MakeSafeFileName(displayName);
            List<string> removed = new List<string>();

            string companionPath = $"{CompanionPrefabGenerator.CompanionsOutputFolder}/{safeName}.prefab";
            string echoPath = $"{CompanionPrefabGenerator.EchoesOutputFolder}/{safeName}_Echo.prefab";
            string recruitPath = $"{CompanionPrefabGenerator.RecruitsOutputFolder}/{safeName}_Recruit.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(companionPath) != null && AssetDatabase.DeleteAsset(companionPath))
                removed.Add("Companion prefab");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(echoPath) != null && AssetDatabase.DeleteAsset(echoPath))
                removed.Add("Echo prefab");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(recruitPath) != null && AssetDatabase.DeleteAsset(recruitPath))
                removed.Add("Recruit prefab");

            string dataAssetPath = AssetDatabase.GetAssetPath(definition);
            RemoveFromRegistry(definition);

            if (!string.IsNullOrEmpty(dataAssetPath) && AssetDatabase.DeleteAsset(dataAssetPath))
                removed.Add("data asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            NamedPioneerCatalog.ReloadCache();

            return removed.Count > 0
                ? $"Deleted {displayName}: {string.Join(", ", removed)}."
                : $"{displayName}: nothing found to delete.";
        }

        private static void RemoveFromRegistry(NamedPioneerDefinition definition)
        {
            CompanionCatalogRegistry registry = AssetDatabase.LoadAssetAtPath<CompanionCatalogRegistry>(RegistryPath);
            if (registry == null)
                return;

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty companionsProp = serialized.FindProperty("companions");

            List<NamedPioneerDefinition> remaining = new List<NamedPioneerDefinition>();
            for (int i = 0; i < companionsProp.arraySize; i++)
            {
                NamedPioneerDefinition entry =
                    companionsProp.GetArrayElementAtIndex(i).objectReferenceValue as NamedPioneerDefinition;
                if (entry != null && entry != definition)
                    remaining.Add(entry);
            }

            companionsProp.arraySize = remaining.Count;
            for (int i = 0; i < remaining.Count; i++)
                companionsProp.GetArrayElementAtIndex(i).objectReferenceValue = remaining[i];

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
        }

        public static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
