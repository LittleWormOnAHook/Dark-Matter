using Project.Core;
using Project.Vehicles;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Bottom HUD chrome (hotbar + tool bar) is visible during normal gameplay and the journal Inventory tab.
    /// Hidden on other journal tabs and modal overlays (building control, etc.).
    /// </summary>
    public static class GameplayHudVisibility
    {
        /// <summary>
        /// Tilde (~) hides only gameplay chrome. Journal, popups, input, and systems keep running.
        /// </summary>
        public static bool CinematicChromeHidden { get; private set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCinematicStatic()
        {
            CinematicChromeHidden = false;
        }

        public static void ToggleCinematicChrome()
        {
            CinematicChromeHidden = !CinematicChromeHidden;
            if (DMUiToolkitHud.InstanceOrNull != null)
                DMUiToolkitHud.RefreshMenuChrome();
        }

        public static void ClearCinematicChrome()
        {
            if (!CinematicChromeHidden)
                return;
            CinematicChromeHidden = false;
            if (DMUiToolkitHud.InstanceOrNull != null)
                DMUiToolkitHud.RefreshMenuChrome();
        }

        public static void SetInventoryModeHudVisible(bool visible)
        {
            if (!GameSession.HasStarted || MainMenuController.BlocksGameplayHud)
                visible = false;

            InventoryUI inventory = Object.FindAnyObjectByType<InventoryUI>();
            inventory?.SetBottomHudVisible(visible);

            FindQuestHud()?.SetGameplayVisible(visible);
        }

        public static void SetGameplayHudVisible(bool visible)
        {
            // Any "show the gameplay HUD" request that lands while the player is driving a vehicle
            // (e.g. RefreshGameplayHud()'s fallthrough after closing an unrelated menu) must not
            // undo the hotbar/tool-bar/pet/companion suppression — re-apply it instead of the normal
            // bottom-HUD show, and leave the Temperature/Hazards cluster + vehicle HUD alone.
            if (visible && PlayerVehicleState.IsMounted)
            {
                Object.FindAnyObjectByType<ToolBarUI>()?.SetVehicleModeHudSuppressed(true);
                FindQuestHud()?.SetGameplayVisible(true);
                return;
            }

            SetInventoryModeHudVisible(visible);
        }

        public static void SetJournalTabHud(JournalWindowId? windowId)
        {
            bool showBottomHud = windowId == JournalWindowId.Inventory;
            SetInventoryModeHudVisible(showBottomHud);

            // Quest tracker stays hidden while any journal tab is open (BlocksCombatInput).
            FindQuestHud()?.SetGameplayVisible(true);
        }

        public static void RefreshGameplayHud()
        {
            if (!GameSession.HasStarted || MainMenuController.BlocksGameplayHud)
            {
                SetInventoryModeHudVisible(false);
                Object.FindAnyObjectByType<ToolBarUI>()?.SetGameplayVisible(false);
                FindQuestHud()?.SetGameplayVisible(false);
                return;
            }

            // Hot Cross is a separate overlay host — mount on every HUD refresh / session path
            // so it does not wait for an inventory pickup to appear.
            DMUiToolkitHotCross.EnsureHost();

            if (BuildingControlPanelUI.IsOpen)
            {
                SetInventoryModeHudVisible(false);
                FindQuestHud()?.SetGameplayVisible(true);
                return;
            }

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>();
            if (journal != null && journal.IsOpen)
            {
                FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
                if (navigator != null && navigator.IsAnyOpen)
                {
                    SetJournalTabHud(navigator.CurrentWindow);
                    return;
                }
            }

            SetGameplayHudVisible(true);
            FindQuestHud()?.SetGameplayVisible(true);
        }

        public static void SetModalOverlayOpen(bool open)
        {
            if (open)
                SetInventoryModeHudVisible(false);
            else
                RefreshGameplayHud();
        }

        private static ActiveQuestHudUI FindQuestHud()
        {
            return Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include);
        }
    }
}
