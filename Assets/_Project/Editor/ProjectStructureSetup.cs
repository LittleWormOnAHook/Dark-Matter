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
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Settings);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.SettingsInput);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.World);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.WorldTerrain);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ConceptualUiArt);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Data);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsMelee);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsRanged);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsAmmo);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsResources);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsTools);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsConsumables);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsVehicles);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsNodes);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesWeapons);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesConsumables);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesAmmo);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesResources);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.RecipesModules);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Prefabs);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsBuildings);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombatEnemies);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombatProjectiles);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombatVfx);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCrafting);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCraftingStations);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsEnvironment);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsEnvironmentCameraShake);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsEnvironmentExposure);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItems);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsHeld);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsAmmo);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWeapons);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWeaponsMelee);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWeaponsRanged);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsTools);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsNpcs);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPets);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPlayers);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCompanions);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsUi);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsVehicles);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorld);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorldResources);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsModels);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Resources);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesCrafting);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesQuests);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesUi);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesCombat);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesOptics);

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scenes);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scenes + "/Pioneer");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scripts);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ScriptsPrototypes);
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
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Editor);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EditorDevTools);

            AssetDatabase.Refresh();
            ProjectFolderColorizer.RefreshFolderColors();
            Debug.Log("Dark Matter Genesis folder structure is ready under Assets/_Project.");
        }
    }
}
