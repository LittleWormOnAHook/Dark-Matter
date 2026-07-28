using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Tints Project window folders under Assets/_Project for quicker navigation.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectFolderColorizer
    {
        private static readonly Dictionary<string, Color> FolderColors = new Dictionary<string, Color>
        {
            { "Assets/_Project", new Color(0.28f, 0.28f, 0.28f) },
            { ProjectAssetPaths.Animations, new Color(0.95f, 0.55f, 0.15f) },
            { ProjectAssetPaths.AnimationsEnemies, new Color(0.90f, 0.45f, 0.18f) },
            { ProjectAssetPaths.AnimationsNpcs, new Color(0.92f, 0.60f, 0.22f) },
            { ProjectAssetPaths.Art, new Color(0.92f, 0.35f, 0.55f) },
            { ProjectAssetPaths.ArtIcons, new Color(0.95f, 0.42f, 0.60f) },
            { ProjectAssetPaths.ArtTextures, new Color(0.88f, 0.32f, 0.50f) },
            { ProjectAssetPaths.Audio, new Color(0.95f, 0.85f, 0.20f) },
            { ProjectAssetPaths.ConceptualUiArt, new Color(0.58f, 0.45f, 0.92f) },
            { ProjectAssetPaths.Data, new Color(0.30f, 0.78f, 0.35f) },
            { ProjectAssetPaths.Documentation, new Color(0.75f, 0.70f, 0.62f) },
            { ProjectAssetPaths.DocumentationArchitecture, new Color(0.68f, 0.55f, 0.78f) },
            { ProjectAssetPaths.DocumentationAudits, new Color(0.60f, 0.50f, 0.72f) },
            { ProjectAssetPaths.Editor, new Color(0.55f, 0.55f, 0.55f) },
            { ProjectAssetPaths.EditorDevTools, new Color(0.48f, 0.48f, 0.48f) },
            { ProjectAssetPaths.Features, new Color(0.75f, 0.18f, 0.48f) },
            { ProjectAssetPaths.FeaturesCommunications, new Color(0.56f, 0.12f, 0.37f) },
            { ProjectAssetPaths.FeaturesDirectors, new Color(0.62f, 0.22f, 0.72f) },
            { ProjectAssetPaths.FeaturesValidation, new Color(0.45f, 0.58f, 0.48f) },
            { ProjectAssetPaths.FeaturesGameState, new Color(0.28f, 0.72f, 0.82f) },
            { ProjectAssetPaths.FeaturesWorldState, new Color(0.83f, 0.63f, 0.09f) },
            { ProjectAssetPaths.Materials, new Color(0.20f, 0.75f, 0.85f) },
            { ProjectAssetPaths.MiscToolsAndShaders, new Color(0.72f, 0.48f, 0.22f) },
            { ProjectAssetPaths.Prefabs, new Color(0.30f, 0.50f, 0.95f) },
            { ProjectAssetPaths.Resources, new Color(0.85f, 0.30f, 0.30f) },
            { ProjectAssetPaths.Scenes, new Color(0.65f, 0.35f, 0.90f) },
            { ProjectAssetPaths.Scripts, new Color(0.35f, 0.70f, 0.95f) },
            { ProjectAssetPaths.Settings, new Color(0.29f, 0.29f, 0.35f) },
            { ProjectAssetPaths.SettingsInput, new Color(0.35f, 0.35f, 0.42f) },
            { ProjectAssetPaths.Shaders, new Color(0.80f, 0.25f, 0.80f) },
            { ProjectAssetPaths.Textures, new Color(0.88f, 0.42f, 0.62f) },
            { ProjectAssetPaths.TexturesUi, new Color(0.82f, 0.38f, 0.58f) },
            { ProjectAssetPaths.World, new Color(0.38f, 0.68f, 0.42f) },
            { ProjectAssetPaths.WorldTerrain, new Color(0.32f, 0.58f, 0.36f) },
            { ProjectAssetPaths.PrefabsBuildings, new Color(0.48f, 0.58f, 0.92f) },
            { ProjectAssetPaths.PrefabsCrafting, new Color(0.45f, 0.65f, 0.95f) },
            { ProjectAssetPaths.PrefabsCraftingStations, new Color(0.40f, 0.60f, 0.90f) },
            { ProjectAssetPaths.PrefabsEnvironment, new Color(0.32f, 0.72f, 0.48f) },
            { ProjectAssetPaths.PrefabsEnvironmentCameraShake, new Color(0.30f, 0.68f, 0.46f) },
            { ProjectAssetPaths.PrefabsEnvironmentExposure, new Color(0.34f, 0.70f, 0.44f) },
            { ProjectAssetPaths.PrefabsItems, new Color(0.40f, 0.55f, 0.90f) },
            { ProjectAssetPaths.PrefabsItemsHeld, new Color(0.38f, 0.52f, 0.86f) },
            { ProjectAssetPaths.PrefabsItemsWorld, new Color(0.36f, 0.50f, 0.82f) },
            { ProjectAssetPaths.PrefabsItemsAmmo, new Color(0.34f, 0.58f, 0.78f) },
            { ProjectAssetPaths.PrefabsWeapons, new Color(0.88f, 0.42f, 0.38f) },
            { ProjectAssetPaths.PrefabsWeaponsMelee, new Color(0.86f, 0.40f, 0.36f) },
            { ProjectAssetPaths.PrefabsWeaponsRanged, new Color(0.84f, 0.38f, 0.42f) },
            { ProjectAssetPaths.PrefabsTools, new Color(0.72f, 0.55f, 0.35f) },
            { ProjectAssetPaths.PrefabsUi, new Color(0.50f, 0.70f, 1.00f) },
            { ProjectAssetPaths.PrefabsPlayers, new Color(0.35f, 0.45f, 0.85f) },
            { ProjectAssetPaths.PrefabsPets, new Color(0.45f, 0.55f, 0.90f) },
            { ProjectAssetPaths.PrefabsCompanions, new Color(0.42f, 0.50f, 0.88f) },
            { ProjectAssetPaths.PrefabsNpcs, new Color(0.55f, 0.45f, 0.95f) },
            { ProjectAssetPaths.PrefabsVehicles, new Color(0.40f, 0.62f, 0.88f) },
            { ProjectAssetPaths.PrefabsCombat, new Color(0.90f, 0.40f, 0.35f) },
            { ProjectAssetPaths.PrefabsCombatEnemies, new Color(0.88f, 0.36f, 0.32f) },
            { ProjectAssetPaths.PrefabsCombatProjectiles, new Color(0.92f, 0.44f, 0.30f) },
            { ProjectAssetPaths.PrefabsCombatVfx, new Color(0.94f, 0.48f, 0.28f) },
            { ProjectAssetPaths.PrefabsWorld, new Color(0.35f, 0.80f, 0.55f) },
            { ProjectAssetPaths.PrefabsWorldResources, new Color(0.32f, 0.75f, 0.50f) },
            { ProjectAssetPaths.PrefabsModels, new Color(0.55f, 0.50f, 0.70f) },
            { ProjectAssetPaths.ItemsData, new Color(0.25f, 0.70f, 0.40f) },
            { ProjectAssetPaths.ItemsMelee, new Color(0.28f, 0.72f, 0.42f) },
            { ProjectAssetPaths.ItemsRanged, new Color(0.26f, 0.68f, 0.44f) },
            { ProjectAssetPaths.ItemsAmmo, new Color(0.30f, 0.66f, 0.40f) },
            { ProjectAssetPaths.ItemsResources, new Color(0.24f, 0.74f, 0.38f) },
            { ProjectAssetPaths.ItemsTools, new Color(0.27f, 0.70f, 0.46f) },
            { ProjectAssetPaths.ItemsConsumables, new Color(0.29f, 0.73f, 0.41f) },
            { ProjectAssetPaths.ItemsVehicles, new Color(0.23f, 0.67f, 0.43f) },
            { ProjectAssetPaths.ItemsNodes, new Color(0.22f, 0.64f, 0.36f) },
            { ProjectAssetPaths.EnemiesData, new Color(0.22f, 0.62f, 0.38f) },
            { ProjectAssetPaths.RecipesData, new Color(0.20f, 0.65f, 0.45f) },
            { ProjectAssetPaths.RecipesWeapons, new Color(0.22f, 0.63f, 0.48f) },
            { ProjectAssetPaths.RecipesConsumables, new Color(0.24f, 0.66f, 0.46f) },
            { ProjectAssetPaths.RecipesAmmo, new Color(0.21f, 0.62f, 0.44f) },
            { ProjectAssetPaths.RecipesResources, new Color(0.23f, 0.64f, 0.42f) },
            { ProjectAssetPaths.RecipesModules, new Color(0.25f, 0.61f, 0.47f) },
            { ProjectAssetPaths.ResourcesQuests, new Color(0.95f, 0.45f, 0.40f) },
            { ProjectAssetPaths.ResourcesCrafting, new Color(0.90f, 0.50f, 0.35f) },
            { ProjectAssetPaths.ResourcesUi, new Color(0.92f, 0.38f, 0.38f) },
            { ProjectAssetPaths.ResourcesCombat, new Color(0.88f, 0.32f, 0.32f) },
            { ProjectAssetPaths.ResourcesOptics, new Color(0.82f, 0.36f, 0.44f) },
        };

        static ProjectFolderColorizer()
        {
            EditorApplication.projectWindowItemOnGUI += DrawFolderColor;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Project + "Refresh Folder Colors", false, 20)]
        public static void RefreshFolderColors()
        {
            EditorApplication.RepaintProjectWindow();
            Debug.Log("Refreshed _Project folder colors in the Project window.");
        }

        private static void DrawFolderColor(string guid, Rect rect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                return;

            if (!path.StartsWith("Assets/_Project"))
                return;

            if (!TryGetColor(path, out Color color))
                return;

            Rect tintRect = rect;
            if (rect.height <= 20f)
                tintRect.xMin += 16f;

            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.22f);
            GUI.DrawTexture(tintRect, EditorGUIUtility.whiteTexture);
            GUI.color = previous;
        }

        private static bool TryGetColor(string path, out Color color)
        {
            if (FolderColors.TryGetValue(path, out color))
                return true;

            int bestLength = -1;
            color = default;

            foreach (KeyValuePair<string, Color> entry in FolderColors)
            {
                if (!IsPathPrefix(path, entry.Key) || entry.Key.Length <= bestLength)
                    continue;

                bestLength = entry.Key.Length;
                color = entry.Value;
            }

            return bestLength >= 0;
        }

        private static bool IsPathPrefix(string path, string prefix)
        {
            if (!path.StartsWith(prefix))
                return false;

            if (path.Length == prefix.Length)
                return true;

            return path[prefix.Length] == '/';
        }
    }
}
