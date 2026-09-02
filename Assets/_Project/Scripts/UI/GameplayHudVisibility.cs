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

            // Survival vitals now follow the exact same visible flag as the hotbar/toolbar/pet bar
            // (only shown together, e.g. on the Journal's Inventory tab) instead of the old "always
            // visible unless blocked" rule, which is what let it linger on non-Inventory tabs.
            if (visible)
                inventory?.EnsureSurvivalStatsHudVisible();
            else
                inventory?.HideSurvivalStatsHud();

            FindXpHud()?.SetVisible(visible);
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

            // Hotbar + tool bar stay visible on the Inventory tab (so items can be dragged to
            // slots), but the Temperature/Hazard panels are journal-only clutter — keep them
            // hidden on every journal tab, Inventory included.
            SetExposureClusterVisible(false);

            // Quest tracker stays hidden while any journal tab is open (BlocksCombatInput).
            FindQuestHud()?.SetGameplayVisible(true);
        }

        public static void SetExposureClusterVisible(bool visible)
        {
            HotbarExposureGaugeCluster cluster = Object.FindAnyObjectByType<HotbarExposureGaugeCluster>();
            cluster?.SetGameplayVisible(visible);
        }

        public static void RefreshGameplayHud()
        {
            if (!GameSession.HasStarted || MainMenuController.BlocksGameplayHud)
            {
                SetInventoryModeHudVisible(false);
                Object.FindAnyObjectByType<ToolBarUI>()?.SetGameplayVisible(false);
                FindXpHud()?.SetVisible(false);
                FindQuestHud()?.SetGameplayVisible(false);
                return;
            }

            if (BuildingControlPanelUI.IsOpen)
            {
                SetInventoryModeHudVisible(false);
                FindXpHud()?.SetVisible(false);
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
            FindXpHud()?.SetVisible(true);
            FindQuestHud()?.SetGameplayVisible(true);
        }

        public static void SetModalOverlayOpen(bool open)
        {
            if (open)
                SetInventoryModeHudVisible(false);
            else
                RefreshGameplayHud();
        }

        private static HotbarXpHud FindXpHud()
        {
            // Include inactive: SetVisible(false) deactivates the GO; default Find skips it and
            // permanently stuck the XP bar hidden after map / optics / menu hide.
            return Object.FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
        }

        private static ActiveQuestHudUI FindQuestHud()
        {
            return Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include);
        }
    }
}
