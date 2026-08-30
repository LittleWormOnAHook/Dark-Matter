using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_RENDER_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

/// <summary>
/// Creates Terrain_X_Y_Content companion scenes with disabled DmChunkProbe placeholders.
/// Menu: Dark Matter Genesis / World / Create Terrain Content Scenes
/// </summary>
public static class DmTerrainContentSceneSetup
{
    const string ScenesFolder = "Assets/_Project/Scenes";
    const string ProbeObjectName = "DmChunkProbe";

    [MenuItem("Dark Matter Genesis/World/Create Terrain Content Scenes", false, 42)]
    public static void CreateAllContentScenes()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Terrain Content Scenes", "Exit play mode first.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ScenesFolder))
        {
            EditorUtility.DisplayDialog("Terrain Content Scenes", "Missing folder: " + ScenesFolder, "OK");
            return;
        }

        int created = 0;
        for (int z = 0; z < Project.World.DmTerrainContentSceneNames.TerrainGridTiles; z++)
        {
            for (int x = 0; x < Project.World.DmTerrainContentSceneNames.TerrainGridTiles; x++)
            {
                if (CreateOrUpdateContentScene(x, z))
                    created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Terrain content scenes updated. Created or refreshed {created} scenes under {ScenesFolder}.");
    }

    static bool CreateOrUpdateContentScene(int tileX, int tileZ)
    {
        string sceneName = Project.World.DmTerrainContentSceneNames.GetContentSceneName(tileX, tileZ);
        string scenePath = Project.World.DmTerrainContentSceneNames.GetContentSceneAssetPath(tileX, tileZ);
        bool isNew = !File.Exists(scenePath);

        Scene scene;
        if (isNew)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        else
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        RemoveForbiddenRoots(scene);
        ConfigureProbe(scene, tileX, tileZ);
        AddToBuildSettings(scenePath);

        EditorSceneManager.SaveScene(scene);
        return isNew;
    }

    static void RemoveForbiddenRoots(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            if (root.CompareTag("MainCamera") || root.name == "Main Camera")
            {
                Object.DestroyImmediate(root);
                continue;
            }

            if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    static void ConfigureProbe(Scene scene, int tileX, int tileZ)
    {
        GameObject probeObject = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == ProbeObjectName)
            {
                probeObject = roots[i];
                break;
            }
        }

        if (probeObject == null)
        {
            probeObject = new GameObject(ProbeObjectName);
            SceneManager.MoveGameObjectToScene(probeObject, scene);
        }

        Vector3 tileCenter = GetTileCenter(tileX, tileZ);
        probeObject.transform.position = tileCenter;

        ReflectionProbe probe = probeObject.GetComponent<ReflectionProbe>();
        if (probe == null)
            probe = probeObject.AddComponent<ReflectionProbe>();

        probe.mode = global::UnityEngine.Rendering.ReflectionProbeMode.Baked;
        probe.resolution = 128;
        probe.size = new Vector3(150f, 150f, 150f);
        probe.center = Vector3.zero;
        probe.nearClipPlane = 0.3f;
        probe.farClipPlane = 100f;
        probe.hdr = true;
        probe.boxProjection = false;
        probe.renderDynamicObjects = false;
        probe.importance = 1;
        probe.intensity = 1f;
        probe.clearFlags = global::UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
        probe.enabled = false;

#if UNITY_RENDER_PIPELINE_HDRP
        HDAdditionalReflectionData hdProbe = probeObject.GetComponent<HDAdditionalReflectionData>();
        if (hdProbe == null)
            hdProbe = probeObject.AddComponent<HDAdditionalReflectionData>();
        hdProbe.enabled = false;
        hdProbe.weight = 1f;
#endif

        Project.World.DmChunkReflectionProbe marker = probeObject.GetComponent<Project.World.DmChunkReflectionProbe>();
        if (marker == null)
            marker = probeObject.AddComponent<Project.World.DmChunkReflectionProbe>();
        marker.SetTileCoordinates(tileX, tileZ);

        probeObject.SetActive(false);
        EditorUtility.SetDirty(probeObject);
    }

    static Vector3 GetTileCenter(int tileX, int tileZ)
    {
        float centerX = (float)Project.World.DmTerrainContentSceneNames.TerrainOriginX
            + tileX * Project.World.DmTerrainContentSceneNames.TerrainTileSizeMeters
            + Project.World.DmTerrainContentSceneNames.TerrainTileSizeMeters * 0.5f;
        float centerZ = (float)Project.World.DmTerrainContentSceneNames.TerrainOriginZ
            + tileZ * Project.World.DmTerrainContentSceneNames.TerrainTileSizeMeters
            + Project.World.DmTerrainContentSceneNames.TerrainTileSizeMeters * 0.5f;
        return new Vector3(centerX, 50f, centerZ);
    }

    static void AddToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == scenePath)
                return;
        }

        EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[current.Length + 1];
        for (int i = 0; i < current.Length; i++)
            next[i] = current[i];
        next[current.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = next;
    }
}
