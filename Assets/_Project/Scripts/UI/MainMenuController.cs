using System;
using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Interaction;
using Project.Managers;
using Project.Pioneers;
using Project.Player;
using Project.Survival;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    [DefaultExecutionOrder(-200)]
    public class MainMenuController : MonoBehaviour
    {
        private const float MenuScale = 1f;
        private static readonly Color MenuBackgroundColor = SurvivalPioneerUiPalette.DarkNavy;

        [SerializeField] private bool buildOnAwake = true;

        private GameObject menuPanel;
        private GameObject menuBackground;
        private SettingsPanelController settingsPanel;
        private SaveSlotsPanelController saveSlotsPanel;
        private GameStartPopup gameStartPopup;
        private PlayerInput playerInput;
        private readonly List<GameObject> hiddenCanvasRoots = new List<GameObject>();

        private Button newGameButton;
        private Button resumeButton;
        private Button saveButton;
        private Button loadButton;
        private Button settingsButton;
        private Button exitButton;
        private TextMeshProUGUI menuMessageLabel;
        private bool pauseOverlayActive;
        private Texture2D pendingSaveScreenshot;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (!buildOnAwake)
                return;

            EnsurePostProcessingController();
            GameAudioManager.EnsureExists();
            GameSettings.Load();
            GameSession.ResetSession();

            gameStartPopup = FindStartPopup();
            playerInput = FindAnyObjectByType<PlayerInput>();
            BuildMainMenu();
            UiSoundHelper.BindButtonsInHierarchy(transform);

            // Loading Genesis owns the screen on boot; keep freshly built chrome hidden until it hands off.
            if (LoadingOverlayController.IsBlockingMenu)
                HideMenuChrome();
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            // Back out one menu layer at a time. These sub-panel checks run regardless of whether a
            // game session has started, so Esc can close Settings/Save/Load from the true main menu
            // too, not just from the in-game pause menu.
            if (settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.Close();
                return;
            }

            if (saveSlotsPanel != null && saveSlotsPanel.IsOpen)
            {
                saveSlotsPanel.Close();
                return;
            }

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return;

            if (!GameSession.HasStarted)
                return;

            if (pauseOverlayActive)
                ResumeFromPause();
            else
                ShowPauseMenu();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying)
                return;

            EnsureExists();
        }

        public static void EnsureExists()
        {
            if (!Application.isPlaying)
                return;

            MainMenuController existing = FindAnyObjectByType<MainMenuController>();
            if (existing != null)
                return;

            Canvas canvas = ResolveMainCanvas();
            if (canvas == null)
                return;

            EnsureEventSystem();
            EnsureGraphicRaycaster(canvas);

            canvas.gameObject.AddComponent<MainMenuController>();
        }

        private static GameStartPopup FindStartPopup()
        {
            return FindAnyObjectByType<GameStartPopup>(FindObjectsInactive.Include);
        }

        public static Canvas ResolveMainCanvas()
        {
            GameStartPopup popup = FindStartPopup();
            if (popup != null && popup.popupPanel != null)
            {
                Canvas popupCanvas = popup.popupPanel.GetComponentInParent<Canvas>();
                if (popupCanvas != null && !IsOpticsOverlayCanvas(popupCanvas))
                    return popupCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && !IsOpticsOverlayCanvas(canvas))
                    return canvas;
            }

            return null;
        }

        public static Transform ResolveCombatHudRoot()
        {
            Canvas canvas = ResolveMainCanvas();
            if (canvas == null)
                return null;

            UIManager uiManager = canvas.GetComponent<UIManager>();
            return uiManager != null ? uiManager.transform : canvas.transform;
        }

        private static bool IsOpticsOverlayCanvas(Canvas canvas)
        {
            return canvas != null && canvas.gameObject.name == "OpticsOverlayCanvas";
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static void EnsureGraphicRaycaster(Canvas canvas)
        {
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private void EnsurePostProcessingController()
        {
            if (FindAnyObjectByType<PostProcessingController>() != null)
                return;

            GameObject bootstrap = new GameObject("PostProcessingController");
            bootstrap.AddComponent<PostProcessingController>();
        }

        private void BuildMainMenu()
        {
            Transform canvasRoot = transform;
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvasRoot = canvas.transform;

            EnsureGraphicRaycaster(canvas);

            menuBackground = MenuUiBuilder.CreateFullScreenPanel(canvasRoot, "MainMenuBackground", MenuBackgroundColor, blockRaycasts: false);

            menuPanel = MenuUiBuilder.CreateFullScreenPanel(canvasRoot, "MainMenuPanel", new Color(0f, 0f, 0f, 0.001f), blockRaycasts: true);

            BuildTitleBlock(menuPanel.transform);
            BuildButtonColumn(menuPanel.transform);
            BuildVersionLabel(menuPanel.transform);

            menuMessageLabel = CreateAnchoredMessageLabel(menuPanel.transform);

            settingsPanel = gameObject.AddComponent<SettingsPanelController>();
            settingsPanel.Build(canvasRoot);

            saveSlotsPanel = gameObject.AddComponent<SaveSlotsPanelController>();
            saveSlotsPanel.Build(canvasRoot, this);

            RefreshMenuButtonStates();
        }

        private void BuildTitleBlock(Transform parent)
        {
            GameObject titleBlock = new GameObject("TitleBlock", typeof(RectTransform), typeof(VerticalLayoutGroup));
            titleBlock.transform.SetParent(parent, false);
            RectTransform titleRect = titleBlock.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(48f, -48f);
            titleRect.sizeDelta = new Vector2(520f, 120f);

            VerticalLayoutGroup layout = titleBlock.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(titleBlock.transform, "DARK MATTER : GENESIS 2160", 34f * MenuScale);
            title.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI subtitle = MenuUiBuilder.CreateTitle(titleBlock.transform, "IO // JUPITER SYSTEM", 16f * MenuScale);
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            subtitle.color = SurvivalPioneerUiPalette.MutedText;
        }

        private void BuildButtonColumn(Transform parent)
        {
            GameObject column = new GameObject("MenuButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);
            RectTransform columnRect = column.GetComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0f, 0.5f);
            columnRect.anchorMax = new Vector2(0f, 0.5f);
            columnRect.pivot = new Vector2(0f, 0.5f);
            columnRect.anchoredPosition = new Vector2(72f, 0f);
            columnRect.sizeDelta = new Vector2(280f, 420f);

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Mathf.RoundToInt(14f * MenuScale);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            Vector2 buttonSize = new Vector2(260f * MenuScale, 48f * MenuScale);
            float buttonFontSize = 20f * MenuScale;

            resumeButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Continue", buttonSize, buttonFontSize);
            newGameButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "New Expedition", buttonSize, buttonFontSize);
            loadButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Load", buttonSize, buttonFontSize);
            saveButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Save", buttonSize, buttonFontSize);
            settingsButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Settings", buttonSize, buttonFontSize);
            exitButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Quit", buttonSize, buttonFontSize);

            resumeButton.onClick.AddListener(ResumeFromPause);
            newGameButton.onClick.AddListener(StartNewGame);
            loadButton.onClick.AddListener(OpenLoad);
            saveButton.onClick.AddListener(OpenSave);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(ExitGame);

            resumeButton.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI CreateAnchoredMessageLabel(Transform parent)
        {
            GameObject messageObject = new GameObject("MenuMessage", typeof(RectTransform));
            messageObject.transform.SetParent(parent, false);
            RectTransform rect = messageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(72f, -260f);
            rect.sizeDelta = new Vector2(420f, 48f);

            TextMeshProUGUI label = messageObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = 16f * MenuScale;
            label.color = SurvivalPioneerUiPalette.Gold;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.gameObject.SetActive(false);
            return label;
        }

        private void BuildVersionLabel(Transform parent)
        {
            GameObject versionObject = new GameObject("VersionLabel", typeof(RectTransform));
            versionObject.transform.SetParent(parent, false);
            RectTransform rect = versionObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(120f, 24f);

            TextMeshProUGUI label = versionObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = "v0.1";
            label.fontSize = 14f;
            label.color = SurvivalPioneerUiPalette.MutedText;
            label.alignment = TextAlignmentOptions.BottomLeft;
        }

        public void ShowMainMenu()
        {
            pauseOverlayActive = false;
            GameSession.SetPhase(GamePhase.MainMenu);

            if (menuPanel == null && buildOnAwake)
                BuildMainMenu();

            MainCanvasFlow.SanitizeCanvasHost(GetComponent<Canvas>() ?? MainMenuController.ResolveMainCanvas());

            HideGameplayUi();

            if (menuBackground != null)
                menuBackground.SetActive(true);
            if (menuPanel != null)
                menuPanel.SetActive(true);

            DestroyLegacyEnvironmentStatusBar();

            settingsPanel?.Close();
            saveSlotsPanel?.Close();
            ClearMenuMessage();

            ResolveStartPopup()?.HidePopup();

            FindAnyObjectByType<UIManager>()?.SetCurrencyHudVisible(false);
            HideHotbars();

            RefreshMenuButtonStates();
            SetGameWorldPaused(true);
            BringMenuToFront();

            // World stays paused and gameplay UI swept away, but the menu itself only appears once the
            // Loading Genesis overlay finishes fading — it re-runs this path on handoff.
            if (LoadingOverlayController.IsBlockingMenu)
                HideMenuChrome();
        }

        private void Start()
        {
            if (GameSession.HasStarted)
                return;

            StartCoroutine(RefreshAfterUiBootstrap());
        }

        private IEnumerator RefreshAfterUiBootstrap()
        {
            // InventoryUI.Start performs the primary refresh after hotbar/toolbar exist.
            yield return null;

            if (!GameSession.HasStarted)
                MainCanvasFlow.Refresh();
        }

        public void HideMenuChrome()
        {
            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        private GameStartPopup ResolveStartPopup()
        {
            if (gameStartPopup == null)
                gameStartPopup = FindStartPopup();
            return gameStartPopup;
        }

        public void ShowPauseMenu()
        {
            pauseOverlayActive = true;

            if (menuBackground != null)
                menuBackground.SetActive(true);
            if (menuPanel != null)
                menuPanel.SetActive(true);

            DestroyLegacyEnvironmentStatusBar();

            settingsPanel?.Close();
            saveSlotsPanel?.Close();
            ClearMenuMessage();
            FindAnyObjectByType<UIManager>()?.SetCurrencyHudVisible(false);
            HideGameplayChromeForMenu();
            RefreshMenuButtonStates();
            SetGameWorldPaused(true);
            BringMenuToFront();
        }

        /// <summary>
        /// True while the main menu or in-game pause overlay should suppress bottom gameplay HUD.
        /// </summary>
        public static bool BlocksGameplayHud
        {
            get
            {
                if (!GameSession.HasStarted)
                    return true;

                MainMenuController menu = FindAnyObjectByType<MainMenuController>();
                return menu != null && menu.pauseOverlayActive;
            }
        }

        /// <summary>
        /// Hides the item/tool hotbars. Called whenever the main menu, pause menu, or any of their
        /// sub-panels (Settings, Save/Load) are open — ShowMainMenu() already sweeps the hotbar away
        /// as part of HideGameplayUi(), but ShowPauseMenu() previously left it fully visible/interactable
        /// behind the pause overlay, and re-opening a sub-panel didn't re-assert it either.
        /// </summary>
        private static void HideHotbars()
        {
            HideGameplayChromeForMenu();
        }

        private static void HideGameplayChromeForMenu()
        {
            GameplayHudVisibility.SetGameplayHudVisible(false);

            ToolBarUI toolbar = FindAnyObjectByType<ToolBarUI>();
            toolbar?.SetGameplayVisible(false);
        }

        private void DestroyLegacyEnvironmentStatusBar()
        {
            if (menuPanel == null)
                return;

            Transform legacyBar = menuPanel.transform.Find("EnvironmentStatusBar");
            if (legacyBar != null)
                Destroy(legacyBar.gameObject);
        }

        private void BringMenuToFront()
        {
            if (menuBackground != null)
                menuBackground.transform.SetAsLastSibling();
            if (menuPanel != null)
                menuPanel.transform.SetAsLastSibling();
        }

        private void RefreshMenuButtonStates()
        {
            if (newGameButton != null)
                newGameButton.gameObject.SetActive(!pauseOverlayActive);
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(pauseOverlayActive);
            if (saveButton != null)
                saveButton.interactable = GameSession.HasStarted;
            if (loadButton != null)
                loadButton.interactable = GameSaveSystem.HasAnySaveFile;
        }

        public void SaveToSlot(int slotIndex)
        {
            if (!GameSession.HasStarted)
            {
                ShowMenuMessage("Start a game before saving.");
                saveSlotsPanel?.Close();
                return;
            }

            if (GameSaveSystem.TrySave(slotIndex, pendingSaveScreenshot, out string message))
            {
                ClearPendingSaveScreenshot();
                saveSlotsPanel?.Close();
                ShowMenuMessage(message);
            }
            else
            {
                saveSlotsPanel?.Close();
                ShowMenuMessage(message);
            }
        }

        public void ClearPendingSaveScreenshot()
        {
            if (pendingSaveScreenshot == null)
                return;

            Destroy(pendingSaveScreenshot);
            pendingSaveScreenshot = null;
        }

        public Texture2D PendingSaveScreenshot => pendingSaveScreenshot;

        public void LoadFromSlot(int slotIndex)
        {
            if (!GameSaveSystem.TryLoad(slotIndex, out string message))
            {
                saveSlotsPanel?.Close();
                ShowMenuMessage(message);
                return;
            }

            saveSlotsPanel?.Close();
            pauseOverlayActive = false;

            HideMenuChrome();

            GameSession.MarkStarted();
            SetGameWorldPaused(false);
            ReleaseGameplayInputCapture();
            RefreshGameplayCamera();
            GameAudioManager.Instance?.StartGameplayMusic();
            UnityEngine.Object.FindAnyObjectByType<UIManager>()?.RefreshSurvivalDisplay();
            RefreshMenuButtonStates();
            MainCanvasFlow.Refresh();
        }

        private void StartNewGame()
        {
            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);

            pauseOverlayActive = false;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            roster?.PrepareNewGameSession();

            if (roster != null && roster.StarterPioneerSelected)
            {
                LoadIntoExpedition();
                return;
            }

            StarterPioneerSelectUI starterUi = StarterPioneerSelectUI.EnsureExists();
            if (starterUi != null)
            {
                starterUi.Show(LoadIntoExpedition);
                return;
            }

            LoadIntoExpedition();
        }

        /// <summary>
        /// Second Loading Genesis pass, then straight into gameplay. Replaces the old start-screen popup
        /// step so the flow is boot loader → menu → expedition loader → game.
        /// </summary>
        private void LoadIntoExpedition()
        {
            GameSession.SetPhase(GamePhase.StartPopup);
            HideMenuChrome();
            HideGameplayChromeForMenu();

            LoadingOverlayController.ShowForGameStart(BeginExpedition);
        }

        private void BeginExpedition()
        {
            GameStartPopup startFlow = ResolveStartPopup();
            if (startFlow != null)
            {
                // The popup no longer shows; it still owns the canonical "begin gameplay" sequence.
                startFlow.HidePopup();
                startFlow.OnStartGameClicked();
                return;
            }

            GameSession.MarkStarted();
            SetGameWorldPaused(false);
            ReleaseGameplayInputCapture();
            RefreshGameplayCamera();
            GameAudioManager.Instance?.StartGameplayMusic();
            MainCanvasFlow.Refresh();
        }

        private void ResumeFromPause()
        {
            pauseOverlayActive = false;
            ClearMenuMessage();

            HideMenuChrome();

            SetGameWorldPaused(false);
            ReleaseGameplayInputCapture();
            MainCanvasFlow.Refresh();
            RefreshMenuButtonStates();
        }

        private void ReleaseGameplayInputCapture()
        {
            GameplayInputRecovery.ReleaseAllInputCapture();
        }

        private void OpenSettings()
        {
            HideHotbars();
            settingsPanel?.Open();
        }

        private void OpenLoad()
        {
            if (!GameSaveSystem.HasAnySaveFile)
            {
                ShowMenuMessage("No save files found.");
                return;
            }

            HideHotbars();
            saveSlotsPanel?.Open(SaveSlotsPanelController.Mode.Load);
        }

        private void OpenSave()
        {
            if (!GameSession.HasStarted)
            {
                ShowMenuMessage("Start a game before saving.");
                return;
            }

            HideHotbars();
            StartCoroutine(OpenSaveSlotsWithScreenshot());
        }

        private IEnumerator OpenSaveSlotsWithScreenshot()
        {
            ClearPendingSaveScreenshot();

            bool restoreBackground = menuBackground != null && menuBackground.activeSelf;
            bool restorePanel = menuPanel != null && menuPanel.activeSelf;

            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);
            saveSlotsPanel?.Close();

            yield return new WaitForEndOfFrame();
            pendingSaveScreenshot = SaveSlotScreenshotUtility.CaptureGameplayScreenshot();

            if (restoreBackground && menuBackground != null)
                menuBackground.SetActive(true);
            if (restorePanel && menuPanel != null)
                menuPanel.SetActive(true);

            saveSlotsPanel?.Open(SaveSlotsPanelController.Mode.Save);
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMenuMessage(string message)
        {
            if (menuMessageLabel == null)
                return;

            menuMessageLabel.text = message;
            menuMessageLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void ClearMenuMessage()
        {
            ShowMenuMessage(string.Empty);
        }

        private void HideGameplayUi()
        {
            Transform canvasRoot = transform;

            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                GameObject child = canvasRoot.GetChild(i).gameObject;
                if (IsMenuProtectedElement(child))
                    continue;

                if (ShouldStayClosedAfterRestore(child))
                {
                    child.SetActive(false);
                    continue;
                }

                if (!child.activeSelf)
                    continue;

                if (!hiddenCanvasRoots.Contains(child))
                    hiddenCanvasRoots.Add(child);

                child.SetActive(false);
            }

            PetUI petUi = FindAnyObjectByType<PetUI>();
            petUi?.HideForStartScreen();
            HideGameplayChromeForMenu();
        }

        private static bool ShouldStayClosedAfterRestore(GameObject candidate)
        {
            return candidate.name == "InventoryPanel";
        }

        private void RestoreGameplayUi()
        {
            foreach (GameObject root in hiddenCanvasRoots)
            {
                if (root != null)
                    root.SetActive(true);
            }

            hiddenCanvasRoots.Clear();
            InventoryUI.CloseAnyOpenInventory();
            JournalPanelUI.CloseAnyOpenJournal();
            GameplayHudVisibility.SetGameplayHudVisible(true);
            FindAnyObjectByType<UIManager>()?.SetCurrencyHudVisible(false);
        }

        public static void RestoreGameplayUiFromMenu()
        {
            MainMenuController controller = FindAnyObjectByType<MainMenuController>();
            controller?.RestoreGameplayUi();
        }

        private bool IsMenuProtectedElement(GameObject candidate)
        {
            return candidate == menuPanel ||
                   candidate == menuBackground ||
                   candidate.name == "MainMenuPanel" ||
                   candidate.name == "MainMenuBackground" ||
                   candidate.name == "SettingsPanel" ||
                   candidate.name == "SaveSlotsPanel" ||
                   candidate.name == "StartPopupPanel" ||
                   candidate.name == "StartScreenBlackBackground" ||
                   candidate.name == "ExposureZoneEntryBanner" ||
                   candidate.name == "PetPanel";
        }

        private void SetGameWorldPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            SetGameplayPaused(paused);
            SetSurvivalSimulationPaused(paused);
        }

        private void SetGameplayPaused(bool paused)
        {
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;

            if (playerInput != null)
                playerInput.enabled = !paused;

            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
                playerController.SetGameplayPaused(paused);
        }

        private static void RefreshGameplayCamera()
        {
            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
                playerController.RefreshCameraFollow();
        }

        private static void SetSurvivalSimulationPaused(bool paused)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            player?.GetComponent<SurvivalStats>()?.SetSimulationPaused(paused);
        }
    }
}
