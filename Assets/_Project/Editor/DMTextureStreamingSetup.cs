#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Opts gameplay textures into Unity mip streaming:
    /// folder pass (world/packs) plus usage pass (playable scene, terrain tiles,
    /// prefabs, terrain layers, player/enemy/item data).
    /// Skips UI, CPU-readable map masks, Design art, and excluded packs.
    /// </summary>
    public static class DMTextureStreamingSetup
    {
        private const string Stamp = "DMTexStream";
        private const string PlayableSceneFallback =
            "Assets/_Project/Scenes/Dark Matter Genesis v1.6.3.1.unity";
        private const string PlayableSceneLegacy =
            "Assets/_Project/Scenes/Dark Matter Genesis v1.6.2.unity";

        private static readonly string[] IncludeFolders =
        {
            "Assets/_Project/World",
            "Assets/_Project/Models",
            "Assets/_Project/Textures",
            "Assets/_Project/Features",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Art",
            "Assets/_Project/Materials",
            "Assets/_Project/Resources",
            "Assets/PolygonSciFiWorlds",
            "Assets/PolygonSciFiSpace",
            "Assets/PolygonNature",
            "Assets/NatureManufacture Assets",
            "Assets/Buildings_constructor",
        };

        private static readonly string[] UsageSearchFolders =
        {
            "Assets/_Project/Prefabs",
            "Assets/_Project/Resources/Pioneers",
            "Assets/_Project/Resources/Echoes",
            "Assets/_Project/Resources/Combat",
            "Assets/_Project/Data/Enemies",
            "Assets/_Project/Data/Creatures",
            "Assets/_Project/Data/Items",
            "Assets/_Project/World",
        };

        private static readonly string[] TerrainLayerFolders =
        {
            "Assets/_Project",
            "Assets/PolygonNature",
            "Assets/PolygonSciFiWorlds",
            "Assets/NatureManufacture Assets",
            "Assets/Buildings_constructor",
        };

        private static readonly string[] ExcludePathParts =
        {
            "/Art/UI/",
            "/Art/Icons/",
            "/Prefabs/UI/",
            "/Resources/UI/",
            "/Resources/Map/",
            "/Documentation/Design/ArtReference/",
            "/LifeSheets/",
            "/HotCross",
            "/UI Toolkit/",
            "/TextMesh Pro/",
            "/Fonts/",
            "/Conceptual UI",
            "DM Terrain Mask",
            "Io_Plan_BiomeMap_TopDown",
            "Io_Plan_BiomeMap_Isometric",
            "FakeMap",
            "DMG_Io_Sector_Map",
            "L.V.E",
            "LVE-",
            "/mocap",
            "PlanetPack02",
            "UIElementsSchema",
            "OlegWER",
            "GDKEditionAutoGen",
        };

        [MenuItem(DarkMatterGenesisEditorMenus.TextureStreamingPreview, false, 40)]
        public static void PreviewWorldStreaming()
        {
            Run(apply: false);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.TextureStreamingApply, false, 41)]
        public static void ApplyWorldStreaming()
        {
            StreamStats preview = Collect(apply: false);
            if (preview.enable <= 0)
            {
                Debug.Log($"[{Stamp}] no new gameplay textures to opt in. already on {preview.already}, skipped {preview.skipped}.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply Gameplay Streaming",
                    "Enable mip streaming on " + preview.enable + " gameplay textures?\n\n" +
                    "Includes scene + prefab used textures (terrain, player, enemies, weapons, world).\n" +
                    "UI, FOW mask, journal biome map, and excluded packs stay off.\n" +
                    "Ctrl+R after this if Auto Refresh is off.",
                    "Apply",
                    "Cancel"))
                return;

            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            StreamStats stats = Collect(apply);
            string verb = apply ? "opted in" : "would opt in";
            Debug.Log(
                $"[{Stamp}] {verb} {stats.enable} " +
                $"(scene/prefab used {stats.fromUsage}, folder-only {stats.fromFolderOnly}), " +
                $"already on {stats.already}, skipped {stats.skipped}.");
        }

        private struct StreamStats
        {
            public int enable;
            public int skipped;
            public int already;
            public int fromUsage;
            public int fromFolderOnly;
        }

        private static StreamStats Collect(bool apply)
        {
            StreamStats stats = new StreamStats();
            HashSet<string> usagePaths = CollectUsedTexturePaths();
            HashSet<string> folderPaths = CollectFolderTexturePaths();
            HashSet<string> allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allPaths.UnionWith(usagePaths);
            allPaths.UnionWith(folderPaths);

            List<string> toEnable = new List<string>();
            foreach (string path in allPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !IsCandidate(path, importer))
                {
                    stats.skipped++;
                    continue;
                }

                bool used = usagePaths.Contains(path);
                if (importer.streamingMipmaps)
                {
                    stats.already++;
                    continue;
                }

                stats.enable++;
                if (used)
                    stats.fromUsage++;
                else
                    stats.fromFolderOnly++;
                toEnable.Add(path);
            }

            if (!apply || toEnable.Count == 0)
                return stats;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < toEnable.Count; i++)
                {
                    string path = toEnable[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Apply Gameplay Streaming",
                            path,
                            (i + 1f) / toEnable.Count))
                        break;

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        continue;

                    importer.streamingMipmaps = true;
                    importer.streamingMipmapsPriority = 0;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            return stats;
        }

        private static HashSet<string> CollectFolderTexturePaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int f = 0; f < IncludeFolders.Length; f++)
            {
                if (!AssetDatabase.IsValidFolder(IncludeFolders[f]))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { IncludeFolders[f] });
                for (int i = 0; i < guids.Length; i++)
                    paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return paths;
        }

        private static HashSet<string> CollectUsedTexturePaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> roots = new List<string>();

            string playable = ResolvePlayableScenePath();
            if (!string.IsNullOrEmpty(playable))
                roots.Add(playable);

            AddMatchingAssets(roots, "t:Scene", new[] { "Assets/_Project/Scenes" }, IsTerrainContentScene);
            AddMatchingAssets(roots, "t:Prefab", new[] { "Assets/_Project/Prefabs" }, path =>
                path.IndexOf("/Prefabs/UI/", StringComparison.OrdinalIgnoreCase) < 0);
            AddMatchingAssets(roots, "t:ScriptableObject", UsageSearchFolders, null);
            AddMatchingAssets(roots, "t:TerrainLayer", TerrainLayerFolders, null);

            string playerVariant = "Assets/_Project/Prefabs/Players/Player_v7 Variant.prefab";
            if (File.Exists(ToDiskPath(playerVariant)))
                roots.Add(playerVariant);

            for (int i = 0; i < roots.Count; i++)
            {
                string root = roots[i];
                if (string.IsNullOrEmpty(root))
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Scan Gameplay Textures",
                        root,
                        (i + 1f) / Mathf.Max(1, roots.Count)))
                    break;

                string[] deps = AssetDatabase.GetDependencies(root, true);
                for (int d = 0; d < deps.Length; d++)
                {
                    string dep = deps[d];
                    if (AssetImporter.GetAtPath(dep) is TextureImporter)
                        paths.Add(dep);
                }
            }

            EditorUtility.ClearProgressBar();
            return paths;
        }

        private static void AddMatchingAssets(
            List<string> roots,
            string filter,
            string[] folders,
            Func<string, bool> extraMatch)
        {
            List<string> valid = new List<string>();
            for (int i = 0; i < folders.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(folders[i]))
                    valid.Add(folders[i]);
            }

            if (valid.Count == 0)
                return;

            string[] guids = AssetDatabase.FindAssets(filter, valid.ToArray());
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (extraMatch != null && !extraMatch(path))
                    continue;
                roots.Add(path);
            }
        }

        private static bool IsTerrainContentScene(string path)
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("Terrain_", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith("_Content.unity", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePlayableScenePath()
        {
            string active = EditorSceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(active)
                && active.IndexOf("Dark Matter Genesis", StringComparison.OrdinalIgnoreCase) >= 0)
                return active;

            if (File.Exists(ToDiskPath(PlayableSceneFallback)))
                return PlayableSceneFallback;
            if (File.Exists(ToDiskPath(PlayableSceneLegacy)))
                return PlayableSceneLegacy;
            return active;
        }

        private static string ToDiskPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static bool IsCandidate(string path, TextureImporter importer)
        {
            if (string.IsNullOrEmpty(path) || importer == null)
                return false;

            for (int i = 0; i < ExcludePathParts.Length; i++)
            {
                if (path.IndexOf(ExcludePathParts[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            if (!importer.mipmapEnabled)
                return false;

            TextureImporterType type = importer.textureType;
            if (type == TextureImporterType.Sprite
                || type == TextureImporterType.GUI
                || type == TextureImporterType.Cursor
                || type == TextureImporterType.SingleChannel)
                return false;

            return type == TextureImporterType.Default
                || type == TextureImporterType.NormalMap
                || type == TextureImporterType.Lightmap
                || type == TextureImporterType.Cookie
                || type == TextureImporterType.DirectionalLightmap
                || type == TextureImporterType.Shadowmask;
        }
    }
}
#endif
