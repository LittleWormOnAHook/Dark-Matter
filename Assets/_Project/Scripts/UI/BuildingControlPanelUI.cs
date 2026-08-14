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
    // Core: singleton lifecycle (EnsureExists/Show/Close), the Update() poll loop, and tab
    // switching. Split across partials by responsibility — see BuildingControlPanelUI.Layout.cs
    // (UI construction), .Overview.cs (Overview/Pioneers/Production/Changes tab data),
    // .Health.cs (Health tab + roster subscription), .Crafting.cs (Craft tab embed/restore).
    // Purely a mechanical reorganization (partial class split) — no behavior changed by the split.
    public partial class BuildingControlPanelUI : MonoBehaviour
    {
        private enum BuildingControlTab
        {
            Overview = 0,
            Pioneers = 1,
            Production = 2,
            Craft = 3,
            Changes = 4,
            Health = 5
        }

        private static readonly string[] TabLabels =
        {
            "Overview",
            "Companions",
            "Production",
            "Craft",
            "Changes",
            "Health"
        };

        private static BuildingControlPanelUI instance;

        private GameObject overlayRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI buildingSubtitleText;
        private RectTransform tabBodyArea;
        private readonly Dictionary<BuildingControlTab, GameObject> tabPanels = new Dictionary<BuildingControlTab, GameObject>();
        private readonly Dictionary<BuildingControlTab, GameObject> tabButtonRoots = new Dictionary<BuildingControlTab, GameObject>();
        private readonly Dictionary<BuildingControlTab, Image> tabButtonBackgrounds = new Dictionary<BuildingControlTab, Image>();
        private readonly Dictionary<BuildingControlTab, TextMeshProUGUI> tabButtonLabels = new Dictionary<BuildingControlTab, TextMeshProUGUI>();

        private RectTransform craftHost;
        private TextMeshProUGUI craftStubText;
        private CraftingUI craftingUi;
        private BuildingControlTab activeTab = BuildingControlTab.Overview;
        private BuildingControlPanel activePanel;
        private Action onClosed;
        private bool built;
        private bool craftEmbedded;

        private TextMeshProUGUI overviewBuildingNameText;
        private TextMeshProUGUI overviewAssignedText;
        private TextMeshProUGUI overviewQueueText;
        private TextMeshProUGUI overviewStormText;
        private TextMeshProUGUI overviewMaintenanceText;
        private TextMeshProUGUI overviewOutputText;
        private TextMeshProUGUI overviewPowerText;
        private Button refuelGeneratorButton;
        private TextMeshProUGUI refuelGeneratorButtonLabel;

        private readonly Button[] pioneerSlotButtons = new Button[BuildingOperationRegistry.MaxAssignedPioneers];
        private readonly TextMeshProUGUI[] pioneerSlotLabels = new TextMeshProUGUI[BuildingOperationRegistry.MaxAssignedPioneers];
        private TextMeshProUGUI pioneerAssignmentHintText;

        private Transform productionListParent;
        private TextMeshProUGUI productionPausedOverlay;
        private Transform changesToggleHost;

        private Transform healthListParent;
        private TextMeshProUGUI healthStatusLabel;
        private PioneerRosterManager healthRoster;
        private bool healthRosterSubscribed;

        private float nextProductionTick;
        private bool lastCrisisState;

        private static readonly Color ActiveTabColor = DarkMatterGenesisUiPalette.ActiveTabBackground;
        private static readonly Color InactiveTabColor = DarkMatterGenesisUiPalette.InactiveTabBackground;
        private static readonly Color ActiveLabelColor = DarkMatterGenesisUiPalette.Gold;
        private static readonly Color InactiveLabelColor = DarkMatterGenesisUiPalette.BodyText;

        public static BuildingControlPanelUI Instance => instance;
        public static bool IsOpen => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;

        public static void CloseAnyOpenBuildingControl()
        {
            if (instance != null && IsOpen)
                instance.Close();
        }

        public static BuildingControlPanelUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("BuildingControlPanelUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<BuildingControlPanelUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        public static void Show(BuildingControlPanel panel, Action closedCallback = null)
        {
            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null || panel == null)
                return;

            BuildingControlPanelUI ui = EnsureExists(canvas.transform);
            ui.Present(panel, closedCallback);
        }

        private static Canvas ResolveGameplayCanvas()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas uiCanvas = uiManager.GetComponent<Canvas>();
                if (uiCanvas != null)
                    return uiCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindAnyObjectByType<Canvas>();
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (UiEscapeGate.TryConsumeEscape())
            {
                Close();
                return;
            }

            bool crisisActive = EnvironmentalCrisisHudMode.IsCrisisActive;
            if (crisisActive != lastCrisisState)
            {
                lastCrisisState = crisisActive;
                RefreshOperationalTabs();
            }

            if (Time.unscaledTime >= nextProductionTick)
            {
                nextProductionTick = Time.unscaledTime + 0.45f;
                TickLiveProduction();
                TickLiveHealthTab();
            }
        }

        private void Present(BuildingControlPanel panel, Action closedCallback)
        {
            activePanel = panel;
            onClosed = closedCallback;
            titleText.text = string.IsNullOrEmpty(panel.BuildingDisplayName) ? "Building Control" : panel.BuildingDisplayName;
            if (buildingSubtitleText != null)
            {
                buildingSubtitleText.text =
                    $"IO // BASE OPS  ·  {FormatBuildingIdLabel(panel.BuildingId)}";
            }

            lastCrisisState = EnvironmentalCrisisHudMode.IsCrisisActive;
            BuildingOperationRegistry.AddDemoQueueEntry(panel.BuildingId);
            UpdateScienceLabTabVisibility();
            EnsureHealthRosterSubscription();
            RefreshOperationalTabs();
            ShowTab(BuildingControlTab.Overview);
            OpenOverlay();
        }

        private void ShowTab(BuildingControlTab tab)
        {
            if (activeTab == BuildingControlTab.Craft && tab != BuildingControlTab.Craft)
                UnembedCraft();

            activeTab = tab;

            foreach (KeyValuePair<BuildingControlTab, GameObject> pair in tabPanels)
                pair.Value.SetActive(pair.Key == tab);

            foreach (KeyValuePair<BuildingControlTab, Image> pair in tabButtonBackgrounds)
            {
                bool active = pair.Key == tab;
                pair.Value.color = active ? ActiveTabColor : InactiveTabColor;
            }

            foreach (KeyValuePair<BuildingControlTab, TextMeshProUGUI> pair in tabButtonLabels)
                pair.Value.color = pair.Key == tab ? ActiveLabelColor : InactiveLabelColor;

            if (tab == BuildingControlTab.Craft)
                RefreshCraftTab();
            else
                RefreshOperationalTab(tab);
        }

        private static string FormatBuildingIdLabel(string buildingId)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
                return "UNKNOWN";

            string normalized = buildingId.Replace('_', ' ').Trim();
            return normalized.ToUpperInvariant();
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (transform.parent != null)
                UiFrontLayer.BringLayerToFront(transform.parent);

            GameplayHudVisibility.SetModalOverlayOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonBuildingControl, true);
        }

        private void Close()
        {
            ScienceLabHealthContextMenu.HideAny();
            UnembedCraft();
            overlayRoot.SetActive(false);
            activePanel = null;

            GameplayHudVisibility.SetModalOverlayOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonBuildingControl, false);

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(false);

            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }
    }
}
