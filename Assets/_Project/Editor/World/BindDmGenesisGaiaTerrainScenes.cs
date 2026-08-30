using System.Collections.Generic;
using System.IO;
using Gaia;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gaia looks for terrain scenes next to TerrainScenes.asset. The 16 DM Genesis
/// tiles live under Sessions/DM Genesis/Terrain Scenes, so storage must live there
/// and Terrain Loader Manager must point at that asset.
/// </summary>
[InitializeOnLoad]
public static class BindDmGenesisGaiaTerrainScenes
{
    const string SessionStorage = "Assets/Gaia User Data/Sessions/DM Genesis/TerrainScenes.asset";
    const string ScenesDir = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes";

    static BindDmGenesisGaiaTerrainScenes()
    {
        EditorApplication.delayCall += BindIfNeeded;
    }

    [MenuItem("Dark Matter Genesis/World/Bind Gaia DM Genesis Terrain Scenes")]
    public static void BindIfNeeded()
    {
        if (!Directory.Exists(ScenesDir) || !File.Exists(SessionStorage))
        {
            return;
        }

        TerrainSceneStorage storage = AssetDatabase.LoadAssetAtPath<TerrainSceneStorage>(SessionStorage);
        if (storage == null)
        {
            Debug.LogWarning("Bind Gaia terrains: missing " + SessionStorage);
            return;
        }

        storage.m_terrainTilesX = 4;
        storage.m_terrainTilesZ = 4;
        storage.m_terrainTilesSize = 2048;
        storage.m_useFloatingPointFix = true;
        storage.m_terrainLoadingEnabled = true;
        storage.m_pos00X = -4096d;
        storage.m_pos00Z = -4096d;
        if (storage.m_terrainScenes == null)
        {
            storage.m_terrainScenes = new List<TerrainScene>();
        }

        storage.m_terrainScenes.RemoveAll(scene =>
            scene == null ||
            string.IsNullOrEmpty(scene.m_scenePath) ||
            !scene.m_scenePath.Replace('\\', '/').Contains("Sessions/DM Genesis/Terrain Scenes"));

        TerrainLoaderManager tlm = Object.FindAnyObjectByType<TerrainLoaderManager>();
        if (tlm != null)
        {
            tlm.TerrainSceneStorage = storage;
            tlm.LoadStorageData();
            EditorUtility.SetDirty(tlm);
        }

        EnsureTerrainScenesInBuildSettings();

        int expected = Directory.GetFiles(ScenesDir, "Terrain_*.unity").Length;
        int bound = storage.m_terrainScenes.Count;
        if (bound < expected)
        {
            Debug.LogWarning("Bind Gaia terrains: storage has " + bound + " scenes, folder has " + expected + ".");
        }
        else
        {
            Debug.Log("Bound " + bound + " DM Genesis terrain scenes to Terrain Loader Manager.");
        }

        EditorUtility.SetDirty(storage);
        AssetDatabase.SaveAssets();
    }

    static void EnsureTerrainScenesInBuildSettings()
    {
        if (!Directory.Exists(ScenesDir))
            return;

        var scenePaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.GetFiles(ScenesDir, "*.unity"))
        {
            string normalized = file.Replace('\\', '/');
            int assetsIndex = normalized.IndexOf("Assets/", System.StringComparison.Ordinal);
            if (assetsIndex >= 0)
                scenePaths.Add(normalized.Substring(assetsIndex));
        }

        if (scenePaths.Count == 0)
            return;

        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        var pathSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < current.Length; i++)
        {
            if (!string.IsNullOrEmpty(current[i].path))
                pathSet.Add(current[i].path.Replace('\\', '/'));
        }

        var next = new List<EditorBuildSettingsScene>(current);
        bool changed = false;
        foreach (string scenePath in scenePaths)
        {
            if (pathSet.Contains(scenePath))
                continue;

            next.Add(new EditorBuildSettingsScene(scenePath, true));
            pathSet.Add(scenePath);
            changed = true;
        }

        if (!changed)
            return;

        EditorBuildSettings.scenes = next.ToArray();
        Debug.Log("Added " + (next.Count - current.Length) + " Gaia terrain scenes to Build Settings.");
    }
}
