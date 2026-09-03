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
    /// fully reinitialize. In-game Apply snapshots the live expedition, reloads, then returns to Settings still paused.
    /// Main Menu from pause goes to the title. Continue Expedition loads that snapshot.
    /// </summary>
    public static class SettingsSceneReloader
    {
        private const string ResumeGameplayPrefsKey = "DMG.ResumeGameplayAfterSettingsApply";

        private static bool pendingResumeGameplay;
        private static bool pendingMenuSettingsReload;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            pendingResumeGameplay = false;
            pendingMenuSettingsReload = false;
        }

        /// <summary>
        /// Safety net after a settings scene reload that skipped the branded boot loader.
        /// Resumes the Apply snapshot when one was saved; otherwise shows the main menu.
        /// </summary>
        public static void EnsureMenuRestoreAfterReload()
        {
            if (ShouldResumeGameplay())
                GameplayResumeRunner.EnsureRunner();
            else
                MenuSettingsReloadRunner.EnsureRunner();
        }

        /// <summary>Called from <see cref="SettingsPanelController"/> after settings are saved.</summary>
        public static void ReloadAfterApply()
        {
            if (!Application.isPlaying)
                return;

            bool wasGameplay = GameSession.HasStarted;
            pendingResumeGameplay = wasGameplay;
            pendingMenuSettingsReload = !wasGameplay;
            PlayerPrefs.SetInt(ResumeGameplayPrefsKey, wasGameplay ? 1 : 0);
            PlayerPrefs.Save();

            if (wasGameplay)
            {
                float previousTimeScale = Time.timeScale;
                Time.timeScale = 1f;

                if (!GameSaveSystem.TrySaveSettingsReloadSnapshot(out string saveMessage))
                    Debug.LogWarning($"SettingsSceneReloader: Could not save session before reload. {saveMessage}");

                if (!GameSaveSystem.TrySaveContinueExpedition(out string continueMessage))
                    Debug.LogWarning($"SettingsSceneReloader: Could not update Continue slot before reload. {continueMessage}");

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

            if (ShouldResumeGameplay())
            {
                GameplayResumeRunner.EnsureRunner();
                return;
            }

            if (pendingMenuSettingsReload)
            {
                pendingMenuSettingsReload = false;
                MenuSettingsReloadRunner.EnsureRunner();
            }
        }

        private static bool ShouldResumeGameplay()
        {
            return pendingResumeGameplay || PlayerPrefs.GetInt(ResumeGameplayPrefsKey, 0) == 1;
        }

        private static void ConsumeResumeFlag()
        {
            pendingResumeGameplay = false;
            if (PlayerPrefs.GetInt(ResumeGameplayPrefsKey, 0) != 0)
            {
                PlayerPrefs.SetInt(ResumeGameplayPrefsKey, 0);
                PlayerPrefs.Save();
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

            PickupToastUI.Show("Settings applied.");
        }

        private sealed class GameplayResumeRunner : MonoBehaviour
        {
            public static void EnsureRunner()
            {
                if (FindAnyObjectByType<GameplayResumeRunner>() != null)
                    return;

                GameObject host = new GameObject(nameof(GameplayResumeRunner));
                host.AddComponent<GameplayResumeRunner>();
            }

            private IEnumerator Start()
            {
                ConsumeResumeFlag();

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

                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                if (menu == null)
                {
                    MainMenuController.EnsureExists();
                    menu = FindAnyObjectByType<MainMenuController>();
                }

                const int playerWait = 120;
                for (int p = 0; p < playerWait; p++)
                {
                    if (PlayerLocator.FindPlayerObject() != null)
                        break;
                    yield return null;
                }

                string loadMessage = "Main menu missing.";
                if (menu == null || !GameSaveSystem.TryLoadSettingsReloadSnapshot(out loadMessage))
                {
                    Debug.LogWarning($"SettingsSceneReloader: Could not restore session after settings Apply. {loadMessage}");
                    LoadingOverlayController.ReleaseOpaqueCover();
                    menu?.ShowMainMenu();
                    TryShowSettingsAppliedToast();
                    Destroy(gameObject);
                    yield break;
                }

                menu.ReturnToSettingsAfterApply();
                yield return null;
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
                menu?.InvokeOpenSettings();

                Destroy(gameObject);
            }
        }
    }
}
