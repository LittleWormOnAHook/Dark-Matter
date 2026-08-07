using System;
using System.Collections.Generic;
using Project.Building;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    // Craft tab: embedding/restoring the shared CraftingUI panel inside this building's Craft tab.
    // Split out of BuildingControlPanelUI.cs since it's a self-contained embed/unembed lifecycle
    // distinct from the data-refresh tabs (Overview/Pioneers/Production/Changes/Health).
    public partial class BuildingControlPanelUI
    {
        private void RefreshCraftTab()
        {
            if (activePanel != null && activePanel.HasCraftStation)
            {
                craftStubText.gameObject.SetActive(false);
                EmbedCraft(activePanel.CraftStationType);
                return;
            }

            UnembedCraft();
            craftStubText.gameObject.SetActive(true);
            string stationLabel = activePanel != null && activePanel.HasCraftStation
                ? activePanel.CraftStationType.ToString()
                : "none";
            craftStubText.text =
                "This building does not expose a craft station.\n\n" +
                $"Configured station: {stationLabel}\n" +
                "Bind a CraftingStationType on the BuildingControlPanel to embed production crafting here.";
        }

        private void EmbedCraft(CraftingStationType stationType)
        {
            if (craftEmbedded)
                return;

            if (craftingUi == null)
                craftingUi = FindAnyObjectByType<CraftingUI>();

            CraftingManager craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (craftingManager != null)
                craftingManager.CurrentStation = stationType;

            craftingUi?.EmbedPanel(craftHost, CraftingUiPresentationMode.Production);
            MenuUiBuilder.StretchRectToFill(craftHost);
            craftEmbedded = true;
        }

        private void UnembedCraft()
        {
            if (!craftEmbedded)
                return;

            craftingUi?.RestorePanel();
            craftEmbedded = false;

            CraftingManager craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (craftingManager != null)
                craftingManager.CurrentStation = null;
        }
    }
}
