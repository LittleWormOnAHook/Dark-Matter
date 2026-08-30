#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repair tools for the Gaia stamper dark-square (4-tile splat / stale HDRP basemap).
/// Does not stamp height, does not create impostors, does not touch NavMesh.
/// </summary>
public static class WorldTerrainRepairMenus
{
    const string TerrainDataFolder = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Data";
    const string TerrainDataSuffix = "-20260823 - 024958";

    static readonly string[] KnownDirtyTiles =
    {
        "Terrain_1_1",
        "Terrain_1_2",
        "Terrain_2_1",
        "Terrain_2_2",
    };

    static readonly Dictionary<string, string> DefaultCleanNeighbor = new Dictionary<string, string>
    {
        { "Terrain_1_1", "Terrain_0_1" },
        { "Terrain_1_2", "Terrain_0_2" },
        { "Terrain_2_1", "Terrain_2_0" },
        { "Terrain_2_2", "Terrain_2_3" },
    };

    [MenuItem("Dark Matter Genesis/World/Rebuild Terrain Basemaps", false, 50)]
    public static void RebuildTerrainBasemaps()
    {
        Terrain[] terrains = Selection.GetFiltered<Terrain>(SelectionMode.Deep);
        if (terrains == null || terrains.Length == 0)
            terrains = Terrain.activeTerrains;

        if (terrains == null || terrains.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Rebuild Terrain Basemaps",
                "No terrains loaded or selected.\n\nSelect the 4 tiles in the Hierarchy (Terrain_1_1, Terrain_1_2, Terrain_2_1, Terrain_2_2) and run this again.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Rebuild Terrain Basemaps",
                "Rebuild Unity HDRP basemaps on " + terrains.Length + " loaded terrain(s).\n\nThis does NOT restore splatmaps.",
                "Rebuild",
                "Cancel"))
            return;

        int n = 0;
        try
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if (t == null || t.terrainData == null)
                    continue;
                EditorUtility.DisplayProgressBar("Rebuild Terrain Basemaps", t.name, (float)i / terrains.Length);
                RebuildBasemap(t);
                n++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[DMG] Rebuilt basemaps on " + n + " terrain(s).");
    }

    [MenuItem("Dark Matter Genesis/World/Restore Terrain Layer List (Keep Splat)", false, 52)]
    public static void RestoreTerrainLayerListKeepSplat()
    {
        List<TerrainData> targets = CollectTargetTerrainData(out string how);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Restore Terrain Layer List",
                "No dirty terrains found.\n\nSelect Terrain_1_1, Terrain_1_2, Terrain_2_1, Terrain_2_2 in the Hierarchy (or their TerrainData in Project), then run this again.",
                "OK");
            return;
        }

        var plan = new List<CopyPlan>();
        var missing = new List<string>();
        foreach (TerrainData dst in targets)
        {
            string dstTile = TileKeyFromName(dst.name);
            string srcTile = PickCleanSourceTile(dstTile, targets);
            TerrainData src = LoadTerrainDataAsset(srcTile);
            if (src == null)
            {
                missing.Add(dst.name + "  <-  missing source " + srcTile);
                continue;
            }
            if (src == dst)
            {
                missing.Add(dst.name + "  <-  source resolved to itself");
                continue;
            }
            plan.Add(new CopyPlan { Dst = dst, Src = src, DstTile = dstTile, SrcTile = srcTile });
        }

        var msg = new StringBuilder();
        msg.AppendLine("Copy the original TerrainLayer list from a clean neighbor.");
        msg.AppendLine("Keeps this tile's splat weights. Does NOT copy neighbor alphamaps.");
        msg.AppendLine();
        msg.AppendLine("Targets (" + how + "):");
        foreach (CopyPlan p in plan)
        {
            int dstCount = p.Dst.terrainLayers != null ? p.Dst.terrainLayers.Length : 0;
            int srcCount = p.Src.terrainLayers != null ? p.Src.terrainLayers.Length : 0;
            int splatTex = p.Dst.alphamapTextures != null ? p.Dst.alphamapTextures.Length : 0;
            msg.AppendLine("  " + p.DstTile + "  (" + dstCount + " layers, " + splatTex + " splat tex)  <-  layers from  " + p.SrcTile + "  (" + srcCount + " layers)");
            msg.AppendLine("      dst: " + DescribeLayers(p.Dst.terrainLayers));
            msg.AppendLine("      src: " + DescribeLayers(p.Src.terrainLayers));
        }
        if (missing.Count > 0)
        {
            msg.AppendLine();
            msg.AppendLine("SKIPPED:");
            foreach (string s in missing)
                msg.AppendLine("  " + s);
        }
        msg.AppendLine();
        msg.AppendLine("Do NOT run Restore Terrain Splatmaps On Selection.");

        if (plan.Count == 0)
        {
            EditorUtility.DisplayDialog("Restore Terrain Layer List", msg.ToString(), "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Restore Terrain Layer List", msg.ToString(), "Copy layer list", "Cancel"))
            return;

        int ok = 0;
        try
        {
            for (int i = 0; i < plan.Count; i++)
            {
                CopyPlan p = plan[i];
                EditorUtility.DisplayProgressBar("Restore Terrain Layer List", p.DstTile + " <- " + p.SrcTile, (float)i / plan.Count);
                if (CopyLayersKeepSplat(p.Src, p.Dst))
                    ok++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t != null && t.terrainData != null && targets.Contains(t.terrainData))
                RebuildBasemap(t);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Restore Terrain Layer List",
            "Restored layer lists on " + ok + " / " + plan.Count + " terrain(s) and rebuilt basemaps.\n\nCheck Terrain_2_1 in Scene view.",
            "OK");
        Debug.Log("[DMG] Restore Terrain Layer List finished. " + ok + " tile(s).");
    }

    [MenuItem("Dark Matter Genesis/World/Restore Terrain Splatmaps On Selection", false, 51)]
    public static void RestoreTerrainSplatmapsOnSelection()
    {
        EditorUtility.DisplayDialog(
            "Restore Terrain Splatmaps",
            "Disabled. That copy already wrecked the 2x2 (cross seam).\n\nUse Restore Terrain Layer List (Keep Splat) instead.",
            "OK");
    }

    struct CopyPlan
    {
        public TerrainData Dst;
        public TerrainData Src;
        public string DstTile;
        public string SrcTile;
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

    static bool CopyLayersKeepSplat(TerrainData src, TerrainData dst)
    {
        if (src == null || dst == null)
            return false;

        TerrainLayer[] srcLayers = src.terrainLayers;
        if (srcLayers == null || srcLayers.Length == 0)
        {
            Debug.LogError("[DMG] source has no terrainLayers: " + src.name);
            return false;
        }

        float[,,] maps = ReadAlphamapsPreservingChannels(dst, srcLayers.Length);
        if (maps == null)
        {
            Debug.LogError("[DMG] could not read splat channels on " + dst.name);
            return false;
        }

        Undo.RegisterCompleteObjectUndo(dst, "Restore Terrain Layer List " + dst.name);
        dst.terrainLayers = srcLayers;
        dst.SetAlphamaps(0, 0, maps);
        SetBaseMapDirtyCompat(dst);
        dst.SyncHeightmap();
        EditorUtility.SetDirty(dst);
        Debug.Log("[DMG] " + dst.name + " layers " + DescribeLayers(srcLayers) + " alphamap " + maps.GetLength(2) + " ch");
        return true;
    }

    static float[,,] ReadAlphamapsPreservingChannels(TerrainData td, int layerCount)
    {
        Texture2D[] texs = td.alphamapTextures;
        int w = td.alphamapWidth;
        int h = td.alphamapHeight;
        if (texs != null && texs.Length > 0)
        {
            float[,,] maps = new float[h, w, layerCount];
            for (int ti = 0; ti < texs.Length; ti++)
            {
                Texture2D tex = texs[ti];
                if (tex == null)
                    continue;
                Color[] pixels;
                try
                {
                    pixels = tex.GetPixels();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[DMG] GetPixels failed on " + tex.name + ": " + ex.Message);
                    continue;
                }
                if (pixels == null || pixels.Length != w * h)
                {
                    Debug.LogWarning("[DMG] splat tex size mismatch " + (tex != null ? tex.name : "?") + " " + (pixels != null ? pixels.Length : 0) + " vs " + (w * h));
                    continue;
                }
                for (int i = 0; i < pixels.Length; i++)
                {
                    int x = i % w;
                    int y = i / w;
                    Color c = pixels[i];
                    int baseLayer = ti * 4;
                    if (baseLayer + 0 < layerCount) maps[y, x, baseLayer + 0] = c.r;
                    if (baseLayer + 1 < layerCount) maps[y, x, baseLayer + 1] = c.g;
                    if (baseLayer + 2 < layerCount) maps[y, x, baseLayer + 2] = c.b;
                    if (baseLayer + 3 < layerCount) maps[y, x, baseLayer + 3] = c.a;
                }
            }
            return maps;
        }

        float[,,] current = td.GetAlphamaps(0, 0, w, h);
        if (current.GetLength(2) == layerCount)
            return current;

        float[,,] padded = new float[h, w, layerCount];
        int copy = Math.Min(current.GetLength(2), layerCount);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                for (int k = 0; k < copy; k++)
                    padded[y, x, k] = current[y, x, k];
            }
        }
        return padded;
    }

    static List<TerrainData> CollectTargetTerrainData(out string how)
    {
        var list = new List<TerrainData>();
        var seen = new HashSet<TerrainData>();

        foreach (Terrain t in Selection.GetFiltered<Terrain>(SelectionMode.Deep))
        {
            if (t != null && t.terrainData != null && seen.Add(t.terrainData))
                list.Add(t.terrainData);
        }
        foreach (TerrainData td in Selection.GetFiltered<TerrainData>(SelectionMode.Deep))
        {
            if (td != null && seen.Add(td))
                list.Add(td);
        }

        if (list.Count > 0)
        {
            how = "Hierarchy/Project selection";
            return list;
        }

        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null)
                continue;
            string key = TileKeyFromName(t.name);
            if (Array.IndexOf(KnownDirtyTiles, key) >= 0 && seen.Add(t.terrainData))
                list.Add(t.terrainData);
        }
        if (list.Count > 0)
        {
            how = "loaded known-dirty 2x2 (Terrain_1_1, 1_2, 2_1, 2_2)";
            return list;
        }

        foreach (string tile in KnownDirtyTiles)
        {
            TerrainData td = LoadTerrainDataAsset(tile);
            if (td != null && seen.Add(td))
                list.Add(td);
        }
        how = list.Count > 0 ? "disk assets for known-dirty 2x2" : "none";
        return list;
    }

    static string PickCleanSourceTile(string dstTile, List<TerrainData> targets)
    {
        var targetKeys = new HashSet<string>();
        foreach (TerrainData td in targets)
            targetKeys.Add(TileKeyFromName(td.name));

        string mapped;
        if (DefaultCleanNeighbor.TryGetValue(dstTile, out mapped))
        {
            if (!targetKeys.Contains(mapped) && TerrainDataAssetExists(mapped))
                return mapped;
        }

        int x, y;
        if (TryParseTile(dstTile, out x, out y))
        {
            int[,] dirs = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
            for (int i = 0; i < 4; i++)
            {
                string nb = "Terrain_" + (x + dirs[i, 0]) + "_" + (y + dirs[i, 1]);
                if (targetKeys.Contains(nb))
                    continue;
                if (Array.IndexOf(KnownDirtyTiles, nb) >= 0)
                    continue;
                if (TerrainDataAssetExists(nb))
                    return nb;
            }
        }

        return mapped ?? dstTile;
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
        SetBaseMapDirtyCompat(td);
        terrain.Flush();
        EditorUtility.SetDirty(td);
        EditorUtility.SetDirty(terrain);
    }

    static void SetBaseMapDirtyCompat(TerrainData td)
    {
        MethodInfo mi = typeof(TerrainData).GetMethod(
            "SetBaseMapDirty",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi != null)
            mi.Invoke(td, null);
    }

    static string TileKeyFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";
        Match m = Regex.Match(name, @"Terrain_(\d+)_(\d+)");
        if (m.Success)
            return "Terrain_" + m.Groups[1].Value + "_" + m.Groups[2].Value;
        return name;
    }

    static bool TryParseTile(string tileKey, out int x, out int y)
    {
        x = y = 0;
        Match m = Regex.Match(tileKey ?? "", @"Terrain_(\d+)_(\d+)");
        if (!m.Success)
            return false;
        x = int.Parse(m.Groups[1].Value);
        y = int.Parse(m.Groups[2].Value);
        return true;
    }

    static string TerrainDataAssetPath(string tileKey)
    {
        return TerrainDataFolder + "/" + tileKey + TerrainDataSuffix + ".asset";
    }

    static bool TerrainDataAssetExists(string tileKey)
    {
        return File.Exists(Path.GetFullPath(TerrainDataAssetPath(tileKey)));
    }

    static TerrainData LoadTerrainDataAsset(string tileKey)
    {
        if (string.IsNullOrEmpty(tileKey))
            return null;
        string rel = TerrainDataAssetPath(tileKey);
        TerrainData td = AssetDatabase.LoadAssetAtPath<TerrainData>(rel);
        if (td != null)
            return td;
        string[] guids = AssetDatabase.FindAssets(tileKey + " t:TerrainData");
        if (guids != null && guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
#endif
