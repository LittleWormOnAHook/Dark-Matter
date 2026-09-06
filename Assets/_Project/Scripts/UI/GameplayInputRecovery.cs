using System.Collections;
using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Project.UI
{
    /// <summary>
    /// Clears stale UI input capture and re-enables gameplay controls after menus or dialogs close improperly.
    /// </summary>
    public static class GameplayInputRecovery
    {
        public static void ReleaseAllInputCapture()
        {
            CloseAllGameplayUi();
            FinalizeGameplayInput();
        }


        /// <summary>
        /// Synchronous ghost-pause unlock for CanProcess / InputHost.
        /// If pauseOverlayActive (or MainMenu visible flag) is stuck with no painted chrome, clear it.
        /// stamp: journal-hotkeys-ghost-pause 0905
        /// </summary>
        public static void ClearGhostPauseOverlay()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            if (DMUiToolkitDeath.IsOpen)
                return;

            DMUiToolkitMainMenu.SyncVisibilityToPainted();

            bool pauseUi = DMUiToolkitMainMenu.IsVisible || DMUiToolkitMenuPanels.IsAnySubPanelOpen;
            if (MainMenuController.BlocksGameplayHud && !pauseUi)
            {
                MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                menu?.ClearGhostPauseOverlay();
                pauseUi = DMUiToolkitMainMenu.IsVisible || DMUiToolkitMenuPanels.IsAnySubPanelOpen;
            }

            if (pauseUi)
                return;

            // Do not clear player pause while journal/nav chrome is open.
            FullscreenUiNavigator nav = FullscreenUiNavigator.Instance;
            if (nav != null && nav.IsAnyOpen)
                return;
            if (DMUiToolkitMenus.IsOpen || DMUiToolkitMenus.HasPendingShow)
                return;

            // Invector can still move/jump while _gameplayPaused blocks CanProcess journal hotkeys.
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null && player.IsGameplayPaused)
            {
                player.SetGameplayPaused(false);
                if (Time.timeScale <= 0.01f)
                    Time.timeScale = 1f;
            }
        }


        /// <summary>
        /// Clears stuck player/menu flags after climb/ESC when no real UI is up.
        /// Never ForceHide / CloseAnyOpenJournal / RepairOpen here - those raced ForceShow
        /// pending binds and made journal hotkeys (J/I/T/C/U) appear dead while Esc still worked.
        /// Toolkit LateUpdate SyncFromNavigator owns show/hide chrome; this only unlocks input.
        /// </summary>
        public static void RecoverGhostUiLocks()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            // Game Over owns the pointer; do not clear inventory-open under it.
            if (DMUiToolkitDeath.IsOpen)
                return;

            // stamp: journal-hotkeys-ghost-pause 0905 - always sync + clear stuck pauseOverlayActive when toolkit pause not painted.
            ClearGhostPauseOverlay();

            bool pauseUi = DMUiToolkitMainMenu.IsVisible || DMUiToolkitMenuPanels.IsAnySubPanelOpen;
            if (pauseUi)
                return;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            bool navOpen = navigator != null && navigator.IsAnyOpen;
            bool journalUi = DMUiToolkitMenus.IsOpen || DMUiToolkitMenus.HasPendingShow;

            // Climb/ESC can leave the navigator stack open with no painted Toolkit journal.
            // Treat that as a ghost, not real UI â€” re-paint so J/I/T/C open a visible tab.
            if (navOpen && !journalUi)
            {
                DMUiToolkitMenus.RepairOpenNavigatorChrome();
                return;
            }

            // Real journal / pause UI still up â€” never touch chrome or navigator.
            if (journalUi || HasVisibleUiBlockingInput())
                return;

            PlayerController player = PlayerLocator.FindPlayerController();
            if (player == null)
                return;

            bool playerJournal = player.IsJournalOpen;
            bool playerPaused = player.IsGameplayPaused;
            bool stuckFlags = playerJournal
                || playerPaused
                || player.IsInventoryOpen
                || player.IsMapOpen
                || player.IsQuestDialogOpen
                || player.IsLootDialogOpen
                || player.IsBuildingControlOpen;

            bool frozenWorld = Time.timeScale <= 0.01f && !MainMenuController.BlocksGameplayHud;

            if (!stuckFlags && !frozenWorld && !GameplayMenuTime.IsInventoryPaused)
                return;

            // Flag-only clear â€” do not CloseAnyOpenJournal (navigator already empty).
            if (playerJournal)
                player.SetJournalOpen(false);
            if (player.IsInventoryOpen)
                player.SetInventoryOpen(false);
            if (player.IsMapOpen)
                player.SetMapOpen(false);
            if (playerPaused && !MainMenuController.BlocksGameplayHud)
                player.SetGameplayPaused(false);

            FinalizeGameplayInput();
            GameplayHudVisibility.ClearCinematicChrome();
            DMUiToolkitHotCross.EnsureHost();
            GameplayHudVisibility.RefreshGameplayHud();
        }

        public static void FinalizeGameplayInput()
        {
            if (!GameSession.HasStarted)
                return;

            // Inventory (and other menu reasons) may leave timeScale at 0/0.2. Clear our holds,
            // but never stomp a hard main-menu / boot pause.
            GameplayMenuTime.ClearAll();

            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null && player.IsGameplayPaused)
                return;

            if (Time.timeScale <= 0f)
                Time.timeScale = 1f;

            PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
                playerInput.ActivateInput();
            }

            player?.EnsureGameplayInputReady();

            // Fallback for startup order: if the player was not found this frame, still put
            // the cursor into gameplay mode once no blocking UI is open.
            if (player == null && !HasVisibleUiBlockingInput())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            RefreshInvectorInputLocks();

            GameplayHudVisibility.RefreshGameplayHud();
            QueueCursorRestore();
        }

        /// <summary>
        /// Relock the cursor after a menu closes. Runs this frame and the next so a UI click
        /// in the Game view cannot leave Cursor.lockState at None.
        /// </summary>
        public static void QueueCursorRestore()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;
            if (CursorRestoreRunner.IsTearingDown)
                return;

            CursorRestoreRunner.Kick();
        }

        public static void CancelPendingCursorRestore()
        {
            CursorRestoreRunner.Cancel();
        }

        private static bool HasVisibleUiBlockingInput()
        {
            if (DMUiToolkitDeath.IsOpen)
                return true;

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null && journal.IsOpen)
                return true;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;

            return EnemyLootDialogUI.IsDialogOpen ||
                   QuestGiverDialogUI.IsDialogOpen ||
                   PptDirectionsMenuUI.IsOpen ||
                   BuildingControlPanelUI.IsOpen ||
                   WeaponModeSwitchMenuUI.IsOpen ||
                   CraftingUI.IsAnyStandaloneOpen ||
                   WalkerDrillInteractMenuUI.IsOpen ||
                   HovercraftInteractMenuUI.IsOpen ||
                   QuoraShelterMenuUI.IsOpen ||
                   DMUiToolkitDevPanel.IsOpen;
        }

        private static void RefreshInvectorInputLocks()
        {
            GameObject playerObject = PlayerLocator.FindPlayerObject();
            if (playerObject == null)
                return;

            PioneerInvectorInputBridge bridge = playerObject.GetComponent<PioneerInvectorInputBridge>();
            PioneerShooterMeleeInput shooterInput = playerObject.GetComponent<PioneerShooterMeleeInput>();
            if (bridge != null && shooterInput != null)
                bridge.ApplyInputLocks(shooterInput);
        }

        private static void CloseAllGameplayUi()
        {
            EnemyLootDialogUI.CloseAnyOpenLoot();
            JournalPanelUI.CloseAnyOpenJournal();
            MapUI.CloseAnyOpenMap();
            InventoryUI.CloseAnyOpenInventory();
            PetUI.CloseAnyOpenPet();
            QuestGiverDialogUI.CloseAnyOpenQuestDialog();
            PptDirectionsMenuUI.CloseAnyOpen();
            BuildingControlPanelUI.CloseAnyOpenBuildingControl();
            WeaponModeSwitchMenuUI.HideAny();
            CraftingUI.CloseAnyOpenStandalone();
            WalkerDrillInteractMenuUI.CloseAny();
            HovercraftInteractMenuUI.CloseAny();
            Object.FindAnyObjectByType<OpticsController>()?.CloseOpticsIfActive();
        }

        private sealed class CursorRestoreRunner : MonoBehaviour
        {
            private static CursorRestoreRunner instance;
            private static bool tearingDown;
            private int token;

            public static bool IsTearingDown => tearingDown || !Application.isPlaying;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void ResetStatics()
            {
                instance = null;
                tearingDown = false;
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
            private static void HookSceneTeardown()
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
            }

            private static void OnSceneUnloaded(Scene scene)
            {
                // Additive Gaia tile unloads must not disable cursor restore.
                if (Application.isPlaying)
                    return;

                tearingDown = true;
                instance = null;
            }

            public static void Kick()
            {
                if (IsTearingDown)
                    return;

                if (instance == null)
                {
                    GameObject go = new GameObject("GameplayCursorRestore");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    instance = go.AddComponent<CursorRestoreRunner>();
                }

                instance.token++;
                instance.StartCoroutine(instance.RestoreAfterUiClick(instance.token));
            }

            public static void Cancel()
            {
                if (instance == null)
                    return;

                instance.token++;
                Object.Destroy(instance.gameObject);
                instance = null;
            }

            private void OnDestroy()
            {
                if (instance == this)
                    instance = null;
            }

            private IEnumerator RestoreAfterUiClick(int generation)
            {
                ApplyIfIdle();
                yield return null;
                if (this == null || generation != token)
                    yield break;
                ApplyIfIdle();
                if (generation == token && instance == this)
                    Destroy(gameObject);
            }

            private static void ApplyIfIdle()
            {
                if (HasVisibleUiBlockingInput())
                    return;

                PlayerController player = PlayerLocator.FindPlayerController();
                if (player != null)
                {
                    if (player.IsGameplayPaused)
                        return;
                    player.ApplyCursorState();
                    return;
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}

