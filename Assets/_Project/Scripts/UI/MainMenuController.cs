using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Managers;
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
        private const float MenuScale = 1.5f;

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
        private Button saveGameButton;
        private Button loadGameButton;
        private Button settingsButton;
        private Button exitButton;
        private Toggle mapSystemToggle;
        private TextMeshProUGUI menuMessageLabel;
        private bool pauseOverlayActive;
        private Texture2D pendingSaveScreenshot;

        private void Awake()
        {
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

            if (pauseOverlayActive)
                ResumeFromPause();
            else
                ShowPauseMenu();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        public static void EnsureExists()
        {
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

            menuBackground = MenuUiBuilder.CreateFullScreenPanel(canvasRoot, "MainMenuBackground", Color.black, blockRaycasts: false);

            menuPanel = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            menuPanel.transform.SetParent(canvasRoot, false);

            Image panelImage = menuPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = new Color(0.05f, 0.05f, 0.07f, 0.94f);
            panelImage.raycastTarget = true;

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420f * MenuScale, 900f);

            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                Mathf.RoundToInt(28f * MenuScale),
                Mathf.RoundToInt(28f * MenuScale),
                Mathf.RoundToInt(28f * MenuScale),
                Mathf.RoundToInt(28f * MenuScale));
            layout.spacing = Mathf.RoundToInt(28f * MenuScale);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            MenuUiBuilder.CreateTitle(menuPanel.transform, "Survival Pioneer", 42f * MenuScale);

            TextMeshProUGUI subtitle = MenuUiBuilder.CreateTitle(menuPanel.transform, "Pi Network Survival", 18f * MenuScale);
            subtitle.color = new Color(0.75f, 0.75f, 0.75f, 1f);

            mapSystemToggle = MenuUiBuilder.CreateToggleRow(menuPanel.transform, "Map System", GameSettings.MapSystemEnabled);
            mapSystemToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMapSystemEnabled(value);
                GameSettings.Save();
                MapUI.ApplyMapSystemEnabled(value);
            });

            Vector2 buttonSize = new Vector2(280f * MenuScale, 52f * MenuScale);
            float buttonFontSize = 24f * MenuScale;

            newGameButton = MenuUiBuilder.CreateButton(menuPanel.transform, "New Game", buttonSize, buttonFontSize);
            resumeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Resume", buttonSize, buttonFontSize);
            saveGameButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Save Game", buttonSize, buttonFontSize);
            loadGameButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Load Game", buttonSize, buttonFontSize);
            settingsButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Settings", buttonSize, buttonFontSize);
            exitButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Exit", buttonSize, buttonFontSize);

            newGameButton.onClick.AddListener(StartNewGame);
            resumeButton.onClick.AddListener(ResumeFromPause);
            saveGameButton.onClick.AddListener(SaveGame);
            loadGameButton.onClick.AddListener(LoadGame);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(ExitGame);

            resumeButton.gameObject.SetActive(false);

            menuMessageLabel = MenuUiBuilder.CreateTitle(menuPanel.transform, string.Empty, 16f * MenuScale);
            menuMessageLabel.color = new Color(0.85f, 0.68f, 0.18f, 1f);
            menuMessageLabel.gameObject.SetActive(false);

            settingsPanel = gameObject.AddComponent<SettingsPanelController>();
            settingsPanel.Build(canvasRoot);

            saveSlotsPanel = gameObject.AddComponent<SaveSlotsPanelController>();
            saveSlotsPanel.Build(canvasRoot, this);

            RefreshMenuButtonStates();
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
            ClearMenuMessage();

            ResolveStartPopup()?.HidePopup();

            if (mapSystemToggle != null)
                mapSystemToggle.SetIsOnWithoutNotify(GameSettings.MapSystemEnabled);

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
            ClearMenuMessage();
            if (mapSystemToggle != null)
                mapSystemToggle.SetIsOnWithoutNotify(GameSettings.MapSystemEnabled);
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
            if (saveGameButton != null)
                saveGameButton.interactable = GameSession.HasStarted;
            if (loadGameButton != null)
                loadGameButton.interactable = GameSaveSystem.HasAnySaveFile;
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
                if (loadGameButton != null)
                    loadGameButton.interactable = true;
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
            RefreshMenuButtonStates();
        }

        private void OpenSettings()
        {
            settingsPanel?.Open();
        }

        private void SaveGame()
        {
            if (!GameSession.HasStarted)
            {
                ShowMenuMessage("Start a game before saving.");
                return;
            }

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

        private void LoadGame()
        {
            if (!GameSaveSystem.HasAnySaveFile)
            {
                ShowMenuMessage("No save files found.");
                return;
            }

            saveSlotsPanel?.Open(SaveSlotsPanelController.Mode.Load);
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
            SurvivalStats stats = FindAnyObjectByType<SurvivalStats>();
            stats?.SetSimulationPaused(paused);
        }
    }
}
