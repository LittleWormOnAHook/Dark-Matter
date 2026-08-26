#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Rebuilds alphamaps on all 16 Gaia tiles by learning height+slope ->
    /// layer weights from Terrain_0_1, then painting that mapping onto each
    /// tile's own heightmap. Uses Terrain_1_1's TerrainLayer list, 0_1's height/slope look.
    /// Does not stamp height, does not touch NavMesh, does not copy splatmaps 1:1.
    /// </summary>
    public static class RebuildSplatmapsFromHeight
    {
        const int HeightBins = 48;
        const int SlopeBins = 16;
        const int TeacherStride = 4;
        const string TeacherKey = "Terrain_0_1";
        const string LayerSourceKey = "Terrain_1_1";

        static readonly string[] AllTiles =
        {
            "Terrain_0_0", "Terrain_0_1", "Terrain_0_2", "Terrain_0_3",
            "Terrain_1_0", "Terrain_1_1", "Terrain_1_2", "Terrain_1_3",
            "Terrain_2_0", "Terrain_2_1", "Terrain_2_2", "Terrain_2_3",
            "Terrain_3_0", "Terrain_3_1", "Terrain_3_2", "Terrain_3_3",
        };

        [MenuItem("Dark Matter Genesis/World/Rebuild Splatmaps From Height (Like Gaia)", false, 53)]
        public static void Rebuild()
        {
            TerrainData teacher = FindTerrainData(TeacherKey);
            if (teacher == null)
            {
                EditorUtility.DisplayDialog(
                    "Rebuild Splatmaps From Height",
                    "Could not find Terrain_0_1 TerrainData (look teacher).",
                    "OK");
                return;
            }

            TerrainData layerSrc = FindTerrainData(LayerSourceKey);
            if (layerSrc == null)
            {
                EditorUtility.DisplayDialog(
                    "Rebuild Splatmaps From Height",
                    "Could not find Terrain_1_1 TerrainData (layer source).",
                    "OK");
                return;
            }

            TerrainLayer[] paintLayers = layerSrc.terrainLayers;
            if (paintLayers == null || paintLayers.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Rebuild Splatmaps From Height",
                    "Terrain_1_1 has no TerrainLayers.",
                    "OK");
                return;
            }

            TerrainLayer[] teacherLayers = teacher.terrainLayers;
            if (teacherLayers == null || teacherLayers.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Rebuild Splatmaps From Height",
                    "Terrain_0_1 has no TerrainLayers to learn from.",
                    "OK");
                return;
            }

            int[] teacherToPaint = MapLayers(teacherLayers, paintLayers);

            var targets = new List<TerrainData>();
            var missing = new List<string>();
            foreach (string key in AllTiles)
            {
                TerrainData td = FindTerrainData(key);
                if (td == null)
                    missing.Add(key);
                else
                    targets.Add(td);
            }

            var msg = "Start clean: paint all " + targets.Count + " tiles from height + slope.\n";
            msg += "Look (height/slope): Terrain_0_1  (" + teacherLayers.Length + " layers: " + DescribeLayers(teacherLayers) + ")\n";
            msg += "Layers applied: Terrain_1_1  (" + paintLayers.Length + " layers: " + DescribeLayers(paintLayers) + ")\n";
            msg += "Learns 0_1's look, remaps onto 1_1's layers, paints every tile's own heightmap.\n";
            msg += "Backups: Assets/_Project/Backups/Splatmaps-20260825-all16/\n";
            if (missing.Count > 0)
            {
                msg += "\nMISSING:\n";
                foreach (string s in missing)
                    msg += "  " + s + "\n";
            }

            if (!EditorUtility.DisplayDialog("Rebuild Splatmaps From Height", msg, "Paint all 16", "Cancel"))
                return;

            EditorUtility.DisplayProgressBar("Rebuild Splatmaps From Height", "Learning from Terrain_0_1...", 0f);
            Lut lut;
            try
            {
                lut = Learn(teacher, teacherToPaint, paintLayers.Length);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Rebuild Splatmaps From Height", "Learn failed: " + ex.Message, "OK");
                return;
            }

            int ok = 0;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    TerrainData dst = targets[i];
                    string key = TileKey(dst.name);
                    EditorUtility.DisplayProgressBar(
                        "Rebuild Splatmaps From Height",
                        "Painting " + key + "  (" + (i + 1) + "/" + targets.Count + ")",
                        (float)i / targets.Count);
                    if (PaintTile(dst, paintLayers, lut, key))
                        ok++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null)
                    continue;
                string key = TileKey(t.name);
                if (Array.IndexOf(AllTiles, key) >= 0)
                    RebuildBasemap(t);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Rebuild Splatmaps From Height",
                "Painted " + ok + " / " + targets.Count + " tile(s).\nLook from 0_1, layers from 1_1.\n\nCheck a seam between 0_1 and 1_1.",
                "OK");
        }

        struct Lut
        {
            public float HeightMin;
            public float HeightRange;
            public int LayerCount;
            public double[] Sum;
            public int[] Count;
        }

        static Lut Learn(TerrainData src, int[] teacherToPaint, int paintLayerCount)
        {
            float heightMin, heightMax;
            MeasureHeightRange(src, out heightMin, out heightMax);
            float range = Mathf.Max(1f, heightMax - heightMin);

            int bins = HeightBins * SlopeBins;
            var lut = new Lut
            {
                HeightMin = heightMin,
                HeightRange = range,
                LayerCount = paintLayerCount,
                Sum = new double[bins * paintLayerCount],
                Count = new int[bins]
            };

            int sw = src.alphamapWidth;
            int sh = src.alphamapHeight;
            float[,,] srcMaps = src.GetAlphamaps(0, 0, sw, sh);
            int srcLayerCount = srcMaps.GetLength(2);

            for (int y = 0; y < sh; y += TeacherStride)
            {
                for (int x = 0; x < sw; x += TeacherStride)
                {
                    float u = (x + 0.5f) / sw;
                    float v = (y + 0.5f) / sh;
                    float hNorm = (src.GetInterpolatedHeight(u, v) - heightMin) / range;
                    float sNorm = src.GetSteepness(u, v) / 90f;
                    int bin = BinIndex(hNorm, sNorm);
                    lut.Count[bin]++;
                    int baseOff = bin * paintLayerCount;
                    int copy = Math.Min(srcLayerCount, teacherToPaint.Length);
                    for (int k = 0; k < copy; k++)
                    {
                        int dstLayer = teacherToPaint[k];
                        if (dstLayer < 0)
                            dstLayer = Mathf.Min(k, paintLayerCount - 1);
                        if (dstLayer < 0 || dstLayer >= paintLayerCount)
                            continue;
                        lut.Sum[baseOff + dstLayer] += srcMaps[y, x, k];
                    }
                }
            }

            FillEmptyBins(lut.Sum, lut.Count, paintLayerCount);
            return lut;
        }

        static int[] MapLayers(TerrainLayer[] src, TerrainLayer[] dst)
        {
            var map = new int[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                map[i] = -1;
                TerrainLayer s = src[i];
                if (s == null)
                    continue;
                for (int j = 0; j < dst.Length; j++)
                {
                    if (dst[j] == s)
                    {
                        map[i] = j;
                        break;
                    }
                }
                if (map[i] < 0)
                {
                    for (int j = 0; j < dst.Length; j++)
                    {
                        if (dst[j] != null && s != null && dst[j].name == s.name)
                        {
                            map[i] = j;
                            break;
                        }
                    }
                }
                if (map[i] < 0)
                    map[i] = Mathf.Min(i, dst.Length - 1);
            }
            return map;
        }

        static bool PaintTile(TerrainData dst, TerrainLayer[] teacherLayers, Lut lut, string dstKey)
        {
            if (dst == null)
                return false;

            Undo.RegisterCompleteObjectUndo(dst, "Rebuild splatmaps from height " + dst.name);
            dst.terrainLayers = teacherLayers;

            int layerCount = teacherLayers.Length;
            int dw = dst.alphamapWidth;
            int dh = dst.alphamapHeight;
            float[,,] dstMaps = new float[dh, dw, layerCount];

            for (int y = 0; y < dh; y++)
            {
                if ((y & 127) == 0)
                    EditorUtility.DisplayProgressBar(
                        "Rebuild Splatmaps From Height",
                        dstKey + "  " + y + "/" + dh,
                        (float)y / dh);

                for (int x = 0; x < dw; x++)
                {
                    float u = (x + 0.5f) / dw;
                    float v = (y + 0.5f) / dh;
                    float hNorm = (dst.GetInterpolatedHeight(u, v) - lut.HeightMin) / lut.HeightRange;
                    float sNorm = dst.GetSteepness(u, v) / 90f;
                    SampleBin(lut, layerCount, hNorm, sNorm, dstMaps, y, x);
                }
            }

            BlurAndNormalize(dstMaps);
            dst.SetAlphamaps(0, 0, dstMaps);
            SetBaseMapDirty(dst);
            dst.SyncHeightmap();
            EditorUtility.SetDirty(dst);
            Debug.Log("[DMG] Height-splat painted " + dst.name + " look=0_1 layers=1_1 count=" + layerCount);
            return true;
        }

        static string DescribeLayers(TerrainLayer[] layers)
        {
            if (layers == null || layers.Length == 0)
                return "(none)";
            var names = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                names[i] = layers[i] != null ? layers[i].name : "?";
            return string.Join(", ", names);
        }

        static void MeasureHeightRange(TerrainData td, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            int res = td.heightmapResolution;
            int step = Mathf.Max(1, res / 128);
            for (int y = 0; y < res; y += step)
            {
                for (int x = 0; x < res; x += step)
                {
                    float u = (float)x / (res - 1);
                    float v = (float)y / (res - 1);
                    float h = td.GetInterpolatedHeight(u, v);
                    if (h < min) min = h;
                    if (h > max) max = h;
                }
            }
        }

        static int BinIndex(float hNorm, float sNorm)
        {
            int hi = Mathf.Clamp(Mathf.FloorToInt(hNorm * HeightBins), 0, HeightBins - 1);
            int si = Mathf.Clamp(Mathf.FloorToInt(sNorm * SlopeBins), 0, SlopeBins - 1);
            return hi * SlopeBins + si;
        }

        static void FillEmptyBins(double[] sum, int[] count, int layerCount)
        {
            for (int hi = 0; hi < HeightBins; hi++)
            {
                for (int si = 0; si < SlopeBins; si++)
                {
                    int bin = hi * SlopeBins + si;
                    if (count[bin] > 0)
                        continue;
                    bool found = false;
                    for (int rad = 1; rad <= HeightBins && !found; rad++)
                    {
                        for (int dh = -rad; dh <= rad && !found; dh++)
                        {
                            int nHi = hi + dh;
                            if (nHi < 0 || nHi >= HeightBins)
                                continue;
                            for (int ds = -rad; ds <= rad && !found; ds++)
                            {
                                int nSi = si + ds;
                                if (nSi < 0 || nSi >= SlopeBins)
                                    continue;
                                int nBin = nHi * SlopeBins + nSi;
                                if (count[nBin] <= 0)
                                    continue;
                                int srcOff = nBin * layerCount;
                                int dstOff = bin * layerCount;
                                for (int k = 0; k < layerCount; k++)
                                    sum[dstOff + k] = sum[srcOff + k];
                                count[bin] = count[nBin];
                                found = true;
                            }
                        }
                    }
                    if (!found)
                    {
                        count[bin] = 1;
                        sum[bin * layerCount] = 1.0;
                    }
                }
            }
        }

        static void SampleBin(Lut lut, int layerCount, float hNorm, float sNorm, float[,,] maps, int y, int x)
        {
            float h = Mathf.Clamp01(hNorm) * (HeightBins - 1);
            float s = Mathf.Clamp01(sNorm) * (SlopeBins - 1);
            int h0 = Mathf.Clamp(Mathf.FloorToInt(h), 0, HeightBins - 1);
            int s0 = Mathf.Clamp(Mathf.FloorToInt(s), 0, SlopeBins - 1);
            int h1 = Mathf.Min(h0 + 1, HeightBins - 1);
            int s1 = Mathf.Min(s0 + 1, SlopeBins - 1);
            float fh = h - h0;
            float fs = s - s0;

            for (int k = 0; k < layerCount; k++)
            {
                float w00 = BinAvg(lut, h0, s0, k, layerCount);
                float w10 = BinAvg(lut, h1, s0, k, layerCount);
                float w01 = BinAvg(lut, h0, s1, k, layerCount);
                float w11 = BinAvg(lut, h1, s1, k, layerCount);
                maps[y, x, k] = Mathf.Lerp(Mathf.Lerp(w00, w10, fh), Mathf.Lerp(w01, w11, fh), fs);
            }

            float total = 0f;
            for (int k = 0; k < layerCount; k++)
                total += maps[y, x, k];
            if (total < 0.0001f)
            {
                maps[y, x, 0] = 1f;
            }
            else
            {
                float inv = 1f / total;
                for (int k = 0; k < layerCount; k++)
                    maps[y, x, k] *= inv;
            }
        }

        static float BinAvg(Lut lut, int hi, int si, int layer, int layerCount)
        {
            int bin = hi * SlopeBins + si;
            int c = lut.Count[bin];
            if (c <= 0)
                return 0f;
            return (float)(lut.Sum[bin * layerCount + layer] / c);
        }

        static void BlurAndNormalize(float[,,] maps)
        {
            int h = maps.GetLength(0);
            int w = maps.GetLength(1);
            int layers = maps.GetLength(2);
            var tmp = (float[,,])maps.Clone();
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    float total = 0f;
                    for (int k = 0; k < layers; k++)
                    {
                        float v = tmp[y, x, k] * 4f
                                  + tmp[y - 1, x, k] + tmp[y + 1, x, k]
                                  + tmp[y, x - 1, k] + tmp[y, x + 1, k];
                        v /= 8f;
                        maps[y, x, k] = v;
                        total += v;
                    }
                    if (total < 0.0001f)
                    {
                        maps[y, x, 0] = 1f;
                    }
                    else
                    {
                        float inv = 1f / total;
                        for (int k = 0; k < layers; k++)
                            maps[y, x, k] *= inv;
                    }
                }
            }
        }

        static TerrainData FindTerrainData(string tileKey)
        {
            if (string.IsNullOrEmpty(tileKey))
                return null;
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t != null && t.terrainData != null && TileKey(t.name) == tileKey)
                    return t.terrainData;
            }
            string[] guids = AssetDatabase.FindAssets(tileKey + " t:TerrainData");
            if (guids == null)
                return null;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.IndexOf("Backups", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (path.IndexOf("splat-backup", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                TerrainData td = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (td != null && TileKey(td.name) == tileKey)
                    return td;
            }
            return null;
        }

        static string TileKey(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            var m = System.Text.RegularExpressions.Regex.Match(name, @"Terrain_(\d+)_(\d+)");
            if (m.Success)
                return "Terrain_" + m.Groups[1].Value + "_" + m.Groups[2].Value;
            return name;
        }

        static void RebuildBasemap(Terrain terrain)
        {
            TerrainData td = terrain.terrainData;
            if (td == null)
                return;
            Undo.RegisterCompleteObjectUndo(td, "Rebuild Terrain Basemap " + terrain.name);
            float[,] heights = td.GetHeights(0, 0, 1, 1);
            float orig = heights[0, 0];
            heights[0, 0] = orig + 1e-6f;
            td.SetHeights(0, 0, heights);
            heights[0, 0] = orig;
            td.SetHeights(0, 0, heights);
            td.SyncHeightmap();
            SetBaseMapDirty(td);
            terrain.Flush();
            EditorUtility.SetDirty(td);
            EditorUtility.SetDirty(terrain);
        }

        static void SetBaseMapDirty(TerrainData td)
        {
            MethodInfo mi = typeof(TerrainData).GetMethod(
                "SetBaseMapDirty",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
                mi.Invoke(td, null);
        }
    }
}
#endif
