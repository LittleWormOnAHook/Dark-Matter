using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Audio;
using Project.Managers;
using Project.Player;
using Project.Survival;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    public class GameStartPopup : MonoBehaviour
    {
        public enum PopupContentType
        {
            MessageOnly,
            ImageOnly,
            Both
        }

        [Header("Settings")]
        public PopupContentType contentType = PopupContentType.Both;

        [TextArea(3, 10)]
        public string messageText =
            "Welcome, Pioneer!\n\nSurvival is key. Gather resources, watch your stats, and earn Pi rewards.\n\n[WASD] Move  |  [E] Interact  |  [I] Inventory\n[1-2] Weapons  |  [N] Scanner  |  [B] Binoculars\n[RMB] Block / Optics  |  [LMB] Attack  |  [M] Map  |  [Scroll] Zoom";

        public Sprite imageSprite;
        public bool showOnStart = false;

        [Header("UI References")]
        public GameObject popupPanel;
        public TextMeshProUGUI textDisplay;
        public Image imageDisplay;

        [Tooltip("Start Game button — gameplay begins only when this is pressed.")]
        public Button closeButton;

        [Header("Optional")]
        [Tooltip("Full-screen black backdrop. Created automatically if left empty.")]
        public GameObject screenOverlay;

        private PlayerInput playerInput;
        private TextMeshProUGUI buttonTextComponent;
        private string originalButtonText = "START GAME";
        private readonly List<GameObject> hiddenCanvasRoots = new List<GameObject>();

        private void Awake()
        {
            EnsureScreenOverlay();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnStartGameClicked);
                closeButton.onClick.AddListener(OnStartGameClicked);

                buttonTextComponent = closeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonTextComponent != null)
                    originalButtonText = buttonTextComponent.text;
            }

            ApplyPopupFonts();

            playerInput = FindAnyObjectByType<PlayerInput>();
            MainMenuController.EnsureExists();

            if (FindAnyObjectByType<MainMenuController>() == null && showOnStart)
                ShowPopup();
        }

        private void Start()
        {
            if (showOnStart)
                StartCoroutine(RefreshStartScreenUiNextFrame());
            else if (popupPanel != null && GameSession.Phase == GamePhase.MainMenu)
                popupPanel.SetActive(false);
        }

        public void ShowPopup()
        {
            EnsureScreenOverlay();
            GameSession.SetPhase(GamePhase.StartPopup);
            HideOtherCanvasUi();
            HideRuntimeStartScreenUi();

            if (screenOverlay != null)
            {
                screenOverlay.SetActive(true);
                screenOverlay.transform.SetAsLastSibling();
            }

            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                popupPanel.transform.SetAsLastSibling();
            }

            if (textDisplay != null)
            {
                textDisplay.text = messageText;
                textDisplay.gameObject.SetActive(
                    contentType == PopupContentType.MessageOnly || contentType == PopupContentType.Both);
            }

            if (imageDisplay != null)
            {
                if (imageSprite != null)
                {
                    imageDisplay.sprite = imageSprite;
                    imageDisplay.gameObject.SetActive(
                        contentType == PopupContentType.ImageOnly || contentType == PopupContentType.Both);
                }
                else
                {
                    imageDisplay.gameObject.SetActive(false);
                }
            }

            if (buttonTextComponent != null)
                buttonTextComponent.text = originalButtonText;

            SetGameplayPaused(true);
        }

        private void ApplyPopupFonts()
        {
            TmpUiHelper.ApplyDefaultFont(textDisplay);
            TmpUiHelper.ApplyDefaultFont(buttonTextComponent);
        }

        public void HidePopup()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);

            if (screenOverlay != null)
                screenOverlay.SetActive(false);
        }

        public void OnStartGameClicked()
        {
            if (GameSession.HasStarted)
                return;

            GameSession.MarkStarted();

            if (popupPanel != null)
                popupPanel.SetActive(false);

            if (screenOverlay != null)
                screenOverlay.SetActive(false);

            RestoreHiddenCanvasUi();
            MainMenuController.RestoreGameplayUiFromMenu();
            HideRuntimeStartScreenUi();
            Time.timeScale = 1f;
            SetGameplayPaused(false);
            RefreshGameplayCamera();
            GameAudioManager.Instance?.StartGameplayMusic();

            SurvivalStats survivalStats = FindAnyObjectByType<SurvivalStats>();
            survivalStats?.ResetForNewGame();
            survivalStats?.SetSimulationPaused(false);

            FindAnyObjectByType<UIManager>()?.SyncSurvivalBars();

            SimpleGameManager.Instance?.BeginNewGameSession();
        }

        private IEnumerator RefreshStartScreenUiNextFrame()
        {
            yield return null;
            HideOtherCanvasUi();
            HideRuntimeStartScreenUi();

            if (screenOverlay != null)
                screenOverlay.transform.SetAsLastSibling();

            if (popupPanel != null)
                popupPanel.transform.SetAsLastSibling();
        }

        private void EnsureScreenOverlay()
        {
            if (screenOverlay != null || popupPanel == null)
                return;

            Transform canvasRoot = popupPanel.transform.parent;
            if (canvasRoot == null)
                return;

            screenOverlay = new GameObject(
                "StartScreenBlackBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            screenOverlay.transform.SetParent(canvasRoot, false);

            RectTransform rect = screenOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = screenOverlay.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            screenOverlay.SetActive(false);
        }

        private void HideOtherCanvasUi()
        {
            if (popupPanel == null)
                return;

            Transform canvasRoot = popupPanel.transform.parent;
            if (canvasRoot == null)
                return;

            for (int i = 0; i < canvasRoot.childCount; i++)
            {
                GameObject childObject = canvasRoot.GetChild(i).gameObject;

                if (IsStartScreenElement(childObject))
                    continue;

                if (!childObject.activeSelf)
                    continue;

                if (ShouldStayClosedAfterRestore(childObject))
                {
                    childObject.SetActive(false);
                    continue;
                }

                if (!hiddenCanvasRoots.Contains(childObject))
                    hiddenCanvasRoots.Add(childObject);

                childObject.SetActive(false);
            }
        }

        private static bool ShouldStayClosedAfterRestore(GameObject candidate)
        {
            return candidate.name == "InventoryPanel";
        }

        private void HideRuntimeStartScreenUi()
        {
            PetUI petUi = FindAnyObjectByType<PetUI>();
            if (petUi != null)
                petUi.HideForStartScreen();

            GameObject petPanel = GameObject.Find("PetPanel");
            if (petPanel != null && petPanel.activeSelf)
                petPanel.SetActive(false);
        }

        private bool IsStartScreenElement(GameObject candidate)
        {
            if (candidate == popupPanel || candidate == screenOverlay)
                return true;

            return candidate.name == "PetPanel" ||
                   candidate.name == "MainMenuPanel" ||
                   candidate.name == "MainMenuBackground" ||
                   candidate.name == "SettingsPanel" ||
                   candidate.name == "SaveSlotsPanel";
        }

        private void RestoreHiddenCanvasUi()
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

        private void RefreshGameplayCamera()
        {
            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
                playerController.RefreshCameraFollow();
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

            CameraController cameraController = FindAnyObjectByType<CameraController>();
            if (cameraController != null)
                cameraController.SetInventoryOpen(paused);
        }
    }
}
