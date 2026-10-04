using Invector.vCharacterController;
using Project.Core;
using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// Scales Invector's turn lean (RotationMagnitude) by the DM_ClimbDashProfile "turn lean" settings and
    /// optionally adds a small procedural upper-body roll. Invector writes the lean in FixedUpdate; this
    /// overwrites it in Update, before the animator (Normal update mode) evaluates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMLocomotionLeanDriver : MonoBehaviour
    {
        private vThirdPersonMotor motor;
        private Animator animator;
        private DMClimbController climb;
        private Transform spine;
        private Transform chest;

        private float lean;
        private float leanVelocity;
        private bool driving;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null || player.GetComponent<DMLocomotionLeanDriver>() != null)
                return;
            if (player.GetComponent<vThirdPersonMotor>() == null)
                return;

            player.AddComponent<DMLocomotionLeanDriver>();
        }

        private void Awake()
        {
            motor = GetComponent<vThirdPersonMotor>();
            climb = GetComponent<DMClimbController>();
        }

        private bool ResolveAnimator()
        {
            if (animator != null)
                return true;
            animator = motor != null && motor.animator != null ? motor.animator : GetComponentInChildren<Animator>();
            if (animator == null)
                return false;
            if (animator.isHuman)
            {
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            }
            return true;
        }

        private bool CanLean(DM_ClimbDashProfile p)
        {
            if (p == null || !p.leanOverrideEnabled || motor == null || !motor.useLeanMovementAnim)
                return false;
            if (motor.disableAnimations || motor.customAction || motor.isDead)
                return false;
            if (climb != null && climb.IsClimbing)
                return false;
            return motor.inputMagnitude >= 0.1f;
        }

        private void Update()
        {
            DM_ClimbDashProfile p = DM_ClimbDashProfile.Live;
            if (!ResolveAnimator())
                return;

            if (!CanLean(p))
            {
                // Hand RotationMagnitude back to Invector (turn-on-spot uses it while idle).
                lean = 0f;
                leanVelocity = 0f;
                driving = false;
                return;
            }

            float runT = Mathf.InverseLerp(0.5f, 1f, motor.inputMagnitude);
            float strength = Mathf.Lerp(p.leanWalkStrength, p.leanRunStrength, runT);
            float target = Mathf.Clamp(motor.rotationMagnitude * strength, -p.leanMaxValue, p.leanMaxValue);
            lean = Mathf.SmoothDamp(lean, target, ref leanVelocity, Mathf.Max(0.01f, p.leanSmoothTime), Mathf.Infinity, Time.deltaTime);
            animator.SetFloat(vAnimatorParameters.RotationMagnitude, lean);
            driving = true;
        }

        private void LateUpdate()
        {
            if (!driving || animator == null)
                return;
            DM_ClimbDashProfile p = DM_ClimbDashProfile.Live;
            if (p == null || p.leanSpineRollDegrees <= 0.01f || !motor.isGrounded)
                return;
            if (motor.isStrafing && !p.leanSpineRollWhileStrafing)
                return;

            float amount = Mathf.Clamp(lean / Mathf.Max(0.2f, p.leanMaxValue), -1f, 1f);
            float roll = -amount * p.leanSpineRollDegrees;
            if (Mathf.Abs(roll) < 0.01f)
                return;

            Vector3 axis = transform.forward;
            if (spine != null && chest != null)
            {
                spine.rotation = Quaternion.AngleAxis(roll * 0.5f, axis) * spine.rotation;
                chest.rotation = Quaternion.AngleAxis(roll * 0.5f, axis) * chest.rotation;
            }
            else if (spine != null)
            {
                spine.rotation = Quaternion.AngleAxis(roll, axis) * spine.rotation;
            }
        }
    }
}
