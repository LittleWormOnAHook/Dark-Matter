using Invector.vCharacterController;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-20i: Invector forces the player Animator to AnimatorUpdateMode.Fixed (vThirdPersonMotor.Init,
    /// PioneerInvectorBootstrap), so the pose only advances on physics ticks while the interpolated root moves every frame:
    /// visible model stepping / jitter at high FPS. When DM_CombatCoreProfile.playerAnimatorPerFrameUpdate is on this keeps
    /// the player Animator on Normal (re-applied every frame, so Init / revive / bootstrap resets are undone). With Normal,
    /// vShooterMeleeInput.LateUpdate's Fixed-only gate no longer skips, so aim IK runs every frame too.
    /// Off = restores Fixed once (Invector default).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class DMPlayerAnimatorUpdateMode : MonoBehaviour
    {
        private vThirdPersonMotor _motor;
        private bool _wasOn;

        public static void Ensure(GameObject player)
        {
            if (player != null && player.GetComponent<DMPlayerAnimatorUpdateMode>() == null)
                player.AddComponent<DMPlayerAnimatorUpdateMode>();
        }

        private void Awake() { _motor = GetComponent<vThirdPersonMotor>(); }

        private void Update()
        {
            Animator a = _motor != null ? _motor.animator : GetComponent<Animator>();
            if (a == null)
                return;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            bool on = p != null && p.playerAnimatorPerFrameUpdate;
            if (on)
            {
                if (a.updateMode != AnimatorUpdateMode.Normal)
                    a.updateMode = AnimatorUpdateMode.Normal;
            }
            else if (_wasOn && a.updateMode == AnimatorUpdateMode.Normal)
            {
                a.updateMode = AnimatorUpdateMode.Fixed;
            }
            _wasOn = on;
        }
    }
}
