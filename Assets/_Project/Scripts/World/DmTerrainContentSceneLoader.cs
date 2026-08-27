using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.World
{
    /// <summary>
    /// Loads companion Terrain_X_Y_Content scenes when matching regular Gaia terrain tiles load.
    /// Never pairs impostor, collider, or backup terrain scenes.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class DmTerrainContentSceneLoader : MonoBehaviour
    {
        private static DmTerrainContentSceneLoader _instance;

        private readonly HashSet<string> _loadedContentScenes = new HashSet<string>();
        private readonly Dictionary<string, AsyncOperation> _pendingLoads = new Dictionary<string, AsyncOperation>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (!Application.isPlaying || _instance != null)
                return;

            GameObject host = new GameObject(nameof(DmTerrainContentSceneLoader));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<DmTerrainContentSceneLoader>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;

            for (int i = 0; i < SceneManager.sceneCount; i++)
                TryLoadContentForTerrainScene(SceneManager.GetSceneAt(i));
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
                _instance = null;
            }
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_instance == null)
                return;

            _instance.TryLoadContentForTerrainScene(scene);
        }

        private static void HandleSceneUnloaded(Scene scene)
        {
            if (_instance == null)
                return;

            _instance.TryUnloadContentForTerrainScene(scene);
        }

        private void TryLoadContentForTerrainScene(Scene terrainScene)
        {
            if (!terrainScene.IsValid() || !terrainScene.isLoaded)
                return;

            if (!DmTerrainContentSceneNames.TryParseRegularTerrainScene(terrainScene.name, out int tileX, out int tileZ))
                return;

            string contentSceneName = DmTerrainContentSceneNames.GetContentSceneName(tileX, tileZ);
            Scene existing = SceneManager.GetSceneByName(contentSceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                _loadedContentScenes.Add(contentSceneName);
                return;
            }

            if (_pendingLoads.ContainsKey(contentSceneName))
                return;

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(contentSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogWarning(
                    $"[DmTerrainContentSceneLoader] Missing content scene '{contentSceneName}' for terrain '{terrainScene.name}'. Add it to Editor Build Settings.");
                return;
            }

            _pendingLoads[contentSceneName] = loadOp;
            loadOp.completed += _ =>
            {
                _pendingLoads.Remove(contentSceneName);
                _loadedContentScenes.Add(contentSceneName);
            };
        }

        private void TryUnloadContentForTerrainScene(Scene terrainScene)
        {
            if (!DmTerrainContentSceneNames.TryParseRegularTerrainScene(terrainScene.name, out int tileX, out int tileZ))
                return;

            string contentSceneName = DmTerrainContentSceneNames.GetContentSceneName(tileX, tileZ);
            _pendingLoads.Remove(contentSceneName);

            Scene contentScene = SceneManager.GetSceneByName(contentSceneName);
            if (!contentScene.IsValid() || !contentScene.isLoaded)
            {
                _loadedContentScenes.Remove(contentSceneName);
                return;
            }

            _loadedContentScenes.Remove(contentSceneName);
            SceneManager.UnloadSceneAsync(contentScene);
        }
    }
}
