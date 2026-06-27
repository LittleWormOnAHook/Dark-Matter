using Project.Core;
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
            if (!GameSession.HasStarted)
                visible = false;

            InventoryUI inventory = Object.FindAnyObjectByType<InventoryUI>();
            inventory?.SetBottomHudVisible(visible);
        }

        public static void SetGameplayHudVisible(bool visible)
        {
            SetInventoryModeHudVisible(visible);
        }

        public static void SetJournalTabHud(JournalWindowId? windowId)
        {
            bool showBottomHud = windowId == JournalWindowId.Inventory;
            SetInventoryModeHudVisible(showBottomHud);
        }

        public static void RefreshGameplayHud()
        {
            if (!GameSession.HasStarted)
            {
                SetInventoryModeHudVisible(false);
                return;
            }

            if (BuildingControlPanelUI.IsOpen)
            {
                SetInventoryModeHudVisible(false);
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
