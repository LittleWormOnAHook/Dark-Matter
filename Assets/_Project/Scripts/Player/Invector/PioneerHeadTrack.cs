using Invector.vCharacterController;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Head track with null-safe camera access during the first frames before Camera.main is ready.
    /// </summary>[DisallowMultipleComponent]
    public class PioneerHeadTrack : vHeadTrack
    {
        protected override Vector3 GetLookPoint()
        {
            if (cameraMain == null)
                cameraMain = ResolveGameplayCamera();

            if (cameraMain == null)
            {
                LookDirection = transform.forward;
                return headPoint + (transform.forward * 100f);
            }

            return base.GetLookPoint();
        }

        private Camera ResolveGameplayCamera()
        {
            PioneerShooterMeleeInput shooterInput = GetComponent<PioneerShooterMeleeInput>();
            if (shooterInput != null && shooterInput.tpCamera != null && shooterInput.tpCamera.targetCamera != null)
                return shooterInput.tpCamera.targetCamera;

            Camera main = Camera.main;
            if (main != null)
                return main;

            return GetComponentInChildren<Camera>(true);
        }
    }
}
