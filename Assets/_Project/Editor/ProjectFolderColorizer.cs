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
            { ProjectAssetPaths.Art, new Color(0.92f, 0.35f, 0.55f) },
            { ProjectAssetPaths.Audio, new Color(0.95f, 0.85f, 0.20f) },
            { ProjectAssetPaths.Data, new Color(0.30f, 0.78f, 0.35f) },
            { ProjectAssetPaths.Editor, new Color(0.55f, 0.55f, 0.55f) },
            { ProjectAssetPaths.Materials, new Color(0.20f, 0.75f, 0.85f) },
            { ProjectAssetPaths.Prefabs, new Color(0.30f, 0.50f, 0.95f) },
            { ProjectAssetPaths.Resources, new Color(0.85f, 0.30f, 0.30f) },
            { ProjectAssetPaths.Scenes, new Color(0.65f, 0.35f, 0.90f) },
            { ProjectAssetPaths.Scripts, new Color(0.35f, 0.70f, 0.95f) },
            { ProjectAssetPaths.Shaders, new Color(0.80f, 0.25f, 0.80f) },
            { ProjectAssetPaths.PrefabsCrafting, new Color(0.45f, 0.65f, 0.95f) },
            { ProjectAssetPaths.PrefabsItems, new Color(0.40f, 0.55f, 0.90f) },
            { ProjectAssetPaths.PrefabsUi, new Color(0.50f, 0.70f, 1.00f) },
            { ProjectAssetPaths.PrefabsPlayers, new Color(0.35f, 0.45f, 0.85f) },
            { ProjectAssetPaths.PrefabsNpcs, new Color(0.55f, 0.45f, 0.95f) },
            { ProjectAssetPaths.PrefabsCombat, new Color(0.90f, 0.40f, 0.35f) },
            { ProjectAssetPaths.PrefabsWorld, new Color(0.35f, 0.80f, 0.55f) },
            { ProjectAssetPaths.ItemsData, new Color(0.25f, 0.70f, 0.40f) },
            { ProjectAssetPaths.RecipesData, new Color(0.20f, 0.65f, 0.45f) },
            { ProjectAssetPaths.ResourcesQuests, new Color(0.95f, 0.45f, 0.40f) },
            { ProjectAssetPaths.ResourcesCrafting, new Color(0.90f, 0.50f, 0.35f) },
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
                if (path.StartsWith(entry.Key) && entry.Key.Length > bestLength)
                {
                    bestLength = entry.Key.Length;
                    color = entry.Value;
                }
            }

            return bestLength >= 0;
        }
    }
}
