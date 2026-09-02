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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            pendingReturnToMainMenu = false;
            pendingMenuSettingsReload = false;
        }

        /// <summary>
        /// Safety net after a settings scene reload that skipped the branded boot loader.
        /// Shows the main menu even if the pending-reload flags were cleared.
        /// </summary>
        public static void EnsureMenuRestoreAfterReload()
        {
            MenuSettingsReloadRunner.EnsureRunner();
        }

        /// <summary>Called from <see cref="SettingsPanelController"/> after settings are saved.</summary>
        public static void ReloadAfterApply()
        {
            if (!Application.isPlaying)
                return;

            bool wasGameplay = GameSession.HasStarted;
            // Always land on the main menu after a graphics Apply reload — never a UI-less world.
            pendingReturnToMainMenu = wasGameplay;
            pendingMenuSettingsReload = true;

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

        private static void TryShowSettingsAppliedToast()
        {
            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
                canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            if (PickupToastUI.EnsureExists(canvas.transform) == null)
                return;

            PickupToastUI.Show("Settings applied. Progress saved.");
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
                if (DMUiToolkitConfig.IsEnabled)
                    DMUiToolkitBootstrap.EnsureExists();
                yield return null;

                GameSession.ResetSession();
                ApplySettingsAfterReload();

                LoadingOverlayController.ReleaseOpaqueCover();
                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                if (menu == null)
                {
                    MainMenuController.EnsureExists();
                    menu = FindAnyObjectByType<MainMenuController>();
                }

                menu?.ShowMainMenu();
                yield return null;
                menu?.ShowMainMenu();

                TryShowSettingsAppliedToast();
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
                if (DMUiToolkitConfig.IsEnabled)
                    DMUiToolkitBootstrap.EnsureExists();
                yield return null;

                // Menu-only Apply reload can still leave HasStarted true if Play-from-scene
                // left a stale Playing phase — always reset so ShowMainMenu is authoritative.
                GameSession.ResetSession();
                ApplySettingsAfterReload();

                LoadingOverlayController.ReleaseOpaqueCover();
                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                if (menu == null)
                {
                    MainMenuController.EnsureExists();
                    menu = FindAnyObjectByType<MainMenuController>();
                }

                menu?.ShowMainMenu();
                yield return null;
                menu?.ShowMainMenu();

                Destroy(gameObject);
            }
        }
    }
}
