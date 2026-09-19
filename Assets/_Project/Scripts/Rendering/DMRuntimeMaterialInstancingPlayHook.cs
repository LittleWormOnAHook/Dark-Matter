using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Rendering
{
    /// <summary>
    /// Play-mode sweep: clone shared project materials on all scene renderers after load,
    /// and on additive scene loads. Dynamic spawns should call
    /// <see cref="DMRuntimeHierarchyMaterialInstancer.NotifySpawned"/>.
    /// </summary>
    public static class DMRuntimeMaterialInstancingPlayHook
    {
        private static bool _subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying)
                return;

            Subscribe();
            InstanceScene(SceneManager.GetActiveScene());
        }

        private static void Subscribe()
        {
            if (_subscribed)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            _subscribed = true;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isPlaying)
                return;

            InstanceScene(scene);
        }

        private static void InstanceScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                    continue;

                DMRuntimeHierarchyMaterialInstancer.InstanceHierarchyMaterials(
                    root.transform,
                    includeInactive: true,
                    skipParticleRenderers: true);
            }
        }
    }
}
