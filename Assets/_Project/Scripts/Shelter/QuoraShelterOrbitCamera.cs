using Invector.vCamera;
using Project.CameraFx;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Shelter
{
    /// <summary>
    /// Orbits a dedicated shelter camera around a deployed Quora Shelter while the player is sheltered inside.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuoraShelterOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform orbitPivot;
        [SerializeField] private Camera shelterCamera;
        [SerializeField] private float defaultDistance = 8f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 65f;

        private Camera playerCamera;
        private vThirdPersonCamera invectorCamera;
        private PlayerController player;
        private float orbitYaw;
        private float orbitPitch = 18f;
        private float orbitDistance;
        private bool active;

        public bool IsActive => active;

        public void Configure(Transform pivot)
        {
            orbitPivot = pivot;
        }

        public void Activate(PlayerController owner)
        {
            if (active || owner == null)
                return;

            player = owner;
            orbitDistance = defaultDistance;
            CachePlayerCameras(owner);
            DisablePlayerCameras(true);
            EnsureShelterCamera();
            shelterCamera.enabled = true;

            Vector3 pivot = ResolvePivotPosition();
            Vector3 toCamera = shelterCamera.transform.position - pivot;
            if (toCamera.sqrMagnitude < 0.01f)
                toCamera = Vector3.back * defaultDistance;

            orbitYaw = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Clamp(-Vector3.Angle(Vector3.up, toCamera.normalized) + 90f, minPitch, maxPitch);
            active = true;
            ApplyShelterOrbitCursorLock();
            ApplyOrbitPose(true);

            GameplayAudioUtility.EnsureListenerOnCamera(shelterCamera);
            CameraShakeListener.EnsureOn(shelterCamera);
        }

        public void Deactivate()
        {
            if (!active)
                return;

            if (shelterCamera != null)
                shelterCamera.enabled = false;

            DisablePlayerCameras(false);

            Camera restoreCamera = player != null ? player.GameplayCamera : playerCamera;
            GameplayAudioUtility.EnsureListenerOnCamera(restoreCamera);
            CameraShakeListener.EnsureOn(restoreCamera);

            active = false;
            player = null;
            playerCamera = null;
            invectorCamera = null;
        }

        private void LateUpdate()
        {
            if (!active)
                return;

            if (!IsMenuBlockingOrbit())
            {
                ApplyShelterOrbitCursorLock();
                ApplyLookInput();
                HandleScrollZoom();
            }

            ApplyOrbitPose(false);
        }

        private static void ApplyShelterOrbitCursorLock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private bool IsMenuBlockingOrbit()
        {
            if (QuoraShelterMenuUI.IsOpen)
                return true;

            return player != null && player.IsBuildingControlOpen;
        }

        private void ApplyLookInput()
        {
            Vector2 lookDelta = Vector2.zero;

            if (Mouse.current != null)
                lookDelta += Mouse.current.delta.ReadValue() * mouseSensitivity;

            Gamepad pad = Gamepad.current;
            if (pad != null)
                lookDelta += pad.rightStick.ReadValue() * (mouseSensitivity * 24f);

            if (lookDelta.sqrMagnitude <= 0.0001f)
                return;

            orbitYaw += lookDelta.x;
            orbitPitch = Mathf.Clamp(orbitPitch - lookDelta.y, minPitch, maxPitch);
        }

        private void HandleScrollZoom()
        {
            if (Mouse.current == null || player == null)
                return;

            if (player.IsInventoryOpen || player.IsJournalOpen || player.IsMapOpen)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            const float scrollUnitsPerNotch = 120f;
            float notches = scroll / scrollUnitsPerNotch;
            if (Mathf.Abs(notches) < 0.05f)
                notches = Mathf.Sign(scroll);

            orbitDistance = Mathf.Clamp(orbitDistance - notches * 1.25f, minDistance, maxDistance);
        }

        private void ApplyOrbitPose(bool snap)
        {
            if (shelterCamera == null)
                return;

            Vector3 pivot = ResolvePivotPosition();
            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 desiredPosition = pivot + rotation * (Vector3.back * orbitDistance);

            shelterCamera.transform.rotation = rotation;
            shelterCamera.transform.position = snap
                ? desiredPosition
                : Vector3.Lerp(shelterCamera.transform.position, desiredPosition, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private Vector3 ResolvePivotPosition()
        {
            if (orbitPivot != null)
                return orbitPivot.position;

            return transform.position + Vector3.up * 1.5f;
        }

        private void EnsureShelterCamera()
        {
            if (shelterCamera != null)
                return;

            GameObject cameraObject = new GameObject("ShelterOrbitCamera");
            cameraObject.transform.SetParent(transform, false);
            shelterCamera = cameraObject.AddComponent<Camera>();
            shelterCamera.enabled = false;

            if (playerCamera != null)
            {
                shelterCamera.fieldOfView = playerCamera.fieldOfView;
                shelterCamera.nearClipPlane = playerCamera.nearClipPlane;
                shelterCamera.farClipPlane = playerCamera.farClipPlane;
            }
        }

        private void CachePlayerCameras(PlayerController owner)
        {
            playerCamera = owner.GameplayCamera;
            PioneerShooterMeleeInput shooterInput = owner.GetComponent<PioneerShooterMeleeInput>();
            if (shooterInput != null)
                invectorCamera = shooterInput.tpCamera;
        }

        private void DisablePlayerCameras(bool disable)
        {
            if (playerCamera != null)
                playerCamera.enabled = !disable;

            if (invectorCamera != null)
                invectorCamera.enabled = !disable;
        }
    }
}
