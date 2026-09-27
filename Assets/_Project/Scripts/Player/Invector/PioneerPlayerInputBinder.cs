using System.Collections;
using Invector.vCharacterController;
using Project.Building;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Player;
using Project.UI;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Routes Input System actions to Pioneer handlers at runtime.
    /// PlayerInput must use Invoke C Sharp Events (notification behavior 3) for onActionTriggered.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public class PioneerPlayerInputBinder : MonoBehaviour
    {
        private static UIManager cachedUiManager;

        private PlayerInput _playerInput;
        private PlayerController _playerController;
        private MeleeCombatController _melee;
        private RangedCombatController _ranged;
        private PioneerInvectorInputBridge _invectorInput;
        private Coroutine _activateRoutine;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _playerController = GetComponent<PlayerController>();
            _melee = GetComponent<MeleeCombatController>();
            _ranged = GetComponent<RangedCombatController>();
            _invectorInput = GetComponent<PioneerInvectorInputBridge>();

            if (_playerInput != null && _playerInput.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
                _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        }

        private void OnEnable()
        {
            GameSession.GameStarted += ScheduleEnsureGameplayInputActive;

            if (_playerInput != null)
                _playerInput.onActionTriggered += HandleAction;

            ScheduleEnsureGameplayInputActive();
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= ScheduleEnsureGameplayInputActive;

            if (_playerInput != null)
                _playerInput.onActionTriggered -= HandleAction;

            if (_activateRoutine != null)
            {
                StopCoroutine(_activateRoutine);
                _activateRoutine = null;
            }
        }

        private void ScheduleEnsureGameplayInputActive()
        {
            if (!isActiveAndEnabled)
                return;

            if (_activateRoutine != null)
                StopCoroutine(_activateRoutine);

            _activateRoutine = StartCoroutine(EnsureGameplayInputActiveWhenReady());
        }

        private IEnumerator EnsureGameplayInputActiveWhenReady()
        {
            // PioneerPlayerInputBinder runs before PlayerInput.OnEnable (execution order -250 vs 0).
            // Yield so PlayerInput finishes its own enable/activate pass before we touch ActivateInput.
            yield return null;

            _activateRoutine = null;
            EnsureGameplayInputActive();
        }

        private void EnsureGameplayInputActive()
        {
            if (_playerInput == null || !GameSession.HasStarted)
                return;

            if (MainMenuController.BlocksGameplayHud)
                return;

            if (_playerController != null && _playerController.IsGameplayPaused)
                return;

            if (!_playerInput.isActiveAndEnabled)
                return;

            if (_playerInput.inputIsActive)
                return;

            _playerInput.ActivateInput();
        }

        private void HandleAction(InputAction.CallbackContext context)
        {
            if (context.action == null || context.action.actionMap == null)
                return;

            if (context.action.actionMap.name != "Player")
                return;

            switch (context.action.name)
            {
                case "Move":
                    if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
                        PlayerVehicleState.ActiveCraft.OnMove(context);
                    else
                        _playerController?.OnMove(context);
                    break;
                case "Look":
                    if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
                        PlayerVehicleState.ActiveCraft.OnLook(context);
                    else
                        _playerController?.OnLook(context);
                    break;
                case "Use":
                    if (context.performed)
                        _playerController?.OnUse(context);
                    break;
                case "Jump":
                    _playerController?.OnJump(context);
                    break;
                case "Sprint":
                    if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
                        PlayerVehicleState.ActiveCraft.OnSprint(context);
                    else
                        _playerController?.OnSprint(context);
                    break;
                case "Crouch":
                    // B exits ammo menu; do not crouch on the same press.
                    if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                        break;
                    _playerController?.OnCrouch(context);
                    break;
                case "Attack":
                    if (DMBuildingMode.IsActive)
                        break;
                    if (context.performed &&
                        (_playerController == null || !_playerController.IsOpticsOpen))
                    {
                        if (_invectorInput != null && _invectorInput.BlocksWeaponFireForGrenade)
                            break;

                        if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
                            PlayerVehicleState.ActiveCraft.OnAttack();
                        else
                        {
                            _melee?.OnAttack(context);
                            _ranged?.OnAttack(context);
                        }
                    }
                    break;
                case "Block":
                    if (DMBuildingMode.IsActive)
                        break;
                    if (_invectorInput != null)
                        _invectorInput.OnBlock(context);
                    _melee?.OnBlock(context);
                    _ranged?.OnBlock(context);
                    break;
                case "SwitchWeapon":
                    // Hot Cross owns SwitchWeapon: tap cycles weapon focus, hold arms (GameplayKeyboardShortcuts).
                    break;
                case "Inventory":
                case "Map":
                case "Craft":
                case "Blueprints":
                case "Pioneers":
                case "Skills":
                case "Echoes":
                case "Achievements":
                case "Character":
                case "Pets":
                    // Unbound: open Journal (J / D-Pad Up), then navigate tabs with D-Pad / stick.
                    break;
                case "Journal":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true))
                            ResolveUiManager()?.OnToggleJournal(context);
                    }
                    break;
                case "Aim":
                    // ADS / aim — same physical as Block on pad (LT) and RMB; Block still routes melee block.
                    if (_invectorInput != null)
                        _invectorInput.OnBlock(context);
                    _ranged?.OnBlock(context);
                    break;
                case "AmmoCycle":
                    // Tap/hold owned by GameplayKeyboardShortcuts Hot Cross poll (reads this action).
                    break;
                case "Binoculars":
                    // Keyboard B is tap vs hold in GameplayKeyboardShortcuts.
                    // This action still opens binoculars for the gamepad binding.
                    if (context.performed
                        && (Keyboard.current == null || !Keyboard.current.bKey.isPressed))
                        GameplayKeyboardShortcuts.TryUseToolFromAction(ToolType.Binoculars);
                    break;
                case "Scanner":
                    if (context.performed)
                        GameplayKeyboardShortcuts.TryUseToolFromAction(ToolType.Scanner);
                    break;
                case "Jetpack":
                    // Boost hold is polled by DMJetpackInputBridge (Jump/Space/A + this action).
                    break;
                case "Reload":
                    // Hold/tap owned by WeaponModeSwitchController (reads this action).
                    break;
                case "Dodge":
                    if (context.performed)
                        TryDodgeRollFromInput();
                    break;
                case "Dash":
                    // 0925-rotate: Alt + scroll rotates build pieces, so keyboard Alt never dashes in build mode.
                    if (context.performed && !(DMBuildingMode.IsActive && context.control != null && context.control.device is Keyboard))
                        TryDashFromInput();
                    break;
                case "Pause":
                    if (context.performed)
                        GameplayKeyboardShortcuts.HandleEscapePressed();
                    break;
                case "MinimapZoomIn":
                    if (context.performed)
                        GameplayKeyboardShortcuts.TryMinimapZoom(zoomIn: true);
                    break;
                case "MinimapZoomOut":
                    if (context.performed)
                        GameplayKeyboardShortcuts.TryMinimapZoom(zoomIn: false);
                    break;
                case "CinematicHud":
                    if (context.performed)
                        GameplayKeyboardShortcuts.TryToggleCinematicHudFromAction();
                    break;
            }
        }


                private void TryDodgeRollFromInput()
        {
            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            vThirdPersonController cc = GetComponent<vThirdPersonController>();
            if (cc == null)
                cc = GetComponentInChildren<vThirdPersonController>();
            if (cc == null)
                return;

            // Mirror Invector RollConditions without GenericInput (pad uses Input System, not rollInput).
            if (cc.isRolling && !cc.canRollAgain)
                return;
            if (!cc.isGrounded || cc.customAction)
                return;
            if (cc.rollStamina > 0f && cc.currentStamina < cc.rollStamina)
                return;

            if (cc.input.sqrMagnitude < 0.01f)
            {
                // Idle stick: roll camera-forward so B still dodges when standing still.
                Transform pivot = cc.rotateTarget != null
                    ? cc.rotateTarget
                    : (Camera.main != null ? Camera.main.transform : cc.transform);
                Vector3 forward = pivot.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f)
                    forward = cc.transform.forward;
                cc.input = forward.normalized;
            }

            cc.Roll();
        }

        private void TryDashFromInput()
        {
            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            var dash = GetComponent<Project.Features.Dash.DMDashController>();
            if (dash == null)
                dash = GetComponentInChildren<Project.Features.Dash.DMDashController>();
            dash?.TryStartDashFromInput();
        }
        private static UIManager ResolveUiManager()
        {
            if (cachedUiManager != null)
                return cachedUiManager;

            cachedUiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            return cachedUiManager;
        }
    }
}
