using Project.Core;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK GAME OVER overlay with Retry / End Game. Forwards from UIManager.ShowDeathPopup.
    /// </summary>
    [DefaultExecutionOrder(-376)]
    [DisallowMultipleComponent]
    public class DMUiToolkitDeath : MonoBehaviour
    {
        private static DMUiToolkitDeath instance;

        private UIDocument document;
        private VisualElement root;
        private Button retryButton;
        private Button exitButton;
        private bool bound;
        private bool wired;
        private bool open;

        public static bool IsOpen => instance != null && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitDeath EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DeathName,
                DMUiToolkitOverlayDocument.DeathUxml,
                DMUiToolkitOverlayDocument.DeathUss,
                DMUiToolkitOverlayDocument.DeathSort);
            if (doc == null)
                return null;

            DMUiToolkitDeath host = doc.GetComponent<DMUiToolkitDeath>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDeath>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow()
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitDeath host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal();
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

            root = tree.Q<VisualElement>("death-root") ?? tree;
            retryButton = tree.Q<Button>("death-retry");
            exitButton = tree.Q<Button>("death-exit");
            Wire();
            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (retryButton != null)
                retryButton.clicked += HandleRetry;
            if (exitButton != null)
                exitButton.clicked += HandleExit;
            wired = true;
        }

        private void ShowInternal()
        {
            BindTree();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            open = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                pc.SetInventoryOpen(true);
        }

        private void HideInternal()
        {
            open = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                pc.SetInventoryOpen(false);
        }

        private void HandleRetry()
        {
            HideInternal();
            UIManager ui = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui != null)
                ui.RespawnPlayer();
        }

        private void HandleExit()
        {
            HideInternal();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving || instance == null || !instance.open)
                return;

            UIManager ui = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
                return;

            Transform death = ui.transform.Find("DeathPopupPanel");
            if (death != null && death.gameObject.activeSelf)
                death.gameObject.SetActive(false);
        }
    }
}
