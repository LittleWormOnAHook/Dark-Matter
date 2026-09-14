using Invector.vCharacterController;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Hybrid locomotion: CompanionFollowController owns translation; this bridge drives Invector
    /// Free Locomotion animator params (same tree as Player_v7 Variant).
    /// </summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public class CompanionInvectorMotorBridge : MonoBehaviour
    {
        private const float MoveEnterThreshold = 0.18f;
        private const float MoveExitThreshold = 0.08f;
        private const float IdleHoldSeconds = 0.2f;
        private const float AnimDamp = 0.12f;
        private const float StrafeForwardCutoff = 0.12f;
        private const float StrafeLateralCutoff = 0.08f;
        private static readonly int LocomotionAnimSpeed = Animator.StringToHash("LocomotionAnimSpeed");

        private CompanionFollowController _followController;
        private CompanionCombatController _combatController;
        private CompanionInvectorBootstrap _bootstrap;
        private vThirdPersonController _controller;
        private bool _initialized;
        private bool _animMoving;
        private float _stoppedSeconds;
        private Vector3 _lastMeasuredPosition;
        private bool _hasMeasuredPosition;
        private bool _locomotionAnimSpeedAvailable;
        private bool _animatorParamsCached;

        private void Awake()
        {
            _followController = GetComponent<CompanionFollowController>();
            _bootstrap = GetComponent<CompanionInvectorBootstrap>();
            _controller = GetComponent<vThirdPersonController>();
            _lastMeasuredPosition = transform.position;
            _hasMeasuredPosition = true;
        }

        private void FixedUpdate()
        {
            if (_controller == null || _followController == null)
                return;

            _bootstrap?.EnsureInvectorInitialized();
            EnsureControllerReady();
        }

        private void LateUpdate()
        {
            if (_controller == null || _followController == null)
                return;

            EnsureAnimatorReady();
            if (_controller.animator == null)
                return;

            if (_controller.animator.updateMode != AnimatorUpdateMode.Normal)
                _controller.animator.updateMode = AnimatorUpdateMode.Normal;

            EnsureLocomotionAnimatorWrites();
            CacheAnimatorParameters();

            float deltaTime = Time.deltaTime;
            float measuredSpeed = SampleMeasuredSpeed(deltaTime);
            ApplyFollowLocomotionMotor(measuredSpeed, deltaTime);
            WriteLocomotionAnimatorParams(measuredSpeed, deltaTime);
        }

        private float SampleMeasuredSpeed(float deltaTime)
        {
            Vector3 position = transform.position;
            if (!_hasMeasuredPosition)
            {
                _lastMeasuredPosition = position;
                _hasMeasuredPosition = true;
                return 0f;
            }

            Vector3 delta = position - _lastMeasuredPosition;
            _lastMeasuredPosition = position;
            delta.y = 0f;
            if (deltaTime <= 0.0001f)
                return 0f;

            return delta.magnitude / deltaTime;
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

        private void CacheAnimatorParameters()
        {
            if (_animatorParamsCached)
                return;

            Animator animator = _controller.animator;
            if (animator == null)
                return;

            _locomotionAnimSpeedAvailable = HasFloatParameter(animator, LocomotionAnimSpeed);
            _animatorParamsCached = true;
        }

        private void ApplyFollowLocomotionMotor(float measuredSpeed, float deltaTime)
        {
            LockFreeLocomotion();

            bool meleeLocked = ShouldSuppressLocomotionAnimator();
            float controllerSpeed = meleeLocked ? 0f : _followController.CurrentSpeed;
            float speed = ResolvePresentationSpeed(measuredSpeed, controllerSpeed);
            Vector3 worldDirection = meleeLocked ? Vector3.zero : _followController.CurrentMoveDirection;
            worldDirection.y = 0f;

            if (speed > MoveEnterThreshold)
            {
                _animMoving = true;
                _stoppedSeconds = 0f;
            }
            else if (speed <= MoveExitThreshold)
            {
                _stoppedSeconds += deltaTime;
                if (_stoppedSeconds >= IdleHoldSeconds)
                    _animMoving = false;
            }
            else
            {
                _stoppedSeconds = 0f;
            }

            _controller.isGrounded = true;
            _controller.useRootMotion = false;
            _controller.lockMovement = true;

            if (!_animMoving || speed <= MoveExitThreshold)
            {
                _controller.moveDirection = Vector3.zero;
                _controller.input = Vector3.zero;
                _controller.isSprinting = false;
                _controller.isStrafing = false;
                return;
            }

            if (worldDirection.sqrMagnitude > 0.0001f)
                worldDirection.Normalize();
            else
                worldDirection = transform.forward;

            float walk = _followController.WalkSpeed;
            float run = Mathf.Max(walk + 0.01f, _followController.RunSpeed);
            bool sprinting = speed >= run * 0.85f;

            Vector3 local = transform.InverseTransformDirection(worldDirection);
            local.y = 0f;
            if (local.sqrMagnitude > 0.0001f)
                local.Normalize();

            bool strafe = Mathf.Abs(local.x) > Mathf.Abs(local.z) * 0.65f
                && Mathf.Abs(local.x) > StrafeLateralCutoff;
            _controller.isStrafing = strafe;
            _controller.isSprinting = sprinting;

            Vector3 input = new Vector3(
                strafe ? Mathf.Clamp(local.x, -1f, 1f) : 0f,
                0f,
                strafe ? Mathf.Clamp(local.z, -1f, 1f) : Mathf.Clamp01(local.z));

            if (!strafe && local.z > StrafeForwardCutoff)
                input.z = 1f;

            input = sprinting ? Vector3.ClampMagnitude(input, 1f) : Vector3.ClampMagnitude(input, 0.5f);

            _controller.moveDirection = worldDirection;
            _controller.input = input;
            _controller.UpdateMotor();

            var moveSpeed = _controller.isStrafing ? _controller.strafeSpeed : _controller.freeSpeed;
            _controller.SetAnimatorMoveSpeed(moveSpeed);
        }

        private void WriteLocomotionAnimatorParams(float measuredSpeed, float deltaTime)
        {
            Animator animator = _controller.animator;
            if (animator == null || !animator.isInitialized)
                return;

            float controllerSpeed = _followController.CurrentSpeed;
            float speed = ResolvePresentationSpeed(measuredSpeed, controllerSpeed);
            float walk = _followController.WalkSpeed;
            float run = Mathf.Max(walk + 0.01f, _followController.RunSpeed);

            float magnitude = 0f;
            float inputVertical = 0f;
            float inputHorizontal = 0f;

            if (_animMoving && speed > MoveExitThreshold)
            {
                bool sprinting = speed >= run * 0.85f;
                magnitude = sprinting
                    ? Mathf.Clamp(speed / run, 0.55f, 1f)
                    : Mathf.Clamp((speed / walk) * 0.5f, 0.1f, 0.55f);

                Vector3 worldDirection = _followController.CurrentMoveDirection;
                worldDirection.y = 0f;
                if (worldDirection.sqrMagnitude > 0.0001f)
                {
                    worldDirection.Normalize();
                    Vector3 local = transform.InverseTransformDirection(worldDirection);
                    local.y = 0f;
                    if (local.sqrMagnitude > 0.0001f)
                        local.Normalize();

                    bool strafe = _controller.isStrafing;
                    inputVertical = strafe ? Mathf.Clamp(local.z, -1f, 1f) : Mathf.Clamp01(local.z);
                    inputHorizontal = strafe ? Mathf.Clamp(local.x, -1f, 1f) : 0f;
                    if (!strafe && local.z > StrafeForwardCutoff)
                        inputVertical = 1f;
                }
                else
                {
                    inputVertical = 1f;
                }
            }

            _controller.inputMagnitude = magnitude;
            _controller.verticalSpeed = inputVertical;
            _controller.horizontalSpeed = inputHorizontal;
            animator.SetFloat(vAnimatorParameters.InputHorizontal, inputHorizontal, AnimDamp, deltaTime);
            animator.SetFloat(vAnimatorParameters.InputVertical, inputVertical, AnimDamp, deltaTime);
            animator.SetFloat(vAnimatorParameters.InputMagnitude, magnitude, AnimDamp, deltaTime);
            animator.SetFloat(vAnimatorParameters.RotationMagnitude, 0f);
            animator.SetBool(vAnimatorParameters.IsStrafing, _controller.isStrafing);
            animator.SetBool(vAnimatorParameters.IsGrounded, true);
            animator.SetBool(vAnimatorParameters.IsSprinting, _controller.isSprinting);

            if (!_locomotionAnimSpeedAvailable)
                return;

            float animSpeed = 0.75f;
            if (_animMoving && speed > MoveExitThreshold)
            {
                if (speed >= run * 0.85f)
                {
                    float runRatio = speed / run;
                    animSpeed = Mathf.Clamp(0.9f * runRatio, 0.45f, 1.2f);
                }
                else
                {
                    float walkRatio = speed / walk;
                    animSpeed = Mathf.Clamp(0.7f * walkRatio, 0.12f, 1f);
                }
            }

            animator.SetFloat(LocomotionAnimSpeed, animSpeed, AnimDamp, deltaTime);
        }

        private static float ResolvePresentationSpeed(float measuredSpeed, float controllerSpeed)
        {
            if (measuredSpeed > MoveExitThreshold)
                return measuredSpeed;

            if (controllerSpeed > MoveEnterThreshold)
                return controllerSpeed;

            return 0f;
        }

        private static bool HasFloatParameter(Animator target, int parameterHash)
        {
            if (target == null)
                return false;

            for (int i = 0; i < target.parameterCount; i++)
            {
                AnimatorControllerParameter parameter = target.GetParameter(i);
                if (parameter.type == AnimatorControllerParameterType.Float
                    && parameter.nameHash == parameterHash)
                    return true;
            }

            return false;
        }

        private bool ShouldSuppressLocomotionAnimator()
        {
            if (_combatController == null)
                _combatController = GetComponent<CompanionCombatController>();

            return _combatController != null
                && _combatController.IsAttackPending
                && !_followController.IsRangedCombatEngaged;
        }
    }
}
