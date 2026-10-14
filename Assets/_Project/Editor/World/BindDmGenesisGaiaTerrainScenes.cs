using System.Collections.Generic;
using System.IO;
using Gaia;
using Project.Player;
using Project.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gaia looks for terrain scenes next to TerrainScenes.asset. The 16 DM Genesis
/// tiles live under Sessions/DM Genesis/Terrain Scenes, so storage must live there
/// and Terrain Loader Manager must point at that asset.
/// <para>
/// Terrain on/off for audits is <b>not</b> a Gaia-only asset. Authority is
/// <see cref="DMPlayerSystemsProfile.terrainLoading"/> (Inspector: World → Terrain Loading)
/// on the live player: prefab
/// <c>Assets/_Project/Prefabs/Players/Player_v7.prefab</c>, optional overrides on
/// <c>Player_v7 Variant.prefab</c> or the open scene's <c>Player_v7</c> instance
/// (Genesis Studio → Player). Read via <see cref="DMPlayerSystemsProfile.IsWorldTerrainLoadingEnabled"/>.
/// </para>
/// </summary>
[InitializeOnLoad]
public static class BindDmGenesisGaiaTerrainScenes
{
    const string SessionStorage = "Assets/Gaia User Data/Sessions/DM Genesis/TerrainScenes.asset";
    const string ScenesDir = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes";
    const int MaxDeferredBindAttempts = 120;

    static int deferredBindAttempts;
    static bool lastKnownTerrainLoadingEnabled = true;
    static bool terrainLoadingPollInitialized;

    static BindDmGenesisGaiaTerrainScenes()
    {
        EditorSceneManager.sceneOpened += OnEditorSceneOpened;
        EditorApplication.delayCall += BindIfNeeded;
        EditorApplication.update += PollTerrainLoadingProfileToggle;
    }

    static void OnEditorSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        deferredBindAttempts = 0;
        terrainLoadingPollInitialized = false;
        EditorApplication.delayCall += BindIfNeeded;
    }

    /// <summary>
    /// When World → Terrain Loading is toggled on the live <see cref="DMPlayerSystemsProfile"/>,
    /// re-bind or unload so audit-off does not leave stale loader state when ticked back on.
    /// </summary>
    static void PollTerrainLoadingProfileToggle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool enabled = DMPlayerSystemsProfile.IsWorldTerrainLoadingEnabled();
        if (!terrainLoadingPollInitialized)
        {
            terrainLoadingPollInitialized = true;
            lastKnownTerrainLoadingEnabled = enabled;
            return;
        }

        if (enabled == lastKnownTerrainLoadingEnabled)
            return;

        lastKnownTerrainLoadingEnabled = enabled;
        deferredBindAttempts = 0;
        EditorApplication.delayCall += BindIfNeeded;
    }

    [MenuItem("Dark Matter Genesis/World/Bind Gaia DM Genesis Terrain Scenes", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Dark_Matter_Genesis_World_Bind_Gaia_DM_Genesis_Terrain_Scenes)]
    public static void BindIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!Directory.Exists(ScenesDir) || !File.Exists(SessionStorage))
            return;

        bool terrainLoadingEnabled = DMPlayerSystemsProfile.IsWorldTerrainLoadingEnabled();
        lastKnownTerrainLoadingEnabled = terrainLoadingEnabled;

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
        storage.m_terrainLoadingEnabled = terrainLoadingEnabled;
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

        EnsureTerrainScenesInBuildSettings();

        int expected = Directory.GetFiles(ScenesDir, "Terrain_*.unity").Length;
        int bound = storage.m_terrainScenes.Count;
        if (bound < expected)
        {
            Debug.LogWarning("Bind Gaia terrains: storage has " + bound + " scenes, folder has " + expected + ".");
        }

        EditorUtility.SetDirty(storage);
        AssetDatabase.SaveAssets();

        if (!TryRefreshTerrainLoaderManager(storage, terrainLoadingEnabled, out bool loaderDeferred))
            return;

        if (loaderDeferred)
            return;

        if (terrainLoadingEnabled && bound >= expected)
        {
            Debug.Log("Bound " + bound + " DM Genesis terrain scenes to Terrain Loader Manager.");
        }

        DmGaiaTerrainStreamingGate.ApplyFromProfiles();
    }

    /// <summary>
    /// Gaia LoadStorageData opens terrain scenes; Malbers/Gaia callbacks require the host scene to be loaded first.
    /// Skipped when <see cref="DMPlayerSystemsProfile.terrainLoading"/> is off (audit / combat focus).
    /// </summary>
    static bool TryRefreshTerrainLoaderManager(
        TerrainSceneStorage storage,
        bool terrainLoadingEnabled,
        out bool deferred)
    {
        deferred = false;

        TerrainLoaderManager tlm = Object.FindAnyObjectByType<TerrainLoaderManager>();
        if (tlm == null)
            return true;

        tlm.TerrainSceneStorage = storage;
        EditorUtility.SetDirty(tlm);

        if (!terrainLoadingEnabled)
            return true;

        if (!IsEditorReadyForGaiaTerrainLoad(tlm))
        {
            deferred = true;
            ScheduleDeferredBind();
            return true;
        }

        deferredBindAttempts = 0;
        tlm.LoadStorageData();
        return true;
    }

    static bool IsEditorReadyForGaiaTerrainLoad(TerrainLoaderManager tlm)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return false;

        Scene active = EditorSceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
            return false;

        if (tlm == null)
            return false;

        Scene hostScene = tlm.gameObject.scene;
        if (!hostScene.IsValid() || !hostScene.isLoaded)
            return false;

        // Loader must belong to the open edit-mode scene (not a stale object mid-open).
        if (hostScene.handle != active.handle)
            return false;

        return true;
    }

    static void ScheduleDeferredBind()
    {
        if (deferredBindAttempts >= MaxDeferredBindAttempts)
        {
            Debug.LogWarning(
                "Bind Gaia terrains: deferred terrain loader refresh gave up after " +
                MaxDeferredBindAttempts + " attempts (active scene may not be loaded yet).");
            return;
        }

        deferredBindAttempts++;
        EditorApplication.delayCall += BindIfNeeded;
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
