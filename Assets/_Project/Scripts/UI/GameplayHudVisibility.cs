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

            Object.FindAnyObjectByType<HotbarXpHud>()?.SetVisible(visible);
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
                Object.FindAnyObjectByType<HotbarXpHud>()?.SetVisible(false);
                return;
            }

            if (BuildingControlPanelUI.IsOpen)
            {
                SetInventoryModeHudVisible(false);
                Object.FindAnyObjectByType<HotbarXpHud>()?.SetVisible(false);
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
            Object.FindAnyObjectByType<HotbarXpHud>()?.SetVisible(true);
        }

        public static void SetModalOverlayOpen(bool open)
        {
            if (open)
                SetInventoryModeHudVisible(false);
            else
                RefreshGameplayHud();
        }
    }
}
