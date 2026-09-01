using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK start-game briefing. ShowPopup / HidePopup / OnStartGameClicked still live on GameStartPopup.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-373)]
    [DisallowMultipleComponent]
    public class DMUiToolkitGameStart : MonoBehaviour
    {
        private static DMUiToolkitGameStart instance;

        private UIDocument document;
        private VisualElement root;
        private Label bodyLabel;
        private Button startButton;
        private bool bound;
        private bool wired;
        private bool open;
        private GameStartPopup source;

        public static bool IsOpen => instance != null && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitGameStart EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.GameStartName,
                DMUiToolkitOverlayDocument.GameStartUxml,
                DMUiToolkitOverlayDocument.GameStartUss,
                DMUiToolkitOverlayDocument.GameStartSort);
            if (doc == null)
                return null;

            DMUiToolkitGameStart host = doc.GetComponent<DMUiToolkitGameStart>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitGameStart>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(GameStartPopup popup)
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return false;

            DMUiToolkitGameStart host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal(popup);
            return true;
        }

        public static void Hide()
        {
            instance?.HideInternal();
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

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            if (open)
                HideUgui();
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

            root = tree.Q<VisualElement>("gamestart-root") ?? tree;
            bodyLabel = tree.Q<Label>("gamestart-body");
            startButton = tree.Q<Button>("gamestart-start");
            Wire();
            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (startButton != null)
                startButton.clicked += HandleStart;
            wired = true;
        }

        private void ShowInternal(GameStartPopup popup)
        {
            BindTree();
            source = popup;
            if (bodyLabel != null && popup != null)
                bodyLabel.text = popup.messageText ?? string.Empty;

            DMUiToolkitOverlayDocument.SetShown(root, true);
            open = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void HideInternal()
        {
            open = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void HandleStart()
        {
            GameStartPopup popup = source != null
                ? source
                : Object.FindAnyObjectByType<GameStartPopup>(FindObjectsInactive.Include);
            HideInternal();
            popup?.OnStartGameClicked();
        }

        private static void HideUgui()
        {
            GameStartPopup popup = Object.FindAnyObjectByType<GameStartPopup>(FindObjectsInactive.Include);
            if (popup == null)
                return;

            if (popup.popupPanel != null && popup.popupPanel.activeSelf)
                popup.popupPanel.SetActive(false);
            if (popup.screenOverlay != null && popup.screenOverlay.activeSelf)
                popup.screenOverlay.SetActive(false);
        }
    }
}
