using Invector.vCharacterController;
using Project.Core;
using Project.Interaction;
using Project.Survival;
using Project.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Gates Invector input when Pioneer UI/optics block gameplay and syncs stamina with SurvivalStats.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    public class PioneerInvectorInputBridge : MonoBehaviour
    {
        private PioneerInvectorBootstrap _bootstrap;
        private PlayerController _playerController;
        private SurvivalStats _survivalStats;
        private OpticsController _optics;
        private PioneerShooterMeleeInput _shooterInput;
        private vThirdPersonController _motor;
        private bool _combatBlockedByUiPointer;

        public bool IsAiming => _shooterInput != null && (_shooterInput.isAimingByInput || _shooterInput.IsAiming);

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _playerController = GetComponent<PlayerController>();
            _survivalStats = GetComponent<SurvivalStats>();
            _optics = GetComponent<OpticsController>();
            _shooterInput = GetComponent<PioneerShooterMeleeInput>();
            _motor = GetComponent<vThirdPersonController>();
        }

        private void Update()
        {
            if (!_bootstrap.IsActive || _shooterInput == null)
                return;

            RefreshCombatUiPointerBlock();
            ApplyInputLocks(_shooterInput);
            SyncStamina();
        }

        public void ApplyInputLocks(PioneerShooterMeleeInput input)
        {
            bool lockLocomotion = ShouldLockLocomotionInput();
            bool lockCombat = lockLocomotion || _combatBlockedByUiPointer;
            input.SetLockBasicInput(lockLocomotion);
            input.SetLockMeleeInput(lockCombat);
            input.lockCameraInput = ShouldLockCameraInput();

            input.SetLockShooterInput(lockCombat);

            if (lockLocomotion && _motor != null)
                _motor.input = Vector3.zero;
        }

        public bool ShouldLockLocomotionInput()
        {
            if (Time.timeScale <= 0f)
                return true;

            if (_playerController == null)
                return false;

            if (_playerController.IsGameplayPaused || _playerController.BlocksCombatInput)
                return true;

            if (_survivalStats != null && _survivalStats.IsDead)
                return true;

            return false;
        }

        public bool ShouldLockGameplayInput()
        {
            if (ShouldLockLocomotionInput())
                return true;

            return _combatBlockedByUiPointer;
        }

        public bool ShouldLockCameraInput()
        {
            if (Time.timeScale <= 0f)
                return true;

            if (_playerController == null)
                return false;

            if (_playerController.IsGameplayPaused ||
                _playerController.IsInventoryOpen ||
                _playerController.IsJournalOpen ||
                _playerController.IsMapOpen ||
                _playerController.IsQuestDialogOpen ||
                _playerController.IsLootDialogOpen ||
                _playerController.IsBuildingControlOpen)
            {
                return true;
            }

            if (_survivalStats != null && _survivalStats.IsDead)
                return true;

            return false;
        }

        public void ClearMovement()
        {
            if (_motor != null)
                _motor.input = Vector3.zero;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted || !_bootstrap.IsActive)
                return;

            if (ShouldLockGameplayInput())
                return;
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted || !_bootstrap.IsActive)
                return;

            if (ShouldLockGameplayInput())
                return;

            if (_optics != null && _optics.TryHandleBlockInput(context))
                return;
        }

        private void SyncStamina()
        {
            if (_motor == null || _survivalStats == null)
                return;

            _motor.maxStamina = _survivalStats.maxStamina;
            _motor.currentStamina = _survivalStats.CurrentStamina;

            if (_motor.isSprinting && _motor.input.sqrMagnitude > 0.01f)
                _survivalStats.SetSprinting(true);
            else
                _survivalStats.SetSprinting(false);
        }

        private void RefreshCombatUiPointerBlock()
        {
            _combatBlockedByUiPointer = false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (Mouse.current != null)
                _combatBlockedByUiPointer = eventSystem.IsPointerOverGameObject(Mouse.current.deviceId);
            else
                _combatBlockedByUiPointer = eventSystem.IsPointerOverGameObject();
        }
    }
}
