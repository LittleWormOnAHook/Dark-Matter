using System.Collections.Generic;
using Invector.vCamera;
using Invector.vCharacterController;
using Project.Building;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using Project.Survival;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Turret seat: disable Kade's camera, drive prefab seat camera + mouse aim (FPS-style), LMB fire, E/Esc exits.
    /// </summary>
    public static class DMTurretPlayerControl
    {
        private struct DisabledCamera
        {
            public Camera camera;
            public bool wasEnabled;
            public AudioListener listener;
            public bool listenerWasEnabled;
        }

        private static DMTurretRuntime controlling;
        private static Camera turretCamera;
        private static AudioListener turretListener;
        private static vThirdPersonCamera tpCamera;
        private static bool tpWasEnabled;
        private static bool tpLockWasEnabled;
        private static bool spawnedRuntimeCamera;
        private static PioneerShooterMeleeInput meleeInput;
        private static bool savedLockInput;
        private static bool savedLockMovement;
        private static vThirdPersonController controller;
        private static string savedTurretCameraTag;
        private static readonly List<DisabledCamera> disabledCameras = new List<DisabledCamera>(8);

        public static bool IsActive => controlling != null;
        public static DMTurretRuntime Current => controlling;

        public static bool IsControlling(DMTurretRuntime runtime)
        {
            return runtime != null && controlling == runtime;
        }

        /// <summary>
        /// Seated Kade is hidden: renderers off, not a valid enemy sight / aggro target.
        /// Enemies may still engage the player-faction turret.
        /// </summary>
        public static bool IsSeatedPlayer(Transform candidate)
        {
            if (!IsActive || candidate == null)
                return false;

            Transform player = PlayerReference.ResolveTransform();
            if (player == null)
                return false;

            if (candidate == player || candidate.IsChildOf(player) || player.IsChildOf(candidate))
                return true;

            SurvivalStats candidateStats = candidate.GetComponentInParent<SurvivalStats>();
            if (candidateStats == null)
                return false;
            SurvivalStats playerStats = player.GetComponent<SurvivalStats>();
            return playerStats != null && candidateStats == playerStats;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            controlling = null;
            turretCamera = null;
            turretListener = null;
            tpCamera = null;
            spawnedRuntimeCamera = false;
            meleeInput = null;
            controller = null;
            disabledCameras.Clear();
            DMTurretPlayerSeatHider.ClearWithoutRestore();
        }

        public static bool TryEnter(DMTurretRuntime runtime)
        {
            if (runtime == null || runtime.IsDestroyed)
                return false;
            if (controlling == runtime)
                return true;
            if (controlling != null)
                Exit();

            Transform player = PlayerReference.ResolveTransform();
            if (player == null)
                return false;

            controlling = runtime;
            runtime.PauseAutoBrainForPlayerSeat();

            meleeInput = player.GetComponent<PioneerShooterMeleeInput>();
            controller = player.GetComponent<vThirdPersonController>();
            if (meleeInput != null)
            {
                savedLockInput = meleeInput.lockInput;
                meleeInput.lockInput = true;
            }

            if (controller != null)
            {
                savedLockMovement = controller.lockMovement;
                controller.lockMovement = true;
                controller.lockRotation = true;
            }

            EnsureTurretCamera(runtime);
            DisablePlayerViewCameras(player);
            DMTurretPlayerSeatHider.Hide(player);

            PlayerController playerController = PlayerLocator.FindPlayerController();
            if (playerController != null)
            {
                playerController.SetGameplayPaused(false);
                playerController.ApplyCursorState();
            }

            DMUiToolkitTurretCrosshair.TryShow();
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            playerController?.ApplyCursorState();
            return true;
        }

        public static void Exit()
        {
            if (controlling == null && turretCamera == null)
                return;

            DMTurretRuntime exiting = controlling;
            exiting?.StopFireAndCleanupForgeFx();
            DMUiToolkitTurretCrosshair.TryHide();

            if (meleeInput != null)
                meleeInput.lockInput = savedLockInput;
            if (controller != null)
            {
                controller.lockMovement = savedLockMovement;
                controller.lockRotation = false;
            }

            RestorePlayerViewCameras();
            DMTurretPlayerSeatHider.Restore();

            if (turretCamera != null)
            {
                if (!string.IsNullOrEmpty(savedTurretCameraTag))
                    turretCamera.tag = savedTurretCameraTag;
                turretCamera.enabled = false;
                if (turretListener != null)
                    turretListener.enabled = false;

                if (spawnedRuntimeCamera)
                    Object.Destroy(turretCamera.gameObject);
            }

            if (exiting != null)
            {
                if (exiting.ControlMode == DMTurretControlMode.Auto)
                    exiting.SetControlMode(DMTurretControlMode.Auto);
                exiting.ResumeAutoBrainAfterPlayerSeat();
            }

            controlling = null;
            turretCamera = null;
            turretListener = null;
            tpCamera = null;
            spawnedRuntimeCamera = false;
            meleeInput = null;
            controller = null;

            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null)
            {
                player.SetGameplayPaused(false);
                player.ApplyCursorState();
            }
        }

        public static void Tick()
        {
            if (controlling == null || controlling.IsDestroyed)
            {
                Exit();
                return;
            }

            UpdateCameraPose();
            TickFireInput();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
                Exit();
        }

        public static void TickAimOnly()
        {
            if (controlling == null || controlling.IsDestroyed)
                return;
            UpdateCameraPose();
            AimFromLook();
            controlling.ApplySeatCameraPitch();
        }

        private static void TickFireInput()
        {
            if (controlling == null)
                return;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                controlling.BeginFire();
            else
                controlling.StopFire();
        }

        private static void DisablePlayerViewCameras(Transform player)
        {
            disabledCameras.Clear();

            tpCamera = Object.FindAnyObjectByType<vThirdPersonCamera>(FindObjectsInactive.Include);
            if (tpCamera != null)
            {
                tpWasEnabled = tpCamera.enabled;
                tpLockWasEnabled = tpCamera.LockCamera;
                tpCamera.LockCamera = true;
                tpCamera.enabled = false;

                Camera rigCamera = tpCamera.targetCamera != null
                    ? tpCamera.targetCamera
                    : tpCamera.GetComponent<Camera>();
                RegisterDisabledCamera(rigCamera);
            }

            if (player != null)
            {
                Camera[] onPlayer = player.GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < onPlayer.Length; i++)
                    RegisterDisabledCamera(onPlayer[i]);
            }

            Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                Camera cam = all[i];
                if (cam == null || cam == turretCamera || !cam.enabled)
                    continue;
                bool onPlayer = player != null && cam.transform.IsChildOf(player);
                if (onPlayer || cam.CompareTag("MainCamera"))
                    RegisterDisabledCamera(cam);
            }
        }

        private static void RegisterDisabledCamera(Camera cam)
        {
            if (cam == null || cam == turretCamera)
                return;
            for (int i = 0; i < disabledCameras.Count; i++)
            {
                if (disabledCameras[i].camera == cam)
                    return;
            }

            AudioListener listener = cam.GetComponent<AudioListener>();
            disabledCameras.Add(new DisabledCamera
            {
                camera = cam,
                wasEnabled = cam.enabled,
                listener = listener,
                listenerWasEnabled = listener != null && listener.enabled,
            });
            cam.enabled = false;
            if (listener != null)
                listener.enabled = false;
        }

        private static void RestorePlayerViewCameras()
        {
            for (int i = 0; i < disabledCameras.Count; i++)
            {
                DisabledCamera entry = disabledCameras[i];
                if (entry.camera != null)
                    entry.camera.enabled = entry.wasEnabled;
                if (entry.listener != null)
                    entry.listener.enabled = entry.listenerWasEnabled;
            }

            disabledCameras.Clear();

            if (tpCamera != null)
            {
                tpCamera.enabled = tpWasEnabled;
                tpCamera.LockCamera = tpLockWasEnabled;
            }
        }

        private static void EnsureTurretCamera(DMTurretRuntime runtime)
        {
            if (turretCamera != null)
                return;

            spawnedRuntimeCamera = false;
            Camera seat = runtime.ResolvePlayerSeatCamera();
            if (seat != null)
            {
                turretCamera = seat;
                turretListener = seat.GetComponent<AudioListener>();
            }
            else
            {
                GameObject go = new GameObject("DMTurretView");
                turretCamera = go.AddComponent<Camera>();
                turretListener = go.AddComponent<AudioListener>();
                spawnedRuntimeCamera = true;
            }

            savedTurretCameraTag = turretCamera.tag;
            turretCamera.tag = "MainCamera";
            turretCamera.nearClipPlane = 0.05f;
            DMTurretProfile profile = DMTurretProfile.Live;
            turretCamera.fieldOfView = profile != null
                ? profile.PlayerCameraFov
                : DMBuildingGhostProfile.TurretPlayerCameraFov;
            turretCamera.enabled = true;
            if (turretListener != null)
                turretListener.enabled = true;
            UpdateCameraPose();
        }

        private static void UpdateCameraPose()
        {
            if (turretCamera == null || controlling == null)
                return;

            if (controlling.UsesSwivelSeatCamera || !spawnedRuntimeCamera)
                return;

            Transform barrel = controlling.Barrel;
            float back = DMBuildingGhostProfile.TurretPlayerCameraBackMeters;
            float up = DMBuildingGhostProfile.TurretPlayerCameraUpMeters;
            Vector3 los = barrel.forward;
            turretCamera.transform.position = barrel.position - los * back + Vector3.up * up;
            turretCamera.transform.rotation = Quaternion.LookRotation(los, Vector3.up);
        }

        private static void AimFromLook()
        {
            if (controlling == null)
                return;

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0.0001f)
                {
                    DMTurretProfile profile = DMTurretProfile.Live;
                    float sens = profile != null ? profile.PlayerAimMouseSensitivity : 0.35f;
                    controlling.ApplySeatedLook(delta.x * sens, -delta.y * sens);
                    return;
                }
            }

            controlling.HoldSeatedAim();
        }
    }

    /// <summary>Ticks player-control while seated. Lives on the overlay host.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class DMTurretPlayerControlTicker : MonoBehaviour
    {
        private void Update()
        {
            if (!DMTurretPlayerControl.IsActive)
                return;
            DMTurretPlayerControl.Tick();
        }

        private void LateUpdate()
        {
            if (!DMTurretPlayerControl.IsActive)
                return;
            DMTurretPlayerControl.TickAimOnly();
        }
    }
}
