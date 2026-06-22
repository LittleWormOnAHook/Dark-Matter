using UnityEngine;
using ECM2;

namespace Project.Player
{
    /// <summary>
    /// Drives the ECM2 UnityCharacter animator from Character movement state.
    /// Attach to the UnityCharacter model (child of the player root).
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Forward = Animator.StringToHash("Forward");
        private static readonly int Turn = Animator.StringToHash("Turn");
        private static readonly int Ground = Animator.StringToHash("OnGround");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int JumpLeg = Animator.StringToHash("JumpLeg");

        private Character _character;

        private void Awake()
        {
            _character = GetComponentInParent<Character>();
        }

        private void Update()
        {
            if (_character == null) return;

            Animator animator = _character.GetAnimator();
            if (animator == null) return;

            float deltaTime = Time.deltaTime;
            bool isPickingUp = animator.GetCurrentAnimatorStateInfo(0).IsName("Pickup");
            bool isAttacking = animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

            if (isPickingUp && _character.GetSpeed() > 0.15f)
            {
                animator.CrossFadeInFixedTime("Grounded", 0.15f);
                return;
            }

            if (isPickingUp || isAttacking)
                return;

            Vector3 move = transform.InverseTransformDirection(_character.GetMovementDirection());

            float forwardAmount = _character.useRootMotion && _character.GetRootMotionController()
                ? move.z
                : Mathf.InverseLerp(0f, _character.GetMaxSpeed(), _character.GetSpeed());

            animator.SetFloat(Forward, forwardAmount, 0.1f, deltaTime);
            animator.SetFloat(Turn, Mathf.Atan2(move.x, move.z), 0.1f, deltaTime);
            animator.SetBool(Ground, _character.IsGrounded());
            animator.SetBool(Crouch, _character.IsCrouched());

            if (_character.IsFalling())
                animator.SetFloat(Jump, _character.GetVelocity().y, 0.1f, deltaTime);

            float runCycle = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime + 0.2f, 1f);
            float jumpLeg = (runCycle < 0.5f ? 1f : -1f) * forwardAmount;

            if (_character.IsGrounded())
                animator.SetFloat(JumpLeg, jumpLeg);
        }
    }

}
