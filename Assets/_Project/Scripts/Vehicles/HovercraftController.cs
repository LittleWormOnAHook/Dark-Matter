using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Vehicles
{
    /// <summary>
    /// Orchestrates hovercraft input, occupancy, camera, and turret while mounted.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftController : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private HoverPhysicsDriver physicsDriver;
        [SerializeField] private HovercraftOccupancy occupancy;
        [SerializeField] private HovercraftCameraRig cameraRig;
        [SerializeField] private HovercraftTurretController turret;
        [SerializeField] private HovercraftEngineAudio engineAudio;
        [SerializeField] private HovercraftVehicleAudio vehicleAudio;
        [SerializeField] private HovercraftThrusterVfx thrusterVfx;
        [SerializeField] private HovercraftUsable usable;
        [SerializeField] private HovercraftFuelSystem fuelSystem;

        [Header("Audio Clips (optional overrides)")]
        [Tooltip("Looping engine clip for this prefab. Drag an imported MP3/WAV/OGG AudioClip from the Project window. Uses Hovercraft Profile when empty.")]
        [SerializeField] private AudioClip engineRunningClip;
        [Tooltip("Turret fire clip for this prefab. Drag an imported MP3/WAV/OGG AudioClip from the Project window. Uses Hovercraft Profile when empty.")]
        [SerializeField] private AudioClip turretFireClip;
        [Tooltip("Second turret fire clip override. Uses Hovercraft Profile when empty.")]
        [SerializeField] private AudioClip turretFireClip2;
        [Tooltip("Boarding one-shot override.")]
        [SerializeField] private AudioClip boardClip;
        [Tooltip("Exit one-shot override.")]
        [SerializeField] private AudioClip exitClip;
        [Tooltip("Boost one-shot override.")]
        [SerializeField] private AudioClip boostClip;

        private Vector2 _moveInput;
        private bool _ascendInput;
        private bool _descendInput;
        private bool _boosterInput;
        private bool _wasBoosterInput;
        private PlayerController _mountedPlayer;

        public HovercraftProfile Profile => profile;
        public bool IsOccupied => occupancy != null && occupancy.IsOccupied;
        public HovercraftTurretController Turret => turret;
        public HoverPhysicsDriver PhysicsDriver => physicsDriver;
        public HovercraftUsable Usable => usable;

        private void Awake()
        {
            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            if (occupancy == null)
                occupancy = GetComponent<HovercraftOccupancy>();

            if (cameraRig == null)
                cameraRig = GetComponent<HovercraftCameraRig>();

            if (turret == null)
                turret = GetComponent<HovercraftTurretController>();

            if (engineAudio == null)
                engineAudio = GetComponent<HovercraftEngineAudio>();

            if (vehicleAudio == null)
                vehicleAudio = GetComponent<HovercraftVehicleAudio>();

            if (thrusterVfx == null)
                thrusterVfx = GetComponent<HovercraftThrusterVfx>();

            if (usable == null)
                usable = GetComponent<HovercraftUsable>();

            if (fuelSystem == null)
                fuelSystem = GetComponent<HovercraftFuelSystem>();

            ApplyAudioClipOverrides();
        }

        private void ApplyAudioClipOverrides()
        {
            if (engineAudio != null && engineRunningClip != null)
                engineAudio.SetEngineRunningClip(engineRunningClip);

            if (turret != null)
            {
                if (turretFireClip != null)
                    turret.SetTurretFireClip(turretFireClip);

                if (turretFireClip2 != null)
                    turret.SetTurretFireClip2(turretFireClip2);
            }

            if (vehicleAudio == null)
                return;

            if (boardClip != null)
                vehicleAudio.SetBoardClip(boardClip);

            if (exitClip != null)
                vehicleAudio.SetExitClip(exitClip);

            if (boostClip != null)
                vehicleAudio.SetBoostClip(boostClip);
        }

        private void Update()
        {
            if (!IsOccupied)
                return;

            PollMountedDriveInput();
            HandleBoostAudio();

            Vector2 driveInput = _moveInput;
            float verticalInput = ResolveVerticalInput();
            bool boosterActive = _boosterInput;

            // Dry tank — no thrust/climb/boost. The hover spring suspension (separate system) still
            // holds the craft up, so it just sits there dead in the water until refueled.
            if (fuelSystem != null && fuelSystem.ShouldBlockDriveInput)
            {
                driveInput = Vector2.zero;
                verticalInput = 0f;
                boosterActive = false;
            }

            if (physicsDriver != null)
                physicsDriver.SetDriveInput(driveInput, verticalInput, boosterActive);
        }

        public bool TryEnter(PlayerController player)
        {
            if (player == null || occupancy == null)
                return false;

            if (!occupancy.TryEnter(player))
                return false;

            physicsDriver?.SetOccupied(true);
            _mountedPlayer = player;
            _wasBoosterInput = false;
            cameraRig?.Activate(player);
            turret?.Activate(player);
            vehicleAudio?.PlayBoard();
            return true;
        }

        public bool TryExit(PlayerController player)
        {
            if (player == null || occupancy == null)
                return false;

            cameraRig?.Deactivate();
            turret?.Deactivate();

            if (!occupancy.TryExit(player))
                return false;

            physicsDriver?.SetOccupied(false);
            vehicleAudio?.PlayExit();
            _moveInput = Vector2.zero;
            _ascendInput = false;
            _descendInput = false;
            _boosterInput = false;
            _wasBoosterInput = false;
            _mountedPlayer = null;
            return true;
        }

        private void HandleBoostAudio()
        {
            if (vehicleAudio == null)
            {
                _wasBoosterInput = _boosterInput;
                return;
            }

            bool boostStarted = _boosterInput && !_wasBoosterInput;
            bool movingForward = _moveInput.y > 0.05f;
            if (boostStarted && movingForward)
                vehicleAudio.PlayBoost();

            _wasBoosterInput = _boosterInput;
        }

        private void PollMountedDriveInput()
        {
            if (_mountedPlayer != null && _mountedPlayer.BlocksCombatInput)
            {
                _moveInput = Vector2.zero;
                _ascendInput = false;
                _descendInput = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            float lateral = 0f;
            float forward = 0f;
            if (keyboard.aKey.isPressed)
                lateral -= 1f;
            if (keyboard.dKey.isPressed)
                lateral += 1f;
            if (keyboard.wKey.isPressed)
                forward += 1f;
            if (keyboard.sKey.isPressed)
                forward -= 1f;

            _moveInput = Vector2.ClampMagnitude(new Vector2(lateral, forward), 1f);
            _ascendInput = keyboard.upArrowKey.isPressed;
            _descendInput = keyboard.downArrowKey.isPressed;
        }

        private float ResolveVerticalInput()
        {
            if (_ascendInput && _descendInput)
                return 0f;

            if (_ascendInput)
                return 1f;

            return _descendInput ? -1f : 0f;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            // WASD is polled directly while mounted so arrow keys stay free for altitude.
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (!IsOccupied || turret == null || _mountedPlayer == null || _mountedPlayer.BlocksCombatInput)
                return;

            turret.ApplyLookInput(context.ReadValue<Vector2>());
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (!IsOccupied || _mountedPlayer == null || _mountedPlayer.BlocksCombatInput)
            {
                _boosterInput = false;
                return;
            }

            _boosterInput = !context.canceled && context.ReadValueAsButton();
        }

        public void OnAttack()
        {
            if (!IsOccupied || turret == null)
                return;

            turret.TryFire();
        }
    }
}
