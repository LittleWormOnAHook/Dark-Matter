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

                // 2026-11-04f: this reload is single-scene, so it throws away the other world.
                // GetActiveScene() may now be IO (the menu switch sets it active), and reloading
                // IO alone would destroy Mars - the scene that owns the player rig, the runtime UI
                // host and all progression state. Pin the reload to Mars and reset the stack so
                // DMSceneStack stops believing IO is still resident.
                Scene activeScene = SceneManager.GetActiveScene();
                int targetIndex = MarsSceneIndex();
                if (targetIndex >= 0)
                {
                    Debug.Log($"[SettingsSceneReloader] single-scene reload pinned to '{DMSceneStack.MarsSceneName}' "
                        + $"(build index {targetIndex}); active scene was '{activeScene.name}'.");
                    DMSceneStack.ResetForSingleSceneReload();
                    SceneManager.LoadScene(targetIndex);
                }
                else
                {
                    Debug.LogWarning($"[SettingsSceneReloader] '{DMSceneStack.MarsSceneName}' is not in the build scene list; "
                        + $"falling back to the active scene '{activeScene.name}'.");
                    DMSceneStack.ResetForSingleSceneReload();
                    SceneManager.LoadScene(activeScene.buildIndex);
                }
            });
        }

        /// <summary>2026-11-04f: build index of the Mars boot scene, or -1 when it is not in the build list.</summary>
        private static int MarsSceneIndex()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, DMSceneStack.MarsSceneName, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
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
                DMDevCommandState.ClearTransient();
                GameplayMenuTime.ClearAll();

                const int maxFrames = 120;
                for (int i = 0; i < maxFrames; i++)
                {
                    if (FindAnyObjectByType<MainMenuController>() != null
                        && PlayerLocator.FindPlayerObject() != null)
                        break;

                    yield return null;
                }

                yield return null;
                if (DMUiToolkitConfig.IsEnabled)
                    DMUiToolkitBootstrap.EnsureExists();
                yield return null;

                ApplySettingsAfterReload();

                if (!GameSaveSystem.TryRestoreExpeditionAfterSettingsReload(out string restoreMessage))
                    Debug.LogWarning($"SettingsSceneReloader: expedition not restored after reload. {restoreMessage}");

                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                if (menu == null)
                {
                    MainMenuController.EnsureExists();
                    menu = FindAnyObjectByType<MainMenuController>();
                }

                // Return to in-game pause + Settings (snapshot already applied). Do not ResetSession or
                // ShowMainMenu — that left default empty inventory, HasStarted false, and broken Esc/journal.
                LoadingOverlayController.ReleaseOpaqueCover();
                menu?.ReturnToSettingsAfterApply();
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
