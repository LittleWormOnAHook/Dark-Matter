using Project.Core;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK GAME OVER overlay with Retry / End Game. Forwards from UIManager.ShowDeathPopup.
    /// stamp: gameover-mouse-uitk 0905
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
        private bool open;
        private bool uguiHidden;

        public static bool IsOpen => instance != null && instance.open;

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
                return;

            if (open)
            {
                if (!uguiHidden)
                {
                    HideUgui();
                    uguiHidden = true;
                }

                // Ghost-pause / cursor-restore can re-lock after Show; keep Game Over clickable.
                EnsurePointerForOpenDeath();
            }
            else
            {
                uguiHidden = false;
            }
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
            if (root != null && root.pickingMode != PickingMode.Position)
                root.pickingMode = PickingMode.Position;

            Button nextRetry = tree.Q<Button>("death-retry");
            Button nextExit = tree.Q<Button>("death-exit");
            WireButtons(nextRetry, nextExit);

            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void WireButtons(Button nextRetry, Button nextExit)
        {
            if (retryButton != nextRetry)
            {
                if (retryButton != null)
                    retryButton.clicked -= HandleRetry;
                retryButton = nextRetry;
                if (retryButton != null)
                {
                    retryButton.clicked -= HandleRetry;
                    retryButton.clicked += HandleRetry;
                    if (retryButton.pickingMode != PickingMode.Position)
                        retryButton.pickingMode = PickingMode.Position;
                }
            }

            if (exitButton != nextExit)
            {
                if (exitButton != null)
                    exitButton.clicked -= HandleExit;
                exitButton = nextExit;
                if (exitButton != null)
                {
                    exitButton.clicked -= HandleExit;
                    exitButton.clicked += HandleExit;
                    if (exitButton.pickingMode != PickingMode.Position)
                        exitButton.pickingMode = PickingMode.Position;
                }
            }
        }

        private void ShowInternal()
        {
            BindTree();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            open = true;
            EnsurePointerForOpenDeath();

            if (retryButton != null)
                retryButton.Focus();
        }

        private void HideInternal()
        {
            if (!open && (root == null || root.resolvedStyle.display == DisplayStyle.None))
            {
                RestoreGameplayPointerFlags();
                return;
            }

            open = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            RestoreGameplayPointerFlags();
        }

        /// <summary>
        /// Unlock mouse, cancel gameplay cursor relock, and mark player UI-captured so
        /// RecoverGhostUiLocks / ApplyCursorState cannot steal clicks while Game Over is up.
        /// </summary>
        public static void EnsurePointerForOpenDeath()
        {
            if (!IsOpen)
                return;

            GameplayInputRecovery.CancelPendingCursorRestore();

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                // Reuse inventory-open as the existing "UI has the pointer" flag used by combat/look gates.
                if (!pc.IsInventoryOpen)
                    pc.SetInventoryOpen(true);
                else
                    pc.ApplyCursorState();
            }

            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
                cam.SetInventoryOpen(true);
        }

        private static void RestoreGameplayPointerFlags()
        {
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null && pc.IsInventoryOpen)
                pc.SetInventoryOpen(false);

            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
                cam.SetInventoryOpen(false);
        }

        private void HandleRetry()
        {
            if (!open)
                return;

            HideInternal();
            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui != null)
                ui.RespawnPlayer();
            else
            {
                // Fallback if UIManager missing: still try player respawn.
                GameObject player = PlayerLocator.FindPlayerObject();
                player?.GetComponent<PlayerDeathHandler>()?.Respawn();
            }
        }

        private void HandleExit()
        {
            if (!open)
                return;

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

            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
                return;

            Transform death = ui.transform.Find("DeathPopupPanel");
            if (death != null && death.gameObject.activeSelf)
                death.gameObject.SetActive(false);
        }
    }
}
