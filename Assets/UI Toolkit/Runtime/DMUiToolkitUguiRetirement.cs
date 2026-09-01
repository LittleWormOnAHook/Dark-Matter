using Project.Core;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Disables legacy uGUI canvas rendering/raycasts once UITK owns the UI stack.
    /// Logic MonoBehaviours on MainCanvas remain for data bridges until fully migrated.
    /// </summary>
    [DefaultExecutionOrder(-440)]
    [DisallowMultipleComponent]
    public class DMUiToolkitUguiRetirement : MonoBehaviour
    {
        private static bool retired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            if (!DMUiToolkitBootstrap.EnsureExists())
                return;

            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap == null)
                return;

            if (bootstrap.GetComponent<DMUiToolkitUguiRetirement>() == null)
                bootstrap.gameObject.AddComponent<DMUiToolkitUguiRetirement>();
        }

        private void OnEnable()
        {
            GameSession.GameStarted -= TryRetireUguiCanvases;
            GameSession.GameStarted += TryRetireUguiCanvases;
            if (GameSession.HasStarted)
                TryRetireUguiCanvases();
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= TryRetireUguiCanvases;
        }

        internal void TryRetireUguiCanvases()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            RetireCanvas(MainMenuController.ResolveMainCanvas());
            RetireNamedCanvas("OpticsOverlayCanvas");

            GameObject legacyToolkit = GameObject.Find("DMMainCanvas");
            if (legacyToolkit != null)
                legacyToolkit.SetActive(false);

            retired = true;
        }

        private static void RetireCanvas(Canvas canvas)
        {
            if (canvas == null)
                return;

            MainCanvasFlow.SanitizeCanvasHost(canvas);

            if (canvas.TryGetComponent(out UnityEngine.UI.GraphicRaycaster raycaster))
                raycaster.enabled = false;

            canvas.enabled = false;
        }

        private static void RetireNamedCanvas(string canvasName)
        {
            GameObject named = GameObject.Find(canvasName);
            if (named != null && named.TryGetComponent(out Canvas canvas))
                RetireCanvas(canvas);
        }

        public static bool IsRetired => retired;
    }
}
