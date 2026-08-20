using Invector.vCamera;
using Project.CameraFx;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
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
    /// Follow mode stays level (no bank/tilt) with lagged heading for subtle aircraft-style orbit.
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
        private bool _snapPose;
        private Vector3 _followPlanarForward = Vector3.forward;
        private Quaternion _followLookRotation = Quaternion.identity;

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
            _snapPose = true;
            ApplyCameraPose(true);
            GameplayAudioUtility.EnsureListenerOnCamera(_vehicleCamera);
            CameraShakeListener.EnsureOn(_vehicleCamera);
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
            CameraShakeListener.EnsureOn(restoreCamera);

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

            if (_mode == HovercraftCameraMode.Follow && CanHandleFollowZoom() && HandleFollowZoom())
                _snapPose = true;
        }

        private void LateUpdate()
        {
            if (!_active)
                return;

            // Pose after HoverPhysicsDriver LateUpdate so bank matches the visual mesh 1:1 this frame.
            ApplyCameraPose(_snapPose);
            _snapPose = false;
        }

        public void ToggleMode()
        {
            _mode = _mode == HovercraftCameraMode.Cockpit
                ? HovercraftCameraMode.Follow
                : HovercraftCameraMode.Cockpit;

            if (_mode == HovercraftCameraMode.Follow && profile != null)
                _followDistance = Mathf.Clamp(_followDistance, profile.followDistanceMin, profile.followDistanceMax);

            _snapPose = true;
            ApplyCameraPose(true);
        }

        private bool CanHandleFollowZoom()
        {
            if (_player == null)
                return false;

            // Allow zoom while driving; only block when UI that owns the scroll wheel is open.
            if (_player.IsGameplayPaused)
                return false;

            return !_player.IsMapOpen && !_player.IsInventoryOpen && !_player.IsJournalOpen
                && !MapUI.IsMinimapScrollZoomActive;
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

            // Same local bank currently on the visual mesh; applied in craft-root space (not camera
            // local) so cockpit roll matches the hull 1:1 when strafing / turning.
            Quaternion bankLocal = physicsDriver != null
                ? physicsDriver.CurrentVisualBankLocal
                : Quaternion.identity;

            if (_mode == HovercraftCameraMode.Cockpit)
            {
                Transform point = cockpitCamPoint != null ? cockpitCamPoint : transform;
                Vector3 turbulence = physicsDriver != null ? physicsDriver.SampleTurbulenceOffset() : Vector3.zero;
                Quaternion rotation = ResolveBankedCameraRotation(point, bankLocal);
                if (snap)
                {
                    _vehicleCamera.transform.SetPositionAndRotation(point.position + turbulence, rotation);
                }
                else
                {
                    _vehicleCamera.transform.position = point.position + turbulence;
                    _vehicleCamera.transform.rotation = rotation;
                }

                return;
            }

            ApplyFollowCameraPose(snap);
        }

        /// <summary>
        /// Third-person chase cam: level horizon (no bank/tilt), lagged heading so left/right orbit
        /// stays subtle like an aircraft chase camera, and heavily damped lateral turbulence.
        /// </summary>
        private void ApplyFollowCameraPose(bool snap)
        {
            Transform followAnchor = followCamPoint != null ? followCamPoint : transform;
            float lookAhead = profile != null ? profile.followLookAhead : 6f;
            float height = profile != null ? profile.followHeight : 4.5f;
            float headingSmooth = profile != null ? profile.followHeadingSmooth : 2.2f;
            float positionSmooth = profile != null ? profile.followPositionSmooth : 3.5f;
            float lookSmooth = profile != null ? profile.followLookSmooth : 4f;
            float lateralTurbScale = profile != null ? profile.followLateralTurbulenceScale : 0.12f;

            Vector3 targetForward = ResolvePlanarForward(transform.forward);
            if (snap || _followPlanarForward.sqrMagnitude < 0.0001f)
            {
                _followPlanarForward = targetForward;
            }
            else
            {
                float headingT = 1f - Mathf.Exp(-headingSmooth * Time.deltaTime);
                _followPlanarForward = Vector3.Slerp(_followPlanarForward, targetForward, headingT).normalized;
            }

            Vector3 lookTarget = transform.position + targetForward * lookAhead;
            Vector3 offset = -_followPlanarForward * _followDistance + Vector3.up * height;
            Vector3 desiredPosition = followAnchor.position + offset;
            if (physicsDriver != null)
            {
                Vector3 turbulence = physicsDriver.SampleTurbulenceOffset();
                turbulence.x *= lateralTurbScale;
                turbulence.z *= lateralTurbScale;
                desiredPosition += turbulence;
            }

            if (snap)
            {
                _vehicleCamera.transform.position = desiredPosition;
            }
            else
            {
                float positionT = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
                _vehicleCamera.transform.position = Vector3.Lerp(
                    _vehicleCamera.transform.position,
                    desiredPosition,
                    positionT);
            }

            // World-up look keeps the chase cam level — craft visual bank must not roll the camera.
            Vector3 toTarget = lookTarget - _vehicleCamera.transform.position;
            if (toTarget.sqrMagnitude < 0.0001f)
                toTarget = targetForward;

            Quaternion desiredLook = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            if (snap)
            {
                _followLookRotation = desiredLook;
                _vehicleCamera.transform.rotation = desiredLook;
            }
            else
            {
                float lookT = 1f - Mathf.Exp(-lookSmooth * Time.deltaTime);
                _followLookRotation = Quaternion.Slerp(_followLookRotation, desiredLook, lookT);
                _vehicleCamera.transform.rotation = _followLookRotation;
            }
        }

        private static Vector3 ResolvePlanarForward(Vector3 forward)
        {
            Vector3 planar = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (planar.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return planar.normalized;
        }

        /// <summary>
        /// Banks the untilted cam-point rotation by the visual mesh delta in craft-root space.
        /// Cam anchors are siblings of Visual, so point.rotation alone stays level with the RB.
        /// </summary>
        private Quaternion ResolveBankedCameraRotation(Transform point, Quaternion bankLocal)
        {
            if (point == null)
                return transform.rotation * bankLocal;

            // world = root * bank * Inverse(root) * untiltedPointWorld
            return transform.rotation * bankLocal * Quaternion.Inverse(transform.rotation) * point.rotation;
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
