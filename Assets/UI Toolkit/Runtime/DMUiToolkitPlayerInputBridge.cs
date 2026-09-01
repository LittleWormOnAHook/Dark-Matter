using System;

using System.Collections.Generic;

using Project.Core;

using UnityEngine;

using UnityEngine.InputSystem;



namespace Project.UI

{

    /// <summary>

    /// Direct InputAction subscriptions for journal, pause, and hotbar keys while UITK has focus.

    /// Routes journal tabs through <see cref="DMUiToolkitMenus.TryToggleJournalTab"/> when UITK drives.

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



        private void TryBind()

        {

            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)

                return;



            if (bound)

                return;



            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);

            if (playerInput == null || playerInput.actions == null)

                return;



            BindPlayer("Journal", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true);

            });

            BindPlayer("Inventory", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory);

            });

            BindPlayer("Map", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Map);

            });

            BindPlayer("Craft", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes);

            });

            BindPlayer("Blueprints", ctx =>

            {

                if (ctx.performed && ctx.control != null && ctx.control.device is not Keyboard)

                    DMUiToolkitMenus.TrySwitchJournalTab(JournalWindowId.Recipes);

            });

            BindPlayer("Pioneers", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pioneers);

            });

            BindPlayer("Skills", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Skills);

            });

            BindPlayer("Echoes", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Echoes);

            });

            BindPlayer("Achievements", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Achievements);

            });

            BindPlayer("Character", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Character);

            });

            BindPlayer("Pets", ctx =>

            {

                if (ctx.performed)

                    DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pet);

            });

            BindUi("Cancel", ctx =>

            {

                if (ctx.performed)

                    GameplayKeyboardShortcuts.HandleEscapePressed();

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


