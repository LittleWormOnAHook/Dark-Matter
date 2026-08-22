using System.Collections;
using Project.Audio;
using Project.Map;
using Project.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core
{
    /// <summary>
    /// Reloads the active scene after graphics settings Apply so HDRP pipeline, volumes, and quality tiers
    /// fully reinitialize. In-game Apply saves continue progress, reloads, and returns to the main menu.
    /// </summary>
    public static class SettingsSceneReloader
    {
        private static bool pendingReturnToMainMenu;
        private static bool pendingMenuSettingsReload;

        /// <summary>Called from <see cref="SettingsPanelController"/> after settings are saved.</summary>
        public static void ReloadAfterApply()
        {
            if (!Application.isPlaying)
                return;

            bool wasGameplay = GameSession.HasStarted;
            pendingReturnToMainMenu = wasGameplay;
            pendingMenuSettingsReload = !wasGameplay;

            if (wasGameplay)
            {
                float previousTimeScale = Time.timeScale;
                Time.timeScale = 1f;

                if (!GameSaveSystem.TrySaveContinueExpedition(out string saveMessage))
                    Debug.LogWarning($"SettingsSceneReloader: Could not save continue slot before reload. {saveMessage}");

                Time.timeScale = previousTimeScale;
            }

            LoadingOverlayController.ShowForSettingsReload(() =>
            {
                LoadingOverlayController.SuppressNextBootOverlay();
                MapRuntimeCleanup.NotifySceneTransitionStarted();
                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.buildIndex);
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterSceneLoad()
        {
            if (!Application.isPlaying)
                return;

            if (pendingReturnToMainMenu)
            {
                pendingReturnToMainMenu = false;
                MainMenuReturnRunner.EnsureRunner();
                return;
            }

            if (pendingMenuSettingsReload)
            {
                pendingMenuSettingsReload = false;
                MenuSettingsReloadRunner.EnsureRunner();
            }
        }

        private static void ApplySettingsAfterReload()
        {
            GameSettings.ReloadFromPlayerPrefs();
            PostProcessingController.EnsureExists();
            PostProcessingController.Instance?.RebuildRuntimeProfile();
            PostProcessingController.Instance?.ApplyFromSettings();
        }

        private sealed class MainMenuReturnRunner : MonoBehaviour
        {
            public static void EnsureRunner()
            {
                if (FindAnyObjectByType<MainMenuReturnRunner>() != null)
                    return;

                GameObject host = new GameObject(nameof(MainMenuReturnRunner));
                host.AddComponent<MainMenuReturnRunner>();
            }

            private IEnumerator Start()
            {
                const int maxFrames = 120;
                for (int i = 0; i < maxFrames; i++)
                {
                    if (FindAnyObjectByType<MainMenuController>() != null)
                        break;

                    yield return null;
                }

                yield return null;

                GameSession.ResetSession();
                ApplySettingsAfterReload();

                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                menu?.ShowMainMenu();

                LoadingOverlayController.ReleaseOpaqueCover();
                PickupToastUI.Show("Settings applied. Progress saved.");
                Destroy(gameObject);
            }
        }

        private sealed class MenuSettingsReloadRunner : MonoBehaviour
        {
            public static void EnsureRunner()
            {
                if (FindAnyObjectByType<MenuSettingsReloadRunner>() != null)
                    return;

                GameObject host = new GameObject(nameof(MenuSettingsReloadRunner));
                host.AddComponent<MenuSettingsReloadRunner>();
            }

            private IEnumerator Start()
            {
                yield return null;

                ApplySettingsAfterReload();

                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                menu?.ShowMainMenu();

                LoadingOverlayController.ReleaseOpaqueCover();
                Destroy(gameObject);
            }
        }
    }
}
