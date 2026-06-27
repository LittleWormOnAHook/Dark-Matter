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
        private static readonly Color MenuBackgroundColor = new Color(0.039f, 0.055f, 0.078f, 1f);

        [SerializeField] private bool buildOnAwake = true;

        private GameObject menuPanel;
        private GameObject menuBackground;
        private SettingsPanelController settingsPanel;
        private SaveSlotsPanelController saveSlotsPanel;
        private MainMenuWalletPanelController walletPanel;
        private MainMenuWalletPreviewWidget walletPreview;
        private MainMenuEnvironmentStatusBar environmentStatusBar;
        private GameStartPopup gameStartPopup;
        private PlayerInput playerInput;
        private readonly List<GameObject> hiddenCanvasRoots = new List<GameObject>();

        private Button newGameButton;
        private Button resumeButton;
        private Button saveLoadButton;
        private Button walletButton;
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
            ShowMainMenu();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || Keyboard.current == null)
                return;

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (settingsPanel != null && settingsPanel.IsOpen)
                return;

            if (saveSlotsPanel != null && saveSlotsPanel.IsOpen)
                return;

            if (walletPanel != null && walletPanel.IsSwapPanelOpen)
                return;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
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

            if (FindAnyObjectByType<MainMenuController>() != null)
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
                if (popupCanvas != null)
                    return popupCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindAnyObjectByType<Canvas>();
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

            walletPanel = gameObject.AddComponent<MainMenuWalletPanelController>();
            walletPanel.Build(canvasRoot);

            walletPreview = menuPanel.AddComponent<MainMenuWalletPreviewWidget>();
            walletPreview.Build(menuPanel.transform);

            environmentStatusBar = menuPanel.AddComponent<MainMenuEnvironmentStatusBar>();
            environmentStatusBar.Build(menuPanel.transform);

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

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(titleBlock.transform, "PIONEER SURVIVORS 2160", 34f * MenuScale);
            title.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI subtitle = MenuUiBuilder.CreateTitle(titleBlock.transform, "IO // JUPITER SYSTEM", 16f * MenuScale);
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            subtitle.color = new Color(0.62f, 0.72f, 0.82f, 0.95f);
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
            walletButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Wallet", buttonSize, buttonFontSize);
            saveLoadButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Save / Load", buttonSize, buttonFontSize);
            settingsButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Settings", buttonSize, buttonFontSize);
            exitButton = MenuUiBuilder.CreateTiltedMenuButton(column.transform, "Quit", buttonSize, buttonFontSize);

            resumeButton.onClick.AddListener(ResumeFromPause);
            newGameButton.onClick.AddListener(StartNewGame);
            walletButton.onClick.AddListener(OpenWallet);
            saveLoadButton.onClick.AddListener(OpenSaveLoad);
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
            label.color = new Color(0.85f, 0.68f, 0.18f, 1f);
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
            label.color = new Color(0.55f, 0.62f, 0.72f, 0.85f);
            label.alignment = TextAlignmentOptions.BottomLeft;
        }

        public void ShowMainMenu()
        {
            pauseOverlayActive = false;
            GameSession.SetPhase(GamePhase.MainMenu);
            HideGameplayUi();

            if (menuBackground != null)
                menuBackground.SetActive(true);
            if (menuPanel != null)
                menuPanel.SetActive(true);

            settingsPanel?.Close();
            saveSlotsPanel?.Close();
            walletPanel?.CloseSwapPanel();
            ClearMenuMessage();

            ResolveStartPopup()?.HidePopup();

            walletPreview?.Refresh();
            FindAnyObjectByType<UIManager>()?.SetCurrencyHudVisible(false);

            RefreshMenuButtonStates();
            SetGameWorldPaused(true);
            BringMenuToFront();
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

            settingsPanel?.Close();
            saveSlotsPanel?.Close();
            walletPanel?.CloseSwapPanel();
            ClearMenuMessage();
            walletPreview?.Refresh();
            FindAnyObjectByType<UIManager>()?.SetCurrencyHudVisible(false);
            RefreshMenuButtonStates();
            SetGameWorldPaused(true);
            BringMenuToFront();
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
            if (saveLoadButton != null)
                saveLoadButton.interactable = pauseOverlayActive || GameSaveSystem.HasAnySaveFile;
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

            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);

            GameSession.MarkStarted();
            RestoreGameplayUi();
            SetGameWorldPaused(false);
            ReleaseGameplayInputCapture();
            RefreshGameplayCamera();
            GameAudioManager.Instance?.StartGameplayMusic();
            UnityEngine.Object.FindAnyObjectByType<UIManager>()?.RefreshSurvivalDisplay();
            RefreshMenuButtonStates();
        }

        private void StartNewGame()
        {
            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);

            pauseOverlayActive = false;

            StarterPioneerSelectUI starterUi = StarterPioneerSelectUI.EnsureExists();
            if (starterUi != null)
            {
                starterUi.Show(() =>
                {
                    GameSession.SetPhase(GamePhase.StartPopup);
                    ResolveStartPopup()?.ShowPopup();
                });
                return;
            }

            PioneerRosterManager.EnsureExists()?.PrepareNewGameSession();
            GameSession.SetPhase(GamePhase.StartPopup);
            ResolveStartPopup()?.ShowPopup();
        }

        private void ResumeFromPause()
        {
            pauseOverlayActive = false;
            ClearMenuMessage();

            if (menuBackground != null)
                menuBackground.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);

            SetGameWorldPaused(false);
            ReleaseGameplayInputCapture();
            RefreshMenuButtonStates();
        }

        private void ReleaseGameplayInputCapture()
        {
            GameplayInputRecovery.ReleaseAllInputCapture();
        }

        private void OpenSettings()
        {
            settingsPanel?.Open();
        }

        private void OpenWallet()
        {
            walletPanel?.OpenWalletPanel();
        }

        private void OpenSaveLoad()
        {
            if (pauseOverlayActive && GameSession.HasStarted)
            {
                StartCoroutine(OpenSaveSlotsWithScreenshot());
                return;
            }

            if (!GameSaveSystem.HasAnySaveFile)
            {
                ShowMenuMessage("No save files found.");
                return;
            }

            saveSlotsPanel?.Open(SaveSlotsPanelController.Mode.Load);
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
            hiddenCanvasRoots.Clear();
            Transform canvasRoot = transform;

            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                GameObject child = canvasRoot.GetChild(i).gameObject;
                if (IsMenuProtectedElement(child))
                    continue;

                if (!child.activeSelf)
                    continue;

                if (ShouldStayClosedAfterRestore(child))
                {
                    child.SetActive(false);
                    continue;
                }

                hiddenCanvasRoots.Add(child);
                child.SetActive(false);
            }

            PetUI petUi = FindAnyObjectByType<PetUI>();
            petUi?.HideForStartScreen();
            ToolBarUI.ApplyGameplayVisibility();
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
                   candidate.name == "SettingsPanel" ||
                   candidate.name == "SaveSlotsPanel" ||
                   candidate.name == "WalletSwapPanel" ||
                   candidate.name == "StartPopupPanel" ||
                   candidate.name == "StartScreenBlackBackground" ||
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
