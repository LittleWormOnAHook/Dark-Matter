using System.Collections.Generic;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Gaia stamper undo restores height, not splat. Today's stamp added TerrainLayers
    /// Gaia_-20260825-140354_8 and Gaia_-20260825-140357_9 and painted them across tiles.
    /// This strips those layers and renormalizes remaining splat weights.
    /// </summary>
    public static class RemoveStampTerrainLayers
    {
        private const string StampDateToken = "20260825";

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Remove Stamper Terrain Layers")]
        [MenuItem("Dark Matter Genesis/World/Remove Stamper Terrain Layers")]
        public static void RemoveTodaysStamperLayers()
        {
            List<TerrainLayer> stampLayers = FindStampLayers();
            if (stampLayers.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Stamper Layers",
                    "No TerrainLayers named Gaia_-20260825* were found. The dark square may be a different layer — check Paint Terrain > Terrain Layers on a dark tile.",
                    "OK");
                return;
            }

            HashSet<TerrainData> datas = new HashSet<TerrainData>();
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null && terrains[i].terrainData != null)
                    datas.Add(terrains[i].terrainData);
            }

            string[] dataGuids = AssetDatabase.FindAssets("t:TerrainData");
            for (int i = 0; i < dataGuids.Length; i++)
            {
                TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(dataGuids[i]));
                if (data != null)
                    datas.Add(data);
            }

            int terrainsChanged = 0;
            int layersRemoved = 0;
            foreach (TerrainData data in datas)
            {
                int removed = StripLayers(data, stampLayers);
                if (removed > 0)
                {
                    terrainsChanged++;
                    layersRemoved += removed;
                    EditorUtility.SetDirty(data);
                }
            }

            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null)
                    terrains[i].Flush();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DM Genesis] Removed {layersRemoved} stamper layer slot(s) from {terrainsChanged} TerrainData asset(s). Layers: {Describe(stampLayers)}");
            EditorUtility.DisplayDialog(
                "Stamper Layers Removed",
                $"Stripped today's stamper layers from {terrainsChanged} terrain(s).\n{Describe(stampLayers)}\n\nScene view should brighten. If a tile is still dark, select it and Flush isn't enough — check Paint layers again.",
                "OK");
        }

        private static List<TerrainLayer> FindStampLayers()
        {
            List<TerrainLayer> result = new List<TerrainLayer>();
            string[] guids = AssetDatabase.FindAssets("t:TerrainLayer");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null || string.IsNullOrEmpty(layer.name))
                    continue;

                if (layer.name.Contains(StampDateToken))
                    result.Add(layer);
            }

            return result;
        }

        private static int StripLayers(TerrainData data, List<TerrainLayer> stampLayers)
        {
            if (data == null || data.terrainLayers == null || data.terrainLayers.Length == 0)
                return 0;

            TerrainLayer[] current = data.terrainLayers;
            List<int> keepIndices = new List<int>(current.Length);
            List<TerrainLayer> keepLayers = new List<TerrainLayer>(current.Length);
            int removed = 0;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != null && stampLayers.Contains(current[i]))
                {
                    removed++;
                    continue;
                }

                keepIndices.Add(i);
                keepLayers.Add(current[i]);
            }

            if (removed == 0)
                return 0;

            if (keepLayers.Count == 0)
            {
                Debug.LogWarning($"[DM Genesis] {data.name} would have 0 layers after strip — skipped.");
                return 0;
            }

            int width = data.alphamapWidth;
            int height = data.alphamapHeight;
            float[,,] maps = data.GetAlphamaps(0, 0, width, height);
            float[,,] next = new float[height, width, keepLayers.Count];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = 0; k < keepIndices.Count; k++)
                    {
                        float w = maps[y, x, keepIndices[k]];
                        next[y, x, k] = w;
                        sum += w;
                    }

                    if (sum < 0.0001f)
                    {
                        next[y, x, 0] = 1f;
                    }
                    else
                    {
                        float inv = 1f / sum;
                        for (int k = 0; k < keepLayers.Count; k++)
                            next[y, x, k] *= inv;
                    }
                }
            }

            Undo.RecordObject(data, "Remove stamper terrain layers");
            data.terrainLayers = keepLayers.ToArray();
            data.SetAlphamaps(0, 0, next);
            return removed;
        }

        private static string Describe(List<TerrainLayer> layers)
        {
            string[] names = new string[layers.Count];
            for (int i = 0; i < layers.Count; i++)
                names[i] = layers[i] != null ? layers[i].name : "?";
            return string.Join(", ", names);
        }
    }
}
