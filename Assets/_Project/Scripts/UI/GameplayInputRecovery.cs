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

        private static bool HasVisibleUiBlockingInput()
        {
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
                   QuoraShelterMenuUI.IsOpen;
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