using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK start-game briefing host. Never shown — flow is boot loader → menu → expedition loader → game.
    /// </summary>
    [DefaultExecutionOrder(-373)]
    [DisallowMultipleComponent]
    public class DMUiToolkitGameStart : MonoBehaviour
    {
        private static DMUiToolkitGameStart instance;

        private UIDocument document;
        private VisualElement root;
        private bool bound;

        public static bool IsOpen => false;

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

        /// <summary>
        /// Never show START GAME overlay. Auto-advance into the canonical begin sequence.
        /// </summary>
        public static bool TryShow(GameStartPopup popup)
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return false;

            EnsureHost();
            instance?.HideInternal();

            if (popup != null)
            {
                popup.HidePopup();
                popup.OnStartGameClicked();
            }

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
            HideInternal();
            bound = root != null;
        }

        private void HideInternal()
        {
            DMUiToolkitOverlayDocument.SetShown(root, false);
        }
    }
}
