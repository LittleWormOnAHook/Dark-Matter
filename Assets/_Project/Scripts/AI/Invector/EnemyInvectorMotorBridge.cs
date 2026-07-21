using Invector.vCharacterController;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Drives Invector locomotion animator params from EnemyAiController transform movement.
    /// Motor params update in FixedUpdate; animator ticks once in LateUpdate when needed.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class EnemyInvectorMotorBridge : MonoBehaviour
    {
        private const float MoveSpeedThreshold = 0.08f;

        [SerializeField] private float motorLodDistance;

        private EnemyAiController _aiController;
        private EnemyCombat _enemyCombat;
        private vThirdPersonController _controller;
        private Rigidbody _body;
        private EnemyInvectorBootstrap _bootstrap;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private Transform _playerTransform;
        private bool _initialized;
        private bool _hasInputHorizontal;
        private bool _hasInputVertical;
        private bool _hasInputMagnitude;
        private bool _hasSpeed;

        private void Awake()
        {
            _aiController = GetComponent<EnemyAiController>();
            _enemyCombat = GetComponent<EnemyCombat>();
            _controller = GetComponent<vThirdPersonController>();
            _body = GetComponent<Rigidbody>();
            _bootstrap = GetComponent<EnemyInvectorBootstrap>();
            _health = GetComponent<EnemyHealth>();
            _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
        }

        private bool IsMotorBlocked =>
            _ragdollBridge != null &&
            (_ragdollBridge.IsHitStaggerActive || _ragdollBridge.IsCorpseRagdolled);

        private void Start()
        {
            CacheAnimatorParameters();
            CachePlayerTransform();
        }

        private void FixedUpdate()
        {
            if (_health != null && _health.IsDead)
                return;

            if (IsMotorBlocked)
                return;

            if (_controller == null || _aiController == null)
                return;

            if (!IsWithinMotorLod())
                return;

            _bootstrap?.EnsureInvectorInitialized();
            EnsureControllerReady();
            SyncRigidbodyToTransform();
            ApplyAiLocomotionMotor();
        }

        private void LateUpdate()
        {
            if (_health != null && _health.IsDead)
                return;

            if (IsMotorBlocked)
                return;

            if (_controller == null || _controller.animator == null || _aiController == null)
                return;

            if (_controller.animator.updateMode != AnimatorUpdateMode.Normal)
                _controller.animator.updateMode = AnimatorUpdateMode.Normal;

            if (!ShouldTickAnimator())
                return;

            _controller.UpdateAnimator();
        }

        private bool ShouldTickAnimator()
        {
            if (!IsWithinMotorLod())
                return false;

            if (_enemyCombat != null && _enemyCombat.IsAttacking)
                return true;

            if (_aiController.IsDefensiveActionActive)
                return true;

            if (_aiController.IsEngagedWithTarget)
                return true;

            return _aiController.CurrentLocomotionSpeed > MoveSpeedThreshold;
        }

        private bool IsWithinMotorLod()
        {
            float lodDistance = ResolveMotorLodDistance();
            if (lodDistance <= 0f)
                return true;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return true;

            Vector3 delta = transform.position - mainCamera.transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= lodDistance * lodDistance;
        }

        private float ResolveMotorLodDistance()
        {
            if (motorLodDistance > 0f)
                return motorLodDistance;

            return Project.Core.PlatformGraphicsProfile.HumanoidCullDistance;
        }

        private void CachePlayerTransform()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                _playerTransform = playerObject.transform;
        }

        private void EnsureControllerReady()
        {
            if (_initialized)
                return;

            _controller.lockMovement = true;
            _controller.useRootMotion = false;
            _controller.isGrounded = true;
            _initialized = true;
        }

        private void SyncRigidbodyToTransform()
        {
            if (_body == null || !_body.isKinematic)
                return;

            _body.MovePosition(transform.position);
            _body.MoveRotation(transform.rotation);
        }

        private void ApplyAiLocomotionMotor()
        {
            if (ShouldSuppressLocomotionAnimator())
            {
                ZeroLocomotionPresentation();
                _controller.isGrounded = true;
                return;
            }

            if (_aiController.IsEngagedWithTarget)
                _controller.isStrafing = false;

            float speed = _aiController.CurrentLocomotionSpeed;
            Vector3 worldDirection = transform.TransformDirection(_aiController.CurrentLocalMoveDirection);
            worldDirection.y = 0f;

            bool walkOnly = _aiController.IsWalkOnlyLocomotion;
            float sprintThreshold = walkOnly
                ? float.MaxValue
                : _aiController.ResolveChaseSpeed() * 0.92f;

            bool isMoving = speed > MoveSpeedThreshold && worldDirection.sqrMagnitude > 0.0001f;
            if (isMoving)
            {
                worldDirection.Normalize();
                _controller.moveDirection = worldDirection;
                Vector3 input = transform.InverseTransformDirection(worldDirection);
                if (walkOnly)
                    input = Vector3.ClampMagnitude(input, 0.5f);
                _controller.input = input;
                _controller.isSprinting = !walkOnly && speed >= sprintThreshold;
                _controller.UpdateMotor();
            }
            else
            {
                ZeroLocomotionPresentation();
            }

            _controller.isGrounded = true;

            var moveSpeed = _controller.isStrafing
                ? _controller.strafeSpeed
                : _controller.freeSpeed;

            _controller.SetAnimatorMoveSpeed(moveSpeed);
        }

        private void CacheAnimatorParameters()
        {
            if (_controller == null || _controller.animator == null)
                return;

            Animator animator = _controller.animator;
            _hasInputHorizontal = AnimatorHasParameter(animator, "InputHorizontal");
            _hasInputVertical = AnimatorHasParameter(animator, "InputVertical");
            _hasInputMagnitude = AnimatorHasParameter(animator, "InputMagnitude");
            _hasSpeed = AnimatorHasParameter(animator, "Speed");
        }

        private void ZeroLocomotionPresentation()
        {
            _controller.moveDirection = Vector3.zero;
            _controller.input = Vector3.zero;
            _controller.isSprinting = false;

            if (_controller.animator == null)
                return;

            Animator animator = _controller.animator;
            if (_hasInputHorizontal)
                animator.SetFloat("InputHorizontal", 0f);
            if (_hasInputVertical)
                animator.SetFloat("InputVertical", 0f);
            if (_hasInputMagnitude)
                animator.SetFloat("InputMagnitude", 0f);
            if (_hasSpeed)
                animator.SetFloat("Speed", 0f);
        }

        private static bool AnimatorHasParameter(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
                return false;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                if (animator.GetParameter(i).name == parameterName)
                    return true;
            }

            return false;
        }

        private bool ShouldSuppressLocomotionAnimator()
        {
            return (_enemyCombat != null && _enemyCombat.IsAttacking) ||
                   (_aiController != null && _aiController.IsDefensiveActionActive);
        }
    }
}
