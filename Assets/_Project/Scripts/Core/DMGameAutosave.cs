using System.Collections;
using Project.UI;
using UnityEngine;

namespace Project.Core
{
    [DisallowMultipleComponent]
    public class DMGameAutosave : MonoBehaviour
    {
        private static DMGameAutosave instance;
        private float elapsed;
        private bool autosavePending;
        // Last autosave capture taken with no menu/pause on screen; reused if an autosave fires while a menu is open.
        private Texture2D lastCleanCapture;

        public static void EnsureExists()
        {
            if (!Application.isPlaying)
                return;

            if (instance != null)
                return;

            DMGameAutosave found = FindAnyObjectByType<DMGameAutosave>();
            if (found != null)
            {
                instance = found;
                return;
            }

            GameObject host = new GameObject("DMGameAutosave");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DMGameAutosave>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= HandleGameStarted;
        }

        private void OnDestroy()
        {
            ClearLastCleanCapture();
            if (instance == this)
                instance = null;
        }

        private void HandleGameStarted()
        {
            elapsed = 0f;
            ClearLastCleanCapture();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || LoadingOverlayController.IsBlockingMenu)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < GameSaveSystem.AutosaveIntervalSeconds)
                return;

            elapsed = 0f;
            if (!autosavePending)
                StartCoroutine(AutosaveAfterFrame());
        }

        private IEnumerator AutosaveAfterFrame()
        {
            autosavePending = true;
            // Capture after rendering, same as the manual Save screen's preview grab.
            yield return new WaitForEndOfFrame();
            autosavePending = false;

            if (!GameSession.HasStarted)
                yield break;

            Texture2D screenshot = ResolveAutosaveScreenshot();
            if (GameSaveSystem.TryAutosave(screenshot, out string message))
                Debug.Log("DMGameAutosave: " + message);
        }

        private Texture2D ResolveAutosaveScreenshot()
        {
            if (IsGameplayViewClean())
            {
                Texture2D capture = SaveSlotScreenshotUtility.CaptureGameplayScreenshot();
                if (capture != null)
                {
                    ClearLastCleanCapture();
                    lastCleanCapture = capture;
                }
            }

            // Menu open: reuse the last clean gameplay frame (may be null -> slot keeps its previous preview).
            return lastCleanCapture;
        }

        private static bool IsGameplayViewClean()
        {
            if (LoadingOverlayController.IsBlockingMenu || Time.timeScale <= 0.01f)
                return false;

            if (MainMenuController.PauseOverlayBlocksGameplay()
                || DMUiToolkitMainMenu.IsVisible
                || DMUiToolkitMenuPanels.IsAnySubPanelOpen)
                return false;

            return !DMUiToolkitMenus.IsOpen
                && !DMUiToolkitWorldMenus.IsAnyModalOpen
                && !DMUiToolkitDeath.IsOpen
                && !DMUiToolkitVendor.IsOpen
                && !DMUiToolkitCraft.IsOpen
                && !DMUiToolkitCrate.IsOpen
                && !DMUiToolkitDialogue.IsOpen;
        }

        private void ClearLastCleanCapture()
        {
            if (lastCleanCapture != null)
                Destroy(lastCleanCapture);
            lastCleanCapture = null;
        }
    }
}
