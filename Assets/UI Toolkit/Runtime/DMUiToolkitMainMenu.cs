using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit main menu + pause overlay. Sibling UIDocument sharing UITK_Root Panel Settings.
    /// Settings / Controls / Save slots are UITK when config is enabled.
    /// </summary>
    [DefaultExecutionOrder(-380)]
    [DisallowMultipleComponent]
    public class DMUiToolkitMainMenu : MonoBehaviour
    {
        public const string MainMenuName = "UITK_MainMenu";
        public const int MainMenuSort = 20000;
        public const string MainMenuUxmlPath = "Assets/UI Toolkit/Screens/MainMenu.uxml";
        public const string MainMenuUssPath = "Assets/UI Toolkit/Screens/MainMenu.uss";

        private static DMUiToolkitMainMenu instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement background;
        private VisualElement panel;
        private Button resumeButton;
        private Button continueButton;
        private Button newButton;
        private Button loadButton;
        private Button saveButton;
        private Button settingsButton;
        private Button controlsButton;
        private Button quitButton;
        private Label messageLabel;
        private Label envZoneLabel;
        private Label envTempLabel;
        private Label envConditionLabel;
        private Label envHazardsLabel;
        private VisualElement envBar;

        private bool bound;
        private bool wired;
        private bool visible;
        private bool pauseMode;
        private int zoneIndex;
        private bool uguiHidden;

        public static DMUiToolkitMainMenu Instance => instance;

        public static bool IsVisible => instance != null && instance.visible;


        public static DMUiToolkitMainMenu EnsureHost()
        {
            if (instance != null)
                return instance;

            if (!DMUiToolkitBootstrap.EnsureExists())
                return null;

            UIDocument doc = EnsureDocument();
            if (doc == null)
                return null;

            DMUiToolkitMainMenu host = doc.GetComponent<DMUiToolkitMainMenu>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitMainMenu>();

            host.document = doc;
            host.BindTree();
            host.HideInternal();
            return host;
        }

        private static UIDocument EnsureDocument()
        {
            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            Transform parent = bootstrap != null ? bootstrap.transform.parent : null;
            PanelSettings settings = bootstrap != null && bootstrap.ShellDocument != null
                ? bootstrap.ShellDocument.panelSettings
                : null;

            GameObject host = DMUiToolkitOverlayDocument.FindNamed(MainMenuName);
            if (host == null)
            {
                host = new GameObject(MainMenuName);
                host.transform.SetParent(parent, false);
            }
            else if (bootstrap != null
                && host.transform != bootstrap.transform
                && host.transform.IsChildOf(bootstrap.transform))
            {
                host.transform.SetParent(parent, false);
            }
            else if (parent != null && host.transform.parent != parent && host.transform.parent == null)
            {
                host.transform.SetParent(parent, false);
            }

            DMUiToolkitOverlayDocument.RegisterNamed(MainMenuName, host);

            UIDocument document = host.GetComponent<UIDocument>();
            if (document == null)
                document = host.AddComponent<UIDocument>();

            if (settings != null && document.panelSettings != settings)
                document.panelSettings = settings;

            document.sortingOrder = MainMenuSort;

            VisualTreeAsset tree = DMUiToolkitBootstrap.LoadUxml(MainMenuUxmlPath);
            if (tree != null && document.visualTreeAsset != tree)
                document.visualTreeAsset = tree;

            DMUiToolkitBootstrap.ApplyTheme(document, DMUiToolkitBootstrap.ThemeUssPath);
            DMUiToolkitBootstrap.ApplyTheme(document, MainMenuUssPath);
            return document;
        }

        public static void SyncFromController(MainMenuController controller, bool show, bool pauseOverlay)
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            if (!show && instance == null)
                return;

            DMUiToolkitMainMenu host = EnsureHost();
            if (host == null)
                return;

            if (show)
                host.ShowInternal(pauseOverlay);
            else
                host.HideInternal();
        }

        public void Show(bool pauseOverlay)
        {
            ShowInternal(pauseOverlay);
        }

        public void Hide()
        {
            HideInternal();
        }

        public void RefreshButtonStates(bool pauseOverlay, bool hasContinueSave, bool sessionStarted, bool hasAnySave)
        {
            pauseMode = pauseOverlay;

            SetButtonVisible(resumeButton, pauseOverlay);
            SetButtonVisible(continueButton, !pauseOverlay && hasContinueSave);
            SetButtonVisible(newButton, !pauseOverlay);

            if (saveButton != null)
                saveButton.SetEnabled(sessionStarted);
            if (loadButton != null)
                loadButton.SetEnabled(hasAnySave);

            RefreshEnvironmentBar();
        }

        public void SetMessage(string message)
        {
            if (messageLabel == null)
                return;

            bool hasMessage = !string.IsNullOrEmpty(message);
            messageLabel.text = message ?? string.Empty;
            messageLabel.style.display = hasMessage ? DisplayStyle.Flex : DisplayStyle.None;
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
        }

        private void OnDisable()
        {
            RestoreUguiMenuChrome();
            visible = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
            {
                HideInternal();
                RestoreUguiMenuChrome();
                return;
            }

            if (!bound)
                return;

            if (visible)
                HideUguiMenuChromeOnce();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("mainmenu-root") ?? tree;
            background = tree.Q<VisualElement>("mainmenu-background");
            panel = tree.Q<VisualElement>("mainmenu-panel");
            resumeButton = tree.Q<Button>("btn-resume");
            continueButton = tree.Q<Button>("btn-continue");
            newButton = tree.Q<Button>("btn-new");
            loadButton = tree.Q<Button>("btn-load");
            saveButton = tree.Q<Button>("btn-save");
            settingsButton = tree.Q<Button>("btn-settings");
            controlsButton = tree.Q<Button>("btn-controls");
            quitButton = tree.Q<Button>("btn-quit");
            messageLabel = tree.Q<Label>("menu-message");
            envZoneLabel = tree.Q<Label>("env-zone");
            envTempLabel = tree.Q<Label>("env-temp");
            envConditionLabel = tree.Q<Label>("env-condition");
            envHazardsLabel = tree.Q<Label>("env-hazards");
            envBar = tree.Q<VisualElement>("env-status-bar");

            WireButtons();

            if (envBar != null)
            {
                envBar.UnregisterCallback<ClickEvent>(OnEnvBarClicked);
                envBar.RegisterCallback<ClickEvent>(OnEnvBarClicked);
            }

            if (!visible)
                DMUiToolkitOverlayDocument.SetShown(root, false);

            bound = root != null;
        }

        private void WireButtons()
        {
            if (wired)
                return;

            Wire(resumeButton, () => ResolveController()?.InvokeResumeFromPause());
            Wire(continueButton, () => ResolveController()?.InvokeContinueExpedition());
            Wire(newButton, () => ResolveController()?.InvokeNewGame());
            Wire(loadButton, () => ResolveController()?.InvokeOpenLoad());
            Wire(saveButton, () => ResolveController()?.InvokeOpenSave());
            Wire(settingsButton, () => ResolveController()?.InvokeOpenSettings());
            Wire(controlsButton, () => ResolveController()?.InvokeOpenControls());
            Wire(quitButton, () => ResolveController()?.InvokeExitGame());
            wired = true;
        }

        private static void Wire(Button button, System.Action action)
        {
            if (button == null || action == null)
                return;

            button.clicked -= action;
            button.clicked += action;
        }

        private void ShowInternal(bool pauseOverlay)
        {
            if (LoadingOverlayController.IsBlockingMenu)
            {
                HideInternal();
                return;
            }

            BindTree();
            pauseMode = pauseOverlay;
            visible = true;
            uguiHidden = false;

            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            if (background != null)
                background.style.display = DisplayStyle.Flex;
            if (panel != null)
                panel.style.display = DisplayStyle.Flex;

            MainMenuController controller = ResolveController();
            if (controller != null)
                controller.RefreshToolkitMenuStates();

            RefreshEnvironmentBar();
            HideUguiMenuChromeOnce();
        }

        private void HideInternal()
        {
            visible = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void OnEnvBarClicked(ClickEvent evt)
        {
            zoneIndex = (zoneIndex + 1) % MainMenuZoneProfile.GetDefaultZones().Count;
            RefreshEnvironmentBar();
            evt.StopPropagation();
        }

        private void RefreshEnvironmentBar()
        {
            IReadOnlyList<MainMenuZoneProfile> zones = MainMenuZoneProfile.GetDefaultZones();
            if (zones == null || zones.Count == 0)
                return;

            MainMenuZoneProfile zone = zones[Mathf.Clamp(zoneIndex, 0, zones.Count - 1)];

            if (envZoneLabel != null)
                envZoneLabel.text = $"ZONE {zone.zoneId}";
            if (envTempLabel != null)
                envTempLabel.text = $"{zone.temperatureC:0}°C";

            if (envConditionLabel != null)
            {
                envConditionLabel.text = zone.surfaceCondition;
                bool safe = zone.surfaceCondition == "SAFE";
                envConditionLabel.EnableInClassList("dmg-mainmenu-env-stat--safe", safe);
                envConditionLabel.EnableInClassList("dmg-mainmenu-env-stat--extreme", !safe);
            }

            if (envHazardsLabel != null)
                envHazardsLabel.text = zone.hazardsText;
        }

        private static MainMenuController ResolveController()
        {
            return UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        }

        private static void SetButtonVisible(VisualElement button, bool show)
        {
            if (button == null)
                return;

            button.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HideUguiMenuChromeOnce()
        {
            if (uguiHidden)
                return;

            MainMenuController controller = ResolveController();
            if (controller != null)
                controller.HideLegacyMenuChromeForToolkit();

            uguiHidden = true;
        }

        private void RestoreUguiMenuChrome()
        {
            if (!uguiHidden)
                return;

            uguiHidden = false;
        }
    }
}
