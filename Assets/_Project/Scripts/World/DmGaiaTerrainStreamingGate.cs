using Project.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Project.World
{
    /// <summary>
    /// Applies World → Terrain Loading: Gaia loader on/off and unloads streamed terrain/content scenes when off.
    /// </summary>
    public static class DmGaiaTerrainStreamingGate
    {
        public static void ApplyFromProfiles()
        {
            bool enabled = DMPlayerSystemsProfile.IsWorldTerrainLoadingEnabled();
            DMPlayerSystemsProfile.TryApplyGaiaTerrainLoader(enabled);
            if (!enabled)
                UnloadStreamedTerrainScenes();
        }

        public static void UnloadStreamedTerrainScenes()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.IsValid())
                    continue;

                if (!IsGaiaStreamedSceneName(scene.name))
                    continue;

                if (Application.isPlaying)
                {
                    SceneManager.UnloadSceneAsync(scene);
                    continue;
                }

#if UNITY_EDITOR
                if (scene.isLoaded)
                    EditorSceneManager.UnloadSceneAsync(scene);
#endif
            }
        }

        private static bool IsGaiaStreamedSceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (sceneName.StartsWith("Terrain_"))
                return true;

            return sceneName.EndsWith("_Content", System.StringComparison.Ordinal)
                   && sceneName.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
