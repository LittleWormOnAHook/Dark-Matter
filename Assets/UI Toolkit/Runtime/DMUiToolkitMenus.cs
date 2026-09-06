using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.Pioneers;
using Project.Quests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Fullscreen Toolkit journal shell, inventory grid, and map. Sibling UIDocument that
    /// shares UITK_Root Panel Settings (never nested under UITK_Root). uGUI journal /
    /// inventory / map windows are hidden while these menus drive.
    /// Stamp: DMUiToolkit 0901-lag
    /// </summary>
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public partial class DMUiToolkitMenus : MonoBehaviour
    {
        public const string MenusName = "UITK_Menus";
        /// <summary>Closed / idle — HUD (95) can draw hotbar above inventory when open.</summary>
        public const int MenusSort = 90;
        /// <summary>Open journal shell — above shell (100) so tabs and body are visible.</summary>
        public const int MenusOpenSort = 120;
        public const string JournalUxmlPath = "Assets/UI Toolkit/Screens/Journal.uxml";
        public const string JournalUssPath = "Assets/UI Toolkit/Screens/Journal.uss";
        public const string Stamp = "DMUiToolkit 0901-lag";

        private enum JournalSection
        {
            Quests,
            Chronicle,
            SystemLogs,
            GameLogs
        }

        private static readonly JournalWindowId[] ToolkitWindows =
        {
            JournalWindowId.JournalQuest,
            JournalWindowId.Inventory,
            JournalWindowId.Map,
            JournalWindowId.Pet,
            JournalWindowId.Pioneers,
            JournalWindowId.Character,
            JournalWindowId.Recipes,
            JournalWindowId.Skills,
            JournalWindowId.Echoes,
            JournalWindowId.Achievements
        };

        private static readonly (string Name, JournalWindowId Id)[] TabDefs =
        {
            ("tab-journal", JournalWindowId.JournalQuest),
            ("tab-inventory", JournalWindowId.Inventory),
            ("tab-map", JournalWindowId.Map),
            ("tab-pet", JournalWindowId.Pet),
            ("tab-companions", JournalWindowId.Pioneers),
            ("tab-character", JournalWindowId.Character),
            ("tab-blueprints", JournalWindowId.Recipes),
            ("tab-skills", JournalWindowId.Skills),
            ("tab-echoes", JournalWindowId.Echoes),
            ("tab-achievements", JournalWindowId.Achievements)
        };

        private static DMUiToolkitMenus instance;
        private static bool stamped;

        private UIDocument document;
        private VisualElement root;
        private VisualElement menuRoot;
        private VisualElement journalBody;
        private VisualElement inventoryBody;
        private VisualElement mapBody;
        private VisualElement questsPanel;
        private ScrollView chroniclePanel;
        private ScrollView systemPanel;
        private ScrollView gameLogsPanel;
        private ScrollView questList;
        private Label questDetailTitle;
        private Label questDetailBody;
        private ScrollView questObjectives;
        private Button questAbandon;
        private VisualElement inventoryGrid;
        private VisualElement mapImage;
        private VisualElement mapMarkers;
        private VisualElement mapPlayer;
        private VisualElement mapFog;
        private readonly Dictionary<JournalWindowId, Button> tabs = new Dictionary<JournalWindowId, Button>();
        private readonly Dictionary<JournalSection, Button> subtabs = new Dictionary<JournalSection, Button>();
        private readonly List<VisualElement> invSlots = new List<VisualElement>();
        private readonly List<VisualElement> invIcons = new List<VisualElement>();
        private readonly List<Label> invAmounts = new List<Label>();
        private readonly List<VisualElement> markerPool = new List<VisualElement>();
        private readonly List<InvSlotVisualCache> invSlotVisualCache = new List<InvSlotVisualCache>();

        private struct InvSlotVisualCache
        {
            public ItemData Item;
            public int Amount;
            public bool Unlocked;
        }

        private FullscreenUiNavigator boundNav;
        private JournalPanelUI boundJournal;
        private InventorySystem boundInventory;
        private QuestManager boundQuests;
        private PioneerRosterManager boundRoster;
        private WorldMapProvider boundMap;
        private Texture2D appliedMapTexture;
        private bool boundTree;
        private bool eventsHooked;
        private bool gameplayFullyBound;
        private bool uguiHidden;
        private bool menusVisible;
        public static bool IsOpen => instance != null && (instance.menusVisible || instance.pendingShowWindow.HasValue);
        public static bool IsInventoryOpen =>
            instance != null && instance.menusVisible && instance.paintedWindow == JournalWindowId.Inventory;

        /// <summary>True when UITK paints this journal tab instead of uGUI fullscreen hosts.</summary>
        public static bool HandlesWindow(JournalWindowId windowId)
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return false;
            return IsToolkitWindow(windowId);
        }

        private JournalWindowId? paintedWindow;
        private JournalSection journalSection = JournalSection.Quests;
        private string selectedQuestId;
        private bool abandonConfirmPending;

        public static DMUiToolkitMenus Instance => instance;

        public static bool IsDriving
        {
            get
            {
                if (instance == null || !instance.menusVisible)
                    return false;
                return IsToolkitWindow(FullscreenUiNavigator.Instance != null
                    ? FullscreenUiNavigator.Instance.CurrentWindow
                    : null);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitMenus EnsureHost()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            DMUiToolkitBootstrap.EnsureExists();
            UIDocument hud = DMUiToolkitBootstrap.Instance != null
                ? DMUiToolkitBootstrap.Instance.HudDocument
                : null;
            if (hud == null)
                return null;

            GameObject staleMenus = DMUiToolkitOverlayDocument.FindNamed(MenusName);
            if (staleMenus != null && staleMenus != hud.gameObject)
                staleMenus.SetActive(false);

            DMUiToolkitMenus host = hud.GetComponent<DMUiToolkitMenus>();
            if (host == null)
                host = hud.gameObject.AddComponent<DMUiToolkitMenus>();

            instance = host;
            host.document = hud;
            host.EnsureReady();
            StampOnce("journal mounted on UITK_Hud panel (shared with gameplay HUD)");
            return host;
        }

        /// <summary>Push Toolkit journal visibility immediately when the uGUI navigator opens/closes.</summary>
        public static void SyncFromNavigatorImmediate()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            EnsureHost()?.SyncFromNavigator();
        }

        /// <summary>
        /// Ghost-lock helper: if the navigator stack is empty, force-hide Toolkit journal chrome
        /// even when SyncFromNavigator is waiting on a missing JournalPanelUI bind.
        /// </summary>
        public static void ForceHideIfNavigatorClosed()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return;

            FullscreenUiNavigator nav = FullscreenUiNavigator.Instance;
            if (nav != null && nav.IsAnyOpen)
                return;

            DMUiToolkitMenus host = EnsureHost();
            if (host == null || !host.menusVisible)
                return;

            host.HideMenus();
            host.paintedWindow = null;
            if (host.uguiHidden)
                host.RestoreUguiWindows();
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            HookEvents();
        }

        private void OnDisable()
        {
            boundTree = false;
            UnhookEvents();
            RestoreUguiWindows();
            menusVisible = false;
        }

        private void OnDestroy()
        {
            UnhookEvents();
            RestoreUguiWindows();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
            {
                HideMenus();
                RestoreUguiWindows();
                return;
            }

            if (!boundTree)
                BindTree();

            HookEvents();
            if (!gameplayFullyBound)
                TryBindGameplay();
            FlushPendingShow();
            bool navOpen = boundNav != null && boundNav.IsAnyOpen;
            if (menusVisible || pendingShowWindow.HasValue || navOpen || (Time.frameCount & 15) == 0)
                SyncFromNavigator();
            TickInventoryContextDismiss();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement hudRoot = document.rootVisualElement;
            if (hudRoot == null)
            {
                boundTree = false;
                return;
            }

            menuRoot = hudRoot.Q<VisualElement>("menu-root");
            if (menuRoot == null)
            {
                VisualTreeAsset journalTree = DMUiToolkitBootstrap.LoadUxml(JournalUxmlPath);
                if (journalTree == null)
                {
                    boundTree = false;
                    return;
                }

                TemplateContainer mounted = journalTree.Instantiate();
                hudRoot.Add(mounted);
                StretchOverlay(mounted);
                menuRoot = mounted.Q<VisualElement>("menu-root") ?? mounted;
                StretchOverlay(menuRoot);
                menuRoot.BringToFront();
                DMUiToolkitBootstrap.ApplyTheme(document, JournalUssPath);
            }

            root = menuRoot;
            VisualElement tree = menuRoot;
            journalBody = tree.Q<VisualElement>("journal-body");
            inventoryBody = tree.Q<VisualElement>("inventory-body");
            mapBody = tree.Q<VisualElement>("map-body");
            questsPanel = tree.Q<VisualElement>("quests-panel");
            chroniclePanel = tree.Q<ScrollView>("chronicle-panel");
            systemPanel = tree.Q<ScrollView>("system-panel");
            gameLogsPanel = tree.Q<ScrollView>("game-logs-panel");
            questList = tree.Q<ScrollView>("quest-list");
            questDetailTitle = tree.Q<Label>("quest-detail-title");
            questDetailBody = tree.Q<Label>("quest-detail-body");
            questObjectives = tree.Q<ScrollView>("quest-objectives");
            questAbandon = tree.Q<Button>("quest-abandon");
            inventoryGrid = tree.Q<VisualElement>("inventory-grid");
            mapImage = tree.Q<VisualElement>("map-image");
            mapMarkers = tree.Q<VisualElement>("map-markers");
            mapPlayer = tree.Q<VisualElement>("map-player");
            mapFog = tree.Q<VisualElement>("map-fog");
            BindExtraPanels(tree);
            BindMapPanZoom();

            tabs.Clear();
            for (int i = 0; i < TabDefs.Length; i++)
            {
                Button button = tree.Q<Button>(TabDefs[i].Name);
                if (button == null)
                    continue;

                JournalWindowId id = TabDefs[i].Id;
                tabs[id] = button;
                button.UnregisterCallback<ClickEvent>(OnTabClicked);
                button.userData = id;
                button.RegisterCallback<ClickEvent>(OnTabClicked);
            }

            BindSubtab("subtab-quests", JournalSection.Quests, tree);
            BindSubtab("subtab-chronicle", JournalSection.Chronicle, tree);
            BindSubtab("subtab-system", JournalSection.SystemLogs, tree);
            BindSubtab("subtab-game", JournalSection.GameLogs, tree);

            if (questAbandon != null)
            {
                questAbandon.clicked -= OnAbandonClicked;
                questAbandon.clicked += OnAbandonClicked;
            }

            VisualElement veil = tree.Q<VisualElement>("menu-veil");
            if (veil != null)
            {
                veil.UnregisterCallback<ClickEvent>(OnVeilClicked);
                veil.RegisterCallback<ClickEvent>(OnVeilClicked);
            }

            if (mapPlayer != null && MapUiSprites.PlayerArrow != null)
            {
                DMUiToolkitStyle.TrySetSpriteBackground(mapPlayer, MapUiSprites.PlayerArrow, ScaleMode.ScaleToFit);
                mapPlayer.style.backgroundColor = Color.clear;
                mapPlayer.style.unityBackgroundImageTintColor = new Color(1f, 0.12f, 0.08f, 1f); // bright red player arrow
            }

            BindInventoryStorage(tree);
            BindInventoryHotbar(tree);

            boundTree = menuRoot != null;
            DMUiToolkitInputHost.RegisterKeyRoot(menuRoot);

            if (pendingShowWindow.HasValue)
                FlushPendingShow();
            else if (!menusVisible)
                HideMenus();
        }

        private static void StretchOverlay(VisualElement element)
        {
            if (element == null)
                return;

            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }

        private void BindSubtab(string name, JournalSection section, VisualElement tree)
        {
            Button button = tree.Q<Button>(name);
            if (button == null)
                return;

            subtabs[section] = button;
            button.UnregisterCallback<ClickEvent>(OnSubtabClicked);
            button.userData = section;
            button.RegisterCallback<ClickEvent>(OnSubtabClicked);
        }

        private void HookEvents()
        {
            if (eventsHooked)
                return;

            DMGameLog.Changed -= RefreshGameLogs;
            DMGameLog.Changed += RefreshGameLogs;
            eventsHooked = true;
        }

        private void UnhookEvents()
        {
            DMGameLog.Changed -= RefreshGameLogs;
            eventsHooked = false;

            if (boundNav != null)
            {
                boundNav.OnActiveWindowChanged -= HandleActiveWindowChanged;
                boundNav.OnPauseGameplayChanged -= HandleNavigatorPauseChanged;
                boundNav = null;
            }

            if (boundInventory != null)
            {
                boundInventory.OnInventoryChanged -= RefreshInventory;
                boundInventory = null;
            }

            if (boundQuests != null)
            {
                boundQuests.OnQuestUpdated -= HandleQuestUpdated;
                boundQuests.OnQuestCompleted -= HandleQuestUpdated;
                boundQuests = null;
            }

            if (boundRoster != null)
            {
                boundRoster.OnEchoChronicleChanged -= RefreshChronicleAndSystem;
                boundRoster = null;
            }

            if (boundMap != null)
            {
                boundMap.MapTextureReady -= ApplyMapTexture;
                boundMap = null;
            }

            boundJournal = null;
            gameplayFullyBound = false;
            UnhookExtraGameplay();
        }

        private void TryBindGameplay()
        {
            if (boundJournal == null)
                boundJournal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);

            FullscreenUiNavigator nav = boundJournal != null && boundJournal.Navigator != null
                ? boundJournal.Navigator
                : FullscreenUiNavigator.Instance;
            if (nav != boundNav)
            {
                if (boundNav != null)
                {
                    boundNav.OnActiveWindowChanged -= HandleActiveWindowChanged;
                    boundNav.OnPauseGameplayChanged -= HandleNavigatorPauseChanged;
                }

                boundNav = nav;
                if (boundNav != null)
                {
                    boundNav.OnActiveWindowChanged += HandleActiveWindowChanged;
                    boundNav.OnPauseGameplayChanged += HandleNavigatorPauseChanged;
                }
            }

            if (boundInventory == null)
            {
                InventorySystem inventory = FindAnyObjectByType<InventorySystem>();
                if (inventory != null)
                {
                    boundInventory = inventory;
                    boundInventory.OnInventoryChanged += RefreshInventory;
                }
            }

            EnsureBoundItemActions();

            if (boundQuests == null)
            {
                QuestManager quests = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
                if (quests != null)
                {
                    boundQuests = quests;
                    boundQuests.OnQuestUpdated += HandleQuestUpdated;
                    boundQuests.OnQuestCompleted += HandleQuestUpdated;
                }
            }

            if (boundRoster == null)
            {
                PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
                if (roster != null)
                {
                    boundRoster = roster;
                    boundRoster.OnEchoChronicleChanged += RefreshChronicleAndSystem;
                }
            }

            if (boundMap == null)
            {
                WorldMapProvider map = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();
                if (map != null)
                {
                    boundMap = map;
                    appliedMapTexture = null;
                    boundMap.MapTextureReady += ApplyMapTexture;
                }
            }

            BindExtraGameplay();
            gameplayFullyBound = boundNav != null
                && boundInventory != null
                && boundQuests != null
                && boundRoster != null
                && boundMap != null;
        }

        private void HandleActiveWindowChanged(JournalWindowId? windowId)
        {
            SyncFromNavigator();
        }

        private void HandleNavigatorPauseChanged(bool paused)
        {
            SyncFromNavigator();
        }

        private void SyncFromNavigator()
        {
            if (boundJournal == null)
            {
                if ((Time.frameCount & 31) != 0)
                    return;

                boundJournal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
                if (boundJournal == null)
                    return;
            }

            if (!boundJournal.IsOpen)
            {
                if (menusVisible)
                {
                    HideMenus();
                    paintedWindow = null;
                    if (uguiHidden)
                        RestoreUguiWindows();
                }

                return;
            }

            JournalWindowId? window = boundJournal.ActiveJournalWindow;
            if (!window.HasValue
                || !IsToolkitWindow(window)
                || !GameSession.HasStarted
                || DMUiToolkitLoadingOverlay.IsShowing)
            {
                return;
            }

            if (menusVisible && paintedWindow == window.Value)
            {
                if (window.Value == JournalWindowId.Map)
                    TickMapPlayer();
                JournalPanelUI.EnsurePointerForOpenJournal();
                if (!uguiHidden)
                    HideUguiWindows(window.Value);
                return;
            }

            ForceShow(window.Value);
            if (!uguiHidden)
                HideUguiWindows(window.Value);
        }

        private void ShowMenus(JournalWindowId window)
        {
            if (menuRoot == null)
                BindTree();
            if (menuRoot == null)
                return;

            bool sameWindow = menusVisible && paintedWindow == window;
            if (sameWindow)
            {
                if (window == JournalWindowId.Map)
                    TickMapPlayer();
                return;
            }

            menusVisible = true;
            ReleaseMapPointer();
            menuRoot.pickingMode = PickingMode.Position;
            DMUiToolkitOverlayDocument.SetShown(menuRoot, true);
            if (document != null)
            {
                if (!document.gameObject.activeInHierarchy)
                    document.gameObject.SetActive(true);
                if (!document.enabled)
                    document.enabled = true;
            }

            if (menuRoot != null && window != JournalWindowId.Inventory)
                menuRoot.BringToFront();

            if (root != null)
            {
                root.pickingMode = PickingMode.Position;
                DMUiToolkitOverlayDocument.SetShown(root, true);
            }

            paintedWindow = window;
            PaintTabs(window);
            DMUiToolkitOverlayDocument.SetShown(journalBody, window == JournalWindowId.JournalQuest);
            DMUiToolkitOverlayDocument.SetShown(inventoryBody, window == JournalWindowId.Inventory);
            DMUiToolkitOverlayDocument.SetShown(mapBody, window == JournalWindowId.Map);
            ShowExtraPanel(window);

            if (window == JournalWindowId.JournalQuest)
            {
                ApplyJournalSectionVisibility();
                RefreshJournalContent();
            }
            else if (window == JournalWindowId.Inventory)
            {
                SuppressLegacyInventoryPanel();
                RefreshInventory();
            }
            else if (window == JournalWindowId.Map)
            {
                ApplyMapTexture();
                RefreshMapMarkers();
            }
            else
            {
                RefreshExtraPanel(window);
            }

            JournalPanelUI.EnsurePointerForOpenJournal();
            GameplayMenuTime.SyncJournal(true, window);
            DMUiToolkitHud.RefreshMenuChrome();
        }

        private void HideMenus()
        {
            menusVisible = false;
            paintedWindow = null;
            pendingShowWindow = null;
            ReleaseMapPointer();
            DMUiToolkitWorldMenus.HideItemTooltip();
            DMUiToolkitWorldMenus.HideJournalTip();
            DMUiToolkitPetChrome.HideTooltip();
            DMUiToolkitContext.Hide();
            HideInventoryContextMenu();
            if (menuRoot != null)
            {
                menuRoot.pickingMode = PickingMode.Ignore;
                DMUiToolkitOverlayDocument.SetShown(menuRoot, false);
            }

            if (root != null)
                root.pickingMode = PickingMode.Ignore;

            DMUiToolkitHud.RefreshMenuChrome();
        }

        private void PaintTabs(JournalWindowId active)
        {
            foreach (KeyValuePair<JournalWindowId, Button> pair in tabs)
            {
                bool on = pair.Key == active;
                pair.Value.EnableInClassList("dmg-tab--active", on);
            }
        }

        private void PaintSubtabs()
        {
            foreach (KeyValuePair<JournalSection, Button> pair in subtabs)
                pair.Value.EnableInClassList("dmg-subtab--active", pair.Key == journalSection);
        }

        private void ApplyJournalSectionVisibility()
        {
            PaintSubtabs();
            DMUiToolkitOverlayDocument.SetShown(questsPanel, journalSection == JournalSection.Quests);
            DMUiToolkitOverlayDocument.SetShown(chroniclePanel, journalSection == JournalSection.Chronicle);
            DMUiToolkitOverlayDocument.SetShown(systemPanel, journalSection == JournalSection.SystemLogs);
            DMUiToolkitOverlayDocument.SetShown(gameLogsPanel, journalSection == JournalSection.GameLogs);
        }

        private void OnTabClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not JournalWindowId id)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayButtonClick();

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal == null)
                return;

            if (!journal.IsOpen)
            {
                journal.TryToggleTab(id);
                ForceShow(id);
                return;
            }

            if (journal.ActiveJournalWindow == id)
            {
                JournalPanelUI.EnsurePointerForOpenJournal();
                return;
            }

            if (!journal.SwitchToTab(id))
                return;

            ForceShow(id);
        }

        private void OnSubtabClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not JournalSection section)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayUiHoverTick();
            journalSection = section;
            ApplyJournalSectionVisibility();
            RefreshJournalContent();
        }

        private void OnVeilClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            journal?.ReleaseInputCapture();
            HideMenus();
        }

        private void RefreshJournalContent()
        {
            switch (journalSection)
            {
                case JournalSection.Quests:
                    RefreshQuests();
                    break;
                case JournalSection.Chronicle:
                    RefreshChronicle();
                    break;
                case JournalSection.SystemLogs:
                    RefreshSystemLogs();
                    break;
                default:
                    RefreshGameLogs();
                    break;
            }
        }

        private void HandleQuestUpdated(QuestProgress progress)
        {
            if (progress != null && string.IsNullOrEmpty(selectedQuestId))
                selectedQuestId = progress.questId;

            if (menusVisible && journalSection == JournalSection.Quests)
                RefreshQuests();
        }

        private void RefreshQuests()
        {
            if (questList == null)
                return;

            questList.Clear();
            if (boundQuests == null)
                boundQuests = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();

            if (boundQuests == null)
            {
                questList.Add(MakeEmpty("No quest manager."));
                ApplyQuestDetail(null, null);
                return;
            }

            IReadOnlyList<QuestProgress> all = boundQuests.GetAllProgress();
            int added = 0;
            for (int i = 0; i < all.Count; i++)
            {
                QuestProgress progress = all[i];
                if (progress == null)
                    continue;
                if (progress.status != QuestStatus.Active && progress.status != QuestStatus.Completed)
                    continue;

                QuestDefinition definition = boundQuests.GetDefinition(progress.questId);
                if (definition == null)
                    continue;

                if (string.IsNullOrEmpty(selectedQuestId))
                    selectedQuestId = definition.ResolvedId;

                questList.Add(MakeQuestRow(definition, progress));
                added++;
            }

            if (added == 0)
            {
                questList.Add(MakeEmpty("No active quests."));
                selectedQuestId = null;
            }

            QuestDefinition selectedDef = string.IsNullOrEmpty(selectedQuestId)
                ? null
                : boundQuests.GetDefinition(selectedQuestId);
            QuestProgress selectedProgress = string.IsNullOrEmpty(selectedQuestId)
                ? null
                : boundQuests.GetProgress(selectedQuestId);
            ApplyQuestDetail(selectedDef, selectedProgress);
        }

        private VisualElement MakeQuestRow(QuestDefinition definition, QuestProgress progress)
        {
            bool selected = definition.ResolvedId == selectedQuestId;
            Button row = new Button();
            row.AddToClassList("dmg-quest-row");
            row.EnableInClassList("dmg-quest-row--selected", selected);
            row.style.backgroundColor = QuestUiPalette.GetRowBackgroundColor(progress.status, selected, null);

            Label title = new Label(definition.title);
            title.AddToClassList("dmg-quest-row-title");
            title.style.color = QuestUiPalette.GetTitleColor(progress.status, null);
            title.pickingMode = PickingMode.Ignore;
            row.Add(title);

            Label status = new Label(QuestUiPalette.GetStatusLabel(progress.status));
            status.AddToClassList("dmg-quest-row-status");
            status.style.color = QuestUiPalette.GetStatusLabelColor(progress.status, null);
            status.pickingMode = PickingMode.Ignore;
            row.Add(status);

            string captured = definition.ResolvedId;
            row.clicked += () =>
            {
                selectedQuestId = captured;
                abandonConfirmPending = false;
                RefreshQuests();
            };
            return row;
        }

        private void ApplyQuestDetail(QuestDefinition definition, QuestProgress progress)
        {
            if (questDetailTitle == null)
                return;

            if (definition == null || progress == null)
            {
                questDetailTitle.text = "No active quests";
                if (questDetailBody != null)
                    questDetailBody.text = "Accept and complete quests with companions.";
                questObjectives?.Clear();
                if (questAbandon != null)
                    DMUiToolkitOverlayDocument.SetShown(questAbandon, false);
                return;
            }

            questDetailTitle.text = definition.title;
            questDetailTitle.style.color = QuestUiPalette.GetTitleColor(progress.status, null);
            if (questDetailBody != null)
                questDetailBody.text = definition.description ?? string.Empty;

            if (questObjectives != null)
            {
                questObjectives.Clear();
                if (definition.objectives != null)
                {
                    for (int i = 0; i < definition.objectives.Count; i++)
                    {
                        QuestObjectiveDefinition objective = definition.objectives[i];
                        if (objective == null)
                            continue;

                        int required = Mathf.Max(1, objective.requiredCount);
                        int current = progress.GetObjectiveProgress(i);
                        string label = string.IsNullOrEmpty(objective.description)
                            ? objective.type.ToString()
                            : objective.description;

                        VisualElement row = new VisualElement();
                        row.AddToClassList("dmg-objective-row");
                        Label desc = new Label(label);
                        desc.AddToClassList("dmg-objective-label");
                        desc.style.color = QuestUiPalette.GetObjectiveTextColor(current >= required, progress.status, null);
                        Label count = new Label(Mathf.Min(current, required) + "/" + required);
                        count.AddToClassList("dmg-objective-count");
                        count.style.color = desc.style.color;
                        row.Add(desc);
                        row.Add(count);
                        questObjectives.Add(row);
                    }
                }
            }

            bool canAbandon = progress.status == QuestStatus.Active;
            if (questAbandon != null)
            {
                DMUiToolkitOverlayDocument.SetShown(questAbandon, canAbandon);
                questAbandon.SetEnabled(canAbandon);
                questAbandon.text = abandonConfirmPending ? "Confirm Abandon?" : "Abandon Quest";
            }
        }

        private void OnAbandonClicked()
        {
            if (boundQuests == null || string.IsNullOrEmpty(selectedQuestId))
                return;

            QuestProgress progress = boundQuests.GetProgress(selectedQuestId);
            if (progress == null || progress.status != QuestStatus.Active)
                return;

            if (!abandonConfirmPending)
            {
                abandonConfirmPending = true;
                if (questAbandon != null)
                    questAbandon.text = "Confirm Abandon?";
                return;
            }

            if (!boundQuests.AbandonQuest(selectedQuestId))
                return;

            abandonConfirmPending = false;
            selectedQuestId = null;
            RefreshQuests();
        }

        private void RefreshChronicleAndSystem()
        {
            if (!menusVisible)
                return;
            if (journalSection == JournalSection.Chronicle)
                RefreshChronicle();
            else if (journalSection == JournalSection.SystemLogs)
                RefreshSystemLogs();
        }

        private void RefreshChronicle()
        {
            if (chroniclePanel == null)
                return;

            chroniclePanel.Clear();
            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null || boundRoster.EchoChronicle.Count == 0)
            {
                chroniclePanel.Add(MakeEmpty("Rescue chronicle empty. Successful and failed Neural Echo rescues are recorded here."));
                return;
            }

            int added = 0;
            for (int i = 0; i < boundRoster.EchoChronicle.Count; i++)
            {
                EchoChronicleEntry entry = boundRoster.EchoChronicle[i];
                if (entry == null || entry.simulationIncident)
                    continue;

                chroniclePanel.Add(MakeLogCard(
                    entry.rescueFailed ? "Rescue Failed" : "Rescue Success",
                    entry.echoName,
                    entry.classSummary + "  ·  " + entry.abilitySummary,
                    entry.rescueFailed ? DarkMatterGenesisUiPalette.DangerRed : DarkMatterGenesisUiPalette.PositiveGreen));
                added++;
            }

            if (added == 0)
                chroniclePanel.Add(MakeEmpty("No rescue events yet — simulation logs may still appear under System Logs."));
        }

        private void RefreshSystemLogs()
        {
            if (systemPanel == null)
                return;

            systemPanel.Clear();
            AddSystemNote("Field Note", "Jet Booster", "Jump, then hold Space in the air to burn suit fuel. Fuel, thrust, and ground recovery rank up on the Player skill tree. The Character tab shows remaining jet fuel.", DarkMatterGenesisUiPalette.RichFuchsia);
            AddSystemNote("Field Note", "Walker Drill", "Store it in inventory and right-click Deploy. Approach and press E for Start, Stop, or Collect. Stop reverses the deploy animation.", DarkMatterGenesisUiPalette.SoftBeigeGray);
            AddSystemNote("Field Note", "Hovercraft", "Board to drive with WASD. Hold right mouse to yaw. Left mouse fires the turret. Sprint adds a short boost. Exiting restores on-foot look and cursor lock.", DarkMatterGenesisUiPalette.RichFuchsia);
            AddSystemNote("Field Note", "Field Construction", "Coming online: scan wrecks to learn blueprints, place a hologram ghost, then hold to materialize. Cancel the hold to stop the resource drain.", DarkMatterGenesisUiPalette.SoftBeigeGray);
            AddSystemNote("Field Note", "Io Underground", "The surface map's lower-right question mark marks a later underground passage map. Surface tiles stay the current play space.", DarkMatterGenesisUiPalette.SoftBeigeGray);

            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null)
                return;

            for (int i = 0; i < boundRoster.EchoChronicle.Count; i++)
            {
                EchoChronicleEntry entry = boundRoster.EchoChronicle[i];
                if (entry == null || !entry.simulationIncident)
                    continue;

                systemPanel.Add(MakeLogCard(
                    "Colony Event",
                    entry.echoName,
                    entry.classSummary + "  |  " + entry.abilitySummary,
                    DarkMatterGenesisUiPalette.Gold));
            }
        }

        private void AddSystemNote(string heading, string title, string body, Color color)
        {
            systemPanel.Add(MakeLogCard(heading, title, body, color));
        }

        private void RefreshGameLogs()
        {
            if (gameLogsPanel == null)
                return;
            if (menusVisible && journalSection != JournalSection.GameLogs)
                return;

            gameLogsPanel.Clear();
            IReadOnlyList<DMGameLogEntry> entries = DMGameLog.Entries;
            if (entries == null || entries.Count == 0)
            {
                gameLogsPanel.Add(MakeEmpty("No game logs yet. Pickups, popups, radio, and conversations will appear here as you play."));
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                DMGameLogEntry entry = entries[i];
                string kind = GameLogKindLabel(entry.Kind);
                gameLogsPanel.Add(MakeLogCard(kind, kind, entry.Text, GameLogKindColor(entry.Kind)));
            }
        }

        private static string GameLogKindLabel(DMGameLogKind kind)
        {
            switch (kind)
            {
                case DMGameLogKind.Pickup: return "Pickup";
                case DMGameLogKind.Popup: return "Popup";
                case DMGameLogKind.Radio: return "Radio";
                case DMGameLogKind.Dialogue: return "Dialogue";
                case DMGameLogKind.Prompt: return "Prompt";
                default: return "Other";
            }
        }

        private static Color GameLogKindColor(DMGameLogKind kind)
        {
            switch (kind)
            {
                case DMGameLogKind.Pickup: return DarkMatterGenesisUiPalette.PositiveGreen;
                case DMGameLogKind.Dialogue:
                case DMGameLogKind.Radio: return DarkMatterGenesisUiPalette.RichFuchsia;
                case DMGameLogKind.Prompt: return DarkMatterGenesisUiPalette.SoftBeigeGray;
                case DMGameLogKind.Popup: return DarkMatterGenesisUiPalette.Gold;
                default: return DarkMatterGenesisUiPalette.BodyText;
            }
        }

        private static VisualElement MakeLogCard(string heading, string title, string body, Color headingColor)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("dmg-log-card");
            card.pickingMode = PickingMode.Ignore;

            Label head = new Label(heading ?? string.Empty);
            head.AddToClassList("dmg-log-heading");
            head.style.color = headingColor;
            card.Add(head);

            Label titleLabel = new Label(title ?? string.Empty);
            titleLabel.AddToClassList("dmg-log-title");
            card.Add(titleLabel);

            Label bodyLabel = new Label(body ?? string.Empty);
            bodyLabel.AddToClassList("dmg-log-body");
            card.Add(bodyLabel);
            return card;
        }

        private static Label MakeEmpty(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("dmg-empty");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private void RefreshInventory()
        {
            if (inventoryGrid == null)
                return;

            if (boundInventory == null)
                boundInventory = FindAnyObjectByType<InventorySystem>();
            if (boundInventory == null)
                return;

            // Display is always a locked 10×5 main grid (50 cells); storage unlocks only enable cells.
            int count = InventorySystem.DefaultTotalMainSlots;
            if (boundInventory.inventorySize < count)
                boundInventory.EnsureSlotCounts(count, boundInventory.hotbarSize, boundInventory.toolbarSize, boundInventory.unlockedMainSlots);

            EnsureInventorySlots(count);
            // Force slot chrome refresh so locked/unlocked slate contrast reapplies after layout/style changes.
            invSlotVisualCache.Clear();

            for (int i = 0; i < count; i++)
            {
                VisualElement slot = invSlots[i];
                VisualElement icon = invIcons[i];
                Label amount = invAmounts[i];
                bool unlocked = boundInventory.IsMainSlotUnlocked(i);

                InventorySystem.InventorySlot data = i < boundInventory.slots.Count
                    ? boundInventory.slots[i]
                    : null;
                ItemData item = data != null && !data.IsEmpty ? data.item : null;
                int stack = data != null ? data.amount : 0;

                InvSlotVisualCache cache = i < invSlotVisualCache.Count
                    ? invSlotVisualCache[i]
                    : default;
                bool unchanged = cache.Item == item && cache.Amount == stack && cache.Unlocked == unlocked;

                if (i >= invSlotVisualCache.Count)
                    invSlotVisualCache.Add(default);
                invSlotVisualCache[i] = new InvSlotVisualCache { Item = item, Amount = stack, Unlocked = unlocked };

                slot.EnableInClassList("dmg-inv-slot--locked", !unlocked);
                if (!unlocked)
                {
                    // Cover locked expansion rows with palette Soft Beige-Gray until unlocked.
                    Color locked = DarkMatterGenesisUiPalette.LockedSlotBackground;
                    slot.style.backgroundColor = locked;
                    slot.style.borderTopColor = locked;
                    slot.style.borderRightColor = locked;
                    slot.style.borderBottomColor = locked;
                    slot.style.borderLeftColor = locked;
                    slot.style.unityBackgroundImageTintColor = locked;
                    slot.style.opacity = 1f;
                }
                else
                {
                    slot.style.backgroundColor = DarkMatterGenesisUiPalette.WithAlpha(
                        DarkMatterGenesisUiPalette.DarkNavy, 0.92f);
                    Color unlockedBorder = DarkMatterGenesisUiPalette.WithAlpha(
                        DarkMatterGenesisUiPalette.WarmOffWhite, 0.28f);
                    slot.style.borderTopColor = unlockedBorder;
                    slot.style.borderRightColor = unlockedBorder;
                    slot.style.borderBottomColor = unlockedBorder;
                    slot.style.borderLeftColor = unlockedBorder;
                    slot.style.opacity = 1f;
                }

                if (unchanged && unlocked)
                    continue;

                if (item != null && item.icon != null)
                {
                    if (DMUiToolkitStyle.TrySetSpriteBackground(icon, item.icon, ScaleMode.ScaleToFit))
                        DMUiToolkitOverlayDocument.SetShown(icon, true);
                    else
                        DMUiToolkitOverlayDocument.SetShown(icon, false);
                }
                else
                {
                    DMUiToolkitStyle.ClearBackgroundImage(icon);
                    DMUiToolkitOverlayDocument.SetShown(icon, false);
                }

                if (item != null && stack > 1)
                {
                    amount.text = stack.ToString();
                    DMUiToolkitOverlayDocument.SetShown(amount, true);
                }
                else
                {
                    amount.text = string.Empty;
                    DMUiToolkitOverlayDocument.SetShown(amount, false);
                }
            }

            RefreshInventoryStorage();
            RefreshInventoryHotbar();
        }

        private void EnsureInventorySlots(int count)
        {
            if (inventoryGrid != null)
            {
                inventoryGrid.style.flexDirection = FlexDirection.Row;
                inventoryGrid.style.flexWrap = Wrap.Wrap;
                inventoryGrid.style.justifyContent = Justify.FlexStart;
                inventoryGrid.style.alignContent = Align.FlexStart;
                inventoryGrid.style.width = 860f;
                inventoryGrid.style.maxWidth = 860f;
            }

            while (invSlots.Count < count)
            {
                int index = invSlots.Count;
                VisualElement slot = new VisualElement();
                slot.AddToClassList("dmg-inv-slot");
                slot.name = "inv-slot-" + index;
                slot.pickingMode = PickingMode.Position;
                slot.style.flexGrow = 0f;
                slot.style.flexShrink = 0f;
                slot.style.width = 80f;
                slot.style.height = 80f;
                slot.style.marginTop = 2f;
                slot.style.marginRight = 2f;
                slot.style.marginBottom = 2f;
                slot.style.marginLeft = 2f;
                AttachInventorySlotDrag(slot, index);

                VisualElement icon = new VisualElement();
                icon.AddToClassList("dmg-inv-icon");
                icon.pickingMode = PickingMode.Ignore;
                slot.Add(icon);

                Label amount = new Label();
                amount.AddToClassList("dmg-inv-amount");
                amount.pickingMode = PickingMode.Ignore;
                slot.Add(amount);

                inventoryGrid.Add(slot);
                invSlots.Add(slot);
                invIcons.Add(icon);
                invAmounts.Add(amount);
            }

            while (invSlots.Count > count)
            {
                int last = invSlots.Count - 1;
                VisualElement extra = invSlots[last];
                extra.RemoveFromHierarchy();
                invSlots.RemoveAt(last);
                if (last < invIcons.Count)
                    invIcons.RemoveAt(last);
                if (last < invAmounts.Count)
                    invAmounts.RemoveAt(last);
            }

            for (int i = 0; i < invSlots.Count; i++)
            {
                invSlots[i].style.flexGrow = 0f;
                invSlots[i].style.flexShrink = 0f;
                invSlots[i].style.width = 80f;
                invSlots[i].style.height = 80f;
            }
        }

        private void ApplyMapTexture()
        {
            if (mapImage == null)
                return;

            Texture2D texture = null;
            if (boundMap != null && boundMap.MapTexture != null)
                texture = boundMap.MapTexture;
            else if (WorldMapProvider.Instance != null)
                texture = WorldMapProvider.Instance.MapTexture;

            if (texture == null)
                texture = WorldMapProvider.LoadFakeMapTexture();

            if (texture == null || texture == appliedMapTexture)
                return;

            if (DMUiToolkitStyle.TrySetTextureBackground(mapImage, texture, ScaleMode.ScaleToFit))
                appliedMapTexture = texture;
            ApplyFullMapFog();
        }

        private void RefreshMapMarkers()
        {
            if (mapImage == null || mapMarkers == null)
                return;

            ApplyMapTexture();
            ApplyFullMapFog();

            WorldMapProvider provider = boundMap != null ? boundMap : WorldMapProvider.Instance;
            float viewW = mapImage.resolvedStyle.width;
            float viewH = mapImage.resolvedStyle.height;
            if (viewW <= 1f || viewH <= 1f)
                return;

            Rect fitted = FittedMapRect(viewW, viewH, appliedMapTexture);

            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            int written = 0;
            if (markers != null && provider != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    MapMarker marker = markers[i];
                    if (marker == null || !marker.ShowOnFullMap || !marker.IsRevealedOnMap)
                        continue;

                    VisualElement dot = EnsureMarker(written);
                    Vector2 uv = provider.WorldToMap01(marker.WorldPosition);
                    PlaceOnMap(dot, fitted, uv, 10f);
                    if (marker.IconSprite != null)
                    {
                        DMUiToolkitStyle.TrySetSpriteBackground(dot, marker.IconSprite, ScaleMode.ScaleToFit);
                        dot.style.backgroundColor = Color.clear;
                    }
                    else
                    {
                        DMUiToolkitStyle.ClearBackgroundImage(dot);
                        dot.style.backgroundColor = marker.Color;
                    }

                    written++;
                }
            }

            for (int i = written; i < markerPool.Count; i++)
                DMUiToolkitOverlayDocument.SetShown(markerPool[i], false);

            TickMapPlayer(provider, fitted);
        }

        private void TickMapPlayer()
        {
            if (mapImage == null || mapPlayer == null)
                return;
            WorldMapProvider provider = boundMap != null ? boundMap : WorldMapProvider.Instance;
            float viewW = mapImage.resolvedStyle.width;
            float viewH = mapImage.resolvedStyle.height;
            if (viewW <= 1f || viewH <= 1f)
                return;
            TickMapPlayer(provider, FittedMapRect(viewW, viewH, appliedMapTexture));
        }

        private void TickMapPlayer(WorldMapProvider provider, Rect fitted)
        {
            if (mapPlayer == null || provider == null)
                return;
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;
            Vector2 uv = provider.WorldToMap01(player.transform.position);
            PlaceOnMap(mapPlayer, fitted, uv, 18f);
            SetElementRotate(mapPlayer, -player.transform.eulerAngles.y);
            DMUiToolkitOverlayDocument.SetShown(mapPlayer, true);
        }

        private void ApplyFullMapFog()
        {
            if (mapFog == null || mapImage == null)
                return;

            MapFogOfWar fog = MapFogOfWar.Instance ?? MapFogOfWar.EnsureExists();
            bool show = fog != null && MapFogOfWar.SystemEnabled && fog.FogTexture != null;
            mapFog.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;

            DMUiToolkitStyle.TrySetTextureBackground(mapFog, fog.FogTexture, ScaleMode.StretchToFill);
            float viewW = mapImage.resolvedStyle.width;
            float viewH = mapImage.resolvedStyle.height;
            if (viewW <= 1f || viewH <= 1f)
                return;
            Rect fitted = FittedMapRect(viewW, viewH, appliedMapTexture);
            mapFog.style.position = Position.Absolute;
            mapFog.style.left = fitted.x;
            mapFog.style.top = fitted.y;
            mapFog.style.width = fitted.width;
            mapFog.style.height = fitted.height;
        }

        private VisualElement EnsureMarker(int index)
        {
            while (markerPool.Count <= index)
            {
                VisualElement dot = new VisualElement();
                dot.AddToClassList("dmg-map-marker");
                dot.pickingMode = PickingMode.Ignore;
                mapMarkers.Add(dot);
                markerPool.Add(dot);
            }

            VisualElement marker = markerPool[index];
            DMUiToolkitOverlayDocument.SetShown(marker, true);
            return marker;
        }

        private static void PlaceOnMap(VisualElement element, Rect fitted, Vector2 uv, float size)
        {
            element.style.left = fitted.x + uv.x * fitted.width - size * 0.5f;
            element.style.top = fitted.y + (1f - uv.y) * fitted.height - size * 0.5f;
        }

        private static Rect FittedMapRect(float viewW, float viewH, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
                return new Rect(0f, 0f, viewW, viewH);

            float scale = Mathf.Min(viewW / texture.width, viewH / texture.height);
            float w = texture.width * scale;
            float h = texture.height * scale;
            return new Rect((viewW - w) * 0.5f, (viewH - h) * 0.5f, w, h);
        }

        private static bool IsToolkitWindow(JournalWindowId? window)
        {
            if (!window.HasValue)
                return false;

            JournalWindowId id = window.Value;
            for (int i = 0; i < ToolkitWindows.Length; i++)
            {
                if (ToolkitWindows[i] == id)
                    return true;
            }

            return false;
        }

        private void HideUguiWindows(JournalWindowId window)
        {
            _ = window;
            SetNamedActive("JournalOverlay", false);
            SetNamedActive("JournalTabRailHost", false);
            SetNamedActive("JournalQuestWindowHost", false);
            SetNamedActive("InventoryWindowHost", false);
            SetNamedActive("MapWindowHost", false);
            SetNamedActive("PetWindowHost", false);
            SetNamedActive("PioneersWindowHost", false);
            SetNamedActive("CharacterWindowHost", false);
            SetNamedActive("RecipesWindowHost", false);
            SetNamedActive("CraftWindowHost", false);
            SetNamedActive("SkillsWindowHost", false);
            SetNamedActive("EchoesWindowHost", false);
            SetNamedActive("AchievementsWindowHost", false);
            SetNamedActive("FullMapOverlay", false);
            HideConvertedUguiPanels(true);

            uguiHidden = true;
        }

        private void RestoreUguiWindows()
        {
            if (!uguiHidden)
                return;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            bool journalOpen = journal != null && journal.IsOpen;
            if (journalOpen && !IsToolkitWindow(FullscreenUiNavigator.Instance != null
                    ? FullscreenUiNavigator.Instance.CurrentWindow
                    : null))
            {
                SetNamedActive("JournalOverlay", true);
                SetNamedActive("JournalTabRailHost", true);
            }

            HideConvertedUguiPanels(false);
            uguiHidden = false;
        }

        private readonly System.Collections.Generic.List<global::UnityEngine.CanvasGroup> convertedUguiGroups = new System.Collections.Generic.List<global::UnityEngine.CanvasGroup>();

        private void HideConvertedUguiPanels(bool hide)
        {
            if (!hide)
            {
                for (int i = 0; i < convertedUguiGroups.Count; i++)
                {
                    global::UnityEngine.CanvasGroup group = convertedUguiGroups[i];
                    if (group == null)
                        continue;
                    group.alpha = 1f;
                    group.blocksRaycasts = true;
                    group.interactable = true;
                }

                convertedUguiGroups.Clear();
                return;
            }

            HideUguiBehaviour<SkillsPanelUI>();
            HideUguiBehaviour<PioneerRosterPanelUI>();
            HideUguiBehaviour<PetUI>();
            if (DMUiToolkitConfig.IsEnabled)
                SuppressLegacyInventoryPanel();
        }

        private static void SuppressLegacyInventoryPanel()
        {
            InventoryUI inventoryUi = Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
            inventoryUi?.RestoreInventoryPanel();
        }

        private void HideUguiBehaviour<T>() where T : MonoBehaviour
        {
            T[] found = FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < found.Length; i++)
            {
                T behaviour = found[i];
                if (behaviour == null)
                    continue;
                global::UnityEngine.CanvasGroup group = behaviour.GetComponent<global::UnityEngine.CanvasGroup>();
                if (group == null)
                    group = behaviour.gameObject.AddComponent<global::UnityEngine.CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
                if (!convertedUguiGroups.Contains(group))
                    convertedUguiGroups.Add(group);
            }
        }

        private static void SetNamedActive(string objectName, bool active)
        {
            GameObject host = DMUiToolkitOverlayDocument.FindNamed(objectName);
            if (host != null && host.activeSelf != active)
                host.SetActive(active);
        }

        private float mapZoom = 1f;
        private Vector2 mapPan;
        private bool mapPanning;
        private Vector2 mapPanLast;
        private int mapPointerId = -1;
        private bool mapPanBound;

        private void BindMapPanZoom()
        {
            if (mapImage == null)
                return;

            mapImage.pickingMode = PickingMode.Position;
            if (mapPanBound)
                return;

            mapImage.RegisterCallback<WheelEvent>(OnMapWheel);
            mapImage.RegisterCallback<PointerDownEvent>(OnMapPointerDown);
            mapImage.RegisterCallback<PointerMoveEvent>(OnMapPointerMove);
            mapImage.RegisterCallback<PointerUpEvent>(OnMapPointerUp);
            mapImage.RegisterCallback<PointerCaptureOutEvent>(OnMapPointerCaptureOut);
            mapPanBound = true;
        }

        private void ResetMapView()
        {
            mapZoom = 1f;
            mapPan = Vector2.zero;
            mapPanning = false;
            ApplyMapTransform();
        }

        private void ApplyMapTransform()
        {
            if (mapImage == null)
                return;

            mapImage.style.scale = new Scale(new Vector3(mapZoom, mapZoom, 1f));
            mapImage.style.translate = new Translate(mapPan.x, mapPan.y, 0f);
        }

        private void OnMapWheel(WheelEvent evt)
        {
            if (mapImage == null)
                return;

            float factor = evt.delta.y < 0f ? 1.12f : 0.89f;
            float next = Mathf.Clamp(mapZoom * factor, 1f, 4f);
            if (Mathf.Approximately(next, mapZoom))
                return;

            mapZoom = next;
            if (mapZoom <= 1.01f)
            {
                mapZoom = 1f;
                mapPan = Vector2.zero;
            }

            ApplyMapTransform();
            evt.StopPropagation();
        }

        private void OnMapPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || mapImage == null)
                return;

            mapPanning = mapZoom > 1.01f;
            mapPanLast = (Vector2)evt.position;
            mapPointerId = evt.pointerId;
            mapImage.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMapPointerMove(PointerMoveEvent evt)
        {
            if (!mapPanning || mapImage == null)
                return;

            Vector2 pos = (Vector2)evt.position;
            Vector2 delta = pos - mapPanLast;
            mapPanLast = pos;
            mapPan += delta;
            ApplyMapTransform();
            evt.StopPropagation();
        }

        private void OnMapPointerUp(PointerUpEvent evt)
        {
            ReleaseMapPointer();
        }

        private void OnMapPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            ReleaseMapPointer();
        }

        private void ReleaseMapPointer()
        {
            if (mapImage != null && mapPointerId >= 0 && mapImage.HasPointerCapture(mapPointerId))
                mapImage.ReleasePointer(mapPointerId);

            mapPanning = false;
            mapPointerId = -1;
        }

        private static void StampOnce(string detail)
        {
            // No-op: Stamp string kept as version marker; stop play-time spam.
            if (stamped)
                return;

            stamped = true;
            _ = detail;
        }
    }
}
