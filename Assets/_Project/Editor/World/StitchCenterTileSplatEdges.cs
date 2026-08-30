using System.Collections.Generic;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// After a 1:1 neighbor splat copy, the 2x2 center tiles do not match the gold
    /// tiles around them. Blend each outer edge from the facing neighbor's opposite edge.
    /// Does not stamp height. No NavMesh. No impostors.
    /// </summary>
    public static class StitchCenterTileSplatEdges
    {
        private const int BlendPixels = 48;
        private const string DataFolder = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Data";
        private const string DataSuffix = "-20260823 - 024958";

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Stitch Center Tile Splat Edges")]
        [MenuItem("Dark Matter Genesis/World/Stitch Center Tile Splat Edges")]
        public static void Stitch()
        {
            EdgeJob[] jobs =
            {
                new EdgeJob("Terrain_1_1", "Terrain_1_0", Edge.West, Edge.East),
                new EdgeJob("Terrain_1_1", "Terrain_0_1", Edge.North, Edge.South),
                new EdgeJob("Terrain_1_2", "Terrain_0_2", Edge.North, Edge.South),
                new EdgeJob("Terrain_1_2", "Terrain_1_3", Edge.East, Edge.West),
                new EdgeJob("Terrain_2_1", "Terrain_2_0", Edge.West, Edge.East),
                new EdgeJob("Terrain_2_1", "Terrain_3_1", Edge.South, Edge.North),
                new EdgeJob("Terrain_2_2", "Terrain_2_3", Edge.East, Edge.West),
                new EdgeJob("Terrain_2_2", "Terrain_3_2", Edge.South, Edge.North),
            };

            int ok = 0;
            var missing = new List<string>();
            try
            {
                for (int i = 0; i < jobs.Length; i++)
                {
                    EdgeJob job = jobs[i];
                    EditorUtility.DisplayProgressBar("Stitch splat edges", job.DstTile + " <- " + job.SrcTile, (float)i / jobs.Length);

                    TerrainData dst = LoadData(job.DstTile);
                    TerrainData src = LoadData(job.SrcTile);
                    if (dst == null || src == null)
                    {
                        missing.Add(job.DstTile + " <- " + job.SrcTile);
                        continue;
                    }

                    if (StitchEdge(dst, src, job.DstEdge, job.SrcEdge, BlendPixels))
                    {
                        EditorUtility.SetDirty(dst);
                        ok++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            FlushLoadedTerrains();
            AssetDatabase.SaveAssets();

            string extra = missing.Count > 0 ? "\nMissing:\n  " + string.Join("\n  ", missing.ToArray()) : string.Empty;
            EditorUtility.DisplayDialog(
                "Stitch Center Tile Splat Edges",
                "Blended " + ok + " outer edges from the facing gold tiles (" + BlendPixels + " px)." + extra + "\n\nIf the brown ring is still there, run Rebuild Terrain Basemaps.",
                "OK");
        }

        private enum Edge
        {
            West,
            East,
            South,
            North
        }

        private struct EdgeJob
        {
            public string DstTile;
            public string SrcTile;
            public Edge DstEdge;
            public Edge SrcEdge;

            public EdgeJob(string dstTile, string srcTile, Edge dstEdge, Edge srcEdge)
            {
                DstTile = dstTile;
                SrcTile = srcTile;
                DstEdge = dstEdge;
                SrcEdge = srcEdge;
            }
        }

        private static TerrainData LoadData(string tileKey)
        {
            string path = DataFolder + "/" + tileKey + DataSuffix + ".asset";
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data != null)
                return data;

            string[] guids = AssetDatabase.FindAssets(tileKey + " t:TerrainData");
            for (int i = 0; i < guids.Length; i++)
            {
                TerrainData found = AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (found != null && found.name.StartsWith(tileKey))
                    return found;
            }

            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] == null || terrains[i].terrainData == null)
                    continue;
                if (terrains[i].name.StartsWith(tileKey) || terrains[i].terrainData.name.StartsWith(tileKey))
                    return terrains[i].terrainData;
            }

            return null;
        }

        private static bool StitchEdge(TerrainData dst, TerrainData src, Edge dstEdge, Edge srcEdge, int band)
        {
            int dw = dst.alphamapWidth;
            int dh = dst.alphamapHeight;
            int sw = src.alphamapWidth;
            int sh = src.alphamapHeight;
            if (dw != sw || dh != sh)
            {
                Debug.LogWarning("[DM Genesis] Alphamap size mismatch " + dst.name + " vs " + src.name);
                return false;
            }

            int[] srcToDst = BuildLayerMap(src, dst);
            if (srcToDst == null)
                return false;

            int layers = dst.alphamapLayers;
            if (layers <= 0)
                return false;

            band = Mathf.Clamp(band, 4, Mathf.Min(dw, dh) / 4);
            float[,,] dMaps = dst.GetAlphamaps(0, 0, dw, dh);
            float[,,] sMaps = src.GetAlphamaps(0, 0, sw, sh);

            if (dstEdge == Edge.West || dstEdge == Edge.East)
            {
                int dstX0 = dstEdge == Edge.West ? 0 : dw - 1;
                int dstDir = dstEdge == Edge.West ? 1 : -1;
                int srcX0 = srcEdge == Edge.West ? 0 : sw - 1;
                int srcDir = srcEdge == Edge.West ? 1 : -1;

                for (int i = 0; i < band; i++)
                {
                    float t = 1f - (i / (float)(band - 1));
                    int dx = dstX0 + dstDir * i;
                    int sx = srcX0 + srcDir * i;
                    for (int z = 0; z < dh; z++)
                        LerpPixel(dMaps, sMaps, z, dx, z, sx, t, srcToDst, layers);
                }
            }
            else
            {
                int dstZ0 = dstEdge == Edge.South ? 0 : dh - 1;
                int dstDir = dstEdge == Edge.South ? 1 : -1;
                int srcZ0 = srcEdge == Edge.South ? 0 : sh - 1;
                int srcDir = srcEdge == Edge.South ? 1 : -1;

                for (int i = 0; i < band; i++)
                {
                    float t = 1f - (i / (float)(band - 1));
                    int dz = dstZ0 + dstDir * i;
                    int sz = srcZ0 + srcDir * i;
                    for (int x = 0; x < dw; x++)
                        LerpPixel(dMaps, sMaps, dz, x, sz, x, t, srcToDst, layers);
                }
            }

            Undo.RecordObject(dst, "Stitch terrain splat edges");
            dst.SetAlphamaps(0, 0, dMaps);
            return true;
        }

        private static void LerpPixel(
            float[,,] dst,
            float[,,] src,
            int dz,
            int dx,
            int sz,
            int sx,
            float t,
            int[] srcToDst,
            int dstLayers)
        {
            float[] mixed = new float[dstLayers];
            for (int sl = 0; sl < srcToDst.Length; sl++)
            {
                int dl = srcToDst[sl];
                if (dl < 0)
                    continue;
                mixed[dl] += src[sz, sx, sl];
            }

            float sum = 0f;
            for (int l = 0; l < dstLayers; l++)
            {
                float v = Mathf.Lerp(dst[dz, dx, l], mixed[l], t);
                dst[dz, dx, l] = v;
                sum += v;
            }

            if (sum < 0.0001f)
            {
                dst[dz, dx, 0] = 1f;
                return;
            }

            float inv = 1f / sum;
            for (int l = 0; l < dstLayers; l++)
                dst[dz, dx, l] *= inv;
        }

        private static int[] BuildLayerMap(TerrainData src, TerrainData dst)
        {
            TerrainLayer[] sLayers = src.terrainLayers;
            TerrainLayer[] dLayers = dst.terrainLayers;
            if (sLayers == null || dLayers == null || sLayers.Length == 0 || dLayers.Length == 0)
                return null;

            int[] map = new int[sLayers.Length];
            for (int s = 0; s < sLayers.Length; s++)
            {
                map[s] = -1;
                TerrainLayer sl = sLayers[s];
                if (sl == null)
                    continue;
                for (int d = 0; d < dLayers.Length; d++)
                {
                    if (dLayers[d] == sl)
                    {
                        map[s] = d;
                        break;
                    }
                }
            }

            return map;
        }

        private static void FlushLoadedTerrains()
        {
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null)
                    terrains[i].Flush();
            }
        }
    }
}
