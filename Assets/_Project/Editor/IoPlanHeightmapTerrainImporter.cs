#if UNITY_EDITOR
using System.IO;
using Project.Survival.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Imports Io_Plan_Heightmap (RAW R16 or PNG) into TerrainData for W1 blockout.
    /// Menu: Tools/Dark Matter Genesis/World/Import Io Plan Heightmap → Terrain
    /// </summary>
    public static class IoPlanHeightmapTerrainImporter
    {
        public const string TerrainDataPath = "Assets/_Project/World/Terrain/Io_MainMap_W1.asset";
        public const string RawHeightmapPath = "Assets/_Project/World/Terrain/Io_Plan_Heightmap_R16.raw";
        public const string PngHeightmapPath = "Assets/_Project/World/WorldMap/Io_Plan_Heightmap.png";
        public const string TerrainLayerPath = "Assets/_Project/World/Terrain/NewLayer.terrainlayer";
        public const string TerrainObjectName = "Io_MainMap_W1";

        [MenuItem(SurvivalPioneerEditorMenus.World + "Import Io Plan Heightmap → Terrain", false, 20)]
        public static void ImportHeightmapToTerrainDataMenu()
        {
            if (!TryImportHeightmapToTerrainData(out string message, placeInScene: true))
            {
                EditorUtility.DisplayDialog("Io Heightmap Import", message, "OK");
                return;
            }

            EditorUtility.DisplayDialog("Io Heightmap Import", message, "OK");
        }

        public static bool TryImportHeightmapToTerrainData(out string message, bool placeInScene)
        {
            message = string.Empty;
            if (!TryLoadHeights(out float[,] heights, out int resolution))
            {
                message = "Could not read RAW or PNG heightmap.\nExpected:\n" + RawHeightmapPath + "\nor\n" + PngHeightmapPath;
                return false;
            }

            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }

            // Unity heightmapResolution must be 2^n + 1.
            int terrainRes = Mathf.ClosestPowerOfTwo(resolution - 1) + 1;
            if (terrainRes < 33)
                terrainRes = 33;

            data.heightmapResolution = terrainRes;
            data.size = new Vector3(
                IoSurfaceWorldScale.MainMapSpanMeters,
                IoSurfaceWorldScale.MaxTerrainHeightMeters,
                IoSurfaceWorldScale.MainMapSpanMeters);

            float[,] resized = heights;
            if (terrainRes != resolution)
                resized = ResampleHeights(heights, resolution, terrainRes);

            data.SetHeights(0, 0, resized);

            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerPath);
            if (layer != null)
                data.terrainLayers = new[] { layer };

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Terrain terrain = null;
            if (placeInScene)
            {
                terrain = EnsureTerrainInOpenScene(data);
                Selection.activeObject = terrain != null ? terrain.gameObject : data;
            }

            message =
                $"TerrainData ready at:\n{TerrainDataPath}\n\n" +
                $"Size: {IoSurfaceWorldScale.MainMapSpanMeters:0} × {IoSurfaceWorldScale.MainMapSpanMeters:0} m, " +
                $"height {IoSurfaceWorldScale.MaxTerrainHeightMeters:0} m\n" +
                $"Heightmap resolution: {terrainRes}\n" +
                (terrain != null
                    ? $"Scene object: {TerrainObjectName}"
                    : placeInScene
                        ? "Open a scene, then re-run to place the Terrain object."
                        : "TerrainData asset written.");
            return true;
        }

        public static Terrain EnsureTerrainInOpenScene(TerrainData data)
        {
            if (data == null)
                return null;

            Terrain existing = Object.FindAnyObjectByType<Terrain>();
            GameObject host;
            if (existing != null && existing.terrainData == data)
            {
                host = existing.gameObject;
            }
            else
            {
                Terrain named = null;
                Terrain[] all = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == TerrainObjectName)
                    {
                        named = all[i];
                        break;
                    }
                }

                if (named != null)
                {
                    host = named.gameObject;
                    named.terrainData = data;
                }
                else
                {
                    host = Terrain.CreateTerrainGameObject(data);
                    host.name = TerrainObjectName;
                    Undo.RegisterCreatedObjectUndo(host, "Create Io Main Map Terrain");
                }
            }

            host.transform.position = IoSurfaceWorldScale.TerrainOrigin;
            Terrain terrain = host.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.terrainData = data;
                terrain.groupingID = 0;
                EditorUtility.SetDirty(terrain);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return terrain;
        }

        private static bool TryLoadHeights(out float[,] heights, out int resolution)
        {
            heights = null;
            resolution = 0;

            string rawAbs = Path.GetFullPath(RawHeightmapPath);
            if (File.Exists(rawAbs))
            {
                byte[] bytes = File.ReadAllBytes(rawAbs);
                int sampleCount = bytes.Length / 2;
                int side = Mathf.RoundToInt(Mathf.Sqrt(sampleCount));
                if (side * side != sampleCount)
                    return false;

                resolution = side;
                heights = new float[side, side];
                for (int y = 0; y < side; y++)
                {
                    for (int x = 0; x < side; x++)
                    {
                        // RAW is flipped so image-top (north) → high Z = Unity heightmap Y index.
                        int index = (y * side + x) * 2;
                        ushort value = (ushort)(bytes[index] | (bytes[index + 1] << 8));
                        heights[y, x] = value / 65535f;
                    }
                }

                return true;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PngHeightmapPath);
            if (tex == null)
                return false;

            EnsureTextureReadable(PngHeightmapPath);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PngHeightmapPath);
            if (tex == null || !tex.isReadable)
                return false;

            resolution = tex.width;
            heights = new float[resolution, resolution];
            Color32[] pixels = tex.GetPixels32();
            for (int y = 0; y < resolution; y++)
            {
                int srcY = resolution - 1 - y; // flip: image top → high Z
                for (int x = 0; x < resolution; x++)
                {
                    Color32 c = pixels[srcY * resolution + x];
                    heights[y, x] = c.r / 255f;
                }
            }

            return true;
        }

        private static void EnsureTextureReadable(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            if (importer.isReadable && importer.textureCompression == TextureImporterCompression.Uncompressed)
                return;

            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static float[,] ResampleHeights(float[,] source, int srcRes, int dstRes)
        {
            float[,] dest = new float[dstRes, dstRes];
            float scale = (srcRes - 1f) / Mathf.Max(1, dstRes - 1);
            for (int y = 0; y < dstRes; y++)
            {
                float sy = y * scale;
                int y0 = Mathf.Clamp(Mathf.FloorToInt(sy), 0, srcRes - 1);
                int y1 = Mathf.Min(y0 + 1, srcRes - 1);
                float ty = sy - y0;
                for (int x = 0; x < dstRes; x++)
                {
                    float sx = x * scale;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(sx), 0, srcRes - 1);
                    int x1 = Mathf.Min(x0 + 1, srcRes - 1);
                    float tx = sx - x0;
                    float a = Mathf.Lerp(source[y0, x0], source[y0, x1], tx);
                    float b = Mathf.Lerp(source[y1, x0], source[y1, x1], tx);
                    dest[y, x] = Mathf.Lerp(a, b, ty);
                }
            }

            return dest;
        }
    }
}
#endif
