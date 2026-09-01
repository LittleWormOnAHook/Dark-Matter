using System.Collections;
using Project.Core;
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
                    _playerController?.OnCrouch(context);
                    break;
                case "Attack":
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
                    if (_invectorInput != null)
                        _invectorInput.OnBlock(context);
                    _melee?.OnBlock(context);
                    _ranged?.OnBlock(context);
                    break;
                case "SwitchWeapon":
                    if (context.performed)
                        _melee?.OnSwitchWeapon(context);
                    break;
                case "Inventory":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory))
                            ResolveUiManager()?.OnToggleInventory(context);
                    }
                    break;
                case "Map":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Map))
                            ResolveUiManager()?.OnToggleMap(context);
                    }
                    else if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                    {
                        FindAnyObjectByType<MapUI>(FindObjectsInactive.Include)?.OnToggleMap(context);
                    }
                    break;
                case "Journal":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true))
                            ResolveUiManager()?.OnToggleJournal(context);
                    }
                    break;
                case "Craft":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes))
                            ResolveUiManager()?.OnToggleCraft(context);
                    }
                    break;
                case "Blueprints":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes))
                            ResolveUiManager()?.OnToggleBlueprints(context);
                    }
                    break;
                case "Pioneers":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pioneers))
                            ResolveUiManager()?.OnTogglePioneers(context);
                    }
                    break;
                case "Skills":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Skills))
                            ResolveUiManager()?.OnToggleSkills(context);
                    }
                    break;
                case "Echoes":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Echoes))
                            ResolveUiManager()?.OnToggleEchoes(context);
                    }
                    break;
                case "Achievements":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Achievements))
                            ResolveUiManager()?.OnToggleAchievements(context);
                    }
                    break;
                case "Character":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Character))
                            ResolveUiManager()?.OnToggleCharacter(context);
                    }
                    break;
                case "Pets":
                    if (context.performed)
                    {
                        if (!DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pet))
                            ResolveUiManager()?.OnTogglePets(context);
                    }
                    break;
            }
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
