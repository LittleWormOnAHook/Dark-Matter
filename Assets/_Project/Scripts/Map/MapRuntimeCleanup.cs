using Project.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Map
{
    internal static class MapRuntimeCleanup
    {
        /// <summary>
        /// True while play mode is exiting / domain is tearing down. Used to prevent
        /// runtime spawns (e.g. orphan WorldMapProvider) from OnDestroy cascades.
        /// </summary>
        internal static bool IsQuittingPlayMode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsQuittingPlayMode = false;
            MapRegistry.Clear();
            MapUiSprites.ResetCache();
            OpticsUiSprites.ResetCache();
            WorldMapProvider.ResetStaticState();
            OpticsOverlayUI.ResetRuntimeState();
            PetTamingProgressUI.ResetRuntimeState();

            Application.quitting -= HandleApplicationQuitting;
            Application.quitting += HandleApplicationQuitting;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CleanupStaleRuntimeCanvases()
        {
            if (!Application.isPlaying)
                return;

            OpticsOverlayUI.CleanupStaleRuntimeObjects();
        }

        private static void HandleApplicationQuitting()
        {
            IsQuittingPlayMode = true;
        }

#if UNITY_EDITOR
        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                IsQuittingPlayMode = true;
            else if (state == PlayModeStateChange.EnteredPlayMode)
                IsQuittingPlayMode = false;
        }
#endif
    }
}
