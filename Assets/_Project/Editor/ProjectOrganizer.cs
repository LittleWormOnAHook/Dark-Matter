using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class ProjectOrganizer
    {
        private static readonly (string source, string destination)[] Moves =
        {
            ("Assets/_Project/Prefabs/Items/Crafting Equipment/Cooking Variant.prefab", ProjectAssetPaths.PrefabsCraftingStations + "/Cooking Variant.prefab"),
            ("Assets/_Project/Prefabs/Items/Crafting Equipment/Workbench Variant.prefab", ProjectAssetPaths.PrefabsCraftingStations + "/Workbench Variant.prefab"),
            ("Assets/_Project/Scripts/UI/InteractionText.prefab", ProjectAssetPaths.PrefabsUi + "/InteractionText.prefab"),
            ("Assets/_Project/Scripts/Player/SM_Plant_07 (7).prefab", ProjectAssetPaths.PrefabsWorld + "/SM_Plant_07 (7).prefab"),
            ("Assets/_Project/Data/Items/ItemData.cs", ProjectAssetPaths.Scripts + "/Data/ItemData.cs"),
        };

        private static readonly string[] DeletePaths =
        {
            "Assets/_Project/Scripts/Player/Survival Pioneer.code-workspace",
            "Assets/_Project/Resources/InventorySlot.prefab",
        };

        private static readonly string[] EnsureFolders =
        {
            ProjectAssetPaths.ArtTextures,
            ProjectAssetPaths.Settings,
            ProjectAssetPaths.SettingsInput,
            ProjectAssetPaths.World,
            ProjectAssetPaths.WorldTerrain,
            ProjectAssetPaths.Scenes + "/Pioneer",
            ProjectAssetPaths.ScriptsPrototypes,
            ProjectAssetPaths.EditorDevTools,
            ProjectAssetPaths.PrefabsCraftingStations,
            ProjectAssetPaths.PrefabsItemsWorld,
            ProjectAssetPaths.PrefabsWorld,
            ProjectAssetPaths.Scripts + "/Data",
            ProjectAssetPaths.Scripts + "/Audio",
            ProjectAssetPaths.Scripts + "/Combat",
            ProjectAssetPaths.Scripts + "/Map",
            ProjectAssetPaths.Scripts + "/Pet",
            ProjectAssetPaths.Scripts + "/Quests",
        };

        [MenuItem(SurvivalPioneerEditorMenus.Project + "Organize Project Folders", false, 10)]
        public static void OrganizeProject()
        {
            if (!EditorUtility.DisplayDialog(
                    "Organize _Project",
                    "Creates the canonical folder layout, moves misplaced assets, removes orphan files, and refreshes folder colors. Continue?",
                    "Organize",
                    "Cancel"))
                return;

            ProjectStructureSetup.CreateFolders();
            EnsureOrganizerFolders();

            int moved = 0;
            int deleted = 0;
            int skipped = 0;

            for (int i = 0; i < Moves.Length; i++)
            {
                (string source, string destination) move = Moves[i];
                if (!AssetExists(move.source))
                {
                    skipped++;
                    continue;
                }

                string error = AssetDatabase.MoveAsset(move.source, move.destination);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"ProjectOrganizer: Could not move '{move.source}' -> '{move.destination}': {error}");
                    skipped++;
                }
                else
                {
                    moved++;
                }
            }

            for (int i = 0; i < DeletePaths.Length; i++)
            {
                if (TryDeletePath(DeletePaths[i]))
                    deleted++;
            }

            RemoveEmptyLegacyFolders();
            AssetDatabase.Refresh();
            ProjectFolderColorizer.RefreshFolderColors();

            Debug.Log($"ProjectOrganizer complete. Moved {moved}, deleted {deleted}, skipped {skipped}.");
        }

        private static void EnsureOrganizerFolders()
        {
            for (int i = 0; i < EnsureFolders.Length; i++)
                CraftingEditorUtility.EnsureFolder(EnsureFolders[i]);
        }

        private static void RemoveEmptyLegacyFolders()
        {
            string[] legacyFolders =
            {
                "Assets/_Project/Prefabs/Items/Crafting Equipment",
                "Assets/_Project/Data/Recipes",
                "Assets/_Project/Textures/UI",
                "Assets/_Project/Textures",
            };

            for (int i = 0; i < legacyFolders.Length; i++)
                TryDeleteEmptyFolder(legacyFolders[i]);
        }

        private static void TryDeleteEmptyFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] assets = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (assets.Length > 0)
                return;

            AssetDatabase.DeleteAsset(folderPath);
        }

        private static bool TryDeletePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            string fullPath = Path.GetFullPath(assetPath);
            string metaPath = fullPath + ".meta";
            bool hadAsset = AssetExists(assetPath);
            bool hadFile = File.Exists(fullPath);
            bool hadMeta = File.Exists(metaPath);

            if (!hadAsset && !hadFile && !hadMeta)
                return false;

            if (hadAsset && AssetDatabase.DeleteAsset(assetPath))
                return true;

            if (hadFile)
                File.Delete(fullPath);

            if (hadMeta)
                File.Delete(metaPath);

            return hadFile || hadMeta;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));
        }
    }
}
