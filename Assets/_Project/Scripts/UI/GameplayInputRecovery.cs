using Project.Core;
using Project.Interaction;
using Project.Player;
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
            if (!GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
                playerInput.enabled = true;

            PlayerController player = PlayerLocator.FindPlayerController();
            player?.EnsureGameplayInputReady();

            GameplayHudVisibility.RefreshGameplayHud();
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
            Object.FindAnyObjectByType<OpticsController>()?.CloseOpticsIfActive();
        }
    }
}
