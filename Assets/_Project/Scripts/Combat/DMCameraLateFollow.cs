using Invector.vCamera;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-25c: drives Invector's vThirdPersonCamera in LateUpdate (every rendered frame) so it follows the
    /// interpolated player Rigidbody instead of the raw physics pose (walk jitter fix). Order 600 = after the
    /// player input LateUpdate (500, look input) and before DMCameraCollisionOverlay (10000) / scope (32000).
    /// Toggle: DM_CombatCoreProfile.cameraFollowInLateUpdate (applied live by the DMPlayerHitboxRig poll).
    /// </summary>
    [DefaultExecutionOrder(600)]
    [DisallowMultipleComponent]
    public sealed class DMCameraLateFollow : MonoBehaviour
    {
        private vThirdPersonCamera _cam;

        public static void Sync()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            vThirdPersonCamera.DMFollowInLateUpdate = p != null && p.cameraFollowInLateUpdate;
            vThirdPersonCamera cam = vThirdPersonCamera.instance;
            if (cam != null && cam.GetComponent<DMCameraLateFollow>() == null)
                cam.gameObject.AddComponent<DMCameraLateFollow>();
        }

        private void Awake() { _cam = GetComponent<vThirdPersonCamera>(); }

        private void LateUpdate()
        {
            if (_cam != null && _cam.enabled)
                _cam.DMLateTick();
        }
    }
}
