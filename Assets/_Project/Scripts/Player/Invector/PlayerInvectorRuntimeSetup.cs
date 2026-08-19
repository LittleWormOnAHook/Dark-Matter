using System.Reflection;
using Invector.vCharacterController;
using Invector.vCamera;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Wires Invector camera/head-track references after Meshy avatar swaps and on play bootstrap.
    /// </summary>
    public static class PlayerInvectorRuntimeSetup
    {
        public static void Apply(GameObject root)
        {
            if (root == null)
                return;

            EnsureThirdPersonCameraRigidbody(root);

            PioneerShooterMeleeInput shooterInput = root.GetComponent<PioneerShooterMeleeInput>();
            vHeadTrack headTrack = root.GetComponent<vHeadTrack>();
            Camera gameplayCamera = ResolveGameplayCamera(root, shooterInput);
            if (headTrack != null && gameplayCamera != null)
                headTrack.cameraMain = gameplayCamera;
        }

        /// <summary>
        /// Invector's camera caches a private rigidbody reference. When one already exists on the
        /// prefab, AddComponent fails and FindCamera/Init NRE — prime the cache from GetComponent.
        /// </summary>
        public static void EnsureThirdPersonCameraRigidbody(GameObject root)
        {
            if (root == null)
                return;

            vThirdPersonCamera tpCamera = root.GetComponentInChildren<vThirdPersonCamera>(true);
            if (tpCamera == null)
                return;

            Rigidbody body = tpCamera.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = tpCamera.gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.None;
            }

            FieldInfo field = typeof(vThirdPersonCamera).GetField(
                "_selfRigidbody",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(tpCamera, body);
        }

        public static void SuppressForeignPlayerInputs(PlayerInput activeInput)
        {
            if (activeInput == null)
                return;

            PlayerInput[] allInputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include);

            PioneerInvectorBootstrap activeBootstrap = PioneerInvectorBootstrap.Instance;
            GameObject activeRoot = activeBootstrap != null ? activeBootstrap.gameObject : activeInput.gameObject;

            for (int i = 0; i < allInputs.Length; i++)
            {
                PlayerInput candidate = allInputs[i];
                if (candidate == null || candidate == activeInput)
                    continue;

                if (candidate.gameObject == activeRoot)
                    continue;

                if (!candidate.gameObject.activeInHierarchy)
                {
                    candidate.enabled = false;
                    continue;
                }

                if (candidate.GetComponent<PioneerInvectorBootstrap>() != null)
                    candidate.enabled = false;
            }
        }

        public static Camera ResolveGameplayCamera(GameObject root, PioneerShooterMeleeInput shooterInput = null)
        {
            if (shooterInput == null && root != null)
                shooterInput = root.GetComponent<PioneerShooterMeleeInput>();

            if (shooterInput != null && shooterInput.tpCamera != null && shooterInput.tpCamera.targetCamera != null)
                return shooterInput.tpCamera.targetCamera;

            if (root != null)
            {
                vThirdPersonCamera tpCamera = root.GetComponentInChildren<vThirdPersonCamera>(true);
                if (tpCamera != null && tpCamera.targetCamera != null)
                    return tpCamera.targetCamera;

                Camera childCamera = root.GetComponentInChildren<Camera>(true);
                if (childCamera != null)
                    return childCamera;
            }

            return Camera.main;
        }
    }
}
