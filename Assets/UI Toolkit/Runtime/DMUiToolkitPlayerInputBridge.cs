using System;
using System.Collections.Generic;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Project.UI
{
    /// <summary>
    /// Direct InputAction subscriptions for journal / pause while UITK has focus.
    /// KBM direct hotkeys: Journal (J) and Inventory (I) only — other tabs via UI or gamepad nav.
    /// </summary>
    [DefaultExecutionOrder(-240)]
    [DisallowMultipleComponent]
    public class DMUiToolkitPlayerInputBridge : MonoBehaviour
    {
        private readonly List<(InputAction action, Action<InputAction.CallbackContext> handler)> bindings =
            new List<(InputAction, Action<InputAction.CallbackContext>)>(16);
        private PlayerInput playerInput;
        private bool bound;
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
            if (bootstrap.GetComponent<DMUiToolkitPlayerInputBridge>() == null)
                bootstrap.gameObject.AddComponent<DMUiToolkitPlayerInputBridge>();
        }
        private void OnEnable()
        {
            GameSession.GameStarted += TryBind;
            if (GameSession.HasStarted)
                TryBind();
        }
        private void OnDisable()
        {
            GameSession.GameStarted -= TryBind;
            UnbindAll();
        }
        /// <summary>
        /// 2026-11-04f: called after every Mars &lt;-&gt; IO switch. Both scenes are resident and each
        /// has its own PlayerInput, so the Journal/Inventory/Cancel subscriptions must move to the
        /// rig that actually owns the world being shown (otherwise J and I do nothing in the new
        /// world because they are still bound to the hidden rig's disabled action instance).
        /// </summary>
        public static void RebindForLivePlayer()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;
            if (!DMUiToolkitBootstrap.IsRootActive)
                return;

            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap == null)
                return;

            DMUiToolkitPlayerInputBridge bridge = bootstrap.GetComponent<DMUiToolkitPlayerInputBridge>();
            if (bridge == null)
                bridge = bootstrap.gameObject.AddComponent<DMUiToolkitPlayerInputBridge>();

            bridge.UnbindAll();
            bridge.TryBind();
        }

        private void TryBind()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            // 2026-11-04f: prefer the live rig. FindAnyObjectByType could return the other world's
            // resident PlayerInput, whose action map is disabled while that world is hidden.
            PlayerInput live = PlayerLocator.FindLivePlayerInput();
            if (live == null)
                live = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            if (live == null || live.actions == null)
                return;

            if (bound && live == playerInput)
                return;
            if (bound)
                UnbindAll();

            playerInput = live;
            BindPlayer("Journal", ctx =>
            {
                if (ctx.performed)
                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true);
            });
            BindPlayer("Inventory", ctx =>
            {
                if (!ctx.performed)
                    return;
                if (DMInputSchemeRouter.IsGamepadScheme)
                    return;
                DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory);
            });
            BindUi("Cancel", ctx =>
            {
                if (!ctx.performed)
                    return;
                // Menus/popups: B = back. Gameplay: B is Dodge/Dash (Cancel must not steal it).
                if (GameplayKeyboardShortcuts.IsModalUiOpenForCancel())
                    GameplayKeyboardShortcuts.HandleUiCancelBack();
            });
            bound = true;
        }
        private void BindPlayer(string actionName, Action<InputAction.CallbackContext> handler)
        {
            InputAction action = playerInput.actions.FindAction(actionName, false);
            if (action == null)
                return;
            action.performed -= handler;
            action.performed += handler;
            bindings.Add((action, handler));
        }
        private void BindUi(string actionName, Action<InputAction.CallbackContext> handler)
        {
            InputAction action = playerInput.actions.FindAction(actionName, false);
            if (action == null)
                return;
            action.performed -= handler;
            action.performed += handler;
            bindings.Add((action, handler));
        }
        private void UnbindAll()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                (InputAction action, Action<InputAction.CallbackContext> handler) entry = bindings[i];
                if (entry.action != null)
                    entry.action.performed -= entry.handler;
            }
            bindings.Clear();
            bound = false;
        }
    }
}
