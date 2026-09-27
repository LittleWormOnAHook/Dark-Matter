using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Building
{
    /// <summary>
    /// 0927-cleaner: New Game, Load Game and full scene reloads all run here so nothing from the last session's
    /// build mode survives: build mode is closed, a carried piece goes back, the placement preview is destroyed and
    /// any stray preview objects are swept. New Game also removes every placed piece (a load replaces them anyway).
    /// </summary>
    public static class DMBuildingSceneCleaner
    {
        public static void CleanForSessionChange(bool clearPlacedPieces)
        {
            DMBuildingMode.ForceExit();
            DMBuildingPlacementController.SweepStrayPreviews();
            if (clearPlacedPieces)
                DMBuildingPlacementController.ClearAllPieces();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Additive terrain / content scenes stream in during play; only a full reload is a new session.
            if (mode == LoadSceneMode.Single)
                CleanForSessionChange(false);
        }
    }
}
