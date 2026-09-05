using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Global keyboard router on UITK_Root  -  ESC, pause menu, journal, hotbar, toolbar.
    /// Replaces scattered uGUI Update() polling when UITK is enabled.
    /// </summary>
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public class DMUiToolkitInputHost : MonoBehaviour
    {
        private static readonly HashSet<VisualElement> RegisteredKeyRoots = new HashSet<VisualElement>();

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

            if (bootstrap.GetComponent<DMUiToolkitInputHost>() == null)
                bootstrap.gameObject.AddComponent<DMUiToolkitInputHost>();

            if (bootstrap.GetComponent<DMUiToolkitPlayerInputBridge>() == null)
                bootstrap.gameObject.AddComponent<DMUiToolkitPlayerInputBridge>();
        }

        private void Update()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            TryRegisterBootstrapRoots();

            GameplayKeyboardShortcuts.TryHandleDevPanel();
            GameplayKeyboardShortcuts.TryHandleEscapeAndPause();
            GameplayKeyboardShortcuts.TryHandleCinematicHudToggle();

            if (GameSession.HasStarted
                && !MainMenuController.BlocksGameplayHud
                && !DMUiToolkitLoadingOverlay.IsShowing
                && !DMUiToolkitMainMenu.IsVisible
                && !DMUiToolkitMenuPanels.IsAnySubPanelOpen)
            {
                GameplayKeyboardShortcuts.TryHandleAll();
            }
        }

        public static void RegisterKeyRoot(VisualElement root)
        {
            if (root == null || RegisteredKeyRoots.Contains(root))
                return;

            root.RegisterCallback<KeyDownEvent>(OnToolkitKeyDown, TrickleDown.TrickleDown);
            RegisteredKeyRoots.Add(root);
        }

        private static void TryRegisterBootstrapRoots()
        {
            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap == null)
                return;

            RegisterKeyRoot(bootstrap.ShellDocument != null ? bootstrap.ShellDocument.rootVisualElement : null);
            RegisterKeyRoot(bootstrap.HudDocument != null ? bootstrap.HudDocument.rootVisualElement : null);
        }

        internal static void OnToolkitKeyDown(KeyDownEvent evt)
        {
            if (!Application.isPlaying)
                return;

            if (evt.target is TextField or IntegerField or FloatField or DoubleField or LongField)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    GameplayKeyboardShortcuts.HandleEscapePressed();
                    evt.StopImmediatePropagation();
                    return;
                case KeyCode.C:
                    if (GameplayKeyboardShortcuts.TryHandleJournalKeyCode(KeyCode.C))
                        evt.StopImmediatePropagation();
                    return;
                case KeyCode.J:
                case KeyCode.I:
                case KeyCode.M:
                case KeyCode.K:
                case KeyCode.P:
                case KeyCode.U:
                case KeyCode.T:
                case KeyCode.L:
                case KeyCode.G:
                    if (GameplayKeyboardShortcuts.TryHandleJournalKeyCode(evt.keyCode))
                        evt.StopImmediatePropagation();
                    return;
                case KeyCode.Alpha1:
                case KeyCode.Alpha2:
                case KeyCode.Alpha3:
                case KeyCode.Alpha4:
                case KeyCode.Alpha5:
                case KeyCode.Alpha6:
                case KeyCode.Alpha7:
                case KeyCode.Alpha8:
                case KeyCode.Alpha9:
                case KeyCode.Alpha0:
                    if (GameplayKeyboardShortcuts.TryHandleHotbarKeyCode(evt.keyCode))
                        evt.StopImmediatePropagation();
                    break;
                case KeyCode.N:
                    if (GameplayKeyboardShortcuts.TryHandleToolbarKeyCode(KeyCode.N))
                        evt.StopImmediatePropagation();
                    break;
                case KeyCode.Tab:
                case KeyCode.X:
                    // B/N stay on Update poll (TryHandleToolbarHotkeys) to avoid double-toggle with KeyDown.
                    if (GameplayKeyboardShortcuts.TryHandleHotCrossKeyCode(evt.keyCode))
                        evt.StopImmediatePropagation();
                    break;
                case KeyCode.BackQuote:
                    GameplayKeyboardShortcuts.TryHandleCinematicHudToggle();
                    evt.StopImmediatePropagation();
                    break;
            }
        }
    }
}