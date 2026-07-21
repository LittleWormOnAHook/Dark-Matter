using Invector.vCharacterController;
using Invector.vShooter;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// No-op aim canvas so Invector shooter input skips the missing AimCanvas warning.
    /// Pioneer uses its own combat HUD for aim feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerInvectorNullAimCanvas : vControlAimCanvas
    {
        public override void Init(vThirdPersonController controller)
        {
        }

        public new void SetActiveAim(bool value)
        {
        }

        public new void SetActiveScopeCamera(bool value, bool useUi = false)
        {
        }

        public new void DisableAim()
        {
        }

        public new void DisableScopeCamera()
        {
        }

        public new void SetAimCanvasID(int id)
        {
        }

        public new void SetAimToCenter(bool validPoint = true)
        {
        }

        public new void SetWordPosition(Vector3 wordPosition, bool validPoint = true)
        {
        }

        public new void SetWordPosition(Vector3 centerPosition, Vector3 targetPosition, bool validPoint = true)
        {
        }

        public new void UpdateScopeCamera(
            Vector3 position,
            Vector3 lookDirection,
            Vector3 upDirection,
            float zoom = 60,
            bool isAiming = false)
        {
        }
    }
}
