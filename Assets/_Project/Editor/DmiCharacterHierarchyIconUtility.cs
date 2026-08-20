using System.Collections.Generic;
using Project.AI;
using Project.Creatures;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// DMI lettermark for character Hierarchy rows:
    /// 1) Left object icon via <see cref="EditorGUIUtility.SetIconForObject"/>
    /// 2) Right-side overlay (replaces Invector's yellow T-pose controllerIcon)
    /// </summary>
    [InitializeOnLoad]
    public static class DmiCharacterHierarchyIconUtility
    {
        public const string HierarchyIconPath = "Assets/_Project/Art/DMI_HierarchyIcon.png";
        public const string SourceLogoPath = "Assets/_Project/Art/DMI_Logo_Transparent.png";

        private static Texture2D cachedOverlayIcon;

        private static readonly string[] PrefabSearchFolders =
        {
            "Assets/_Project/Prefabs/Players",
            "Assets/_Project/Prefabs/Combat/Enemies",
            "Assets/_Project/Prefabs/Creatures",
            "Assets/_Project/Prefabs/Companions",
            "Assets/_Project/Resources/Companions",
            "Assets/_Project/Resources/Echoes",
            "Assets/_Project/Resources/Creatures"
        };

        static DmiCharacterHierarchyIconUtility()
        {
#if UNITY_6000_0_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawCharacterOverlayIcon;
#else
#pragma warning disable CS0618
            EditorApplication.hierarchyWindowItemOnGUI += DrawCharacterOverlayIconLegacy;
#pragma warning restore CS0618
#endif
        }

#if UNITY_6000_0_OR_NEWER
        private static void DrawCharacterOverlayIcon(EntityId entityId, Rect selectionRect)
        {
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            DrawOverlayIfCharacter(go, selectionRect);
        }
#else
        private static void DrawCharacterOverlayIconLegacy(int instanceId, Rect selectionRect)
        {
#pragma warning disable CS0618
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
            DrawOverlayIfCharacter(go, selectionRect);
        }
#endif

        /// <summary>
        /// Right-edge Hierarchy badge for players / enemies / creatures.
        /// Drawn after Invector so the DMI mark covers the yellow T-pose controllerIcon.
        /// </summary>
        private static void DrawOverlayIfCharacter(GameObject go, Rect selectionRect)
        {
            if (go == null || !IsCharacterRoot(go))
                return;

            Texture2D icon = GetOverlayIcon();
            if (icon == null)
                return;

            Rect iconRect = new Rect(selectionRect.xMax - 16f, selectionRect.y, 16f, 16f);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }

        private static Texture2D GetOverlayIcon()
        {
            if (cachedOverlayIcon != null)
                return cachedOverlayIcon;

            cachedOverlayIcon = EnsureHierarchyIconTexture();
            return cachedOverlayIcon;
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "Art/Apply DMI Hierarchy Icons To Characters")]
        public static void ApplyFromMenu()
        {
            cachedOverlayIcon = null;
            int updated = ApplyToAllCharacterPrefabs();
            int sceneUpdated = ApplyToOpenSceneCharacters();
            EditorUtility.DisplayDialog(
                "DMI Hierarchy Icons",
                $"Applied DMI logo hierarchy icon to {updated} prefab(s) and {sceneUpdated} scene object(s).\n" +
                "Right-side overlay icons update live in the Hierarchy.",
                "OK");
        }

        public static Texture2D EnsureHierarchyIconTexture()
        {
            if (!AssetDatabase.LoadAssetAtPath<Texture2D>(HierarchyIconPath))
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(SourceLogoPath) == null)
                {
                    Debug.LogError($"[DMI] Missing source logo at {SourceLogoPath}");
                    return null;
                }

                if (!AssetDatabase.CopyAsset(SourceLogoPath, HierarchyIconPath))
                {
                    Debug.LogError($"[DMI] Failed to copy hierarchy icon from {SourceLogoPath}");
                    return null;
                }
            }

            TextureImporter importer = AssetImporter.GetAtPath(HierarchyIconPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    dirty = true;
                }

                if (importer.npotScale != TextureImporterNPOTScale.None)
                {
                    importer.npotScale = TextureImporterNPOTScale.None;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (importer.maxTextureSize > 256)
                {
                    importer.maxTextureSize = 256;
                    dirty = true;
                }

                if (dirty)
                    importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(HierarchyIconPath);
        }

        public static int ApplyToAllCharacterPrefabs()
        {
            Texture2D icon = EnsureHierarchyIconTexture();
            if (icon == null)
                return 0;

            List<string> folders = new List<string>();
            for (int i = 0; i < PrefabSearchFolders.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(PrefabSearchFolders[i]))
                    folders.Add(PrefabSearchFolders[i]);
            }

            if (folders.Count == 0)
                return 0;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", folders.ToArray());
            int updated = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        if (!IsCharacterRoot(root))
                            continue;

                        if (!ApplyIconToObject(root, icon))
                            continue;

                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        updated++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            return updated;
        }

        public static int ApplyToOpenSceneCharacters()
        {
            Texture2D icon = EnsureHierarchyIconTexture();
            if (icon == null)
                return 0;

            int updated = 0;
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    GameObject go = transforms[t].gameObject;
                    if (!IsCharacterRoot(go))
                        continue;

                    if (!ApplyIconToObject(go, icon))
                        continue;

                    EditorUtility.SetDirty(go);
                    updated++;
                }
            }

            return updated;
        }

        private static bool ApplyIconToObject(GameObject go, Texture2D icon)
        {
            if (go == null || icon == null)
                return false;

            Texture current = EditorGUIUtility.GetIconForObject(go);
            if (current == icon)
                return false;

            EditorGUIUtility.SetIconForObject(go, icon);
            return true;
        }

        private static bool IsCharacterRoot(GameObject go)
        {
            if (go == null)
                return false;

            string name = go.name;
            if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                name.StartsWith("Holstered_", System.StringComparison.Ordinal))
                return false;

            if (go.GetComponent<EnemyHealth>() != null)
                return true;
            if (go.GetComponent<DMICreatureBridge>() != null)
                return true;
            if (go.GetComponent<DMICreatureAiController>() != null)
                return true;
            if (go.GetComponent("vThirdPersonController") != null)
                return true;
            if (go.CompareTag("Player"))
                return true;

            return false;
        }
    }
}
