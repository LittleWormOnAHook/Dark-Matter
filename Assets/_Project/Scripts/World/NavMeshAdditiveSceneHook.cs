using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.World
{
    /// <summary>
    /// Registers NavMeshSurface data when Gaia terrain scenes load additively, and removes on unload.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class NavMeshAdditiveSceneHook : MonoBehaviour
    {
        private static NavMeshAdditiveSceneHook _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (!Application.isPlaying || _instance != null)
                return;

            GameObject host = new GameObject(nameof(NavMeshAdditiveSceneHook));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<NavMeshAdditiveSceneHook>();
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
                RegisterSurfacesInScene(SceneManager.GetSceneAt(i), add: true);
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
            RegisterSurfacesInScene(scene, add: true);
        }

        private static void HandleSceneUnloaded(Scene scene)
        {
            RegisterSurfacesInScene(scene, add: false);
        }

        private static void RegisterSurfacesInScene(Scene scene, bool add)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                NavMeshSurface[] surfaces = roots[r].GetComponentsInChildren<NavMeshSurface>(true);
                for (int i = 0; i < surfaces.Length; i++)
                {
                    NavMeshSurface surface = surfaces[i];
                    if (surface == null || surface.navMeshData == null)
                        continue;

                    if (add)
                    {
                        if (!surface.isActiveAndEnabled)
                            surface.enabled = true;
                        surface.AddData();
                    }
                    else
                    {
                        surface.RemoveData();
                    }
                }
            }
        }
    }
}
