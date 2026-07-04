using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.UI;
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
        private PlayerInput _playerInput;
        private PlayerController _playerController;
        private MeleeCombatController _melee;
        private RangedCombatController _ranged;
        private PioneerInvectorInputBridge _invectorInput;

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
            GameSession.GameStarted += EnsureGameplayInputActive;

            if (_playerInput != null)
                _playerInput.onActionTriggered += HandleAction;

            EnsureGameplayInputActive();
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= EnsureGameplayInputActive;

            if (_playerInput != null)
                _playerInput.onActionTriggered -= HandleAction;
        }

        private void EnsureGameplayInputActive()
        {
            if (_playerInput == null || !GameSession.HasStarted)
                return;

            _playerInput.enabled = true;
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
                    _playerController?.OnMove(context);
                    break;
                case "Look":
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
                    _playerController?.OnSprint(context);
                    break;
                case "Crouch":
                    _playerController?.OnCrouch(context);
                    break;
                case "Attack":
                    if (context.performed)
                    {
                        _melee?.OnAttack(context);
                        _ranged?.OnAttack(context);
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
                        FindAnyObjectByType<InventoryUI>()?.OnToggleInventory(context);
                    break;
                case "Map":
                    if (context.performed)
                        FindAnyObjectByType<MapUI>()?.OnToggleMap(context);
                    break;
                case "Journal":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleJournal(context);
                    break;
                case "Craft":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleCraft(context);
                    break;
                case "Recipes":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleRecipes(context);
                    break;
                case "Pioneers":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnTogglePioneers(context);
                    break;
                case "Skills":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleSkills(context);
                    break;
                case "Echoes":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleEchoes(context);
                    break;
                case "Character":
                    if (context.performed)
                        FindAnyObjectByType<UIManager>()?.OnToggleCharacter(context);
                    break;
                case "Pets":
                    if (context.performed)
                        FindAnyObjectByType<PetUI>()?.OnTogglePets(context);
                    break;
            }
        }
    }
}
