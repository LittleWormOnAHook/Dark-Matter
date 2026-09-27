using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class InventoryFullscreenWindow : FullscreenUiWindow
    {
        private InventoryUI inventoryUi;

        public void Configure(InventoryUI inventory)
        {
            inventoryUi = inventory;
        }

        public override void OnShow()
        {
            if (DMUiToolkitMenus.HandlesWindow(JournalWindowId.Inventory))
            {
                if (inventoryUi == null)
                    inventoryUi = FindAnyObjectByType<InventoryUI>();
                inventoryUi?.RestoreInventoryPanel();
                GameplayHudVisibility.SetJournalTabHud(JournalWindowId.Inventory);
                return;
            }

            if (inventoryUi == null)
                inventoryUi = FindAnyObjectByType<InventoryUI>();

            inventoryUi?.EmbedInventoryPanel(contentArea);
            GameplayHudVisibility.SetJournalTabHud(JournalWindowId.Inventory);
        }

        public override void OnHide()
        {
            if (DMUiToolkitMenus.HandlesWindow(JournalWindowId.Inventory))
            {
                if (inventoryUi == null)
                    inventoryUi = FindAnyObjectByType<InventoryUI>();
                inventoryUi?.RestoreInventoryPanel();
                return;
            }

            inventoryUi?.RestoreInventoryPanel();
        }

        public override void Refresh()
        {
            inventoryUi?.RefreshUI();
        }
    }
}
