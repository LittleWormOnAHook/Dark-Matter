using System;
using System.Collections.Generic;
using Project.Building;
using Project.Crafting;
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
    public class BuildingControlPanelUI : MonoBehaviour
    {
        private enum BuildingControlTab
        {
            Overview = 0,
            Pioneers = 1,
            Production = 2,
            Craft = 3,
            Changes = 4
        }

        private static readonly string[] TabLabels =
        {
            "Overview",
            "Pioneers",
            "Production",
            "Craft",
            "Changes"
        };

        private static BuildingControlPanelUI instance;

        private GameObject overlayRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI buildingSubtitleText;
        private RectTransform tabBodyArea;
        private readonly Dictionary<BuildingControlTab, GameObject> tabPanels = new Dictionary<BuildingControlTab, GameObject>();
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

        private readonly Button[] pioneerSlotButtons = new Button[BuildingOperationRegistry.MaxAssignedPioneers];
        private readonly TextMeshProUGUI[] pioneerSlotLabels = new TextMeshProUGUI[BuildingOperationRegistry.MaxAssignedPioneers];

        private Transform productionListParent;
        private TextMeshProUGUI productionPausedOverlay;
        private Transform changesToggleHost;

        private float nextProductionTick;
        private bool lastCrisisState;

        private static readonly Color ActiveTabColor = new Color(0.14f, 0.22f, 0.32f, 0.98f);
        private static readonly Color InactiveTabColor = new Color(0.09f, 0.1f, 0.14f, 0.94f);
        private static readonly Color ActiveLabelColor = new Color(0.55f, 0.88f, 1f, 1f);
        private static readonly Color InactiveLabelColor = new Color(0.72f, 0.78f, 0.86f, 0.88f);

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
            if (!IsOpen || Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
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
            }
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "BuildingControlOverlay", new Color(0f, 0f, 0f, 0.5f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();

            GameObject shell = MenuUiBuilder.CreateFullscreenShell(
                overlayRoot.transform,
                "Building Control",
                out RectTransform contentArea,
                out Button closeButton);
            titleText = MenuUiBuilder.GetShellTitleText(shell);
            buildingSubtitleText = CreateHeaderSubtitle(shell.transform);
            closeButton.onClick.AddListener(Close);

            GameObject layoutRoot = new GameObject("Layout", typeof(RectTransform));
            layoutRoot.transform.SetParent(contentArea, false);
            RectTransform layoutRect = layoutRoot.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(layoutRect);

            VerticalLayoutGroup rootLayout = layoutRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.spacing = 0f;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform));
            tabRow.transform.SetParent(layoutRoot.transform, false);
            HorizontalLayoutGroup tabRowLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabRowLayout.padding = new RectOffset(16, 16, 8, 8);
            tabRowLayout.spacing = 6f;
            tabRowLayout.childAlignment = TextAnchor.MiddleLeft;
            tabRowLayout.childControlWidth = true;
            tabRowLayout.childControlHeight = true;
            tabRowLayout.childForceExpandWidth = false;
            tabRowLayout.childForceExpandHeight = false;

            LayoutElement tabRowLayoutElement = tabRow.AddComponent<LayoutElement>();
            tabRowLayoutElement.minHeight = 52f;
            tabRowLayoutElement.preferredHeight = 52f;

            for (int i = 0; i < TabLabels.Length; i++)
            {
                BuildingControlTab tab = (BuildingControlTab)i;
                CreateTabButton(tabRow.transform, tab, TabLabels[i], theme);
            }

            GameObject bodyHost = new GameObject("TabBody", typeof(RectTransform));
            bodyHost.transform.SetParent(layoutRoot.transform, false);
            tabBodyArea = bodyHost.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(tabBodyArea);
            LayoutElement bodyLayout = bodyHost.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.flexibleWidth = 1f;
            bodyLayout.minHeight = 320f;

            CreateTabPanels(tabBodyArea, theme);

            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void CreateTabPanels(Transform parent, ShiftUiTheme theme)
        {
            tabPanels[BuildingControlTab.Overview] = CreateOverviewTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Pioneers] = CreatePioneersTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Production] = CreateProductionTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Craft] = CreateCraftTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Changes] = CreateChangesTabPanel(parent, theme);

            ShowTab(BuildingControlTab.Overview);
        }

        private GameObject CreateCraftTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = new GameObject("CraftPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            craftHost = new GameObject("CraftHost", typeof(RectTransform)).GetComponent<RectTransform>();
            craftHost.SetParent(panel.transform, false);
            MenuUiBuilder.StretchRectToFill(craftHost);

            craftStubText = CreateBodyText(panel.transform, theme, 20f);
            craftStubText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform stubRect = craftStubText.GetComponent<RectTransform>();
            stubRect.anchorMin = Vector2.zero;
            stubRect.anchorMax = Vector2.one;
            stubRect.offsetMin = new Vector2(24f, 24f);
            stubRect.offsetMax = new Vector2(-24f, -24f);
            craftStubText.gameObject.SetActive(false);

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateOverviewTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "OverviewPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Overview";
            heading.fontStyle = FontStyles.Bold;
            heading.color = Color.white;

            overviewBuildingNameText = CreateBodyText(content, theme, 22f);
            overviewAssignedText = CreateBodyText(content, theme, 20f);
            overviewQueueText = CreateBodyText(content, theme, 20f);
            overviewStormText = CreateBodyText(content, theme, 20f);
            overviewStormText.fontStyle = FontStyles.Bold;
            overviewMaintenanceText = CreateBodyText(content, theme, 20f);
            overviewOutputText = CreateBodyText(content, theme, 20f);

            CreateBodyText(content, theme, 18f).text =
                "Assign pioneers and manage production queues from the other tabs.";

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreatePioneersTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "PioneersPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Pioneer Assignments";
            heading.fontStyle = FontStyles.Bold;
            heading.color = Color.white;

            CreateBodyText(content, theme, 18f).text =
                "Click a slot to cycle through available base pioneers (up to four per building).";

            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
            {
                int slotIndex = i;
                GameObject slotRow = new GameObject($"Slot{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                slotRow.transform.SetParent(content, false);
                LayoutElement rowLayout = slotRow.GetComponent<LayoutElement>();
                rowLayout.minHeight = 52f;
                rowLayout.preferredHeight = 52f;

                Image rowBackground = slotRow.GetComponent<Image>();
                MenuUiBuilder.ApplyUiSprite(rowBackground);
                rowBackground.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

                Button slotButton = slotRow.GetComponent<Button>();
                slotButton.targetGraphic = rowBackground;
                ColorBlock colors = slotButton.colors;
                colors.normalColor = rowBackground.color;
                colors.highlightedColor = new Color(0.18f, 0.22f, 0.3f, 0.98f);
                colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 1f);
                colors.selectedColor = colors.highlightedColor;
                slotButton.colors = colors;
                UiSoundHelper.BindButton(slotButton);
                slotButton.onClick.AddListener(() => OnPioneerSlotClicked(slotIndex));
                pioneerSlotButtons[slotIndex] = slotButton;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(slotRow.transform, false);
                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                if (theme != null)
                    theme.ApplyFont(label);
                else
                    TmpUiHelper.ApplyDefaultFont(label);
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = theme != null ? theme.secondaryTextColor : Color.white;
                label.raycastTarget = false;
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(14f, 6f);
                labelRect.offsetMax = new Vector2(-14f, -6f);
                pioneerSlotLabels[slotIndex] = label;
            }

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateProductionTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "ProductionPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Production Queue";
            heading.fontStyle = FontStyles.Bold;
            heading.color = Color.white;

            CreateBodyText(content, theme, 18f).text =
                "Queued recipes run while you are on expedition and pause during sulfur storms.";

            productionPausedOverlay = CreateBodyText(content, theme, 20f);
            productionPausedOverlay.fontStyle = FontStyles.Bold;
            productionPausedOverlay.color = new Color(1f, 0.78f, 0.45f, 1f);
            productionPausedOverlay.gameObject.SetActive(false);

            GameObject listHost = new GameObject("QueueList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listHost.transform.SetParent(content, false);
            VerticalLayoutGroup listLayout = listHost.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            productionListParent = listHost.transform;

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateChangesTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "ChangesPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Building Settings";
            heading.fontStyle = FontStyles.Bold;
            heading.color = Color.white;

            CreateBodyText(content, theme, 18f).text =
                "Per-building automation and mode toggles. Changes apply to this structure only.";

            GameObject toggleHost = new GameObject("SettingsToggles", typeof(RectTransform), typeof(VerticalLayoutGroup));
            toggleHost.transform.SetParent(content, false);
            VerticalLayoutGroup toggleLayout = toggleHost.GetComponent<VerticalLayoutGroup>();
            toggleLayout.spacing = 10f;
            toggleLayout.childControlWidth = true;
            toggleLayout.childForceExpandWidth = true;
            toggleLayout.childForceExpandHeight = false;
            changesToggleHost = toggleHost.transform;

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateOperationalScrollPanel(Transform parent, string name, out Transform content)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            content = contentObject.transform;
            return panel;
        }

        private static GameObject CreateScrollableTabPanel(
            Transform parent,
            string name,
            string heading,
            params string[] paragraphs)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI headingText = CreateBodyText(content.transform, theme, 26f);
            headingText.text = heading;
            headingText.fontStyle = FontStyles.Bold;
            headingText.color = Color.white;

            for (int i = 0; i < paragraphs.Length; i++)
            {
                TextMeshProUGUI paragraph = CreateBodyText(content.transform, theme, 20f);
                paragraph.text = paragraphs[i];
                paragraph.textWrappingMode = TextWrappingModes.Normal;
            }

            panel.SetActive(false);
            return panel;
        }

        private void CreateTabButton(Transform parent, BuildingControlTab tab, string label, ShiftUiTheme theme)
        {
            GameObject tabObject = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            tabObject.transform.SetParent(parent, false);

            LayoutElement layout = tabObject.AddComponent<LayoutElement>();
            layout.minWidth = 108f;
            layout.preferredWidth = 128f;
            layout.flexibleWidth = 1f;
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;

            Image background = tabObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = InactiveTabColor;

            Button button = tabObject.GetComponent<Button>();
            button.targetGraphic = background;
            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(tabObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(text, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(text);
            text.text = label;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = InactiveLabelColor;
            text.raycastTarget = false;
            MenuUiBuilder.StretchRectToFill(text.GetComponent<RectTransform>());

            tabButtonBackgrounds[tab] = background;
            tabButtonLabels[tab] = text;

            BuildingControlTab captured = tab;
            button.onClick.AddListener(() => ShowTab(captured));
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

        private void RefreshOperationalTabs()
        {
            RefreshOverviewTab();
            RefreshPioneersTab();
            RefreshProductionTab();
            RefreshChangesTab();
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
            }
        }

        private void RefreshOverviewTab()
        {
            if (overviewBuildingNameText == null || activePanel == null)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            int assignedCount = BuildingOperationRegistry.CountAssignedPioneers(state);
            bool crisisActive = EnvironmentalCrisisHudMode.IsCrisisActive;

            string buildingName = string.IsNullOrEmpty(activePanel.BuildingDisplayName)
                ? "Building"
                : activePanel.BuildingDisplayName;

            overviewBuildingNameText.text = $"Building: {buildingName}";
            overviewAssignedText.text =
                $"Assigned pioneers: {assignedCount}/{BuildingOperationRegistry.MaxAssignedPioneers}";
            overviewQueueText.text = $"Production queue: {state.ProductionQueue.Count} entr" +
                (state.ProductionQueue.Count == 1 ? "y" : "ies");
            overviewStormText.text = crisisActive
                ? "Sulfur storm: PAUSED"
                : "Sulfur storm: Running";
            overviewStormText.color = crisisActive
                ? new Color(1f, 0.78f, 0.45f, 1f)
                : new Color(0.55f, 0.88f, 1f, 1f);

            if (overviewMaintenanceText != null)
            {
                overviewMaintenanceText.text = state.Settings.AutoMaintenance
                    ? $"Maintenance: {state.Settings.MaintenancePercent:0}% (auto-scheduled)"
                    : $"Maintenance: {state.Settings.MaintenancePercent:0}% (manual)";
            }

            if (overviewOutputText != null)
            {
                float output = BuildingOperationRegistry.GetEffectiveOutputMultiplier(state);
                overviewOutputText.text = crisisActive
                    ? "Output rate: paused"
                    : $"Output rate: {output:0.00}x";
            }
        }

        private void RefreshPioneersTab()
        {
            if (activePanel == null)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
            {
                if (pioneerSlotLabels[i] == null)
                    continue;

                string assigned = i < state.AssignedPioneers.Count ? state.AssignedPioneers[i] : string.Empty;
                pioneerSlotLabels[i].text = string.IsNullOrEmpty(assigned)
                    ? $"Slot {i + 1}: Unassigned"
                    : $"Slot {i + 1}: {assigned}";

                if (pioneerSlotButtons[i] != null
                    && pioneerSlotButtons[i].TryGetComponent(out Image rowBackground))
                {
                    rowBackground.color = string.IsNullOrEmpty(assigned)
                        ? new Color(0.12f, 0.14f, 0.18f, 0.95f)
                        : new Color(0.14f, 0.22f, 0.32f, 0.98f);
                }
            }
        }

        private void RefreshProductionTab()
        {
            if (productionListParent == null || activePanel == null)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            bool crisisActive = EnvironmentalCrisisHudMode.IsCrisisActive;

            if (productionPausedOverlay != null)
            {
                productionPausedOverlay.gameObject.SetActive(crisisActive);
                productionPausedOverlay.text = crisisActive
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
                bool entryPaused = crisisActive || entry.Paused;
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
            rowBackground.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            GameObject labelObject = new GameObject("RecipeLabel", typeof(RectTransform));
            labelObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI recipeLabel = labelObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(recipeLabel, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(recipeLabel);
            recipeLabel.fontSize = 18f;
            recipeLabel.alignment = TextAlignmentOptions.TopLeft;
            recipeLabel.color = Color.white;
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
            barBackground.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
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
                ? new Color(0.55f, 0.35f, 0.18f, 1f)
                : new Color(0.28f, 0.62f, 0.92f, 1f);
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
            percentLabel.color = theme != null ? theme.secondaryTextColor : new Color(0.8f, 0.86f, 0.92f, 1f);
            percentLabel.raycastTarget = false;
            RectTransform percentRect = percentLabel.rectTransform;
            percentRect.anchorMin = new Vector2(0f, 0f);
            percentRect.anchorMax = new Vector2(1f, 0.22f);
            percentRect.offsetMin = new Vector2(12f, 6f);
            percentRect.offsetMax = new Vector2(-12f, 0f);
            percentLabel.text = $"{Mathf.RoundToInt(entry.Progress * 100f)}%";
        }

        private void OnPioneerSlotClicked(int slotIndex)
        {
            if (activePanel == null)
                return;

            string[] rosterNames = BuildRosterDisplayNames().ToArray();
            BuildingOperationRegistry.CycleAssignSlot(activePanel.BuildingId, slotIndex, rosterNames);
            RefreshOverviewTab();
            RefreshPioneersTab();
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
                    "Accept injured pioneer overflow",
                    () => settings.AcceptInjuredOverflow,
                    value => settings.AcceptInjuredOverflow = value);

                CreateSettingToggle(
                    changesToggleHost,
                    theme,
                    "Prioritize skilled pioneers for shelter",
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

            bool crisisActive = EnvironmentalCrisisHudMode.IsCrisisActive;
            if (crisisActive)
                return;

            BuildingOperationState state = BuildingOperationRegistry.GetOrCreate(activePanel.BuildingId);
            float rate = BuildingOperationRegistry.GetEffectiveOutputMultiplier(state) * 0.012f;
            BuildingOperationRegistry.TickProductionProgress(state, rate, paused: false);
            RefreshProductionTab();
        }

        private static void CreateSettingToggle(
            Transform parent,
            ShiftUiTheme theme,
            string label,
            Func<bool> readValue,
            Action<bool> writeValue)
        {
            GameObject row = new GameObject("SettingRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;

            HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 12f;
            rowGroup.padding = new RectOffset(4, 4, 4, 4);
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = true;
            rowGroup.childControlHeight = true;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleObject.transform.SetParent(row.transform, false);
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 36f;
            toggleLayout.preferredWidth = 36f;

            Image toggleBg = toggleObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(toggleBg);
            toggleBg.color = new Color(0.1f, 0.12f, 0.16f, 1f);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.isOn = readValue();

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(toggleObject.transform, false);
            Image checkImage = checkmark.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(checkImage);
            checkImage.color = new Color(0.45f, 0.82f, 1f, 1f);
            MenuUiBuilder.StretchRectToFill(checkmark.GetComponent<RectTransform>());
            toggle.graphic = checkImage;

            toggle.onValueChanged.AddListener(value => writeValue(value));

            TextMeshProUGUI labelText = CreateBodyText(row.transform, theme, 17f);
            labelText.text = label;
            labelText.textWrappingMode = TextWrappingModes.Normal;
        }

        private static TextMeshProUGUI CreateHeaderSubtitle(Transform shellRoot)
        {
            Transform header = shellRoot.Find("Header");
            if (header == null)
                return null;

            GameObject subtitleObject = new GameObject("Subtitle", typeof(RectTransform));
            subtitleObject.transform.SetParent(header, false);
            TextMeshProUGUI subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(subtitle);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(subtitle);
            subtitle.fontSize = 14f;
            subtitle.fontStyle = FontStyles.Italic;
            subtitle.color = new Color(0.62f, 0.72f, 0.84f, 0.92f);
            subtitle.alignment = TextAlignmentOptions.BottomLeft;
            subtitle.raycastTarget = false;

            RectTransform subtitleRect = subtitleObject.GetComponent<RectTransform>();
            subtitleRect.anchorMin = Vector2.zero;
            subtitleRect.anchorMax = Vector2.one;
            subtitleRect.offsetMin = new Vector2(20f, 6f);
            subtitleRect.offsetMax = new Vector2(-56f, -6f);
            return subtitle;
        }

        private static string FormatBuildingIdLabel(string buildingId)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
                return "UNKNOWN";

            string normalized = buildingId.Replace('_', ' ').Trim();
            return normalized.ToUpperInvariant();
        }

        private static List<string> BuildRosterDisplayNames()
        {
            List<string> names = new List<string>();
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return names;

            IReadOnlyList<SkilledPioneerRecord> skilled = roster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(skilled[i].displayName))
                    names.Add(skilled[i].displayName);
            }

            for (int workerIndex = 1; workerIndex <= roster.WorkerCount; workerIndex++)
                names.Add($"Worker {workerIndex}");

            return names;
        }

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

            craftingUi?.EmbedPanel(craftHost);
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
        }

        private void Close()
        {
            UnembedCraft();
            overlayRoot.SetActive(false);
            activePanel = null;

            GameplayHudVisibility.SetModalOverlayOpen(false);

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetBuildingControlOpen(false);

            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private static void EnsureUiInput(Transform canvasRoot)
        {
            Canvas canvas = canvasRoot.GetComponent<Canvas>() ?? canvasRoot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static TextMeshProUGUI CreateBodyText(Transform parent, ShiftUiTheme theme, float size)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(text);
            else
                TmpUiHelper.ApplyDefaultFont(text);
            text.fontSize = size;
            text.color = theme != null ? theme.secondaryTextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
