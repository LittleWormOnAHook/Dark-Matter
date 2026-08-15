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
    // Data-refresh logic for the Overview/Pioneers/Production/Changes tabs (everything except
    // Health and Craft, which get their own partials). Split out of BuildingControlPanelUI.cs so
    // the "read BuildingOperationState and paint the labels" logic lives apart from panel
    // open/close orchestration and apart from the raw UI-construction code in the Layout partial.
    public partial class BuildingControlPanelUI
    {
        private void UpdateScienceLabTabVisibility()
        {
            bool showHealth = IsActiveScienceLab();
            if (tabButtonRoots.TryGetValue(BuildingControlTab.Health, out GameObject healthButton))
                healthButton.SetActive(showHealth);

            if (!showHealth && activeTab == BuildingControlTab.Health)
                ShowTab(BuildingControlTab.Overview);
        }

        private bool IsActiveScienceLab()
        {
            if (activePanel == null)
                return false;

            string id = activePanel.BuildingId ?? string.Empty;
            string name = activePanel.BuildingDisplayName ?? string.Empty;
            return id.IndexOf("science", System.StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("lab", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("science", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshOperationalTabs()
        {
            UpdateScienceLabTabVisibility();
            RefreshOverviewTab();
            RefreshPioneersTab();
            RefreshProductionTab();
            RefreshChangesTab();
            RefreshHealthTab();
        }

        private void RefreshOperationalTab(BuildingControlTab tab)
        {
            switch (tab)
            {
                case BuildingControlTab.Overview:
                    RefreshOverviewTab();
                    break;
                case BuildingControlTab.Pioneers:
                    RefreshPioneersTab();
                    break;
                case BuildingControlTab.Production:
                    RefreshProductionTab();
                    break;
                case BuildingControlTab.Changes:
                    RefreshChangesTab();
                    break;
                case BuildingControlTab.Health:
                    RefreshHealthTab();
                    break;
            }
        }

        private void RefreshOverviewTab()
        {
            if (overviewBuildingNameText == null || activePanel == null)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            int assignedCount = BuildingOperationRegistry.CountAssignedPioneers(state);
            bool crisisActive = EnvironmentalCrisisHudMode.IsCrisisActive;
            bool opsPaused = EnvironmentalCrisisHudMode.IsOperationsPaused;

            string buildingName = string.IsNullOrEmpty(activePanel.BuildingDisplayName)
                ? "Building"
                : activePanel.BuildingDisplayName;

            overviewBuildingNameText.text = $"Building: {buildingName}";
            overviewAssignedText.text =
                $"Assigned companions: {assignedCount}/{BuildingOperationRegistry.MaxAssignedPioneers}";
            overviewQueueText.text = $"Production queue: {state.ProductionQueue.Count} entr" +
                (state.ProductionQueue.Count == 1 ? "y" : "ies");
            overviewStormText.text = opsPaused
                ? "Sulfur storm: PAUSED"
                : crisisActive
                    ? "Sulfur storm: BUILDING"
                    : "Sulfur storm: Running";
            overviewStormText.color = opsPaused
                ? DarkMatterGenesisUiPalette.WarningText
                : crisisActive
                    ? DarkMatterGenesisUiPalette.Gold
                    : DarkMatterGenesisUiPalette.PositiveGreen;

            if (overviewMaintenanceText != null)
            {
                overviewMaintenanceText.text = state.Settings.AutoMaintenance
                    ? $"Maintenance: {state.Settings.MaintenancePercent:0}% (auto-scheduled)"
                    : $"Maintenance: {state.Settings.MaintenancePercent:0}% (manual)";
            }

            if (overviewOutputText != null)
            {
                float output = BuildingOperationRegistry.GetEffectiveOutputMultiplier(state);
                overviewOutputText.text = opsPaused
                    ? "Output rate: paused"
                    : $"Output rate: {output:0.00}x";
            }

            RefreshGeneratorStatus();
        }

        private void OnRefuelGeneratorClicked()
        {
            if (activePanel == null)
                return;

            PowerGenerator generator = activePanel.GetComponent<PowerGenerator>();
            if (generator == null)
                return;

            GameObject player = Project.Core.PlayerLocator.FindPlayerObject();
            Project.Inventory.InventorySystem inventory = player != null ? player.GetComponent<Project.Inventory.InventorySystem>() : null;

            generator.TryRefuelOneUnit(inventory, out string message);
            if (overviewPowerText != null && !string.IsNullOrEmpty(message))
                overviewPowerText.text = message;

            RefreshOverviewTab();
        }

        private void RefreshGeneratorStatus()
        {
            PowerGenerator generator = activePanel != null ? activePanel.GetComponent<PowerGenerator>() : null;

            if (refuelGeneratorButton != null)
                refuelGeneratorButton.gameObject.SetActive(generator != null);

            if (overviewPowerText == null)
                return;

            if (generator == null)
            {
                overviewPowerText.text = string.Empty;
                return;
            }

            string powerState = generator.HasPower ? "Powered" : "OFFLINE — no fuel";
            overviewPowerText.text = $"Generator: {Mathf.RoundToInt(generator.FuelPercent01 * 100f)}% fuel ({powerState})";
            overviewPowerText.color = generator.HasPower
                ? DarkMatterGenesisUiPalette.PositiveGreen
                : DarkMatterGenesisUiPalette.WarningText;

            if (refuelGeneratorButtonLabel != null)
            {
                refuelGeneratorButtonLabel.text = generator.IsFull ? "Generator Full" : "Load Plasma Fuel";
            }

            if (refuelGeneratorButton != null)
                refuelGeneratorButton.interactable = !generator.IsFull;
        }

        private void RefreshPioneersTab()
        {
            if (activePanel == null)
                return;

            string buildingId = activePanel.BuildingId ?? string.Empty;
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            BuildingControlAssignmentHints.BuildingAssignmentRole assignmentRole =
                BuildingControlAssignmentHints.ResolveRole(buildingId);
            bool specializedBuilding = assignmentRole != BuildingControlAssignmentHints.BuildingAssignmentRole.None;

            if (pioneerAssignmentHintText != null)
                pioneerAssignmentHintText.text = BuildingControlAssignmentHints.BuildAssignmentHint(buildingId, roster);

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
            {
                if (pioneerSlotLabels[i] == null)
                    continue;

                string assignedName = i < state.AssignedPioneers.Count ? state.AssignedPioneers[i] : string.Empty;
                string assignedId = i < state.AssignedPioneerIds.Count ? state.AssignedPioneerIds[i] : string.Empty;
                SkilledPioneerRecord assignedRecord = roster != null && !string.IsNullOrEmpty(assignedId)
                    ? roster.FindSkilledById(assignedId)
                    : null;

                if (string.IsNullOrEmpty(assignedName))
                {
                    pioneerSlotLabels[i].text = $"Slot {i + 1}: Unassigned";
                }
                else
                {
                    string classTag = assignedRecord != null
                        ? $" ({SkilledPioneerClassUtility.ToHudLabel(assignedRecord.pioneerClass)})"
                        : string.Empty;
                    string fitTag = string.Empty;
                    if (assignedRecord != null && specializedBuilding)
                    {
                        fitTag = BuildingControlAssignmentHints.IsIdealAssignment(assignedRecord, buildingId)
                            ? "  ·  IDEAL FIT"
                            : "  ·  SUBOPTIMAL";
                    }

                    pioneerSlotLabels[i].text = $"Slot {i + 1}: {assignedName}{classTag}{fitTag}";
                }

                if (pioneerSlotButtons[i] != null
                    && pioneerSlotButtons[i].TryGetComponent(out Image rowBackground))
                {
                    if (string.IsNullOrEmpty(assignedName))
                    {
                        rowBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.95f);
                    }
                    else if (assignedRecord != null
                        && specializedBuilding
                        && BuildingControlAssignmentHints.IsIdealAssignment(assignedRecord, buildingId))
                    {
                        rowBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.22f);
                    }
                    else if (assignedRecord != null && specializedBuilding)
                    {
                        rowBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.16f);
                    }
                    else
                    {
                        rowBackground.color = ActiveTabColor;
                    }
                }
            }
        }

        private void OnPioneerSlotClicked(int slotIndex)
        {
            if (activePanel == null)
                return;

            BuildRosterAssignableLists(out string[] rosterIds, out string[] rosterNames);
            BuildingOperationRegistry.CycleAssignSlotById(activePanel.BuildingId, slotIndex, rosterIds, rosterNames);
            RefreshOverviewTab();
            RefreshPioneersTab();
        }

        private void RefreshProductionTab()
        {
            if (productionListParent == null || activePanel == null)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            bool opsPaused = EnvironmentalCrisisHudMode.IsOperationsPaused;

            if (productionPausedOverlay != null)
            {
                productionPausedOverlay.gameObject.SetActive(opsPaused);
                productionPausedOverlay.text = opsPaused
                    ? "SULFUR STORM — PRODUCTION QUEUES PAUSED"
                    : string.Empty;
            }

            for (int i = productionListParent.childCount - 1; i >= 0; i--)
                Destroy(productionListParent.GetChild(i).gameObject);

            if (state.ProductionQueue.Count == 0)
            {
                CreateProductionEmptyLabel();
                return;
            }

            ShiftUiTheme theme = ShiftUiTheme.Current;
            for (int i = 0; i < state.ProductionQueue.Count; i++)
            {
                ProductionQueueEntry entry = state.ProductionQueue[i];
                bool entryPaused = opsPaused || entry.Paused;
                CreateProductionQueueRow(theme, entry, entryPaused);
            }
        }

        private void CreateProductionEmptyLabel()
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI emptyLabel = CreateBodyText(productionListParent, theme, 18f);
            emptyLabel.text = "No queued recipes. Add jobs from the Craft tab or building automation.";
        }

        private void CreateProductionQueueRow(ShiftUiTheme theme, ProductionQueueEntry entry, bool paused)
        {
            GameObject row = new GameObject("QueueEntry", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(productionListParent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 72f;
            rowLayout.preferredHeight = 72f;

            Image rowBackground = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(rowBackground);
            rowBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.95f);

            GameObject labelObject = new GameObject("RecipeLabel", typeof(RectTransform));
            labelObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI recipeLabel = labelObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(recipeLabel, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(recipeLabel);
            recipeLabel.fontSize = 18f;
            recipeLabel.alignment = TextAlignmentOptions.TopLeft;
            recipeLabel.color = DarkMatterGenesisUiPalette.BodyText;
            recipeLabel.raycastTarget = false;
            RectTransform recipeRect = recipeLabel.rectTransform;
            recipeRect.anchorMin = new Vector2(0f, 0.55f);
            recipeRect.anchorMax = new Vector2(1f, 1f);
            recipeRect.offsetMin = new Vector2(12f, 0f);
            recipeRect.offsetMax = new Vector2(-12f, -8f);

            string recipeName = string.IsNullOrEmpty(entry.RecipeName) ? "Unknown recipe" : entry.RecipeName;
            string statusSuffix = paused ? " — PAUSED" : string.Empty;
            recipeLabel.text = $"{recipeName}{statusSuffix}";

            GameObject barBackgroundObject = new GameObject("ProgressBackground", typeof(RectTransform), typeof(Image));
            barBackgroundObject.transform.SetParent(row.transform, false);
            Image barBackground = barBackgroundObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(barBackground);
            barBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.98f);
            RectTransform barBackgroundRect = barBackgroundObject.GetComponent<RectTransform>();
            barBackgroundRect.anchorMin = new Vector2(0f, 0.2f);
            barBackgroundRect.anchorMax = new Vector2(1f, 0.45f);
            barBackgroundRect.offsetMin = new Vector2(12f, 0f);
            barBackgroundRect.offsetMax = new Vector2(-12f, 0f);

            GameObject barFillObject = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            barFillObject.transform.SetParent(barBackgroundObject.transform, false);
            Image barFill = barFillObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(barFill);
            barFill.color = paused
                ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.85f)
                : DarkMatterGenesisUiPalette.RichFuchsia;
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = Mathf.Clamp01(entry.Progress);
            MenuUiBuilder.StretchRectToFill(barFillObject.GetComponent<RectTransform>());

            GameObject percentObject = new GameObject("ProgressLabel", typeof(RectTransform));
            percentObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI percentLabel = percentObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(percentLabel);
            else
                TmpUiHelper.ApplyDefaultFont(percentLabel);
            percentLabel.fontSize = 14f;
            percentLabel.alignment = TextAlignmentOptions.BottomRight;
            percentLabel.color = theme != null ? theme.secondaryTextColor : DarkMatterGenesisUiPalette.BodyText;
            percentLabel.raycastTarget = false;
            RectTransform percentRect = percentLabel.rectTransform;
            percentRect.anchorMin = new Vector2(0f, 0f);
            percentRect.anchorMax = new Vector2(1f, 0.22f);
            percentRect.offsetMin = new Vector2(12f, 6f);
            percentRect.offsetMax = new Vector2(-12f, 0f);
            percentLabel.text = $"{Mathf.RoundToInt(entry.Progress * 100f)}%";
        }

        private void RefreshChangesTab()
        {
            if (changesToggleHost == null || activePanel == null)
                return;

            for (int i = changesToggleHost.childCount - 1; i >= 0; i--)
                Destroy(changesToggleHost.GetChild(i).gameObject);

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            BuildingSettings settings = state.Settings;
            ShiftUiTheme theme = ShiftUiTheme.Current;
            string buildingId = activePanel.BuildingId ?? string.Empty;

            CreateSettingToggle(
                changesToggleHost,
                theme,
                "Auto-schedule maintenance",
                () => settings.AutoMaintenance,
                value =>
                {
                    settings.AutoMaintenance = value;
                    RefreshOverviewTab();
                });

            if (buildingId.Contains("command"))
            {
                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Accept injured companion overflow",
                    () => settings.AcceptInjuredOverflow,
                    value => settings.AcceptInjuredOverflow = value);

                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Prioritize skilled companions for shelter",
                    () => settings.PrioritizeSkilledTriage,
                    value => settings.PrioritizeSkilledTriage = value);
            }
            else if (buildingId.Contains("science") || buildingId.Contains("lab"))
            {
                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Batch supply-line production",
                    () => settings.BatchProductionMode,
                    value =>
                    {
                        settings.BatchProductionMode = value;
                        RefreshOverviewTab();
                    });
            }
            else if (buildingId.Contains("harvester") || buildingId.Contains("geothermal"))
            {
                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Deep drill mode (higher yield, higher risk)",
                    () => settings.DeepDrillMode,
                    value => settings.DeepDrillMode = value);
            }
            else
            {
                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Enable batch production mode",
                    () => settings.BatchProductionMode,
                    value =>
                    {
                        settings.BatchProductionMode = value;
                        RefreshOverviewTab();
                    });
            }
        }

        private void TickLiveProduction()
        {
            if (activePanel == null || activeTab != BuildingControlTab.Production)
                return;

            bool opsPaused = EnvironmentalCrisisHudMode.IsOperationsPaused;
            if (opsPaused)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            float rate = BuildingOperationRegistry.GetEffectiveOutputMultiplier(state) * 0.012f;
            BuildingOperationRegistry.TickProductionProgress(state, rate, paused: false);
            RefreshProductionTab();
        }

        private static void BuildRosterAssignableLists(out string[] rosterIds, out string[] rosterNames)
        {
            List<string> ids = new List<string>();
            List<string> names = new List<string>();
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
            {
                rosterIds = System.Array.Empty<string>();
                rosterNames = System.Array.Empty<string>();
                return;
            }

            HashSet<string> trioIds = new HashSet<string>(roster.ExpeditionTrioIds);
            IReadOnlyList<SkilledPioneerRecord> skilled = roster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                SkilledPioneerRecord record = skilled[i];
                if (record == null || trioIds.Contains(record.id))
                    continue;

                if (!string.IsNullOrWhiteSpace(record.displayName))
                {
                    ids.Add(record.id);
                    names.Add(record.displayName);
                }
            }

            ColonistAggregateState colonists = roster.GetColonistState();
            int availableWorkers = colonists.AvailableWorkers;
            for (int workerIndex = 1; workerIndex <= availableWorkers; workerIndex++)
            {
                ids.Add($"colonist:{workerIndex}");
                names.Add($"Colonist {workerIndex}");
            }

            rosterIds = ids.ToArray();
            rosterNames = names.ToArray();
        }

        private static List<string> BuildRosterDisplayNames()
        {
            BuildRosterAssignableLists(out _, out string[] rosterNames);
            return new List<string>(rosterNames);
        }
    }
}
