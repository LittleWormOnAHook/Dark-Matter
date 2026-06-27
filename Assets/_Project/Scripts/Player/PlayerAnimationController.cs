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

        [SerializeField] private float locomotionSmoothTime = 0.12f;

        private Character _character;
        private CombatFocusController _combatFocus;

        private void Awake()
        {
            _character = GetComponentInParent<Character>();
            _combatFocus = GetComponentInParent<CombatFocusController>();
        }

        private void Update()
        {
            if (_character == null) return;

            Animator animator = _character.GetAnimator();
            if (animator == null) return;

            float deltaTime = Time.deltaTime;
            bool isMoving = _character.GetSpeed() > 0.15f;
            bool isPickingUp = animator.GetCurrentAnimatorStateInfo(0).IsName("Pickup");

            if (isPickingUp && isMoving)
                animator.CrossFadeInFixedTime(PlayerCombatAnimationLayer.GroundedStateName, 0.15f);
            else if (isPickingUp)
                return;

            if (!PlayerCombatAnimationLayer.IsUpperBodyAttackPlaying(animator))
                PlayerCombatAnimationLayer.EnsureBaseLocomotionState(animator, _character.IsGrounded());

            Vector3 worldMove = _character.GetMovementDirection();
            if (worldMove.sqrMagnitude < 0.0001f)
            {
                Vector3 velocity = _character.GetVelocity();
                velocity.y = 0f;
                if (velocity.sqrMagnitude > 0.01f)
                    worldMove = velocity;
            }

            Transform moveReference = transform;
            if (_combatFocus != null && _combatFocus.IsLocked && _character.cameraTransform != null)
                moveReference = _character.cameraTransform;

            Vector3 localMove = worldMove.sqrMagnitude > 0.0001f
                ? moveReference.InverseTransformDirection(worldMove).normalized
                : Vector3.zero;

            float speedNorm = _character.useRootMotion && _character.GetRootMotionController()
                ? new Vector2(localMove.x, localMove.z).magnitude
                : Mathf.InverseLerp(0f, _character.GetMaxSpeed(), _character.GetSpeed());

            float forwardAmount = localMove.z * speedNorm;
            float turnAmount = localMove.x * speedNorm;

            animator.SetFloat(Forward, forwardAmount, locomotionSmoothTime, deltaTime);
            animator.SetFloat(Turn, turnAmount, locomotionSmoothTime, deltaTime);
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
