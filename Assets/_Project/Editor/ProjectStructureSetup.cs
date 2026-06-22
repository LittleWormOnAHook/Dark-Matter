using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class ProjectStructureSetup
    {
        [MenuItem(SurvivalPioneerEditorMenus.Project + "Project Structure", false, 0)]
        public static void CreateFolders()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Root);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Animations);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Art);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ArtIcons);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ArtTextures);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Audio);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Materials);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Shaders);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Data);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesData);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Prefabs);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCrafting);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCraftingStations);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItems);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsHeld);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsNpcs);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPlayers);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsUi);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorld);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Resources);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesCrafting);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesQuests);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesUi);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesCombat);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesOptics);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scenes);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Core");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Player");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Inventory");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Crafting");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Survival");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Interaction");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/UI");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Managers");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Audio");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Combat");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Map");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Pet");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Quests");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts + "/Data");

            AssetDatabase.Refresh();
            ProjectFolderColorizer.RefreshFolderColors();
            Debug.Log("Survival Pioneer folder structure is ready under Assets/_Project.");
        }
    }
}
