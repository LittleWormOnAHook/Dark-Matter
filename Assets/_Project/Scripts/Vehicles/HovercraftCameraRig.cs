using Invector.vCamera;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Vehicles
{
    public enum HovercraftCameraMode
    {
        Cockpit,
        Follow
    }

    /// <summary>
    /// Cockpit fixed-forward and external follow cameras. F1 toggles; scroll zooms follow distance.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftCameraRig : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private Transform cockpitCamPoint;
        [SerializeField] private Transform followCamPoint;
        [SerializeField] private Camera vehicleCamera;
        [SerializeField] private HoverPhysicsDriver physicsDriver;

        private Camera _vehicleCamera;
        private PlayerController _player;
        private Camera _playerCamera;
        private vThirdPersonCamera _invectorCamera;
        private PioneerShooterMeleeInput _shooterInput;
        private HovercraftCameraMode _mode = HovercraftCameraMode.Cockpit;
        private float _followDistance;
        private bool _active;

        public HovercraftCameraMode Mode => _mode;
        public bool IsActive => _active;

        public void Configure(
            HovercraftProfile hoverProfile,
            Transform cockpitPoint,
            Transform followPoint,
            Camera vehicleCamera)
        {
            profile = hoverProfile;
            cockpitCamPoint = cockpitPoint;
            followCamPoint = followPoint;
            this.vehicleCamera = vehicleCamera;
            _vehicleCamera = vehicleCamera;
            _followDistance = profile != null ? profile.followDistanceDefault : 14f;
        }

        private void Awake()
        {
            _vehicleCamera = vehicleCamera;
            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            if (profile != null)
                _followDistance = profile.followDistanceDefault;
        }

        public void Activate(PlayerController player)
        {
            if (_active)
                return;

            _player = player;
            CachePlayerCameras(player);
            DisablePlayerCameras(true);
            EnsureVehicleCamera();
            _vehicleCamera.enabled = true;
            if (profile != null)
                _followDistance = profile.followDistanceDefault;
            _active = true;
            ApplyCameraPose(true);
            GameplayAudioUtility.EnsureListenerOnCamera(_vehicleCamera);
        }

        public void Deactivate()
        {
            if (!_active)
                return;

            if (_vehicleCamera != null)
                _vehicleCamera.enabled = false;

            DisablePlayerCameras(false);

            Camera restoreCamera = _player != null ? _player.GameplayCamera : _playerCamera;
            GameplayAudioUtility.EnsureListenerOnCamera(restoreCamera);

            _active = false;
            _player = null;
            _playerCamera = null;
            _invectorCamera = null;
            _shooterInput = null;
        }

        private void Update()
        {
            if (!_active)
                return;

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                ToggleMode();

            bool zoomed = false;
            if (_mode == HovercraftCameraMode.Follow && CanHandleFollowZoom())
                zoomed = HandleFollowZoom();

            ApplyCameraPose(zoomed);
        }

        public void ToggleMode()
        {
            _mode = _mode == HovercraftCameraMode.Cockpit
                ? HovercraftCameraMode.Follow
                : HovercraftCameraMode.Cockpit;

            if (_mode == HovercraftCameraMode.Follow && profile != null)
                _followDistance = Mathf.Clamp(_followDistance, profile.followDistanceMin, profile.followDistanceMax);

            ApplyCameraPose(true);
        }

        private bool CanHandleFollowZoom()
        {
            if (_player == null)
                return false;

            // Allow zoom while driving; only block when UI that owns the scroll wheel is open.
            if (_player.IsGameplayPaused)
                return false;

            return !_player.IsMapOpen && !_player.IsInventoryOpen && !_player.IsJournalOpen;
        }

        /// <summary>
        /// Mouse-wheel zoom for third-person Follow cam. Returns true when distance changed
        /// so the pose can snap instead of lagging behind a slow lerp.
        /// </summary>
        private bool HandleFollowZoom()
        {
            if (Mouse.current == null || profile == null)
                return false;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return false;

            const float scrollUnitsPerNotch = 120f;
            float notches = scroll / scrollUnitsPerNotch;
            // Some mice / OS drivers report ±1 per notch instead of ±120.
            if (Mathf.Abs(notches) < 0.05f)
                notches = Mathf.Sign(scroll);

            float previous = _followDistance;
            _followDistance = Mathf.Clamp(
                _followDistance - notches * profile.scrollDistanceStep,
                profile.followDistanceMin,
                profile.followDistanceMax);

            return !Mathf.Approximately(previous, _followDistance);
        }

        private void ApplyCameraPose(bool snap)
        {
            if (_vehicleCamera == null)
                return;

            if (_mode == HovercraftCameraMode.Cockpit)
            {
                Transform point = cockpitCamPoint != null ? cockpitCamPoint : transform;
                Vector3 turbulence = physicsDriver != null ? physicsDriver.SampleTurbulenceOffset() : Vector3.zero;
                if (snap)
                {
                    _vehicleCamera.transform.SetPositionAndRotation(point.position + turbulence, point.rotation);
                }
                else
                {
                    _vehicleCamera.transform.position = point.position + turbulence;
                    _vehicleCamera.transform.rotation = point.rotation;
                }

                return;
            }

            Transform followAnchor = followCamPoint != null ? followCamPoint : transform;
            Vector3 lookTarget = transform.position + transform.forward * (profile != null ? profile.followLookAhead : 6f);
            Vector3 offset = -transform.forward * _followDistance + Vector3.up * (profile != null ? profile.followHeight : 4.5f);
            Vector3 desiredPosition = followAnchor.position + offset;
            if (physicsDriver != null)
                desiredPosition += physicsDriver.SampleTurbulenceOffset();

            if (snap)
            {
                _vehicleCamera.transform.position = desiredPosition;
            }
            else
            {
                _vehicleCamera.transform.position = Vector3.Lerp(
                    _vehicleCamera.transform.position,
                    desiredPosition,
                    Time.deltaTime * 8f);
            }

            _vehicleCamera.transform.rotation = Quaternion.LookRotation(lookTarget - _vehicleCamera.transform.position, Vector3.up);
        }

        private void CachePlayerCameras(PlayerController player)
        {
            _playerCamera = player != null ? player.GameplayCamera : null;

            if (player != null && player.TryGetComponent(out PioneerInvectorBootstrap bootstrap))
            {
                _shooterInput = bootstrap.ShooterInput;
                if (_shooterInput != null)
                    _invectorCamera = _shooterInput.tpCamera;
            }
        }

        private void DisablePlayerCameras(bool disabled)
        {
            if (_playerCamera != null)
                _playerCamera.enabled = !disabled;

            if (_invectorCamera != null)
            {
                _invectorCamera.enabled = !disabled;
                if (_invectorCamera.targetCamera != null)
                    _invectorCamera.targetCamera.enabled = !disabled;
            }
        }

        private void EnsureVehicleCamera()
        {
            if (_vehicleCamera != null)
                return;

            GameObject cameraObject = new GameObject("HovercraftCamera");
            cameraObject.transform.SetParent(transform, false);
            _vehicleCamera = cameraObject.AddComponent<Camera>();
            _vehicleCamera.tag = "MainCamera";
            _vehicleCamera.nearClipPlane = 0.08f;
            _vehicleCamera.farClipPlane = 2500f;
            _vehicleCamera.fieldOfView = 68f;
        }
    }
}
