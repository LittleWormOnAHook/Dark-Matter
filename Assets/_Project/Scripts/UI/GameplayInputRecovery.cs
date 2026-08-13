using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.InputSystem;

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

            // Inventory (and other menu reasons) may leave timeScale at 0/0.2 — clear our holds,
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
        }

        private static bool HasVisibleUiBlockingInput()
        {
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;

            return EnemyLootDialogUI.IsDialogOpen ||
                   QuestGiverDialogUI.IsDialogOpen ||
                   BuildingControlPanelUI.IsOpen ||
                   WeaponModeSwitchMenuUI.IsOpen ||
                   CraftingUI.IsAnyStandaloneOpen;
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
            BuildingControlPanelUI.CloseAnyOpenBuildingControl();
            WeaponModeSwitchMenuUI.HideAny();
            CraftingUI.CloseAnyOpenStandalone();
            Object.FindAnyObjectByType<OpticsController>()?.CloseOpticsIfActive();
        }
    }
}
