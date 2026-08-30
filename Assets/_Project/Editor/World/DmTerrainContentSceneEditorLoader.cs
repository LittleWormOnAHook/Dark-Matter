using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.World;

/// <summary>
/// Edit-mode: load Terrain_X_Y_Content additively with each regular Gaia tile TLM opens.
/// Content roots stay disabled. Play mode uses DmTerrainContentSceneLoader instead.
/// </summary>
[InitializeOnLoad]
internal static class DmTerrainContentSceneEditorLoader
{
    static DmTerrainContentSceneEditorLoader()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        EditorApplication.delayCall += SyncOpenTerrainTiles;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TryOpenContentForTerrain(scene);
    }

    private static void OnSceneClosed(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TryCloseContentForTerrain(scene);
    }

    private static void SyncOpenTerrainTiles()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            TryOpenContentForTerrain(SceneManager.GetSceneAt(i));
    }

    private static void TryOpenContentForTerrain(Scene terrainScene)
    {
        if (!terrainScene.IsValid() || !terrainScene.isLoaded)
            return;

        if (!DmTerrainContentSceneNames.TryParseRegularTerrainScene(terrainScene.name, out int tileX, out int tileZ))
            return;

        string assetPath = DmTerrainContentSceneNames.GetContentSceneAssetPath(tileX, tileZ);
        if (!System.IO.File.Exists(assetPath))
            return;

        Scene content = SceneManager.GetSceneByPath(assetPath);
        if (!content.IsValid() || !content.isLoaded)
            content = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);

        DmTerrainContentSceneLoader.DisableContentSceneProbes(content);
    }

    private static void TryCloseContentForTerrain(Scene terrainScene)
    {
        if (!DmTerrainContentSceneNames.TryParseRegularTerrainScene(terrainScene.name, out int tileX, out int tileZ))
            return;

        string assetPath = DmTerrainContentSceneNames.GetContentSceneAssetPath(tileX, tileZ);
        Scene content = SceneManager.GetSceneByPath(assetPath);
        if (!content.IsValid() || !content.isLoaded)
            return;

        EditorSceneManager.CloseScene(content, true);
    }
}