using Project.UI;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Central gate for world AI, locomotion helpers, landing probes, and other gameplay tickers
    /// that should not run on the title menu, pause overlay, boot loader, or while timeScale is 0.
    /// </summary>
    public static class GameplayWorldSimulation
    {
        private static int _frozenCacheFrame = -1;
        private static bool _frozenCacheValue;

        public static bool IsFrozen
        {
            get
            {
                int frame = Time.frameCount;
                if (_frozenCacheFrame == frame)
                    return _frozenCacheValue;

                _frozenCacheFrame = frame;
                _frozenCacheValue = ComputeIsFrozen();
                return _frozenCacheValue;
            }
        }

        private static bool ComputeIsFrozen()
        {
            if (!Application.isPlaying)
                return true;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return true;

            if (!GameSession.HasStarted)
                return true;

            if (Time.timeScale <= 0.01f)
                return true;

            if (MainMenuController.BlocksGameplayHud)
                return true;

            return false;
        }

        /// <summary>
        /// Title / pre-game phases must stay at timeScale 0 so the loaded world does not simulate behind menus.
        /// </summary>
        public static void EnforcePreGameplayPause()
        {
            if (!Application.isPlaying || GameSession.HasStarted)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            if (Time.timeScale <= 0.01f)
                return;

            MainMenuController.EnforceTitleWorldPause();
        }
    }
}
