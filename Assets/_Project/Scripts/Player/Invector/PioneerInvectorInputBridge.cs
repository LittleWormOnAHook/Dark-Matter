using Invector.vCharacterController;
using Project.Combat;
using Project.Core;
using Project.Features.Jetpack;
using Project.Features.Dash;
using Project.Interaction;
using Project.Survival;
using Project.UI;
using Project.Vehicles;
using Project.Player;
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
        private DMIGrenadeCookController _grenadeCook;
        private vThirdPersonController _motor;
        private DMJetpackAnimatorDriver _jetpackAnimator;
        private DMLandingDirector _landing;
        private DMDashController _dash;
        private bool _combatBlockedByUiPointer;

        public bool IsAiming => _shooterInput != null && (_shooterInput.isAimingByInput || _shooterInput.IsAiming);

        /// <summary>True while a grenade is held/cooking — weapon Attack/fire must stay off.</summary>
        public bool BlocksWeaponFireForGrenade =>
            _grenadeCook != null && _grenadeCook.BlocksWeaponFire;

        /// <summary>
        /// Force aim while mining resource scan (F / LB) is held on a drawn mining tool.
        /// </summary>
        public void SetMiningScanAimHold(bool held)
        {
            if (_shooterInput == null)
                _shooterInput = GetComponent<PioneerShooterMeleeInput>();

            _shooterInput?.SetMiningScanAimHold(held);
        }

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _playerController = GetComponent<PlayerController>();
            _survivalStats = GetComponent<SurvivalStats>();
            _optics = GetComponent<OpticsController>();
            _shooterInput = GetComponent<PioneerShooterMeleeInput>();
            _grenadeCook = GetComponent<DMIGrenadeCookController>() ??
                           GetComponentInChildren<DMIGrenadeCookController>(true);
            _motor = GetComponent<vThirdPersonController>();
            _jetpackAnimator = GetComponent<DMJetpackAnimatorDriver>();
            _landing = GetComponent<DMLandingDirector>();
            _dash = GetComponent<DMDashController>();
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
            bool lockWeaponFire = lockCombat || BlocksWeaponFireForGrenade;
            input.SetLockBasicInput(lockLocomotion);
            input.SetLockMeleeInput(lockWeaponFire);
            input.lockCameraInput = ShouldLockCameraInput();

            input.SetLockShooterInput(lockWeaponFire);

            if (lockWeaponFire)
                input.isAimingByInput = false;

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

            if (PlayerVehicleState.IsMounted)
                return true;

            if (_dash == null)
                _dash = GetComponent<DMDashController>();

            if (_dash != null && _dash.IsDashing)
                return true;

            if (_landing == null)
                _landing = GetComponent<DMLandingDirector>();

            if (_landing != null && _landing.IsLandingLocked)
                return true;

            if (_jetpackAnimator != null && _jetpackAnimator.IsLandingLocked)
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

            if (PlayerVehicleState.IsMounted)
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

            if (_playerController != null && _playerController.IsOpticsOpen)
                return;

            if (_optics != null && _optics.IsActive)
                return;

            if (ShouldLockGameplayInput())
                return;

            if (BlocksWeaponFireForGrenade)
                return;
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted || !_bootstrap.IsActive)
                return;

            // RMB close must work even while optics have combat input locked.
            if (_optics != null && _optics.TryHandleBlockInput(context))
                return;

            if (ShouldLockGameplayInput())
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
