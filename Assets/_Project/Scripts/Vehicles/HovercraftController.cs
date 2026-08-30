using System.Collections;
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

        [Header("Mouse Steer")]
        [SerializeField] private float mouseSteerSensitivity = 0.12f;

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
        private bool _mouseSteerHeld;
        private PlayerController _mountedPlayer;
        private bool _isExiting;
        private Coroutine _exitRoutine;

        public HovercraftProfile Profile => profile;
        public bool IsOccupied => occupancy != null && occupancy.IsOccupied;
        public bool IsExiting => _isExiting;
        public HovercraftTurretController Turret => turret;
        public HoverPhysicsDriver PhysicsDriver => physicsDriver;
        public HovercraftUsable Usable => usable;

        private void Reset()
        {
            WireSerializedRefs();
        }

        private void OnValidate()
        {
            WireSerializedRefs();
        }

        private void WireSerializedRefs()
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
        }

        private void Awake()
        {
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

            // Freeze drive while descending for exit.
            if (_isExiting)
            {
                if (physicsDriver != null)
                    physicsDriver.SetDriveInput(Vector2.zero, 0f, false);
                return;
            }

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
                physicsDriver.SetDriveInput(driveInput, verticalInput, boosterActive, _mouseSteerHeld);
        }

        public bool TryEnter(PlayerController player)
        {
            if (player == null || occupancy == null || _isExiting)
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
            if (player == null || occupancy == null || _isExiting)
                return false;

            if (!IsOccupied || _mountedPlayer != player)
                return false;

            if (_exitRoutine != null)
                StopCoroutine(_exitRoutine);
            _exitRoutine = StartCoroutine(ExitAfterDescentRoutine(player));
            return true;
        }

        private IEnumerator ExitAfterDescentRoutine(PlayerController player)
        {
            _isExiting = true;
            _moveInput = Vector2.zero;
            _ascendInput = false;
            _descendInput = false;
            _boosterInput = false;
            _wasBoosterInput = false;

            // Begin descent while the player is still mounted.
            physicsDriver?.SetOccupied(false);
            if (physicsDriver != null)
                physicsDriver.SetDriveInput(Vector2.zero, 0f, false);

            float parkedAltitude = profile != null ? profile.parkedAltitudeAboveGround : 0.35f;
            float tolerance = profile != null ? profile.exitAltitudeTolerance : 0.15f;
            float timeout = profile != null ? profile.exitDescentTimeout : 4f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                float altitude = physicsDriver != null ? physicsDriver.GetAltitudeAboveGround() : -1f;
                if (altitude >= 0f && altitude <= parkedAltitude + tolerance)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (occupancy != null && occupancy.IsOccupied)
                occupancy.TryExit(player);

            cameraRig?.Deactivate();
            turret?.Deactivate();

            vehicleAudio?.PlayExit();
            _mountedPlayer = null;
            _exitRoutine = null;
            _isExiting = false;
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
                _mouseSteerHeld = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            float lateral = 0f;
            float forward = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed)
                    lateral -= 1f;
                if (keyboard.dKey.isPressed)
                    lateral += 1f;
                if (keyboard.wKey.isPressed)
                    forward += 1f;
                if (keyboard.sKey.isPressed)
                    forward -= 1f;

                _ascendInput = keyboard.upArrowKey.isPressed;
                _descendInput = keyboard.downArrowKey.isPressed;
            }

            _mouseSteerHeld = false;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                _mouseSteerHeld = true;
                float yaw = mouse.delta.ReadValue().x * mouseSteerSensitivity;
                lateral = Mathf.Clamp(lateral + yaw, -1f, 1f);
            }

            _moveInput = Vector2.ClampMagnitude(new Vector2(lateral, forward), 1f);
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
            if (!IsOccupied || _isExiting || turret == null || _mountedPlayer == null || _mountedPlayer.BlocksCombatInput)
                return;

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                return;

            turret.ApplyLookInput(context.ReadValue<Vector2>());
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (!IsOccupied || _isExiting || _mountedPlayer == null || _mountedPlayer.BlocksCombatInput)
            {
                _boosterInput = false;
                return;
            }

            _boosterInput = !context.canceled && context.ReadValueAsButton();
        }

        public void OnAttack()
        {
            if (!IsOccupied || _isExiting || turret == null)
                return;

            turret.TryFire();
        }
    }
}
