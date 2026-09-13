using Invector.vCharacterController;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Hybrid locomotion: CompanionFollowController owns translation; this bridge drives Invector
    /// Free Locomotion animator params (same tree as Player_v7 Variant).
    /// </summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public class CompanionInvectorMotorBridge : MonoBehaviour
    {
        private const float MoveEnterThreshold = 0.18f;
        private const float MoveExitThreshold = 0.08f;
        private const float IdleHoldSeconds = 0.2f;
        private const float AnimDamp = 0.1f;

        private CompanionFollowController _followController;
        private CompanionCombatController _combatController;
        private CompanionInvectorBootstrap _bootstrap;
        private vThirdPersonController _controller;
        private bool _initialized;
        private bool _animMoving;
        private float _stoppedSeconds;

        private void Awake()
        {
            _followController = GetComponent<CompanionFollowController>();
            _bootstrap = GetComponent<CompanionInvectorBootstrap>();
            _controller = GetComponent<vThirdPersonController>();
        }

        private void FixedUpdate()
        {
            if (_controller == null || _followController == null)
                return;

            _bootstrap?.EnsureInvectorInitialized();
            EnsureControllerReady();
        }

        private void Update()
        {
            if (_controller == null || _followController == null)
                return;

            EnsureAnimatorReady();
            if (_controller.animator == null)
                return;

            if (_controller.animator.updateMode != AnimatorUpdateMode.Normal)
                _controller.animator.updateMode = AnimatorUpdateMode.Normal;

            EnsureLocomotionAnimatorWrites();
            // Order 120: after FollowController.Update, before Animator.Normal evaluates.
            ApplyFollowLocomotionMotor();
            WriteLocomotionAnimatorParams();
        }

        private void EnsureControllerReady()
        {
            LockFreeLocomotion();

            if (_initialized)
                return;

            _controller.lockMovement = true;
            _controller.useRootMotion = false;
            _controller.isStrafing = false;
            _controller.isGrounded = true;
            _initialized = true;
        }

        private void LockFreeLocomotion()
        {
            if (_controller.locomotionType != vThirdPersonMotor.LocomotionType.FreeWithStrafe)
                _controller.locomotionType = vThirdPersonMotor.LocomotionType.FreeWithStrafe;
            if (_controller.useLeanMovementAnim)
                _controller.useLeanMovementAnim = false;
            if (_controller.useTurnOnSpotAnim)
                _controller.useTurnOnSpotAnim = false;
            if (_controller.lockInStrafe)
                _controller.lockInStrafe = false;
        }

        private void EnsureAnimatorReady()
        {
            if (_controller.animator == null)
            {
                _controller.Init();
                if (_controller.animator == null)
                    return;
            }

            Animator animator = _controller.animator;
            if (!animator.enabled)
                animator.enabled = true;

            animator.applyRootMotion = false;
        }

        private void EnsureLocomotionAnimatorWrites()
        {
            Animator animator = _controller.animator;
            if (animator == null)
                return;

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void ApplyFollowLocomotionMotor()
        {
            LockFreeLocomotion();

            bool meleeLocked = ShouldSuppressLocomotionAnimator();
            float speed = meleeLocked ? 0f : _followController.CurrentSpeed;
            Vector3 worldDirection = meleeLocked ? Vector3.zero : _followController.CurrentMoveDirection;
            worldDirection.y = 0f;

            if (speed > MoveEnterThreshold)
            {
                _animMoving = true;
                _stoppedSeconds = 0f;
            }
            else if (speed <= MoveExitThreshold)
            {
                _stoppedSeconds += Time.deltaTime;
                if (_stoppedSeconds >= IdleHoldSeconds)
                    _animMoving = false;
            }
            else
            {
                _stoppedSeconds = 0f;
            }

            _controller.isStrafing = false;
            _controller.isGrounded = true;
            _controller.useRootMotion = false;
            _controller.lockMovement = true;

            if (_animMoving)
            {
                if (worldDirection.sqrMagnitude > 0.0001f)
                    worldDirection.Normalize();
                else
                    worldDirection = transform.forward;

                _controller.moveDirection = worldDirection;
                _controller.input = Vector3.forward;
                _controller.isSprinting = speed >= _followController.RunSpeed * 0.85f;
            }
            else
            {
                _controller.moveDirection = Vector3.zero;
                _controller.input = Vector3.zero;
                _controller.isSprinting = false;
            }
        }

        private void WriteLocomotionAnimatorParams()
        {
            Animator animator = _controller.animator;
            if (animator == null || !animator.isInitialized)
                return;

            float magnitude = 0f;
            float vertical = 0f;
            if (_animMoving)
            {
                magnitude = _controller.isSprinting ? 1f : 0.5f;
                vertical = 1f;
            }

            _controller.inputMagnitude = magnitude;
            _controller.verticalSpeed = vertical;
            _controller.horizontalSpeed = 0f;
            animator.SetFloat(vAnimatorParameters.InputHorizontal, 0f, AnimDamp, Time.deltaTime);
            animator.SetFloat(vAnimatorParameters.InputVertical, vertical, AnimDamp, Time.deltaTime);
            animator.SetFloat(vAnimatorParameters.InputMagnitude, magnitude, AnimDamp, Time.deltaTime);
            animator.SetFloat(vAnimatorParameters.RotationMagnitude, 0f);
            animator.SetBool(vAnimatorParameters.IsStrafing, false);
            animator.SetBool(vAnimatorParameters.IsGrounded, true);
            animator.SetBool(vAnimatorParameters.IsSprinting, _controller.isSprinting);
        }

        private bool ShouldSuppressLocomotionAnimator()
        {
            if (_combatController == null)
                _combatController = GetComponent<CompanionCombatController>();

            // Only freeze legs for a pending melee beat. Ranged fire still kites/walks.
            return _combatController != null
                && _combatController.IsAttackPending
                && !_followController.IsRangedCombatEngaged;
        }
    }
}
