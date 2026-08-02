using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Converts ItemData assets under Resources/Mining and Resources/Harvest to MineHarvestItemData
    /// while preserving asset GUIDs (rewrites m_Script), then clears unused combat/tool fields.
    /// </summary>
    public static class MineHarvestItemMigrator
    {
        private const string LeanScriptGuid = "a8c3e1f04b2d4a6e9f7c5d8b1a0e3f42";
        private const string LeanClassId = "Assembly-CSharp::Project.Data.MineHarvestItemData";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Migrate Mining-Harvest Items To Lean Type", false, 50)]
        public static void MigrateMenu()
        {
            int converted = MigrateAll();
            EditorUtility.DisplayDialog(
                "Mine / Harvest Items",
                converted > 0
                    ? $"Converted and pruned {converted} resource item(s)."
                    : "No items needed conversion (already lean, or folders empty).",
                "OK");
        }

        public static int MigrateAll()
        {
            string[] folders =
            {
                ProjectAssetPaths.ItemsResourcesMining,
                ProjectAssetPaths.ItemsResourcesHarvest
            };

            int converted = 0;
            for (int f = 0; f < folders.Length; f++)
            {
                string folder = folders[f];
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                bool isHarvest = folder.IndexOf("/Harvest", System.StringComparison.OrdinalIgnoreCase) >= 0;
                string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (MigrateAtPath(path, isHarvest))
                        converted++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return converted;
        }

        public static bool MigrateAtPath(string path, bool isHarvest)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                return false;

            // Preserve GUID: swap script reference in YAML, then prune via typed load.
            string text = System.IO.File.ReadAllText(path);
            if (!text.Contains(LeanScriptGuid))
            {
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    @"m_Script: \{fileID: 11500000, guid: [0-9a-fA-F]+, type: 3\}",
                    $"m_Script: {{fileID: 11500000, guid: {LeanScriptGuid}, type: 3}}");
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    @"m_EditorClassIdentifier:.*",
                    $"m_EditorClassIdentifier: {LeanClassId}");
                System.IO.File.WriteAllText(path, text);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            MineHarvestItemData lean = AssetDatabase.LoadAssetAtPath<MineHarvestItemData>(path);
            if (lean == null)
            {
                Debug.LogWarning($"MineHarvestItemMigrator: failed to load lean item at {path}");
                return false;
            }

            lean.gatherKind = isHarvest ? MineHarvestGatherKind.Harvest : MineHarvestGatherKind.Mining;
            lean.itemType = ItemType.Resource;
            AssignGatherDefaults(lean, isHarvest);
            lean.PruneNonGatherFields();
            EditorUtility.SetDirty(lean);
            return true;
        }

        /// <summary>Fills empty yield/grant audio and complete VFX with project defaults.</summary>
        public static void AssignGatherDefaults(MineHarvestItemData lean, bool isHarvest)
        {
            AssignDefaultLootAudio(lean, isHarvest);
            AssignDefaultLootCompleteVfx(lean);
        }

        public static void AssignDefaultLootAudio(MineHarvestItemData lean, bool isHarvest)
        {
            if (lean == null)
                return;

            if (lean.lootYieldClip == null)
            {
                lean.lootYieldClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    isHarvest ? ProjectAssetPaths.AudioBreakWood : ProjectAssetPaths.AudioBreakStone);
            }

            if (lean.lootGrantClip == null)
            {
                lean.lootGrantClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectAssetPaths.AudioPickUp);
            }
        }

        public static void AssignDefaultLootCompleteVfx(MineHarvestItemData lean)
        {
            if (lean == null || lean.lootCompleteVfxPrefab != null)
                return;

            lean.lootCompleteVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ProjectAssetPaths.LootCompleteVfxPrefab);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Assign Mine-Harvest Loot Complete VFX Defaults", false, 51)]
        public static void AssignLootCompleteVfxDefaultsMenu()
        {
            int filled = 0;
            string[] folders =
            {
                ProjectAssetPaths.ItemsResourcesMining,
                ProjectAssetPaths.ItemsResourcesHarvest
            };

            for (int f = 0; f < folders.Length; f++)
            {
                if (!AssetDatabase.IsValidFolder(folders[f]))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:MineHarvestItemData", new[] { folders[f] });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    MineHarvestItemData lean = AssetDatabase.LoadAssetAtPath<MineHarvestItemData>(path);
                    if (lean == null || lean.lootCompleteVfxPrefab != null)
                        continue;

                    AssignDefaultLootCompleteVfx(lean);
                    if (lean.lootCompleteVfxPrefab != null)
                    {
                        EditorUtility.SetDirty(lean);
                        filled++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Mine / Harvest Complete VFX",
                filled > 0
                    ? $"Assigned default complete VFX on {filled} item(s)."
                    : "All lean items already had a complete VFX (or folders empty).",
                "OK");
        }
    }
}
