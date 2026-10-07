using Invector.vCharacterController;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Drives Invector locomotion animator params from EnemyAiController transform movement.
    /// Motor params update in FixedUpdate; animator ticks once in LateUpdate when needed.
    /// With driveLocomotionFromMeasuredVelocity, InputMagnitude / InputVertical / InputHorizontal come
    /// from the root's measured planar velocity (local forward/sideways) so the walk/run cycle matches
    /// how fast the AI really translates the transform, and engaged sideways/backward moves use the
    /// Strafing Movement blend tree instead of the forward-only Free Movement tree.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class EnemyInvectorMotorBridge : MonoBehaviour
    {
        private const float MoveSpeedThreshold = 0.08f;

        [SerializeField] private float motorLodDistance;

        [Header("Velocity-driven locomotion (DM)")]
        [Tooltip("Feed locomotion params from the measured root velocity instead of the AI's intended speed.")]
        [SerializeField] private bool driveLocomotionFromMeasuredVelocity = true;
        [Tooltip("Root speed (m/s at Animator.humanScale 1) of Free Movement at InputMagnitude 0.5 / 1 / 1.5. Measured on Invector@ShooterMelee (FRED humanScale 1.073: 1.87 / 4.15 / 6.34 m/s).")]
        [SerializeField] private float freeWalkClipSpeed = 1.74f;
        [SerializeField] private float freeRunClipSpeed = 3.87f;
        [SerializeField] private float freeSprintClipSpeed = 5.91f;
        [Tooltip("Root speed (m/s at humanScale 1) of Strafing Movement at InputMagnitude 0.5 (walk ring) and 1 (run ring).")]
        [SerializeField] private float strafeWalkClipSpeed = 1.5f;
        [SerializeField] private float strafeRunClipSpeed = 2.8f;
        [Tooltip("While engaged, moves more than this many degrees off the facing direction use strafe clips.")]
        [SerializeField, Range(10f, 90f)] private float strafeAngleThreshold = 40f;
        [Tooltip("Smoothing time constant (seconds) for the measured root velocity.")]
        [SerializeField] private float measuredVelocitySmoothing = 0.08f;

        private EnemyAiController _aiController;
        private EnemyCombat _enemyCombat;
        private vThirdPersonController _controller;
        private Rigidbody _body;
        private EnemyInvectorBootstrap _bootstrap;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private DMSpawnPhysicsStabilizer _stabilizer;
        private UnityEngine.AI.NavMeshAgent _navAgent;
        private Transform _playerTransform;
        private bool _initialized;
        private bool _hasInputHorizontal;
        private bool _hasInputVertical;
        private bool _hasInputMagnitude;
        private bool _hasSpeed;
        private bool _animatorParamsCached;
        private bool _hasIsAiming;
        private Vector3 _measuredVelocity;
        private Vector3 _lastSamplePosition;
        private bool _hasLastSamplePosition;
        private bool _strafeLatched;
        private const float MaxPlausibleSpeed = 14f;
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");

        private void Awake()
        {
            _aiController = GetComponent<EnemyAiController>();
            _enemyCombat = GetComponent<EnemyCombat>();
            _controller = GetComponent<vThirdPersonController>();
            _body = GetComponent<Rigidbody>();
            _bootstrap = GetComponent<EnemyInvectorBootstrap>();
            _health = GetComponent<EnemyHealth>();
            _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            _stabilizer = GetComponent<DMSpawnPhysicsStabilizer>();
            _navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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

            if (_stabilizer == null)
                _stabilizer = GetComponent<DMSpawnPhysicsStabilizer>();
            if (_stabilizer != null && _stabilizer.IsSpawnSettleActive)
                return;

            if (IsMotorBlocked)
                return;

            if (_controller == null || _aiController == null)
                return;

            if (!IsWithinMotorLod())
                return;

            _bootstrap?.EnsureInvectorInitialized();
            EnsureControllerReady();
            EnsureAnimatorReady();
            SyncRigidbodyToTransform();
            ApplyAiLocomotionMotor();
            DMSpawnPhysicsStabilizer.KeepLivingRootKinematic(gameObject);
        }

        private void LateUpdate()
        {
            // Sample every frame (even while blocked) so the first frame after a stagger has no spike.
            SampleMeasuredVelocity();

            if (_health != null && _health.IsDead)
                return;

            if (_stabilizer != null && _stabilizer.IsSpawnSettleActive)
                return;

            if (IsMotorBlocked)
                return;

            if (_controller == null || _aiController == null)
                return;

            EnsureAnimatorReady();
            if (_controller.animator == null)
                return;

            if (_controller.animator.updateMode != AnimatorUpdateMode.Normal)
                _controller.animator.updateMode = AnimatorUpdateMode.Normal;

            if (!ShouldTickAnimator())
            {
                if (driveLocomotionFromMeasuredVelocity)
                    ZeroLocomotionAnimatorFloats();
                return;
            }

            // Engaged / moving humanoids must keep bone writes alive — CullUpdateTransforms
            // intermittently freezes the last pose while NavMesh still translates the root.
            EnsureLocomotionAnimatorWrites();

            if (driveLocomotionFromMeasuredVelocity)
                ApplyMeasuredVelocityLocomotion();

            _controller.UpdateAnimator();
        }

        private void SampleMeasuredVelocity()
        {
            Vector3 position = transform.position;
            float dt = Time.deltaTime;
            if (_hasLastSamplePosition && dt > 0.0001f)
            {
                Vector3 delta = position - _lastSamplePosition;
                delta.y = 0f;
                Vector3 velocity = delta / dt;
                if (velocity.sqrMagnitude > MaxPlausibleSpeed * MaxPlausibleSpeed)
                    velocity = Vector3.zero; // teleport / spawn snap / ragdoll get-up

                float blend = measuredVelocitySmoothing > 0.0001f
                    ? 1f - Mathf.Exp(-dt / measuredVelocitySmoothing)
                    : 1f;
                _measuredVelocity = Vector3.Lerp(_measuredVelocity, velocity, blend);
                if (_measuredVelocity.sqrMagnitude < 0.0004f)
                    _measuredVelocity = Vector3.zero;
            }

            _lastSamplePosition = position;
            _hasLastSamplePosition = true;
        }

        /// <summary>
        /// Writes Invector's locomotion fields from the measured root velocity right before
        /// UpdateAnimator: InputMagnitude is chosen so the blend tree's clip speed equals the real
        /// speed; engaged sideways/backward motion uses strafe locomotion with local direction.
        /// </summary>
        private void ApplyMeasuredVelocityLocomotion()
        {
            Animator animator = _controller.animator;
            if (animator == null)
                return;

            float scale = animator.isHuman ? Mathf.Max(0.1f, animator.humanScale) : 1f;
            bool aiming = _hasIsAiming && animator.GetBool(IsAimingHash);
            bool engaged = _aiController != null && _aiController.IsEngagedWithTarget;
            float speed = _measuredVelocity.magnitude;

            if (speed <= MoveSpeedThreshold)
            {
                _controller.isSprinting = false;
                _controller.inputMagnitude = 0f;
                _controller.verticalSpeed = 0f;
                _controller.horizontalSpeed = 0f;
                // Keep the last locomotion family while idle so Free/Strafe does not flap.
                _strafeLatched = aiming || (engaged && _strafeLatched);
                _controller.isStrafing = _strafeLatched;
                return;
            }

            Vector3 localDirection = transform.InverseTransformDirection(_measuredVelocity / speed);
            localDirection.y = 0f;
            if (localDirection.sqrMagnitude > 0.0001f)
                localDirection.Normalize();
            else
                localDirection = Vector3.forward;

            float offFacing = Mathf.Abs(Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg);
            float strafeWalk = Mathf.Max(0.1f, strafeWalkClipSpeed * scale);
            float strafeRun = Mathf.Max(strafeWalk + 0.1f, strafeRunClipSpeed * scale);
            float angleGate = _strafeLatched ? Mathf.Max(0f, strafeAngleThreshold - 10f) : strafeAngleThreshold;
            bool strafe = aiming || (engaged && offFacing > angleGate && speed <= strafeRun * 1.15f);
            _strafeLatched = strafe;
            _controller.isStrafing = strafe;

            if (strafe)
            {
                float magnitude = speed <= strafeWalk
                    ? 0.5f * speed / strafeWalk
                    : Mathf.Lerp(0.5f, 1f, Mathf.Clamp01((speed - strafeWalk) / (strafeRun - strafeWalk)));
                float ring = Mathf.Max(0.5f, magnitude);
                _controller.isSprinting = false;
                _controller.inputMagnitude = magnitude;
                _controller.verticalSpeed = localDirection.z * ring;
                _controller.horizontalSpeed = localDirection.x * ring;
                return;
            }

            float freeMagnitude = FreeInputMagnitudeForSpeed(speed, scale);
            _controller.isSprinting = freeMagnitude > 1.001f;
            _controller.inputMagnitude = freeMagnitude;
            _controller.verticalSpeed = localDirection.z;
            _controller.horizontalSpeed = 0f;
        }

        private float FreeInputMagnitudeForSpeed(float speed, float scale)
        {
            float walk = Mathf.Max(0.1f, freeWalkClipSpeed * scale);
            float run = Mathf.Max(walk + 0.1f, freeRunClipSpeed * scale);
            float sprint = Mathf.Max(run + 0.1f, freeSprintClipSpeed * scale);
            if (speed <= walk)
                return 0.5f * speed / walk;
            if (speed <= run)
                return Mathf.Lerp(0.5f, 1f, (speed - walk) / (run - walk));
            return Mathf.Lerp(1f, 1.5f, Mathf.Clamp01((speed - run) / (sprint - run)));
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

            return ResolvePresentationSpeed() > MoveSpeedThreshold;
        }

        private bool IsWithinMotorLod()
        {
            float lodDistance = ResolveMotorLodDistance();
            if (lodDistance <= 0f)
                return true;

            // Never LOD-throttle animator/motor while engaged — that produces chase glides
            // at the cull-distance boundary when NavMesh is still driving the root.
            if (_aiController != null && _aiController.IsEngagedWithTarget)
                return true;

            if (ResolvePresentationSpeed() > MoveSpeedThreshold)
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

            _controller.lockMovement = _stabilizer != null && _stabilizer.IsSpawnSettleActive;
            _controller.useRootMotion = false;
            _controller.isGrounded = true;
            _controller.extraGravity = 0f;
            _controller.verticalVelocity = 0f;
            if (_controller.animator != null)
                _controller.animator.applyRootMotion = false;
            _initialized = true;
        }

        private void EnsureAnimatorReady()
        {
            if (_controller == null)
                return;

            Animator animator = _controller.animator;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                    return;

                // Init() assigns the animator reference; recover if a mid-chase Rebind/equip race cleared it.
                _controller.Init();
                animator = _controller.animator != null ? _controller.animator : animator;
                _animatorParamsCached = false;
            }

            if (!animator.enabled)
                animator.enabled = true;

            animator.applyRootMotion = false;

            if (!_animatorParamsCached)
                CacheAnimatorParameters();
        }

        private void EnsureLocomotionAnimatorWrites()
        {
            Animator animator = _controller != null ? _controller.animator : null;
            if (animator == null)
                return;

            bool needsBoneWrites =
                (_aiController != null && _aiController.IsEngagedWithTarget) ||
                ResolvePresentationSpeed() > MoveSpeedThreshold;

            if (!needsBoneWrites)
                return;

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!animator.enabled)
                animator.enabled = true;
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
            // Attack / block may hold upper-body poses, but zeroing locomotion while the root
            // still translates (combat-ring shuffle, chase residual) is the intermittent glide.
            if (ShouldSuppressLocomotionAnimator() && ResolvePresentationSpeed() <= MoveSpeedThreshold)
            {
                ZeroLocomotionPresentation();
                _controller.isGrounded = true;
                return;
            }

            // Velocity-driven mode decides Free vs Strafe in LateUpdate from the real move direction.
            if (_aiController.IsEngagedWithTarget && !driveLocomotionFromMeasuredVelocity)
                _controller.isStrafing = false;

            float speed = ResolvePresentationSpeed();
            Vector3 worldDirection = ResolvePresentationWorldDirection();
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
            _controller.useRootMotion = false;

            var moveSpeed = _controller.isStrafing
                ? _controller.strafeSpeed
                : _controller.freeSpeed;

            _controller.SetAnimatorMoveSpeed(moveSpeed);
        }

        /// <summary>
        /// Prefer AI locomotion, but fall back to rigidbody/transform delta so brief AI zeroing
        /// (attack enter, stamina pause edge, nav velocity dip) cannot leave InputMagnitude at 0
        /// while the root is still sliding.
        /// </summary>
        private float ResolvePresentationSpeed()
        {
            if (driveLocomotionFromMeasuredVelocity)
            {
                float measured = _measuredVelocity.magnitude;
                if (measured > MoveSpeedThreshold)
                    return measured;
            }

            float aiSpeed = _aiController != null ? _aiController.CurrentLocomotionSpeed : 0f;
            if (aiSpeed > MoveSpeedThreshold)
                return aiSpeed;

            if (_body != null && !_body.isKinematic)
            {
                Vector3 v = _body.linearVelocity;
                v.y = 0f;
                float bodySpeed = v.magnitude;
                if (bodySpeed > MoveSpeedThreshold)
                    return bodySpeed;
            }

            NavMeshAgentVelocity(out float agentSpeed, out _);
            return agentSpeed;
        }

        private Vector3 ResolvePresentationWorldDirection()
        {
            if (_aiController != null)
            {
                Vector3 aiLocal = _aiController.CurrentLocalMoveDirection;
                if (aiLocal.sqrMagnitude > 0.0001f)
                    return transform.TransformDirection(aiLocal);
            }

            if (_body != null && !_body.isKinematic)
            {
                Vector3 v = _body.linearVelocity;
                v.y = 0f;
                if (v.sqrMagnitude > 0.0001f)
                    return v.normalized;
            }

            NavMeshAgentVelocity(out float agentSpeed, out Vector3 agentDir);
            if (agentSpeed > MoveSpeedThreshold && agentDir.sqrMagnitude > 0.0001f)
                return agentDir;

            return transform.forward;
        }

        private void NavMeshAgentVelocity(out float speed, out Vector3 flatDirection)
        {
            speed = 0f;
            flatDirection = Vector3.zero;

            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh)
                return;

            Vector3 velocity = _navAgent.velocity;
            velocity.y = 0f;
            speed = velocity.magnitude;
            if (speed > 0.0001f)
                flatDirection = velocity.normalized;
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
            _hasIsAiming = AnimatorHasParameter(animator, "IsAiming");
            _animatorParamsCached = true;
        }

        private void ZeroLocomotionPresentation()
        {
            _controller.moveDirection = Vector3.zero;
            _controller.input = Vector3.zero;
            _controller.isSprinting = false;
            _controller.inputMagnitude = 0f;

            // Velocity-driven mode: LateUpdate owns the animator floats (damped). Hard zero writes
            // here, between frames, collapsed InputMagnitude whenever the AI speed dipped for a frame.
            if (driveLocomotionFromMeasuredVelocity)
                return;

            ZeroLocomotionAnimatorFloats();
        }

        private void ZeroLocomotionAnimatorFloats()
        {
            if (_controller == null || _controller.animator == null)
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
