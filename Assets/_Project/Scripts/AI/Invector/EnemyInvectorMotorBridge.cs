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
            (_ragdollBridge != null &&
            (_ragdollBridge.IsHitStaggerActive || _ragdollBridge.IsCorpseRagdolled)) ||
            DMEnemyKnockdown.IsDown(gameObject); // Slice 2b: animation-only knockdown

        // Stiff-pose fix (2026-10-12): a re-enabled / freshly spawned enemy showed its bind pose for the first frames. Evaluate the animator now.
        // 2026-10-12 crash fix: Animator.Update inside OnEnable ran synchronously inside the Inspector's activate-toggle
        // (SerializedObject.ApplyModifiedProperties -> native crash in DidTypeTreeChange). Now deferred to the first LateUpdate, once.
        private bool _pendingEnableEvaluate;
        private bool _evaluatingEnable;

        private void OnEnable()
        {
            _pendingEnableEvaluate = true;
        }

        private void RunPendingEnableEvaluate()
        {
            if (!_pendingEnableEvaluate || _evaluatingEnable) return;
            _pendingEnableEvaluate = false;
            if (_health != null && _health.IsDead) return;
            Animator an = _controller != null && _controller.animator != null ? _controller.animator : GetComponentInChildren<Animator>();
            if (an == null || !an.isActiveAndEnabled || !an.isInitialized || (_ragdollBridge != null && _ragdollBridge.HasActiveRagdoll)) return;
            _evaluatingEnable = true;
            try { an.Update(0f); }
            finally { _evaluatingEnable = false; }
        }

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
            // Transform-driven AI owns root translation; never leave Invector lockMovement off after spawn settle.
            if (_controller != null)
                _controller.lockMovement = true;
        }

        private void LateUpdate()
        {
            RunPendingEnableEvaluate(); // 2026-10-12 deferred stiff-pose evaluate

            if (_health != null && _health.IsDead)
                return;

            bool farIdleMotor =
                !IsWithinMotorLod()
                && (_aiController == null || !_aiController.IsEngagedWithTarget)
                && (_enemyCombat == null || !_enemyCombat.IsAttacking)
                && _measuredVelocity.sqrMagnitude < MoveSpeedThreshold * MoveSpeedThreshold
                && !IsMotorBlocked;
            if (farIdleMotor)
                return;

            // Sample every frame (even while blocked) so the first frame after a stagger has no spike.
            SampleMeasuredVelocity();
            TrackSlide();

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

            // shakefix: crowd / separation pushes are short and fast. The animator damps InputMagnitude, so the legs would lag the push and the body would
            // slide. For a push in the last 0.25 s write the blend un-damped so the leg cycle starts with the displacement.
            // Legs fix (2026-10-12): StepFlat / ChaseMove / hold repositions are short bursts (0.3-0.6 s); Invector's damped InputMagnitude never got past
            // ~0.05 before the move ended, so the body slid on the Idle / AimedIdle clip. Snap the blend whenever the body moves and the legs lag behind.
            bool legsLag = DM_EnemyEngagementProfile.Live.snapLegsToMovement && _controller.inputMagnitude > 0.2f
                && _controller.animator.GetFloat(InputMagnitudeHash) < _controller.inputMagnitude * 0.6f;
            if (driveLocomotionFromMeasuredVelocity && _measuredVelocity.magnitude > MoveSpeedThreshold
                && (legsLag || (_aiController.PushSnapsLegs && Time.time - _aiController.LastPushTime < 0.25f)))
            {
                _controller.animator.SetFloat(InputMagnitudeHash, _controller.inputMagnitude);
                _controller.animator.SetFloat(InputVerticalHash, _controller.verticalSpeed);
                _controller.animator.SetFloat(InputHorizontalHash, _controller.horizontalSpeed);
            }
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

                float smoothing = measuredVelocitySmoothing;
                if (_aiController != null && _aiController.IsQuickStepActive)
                    smoothing = Mathf.Min(smoothing, DM_EnemyEngagementProfile.Live.stepVelocitySmoothing);

                float blend = smoothing > 0.0001f
                    ? 1f - Mathf.Exp(-dt / smoothing)
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

            if (_aiController != null &&
                _aiController.RecentWallBufferContact &&
                speed < MoveSpeedThreshold * 1.5f)
            {
                ReleaseStepPlaybackBoost(animator);
                _controller.isSprinting = false;
                _controller.inputMagnitude = 0f;
                _controller.verticalSpeed = 0f;
                _controller.horizontalSpeed = 0f;
                return;
            }

            // Quick steps (strafe / backstep / sidestep): leg-cycle exaggeration, see DM_EnemyEngagementProfile "Step animation".
            bool quick = _aiController != null && _aiController.IsQuickStepActive;
            DM_EnemyEngagementProfile stepProfile = quick ? DM_EnemyEngagementProfile.Live : null;
            if (!quick)
                ReleaseStepPlaybackBoost(animator);
            float animSpeed = quick ? speed * stepProfile.stepAnimSpeedScale : speed;

            // Step-end tracking: the legs must stop with the body (audit log [StepAnimEnd] reports the lag).
            if (quick) { _stepWasQuick = true; _stepEndTime = -1f; }
            else if (_stepWasQuick) { _stepWasQuick = false; _stepEndTime = Time.time; _stepLagArmed = true; }

            if (!quick && _stepEndTime >= 0f)
            {
                float blendOut = DM_EnemyEngagementProfile.Live.stepAnimBlendOut;
                if (Time.time - _stepEndTime < 0.5f && speed <= MoveSpeedThreshold)
                {
                    if (blendOut <= 0.0001f) { animator.SetFloat(InputMagnitudeHash, 0f); animator.SetFloat(InputHorizontalHash, 0f); animator.SetFloat(InputVerticalHash, 0f); }
                    else { float dtb = Time.deltaTime; animator.SetFloat(InputMagnitudeHash, 0f, blendOut, dtb); animator.SetFloat(InputHorizontalHash, 0f, blendOut, dtb); animator.SetFloat(InputVerticalHash, 0f, blendOut, dtb); }
                }
                if (_stepLagArmed && (animator.GetFloat(InputMagnitudeHash) < 0.05f || Time.time - _stepEndTime > 0.6f))
                {
                    _stepLagArmed = false;
                    var cc0 = Project.Combat.DM_CombatCoreProfile.Live;
                    if (cc0 != null && cc0.logInterruptsInPlay) Debug.Log($"[StepAnimEnd] {name} lag={(Time.time - _stepEndTime):0.000}s magnitude={animator.GetFloat(InputMagnitudeHash):0.00}", this);
                }
            }

            if (speed <= MoveSpeedThreshold)
            {
                ReleaseStepPlaybackBoost(animator);
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
            if (_aiController != null &&
                _aiController.RecentWallBufferContact &&
                Mathf.Abs(localDirection.z) < 0.2f)
            {
                ReleaseStepPlaybackBoost(animator);
                _strafeLatched = false;
                _controller.isStrafing = false;
                _controller.isSprinting = false;
                _controller.horizontalSpeed = 0f;
                if (speed < MoveSpeedThreshold * 1.5f)
                {
                    _controller.inputMagnitude = 0f;
                    _controller.verticalSpeed = 0f;
                    return;
                }

                _controller.inputMagnitude = FreeInputMagnitudeForSpeed(animSpeed, scale);
                _controller.verticalSpeed = 1f;
                return;
            }

            float angleGate = _strafeLatched ? Mathf.Max(0f, strafeAngleThreshold - 10f) : strafeAngleThreshold;
            bool strafe = aiming || (engaged && offFacing > angleGate && (quick || speed <= strafeRun * 1.15f));
            _strafeLatched = strafe;
            _controller.isStrafing = strafe;

            if (strafe)
            {
                float magnitude = animSpeed <= strafeWalk
                    ? 0.5f * animSpeed / strafeWalk
                    : Mathf.Lerp(0.5f, 1f, Mathf.Clamp01((animSpeed - strafeWalk) / (strafeRun - strafeWalk)));
                ApplyStepPlaybackBoost(animator, quick ? animSpeed / strafeRun : 1f, stepProfile);
                float ring = Mathf.Max(0.5f, magnitude);
                _controller.isSprinting = false;
                _controller.inputMagnitude = magnitude;
                _controller.verticalSpeed = localDirection.z * ring;
                _controller.horizontalSpeed = localDirection.x * ring;
                return;
            }

            float freeMagnitude = FreeInputMagnitudeForSpeed(animSpeed, scale);
            ApplyStepPlaybackBoost(animator, quick ? animSpeed / Mathf.Max(0.1f, freeSprintClipSpeed * scale) : 1f, stepProfile);
            _controller.isSprinting = freeMagnitude > 1.001f;
            _controller.inputMagnitude = freeMagnitude;
            _controller.verticalSpeed = localDirection.z;
            _controller.horizontalSpeed = 0f;
        }

        private float _stepBoostValue;
        // Audit-mode diagnostic (logInterruptsInPlay): the body translates but the locomotion animator has no leg cycle (a visible slide).
        private float _slideTime, _slideNextLog;
        private void TrackSlide()
        {
            Project.Combat.DM_CombatCoreProfile cc = Project.Combat.DM_CombatCoreProfile.Live;
            if (cc == null || !cc.logInterruptsInPlay || _aiController == null || _controller == null || _controller.animator == null)
                return;

            Animator an = _controller.animator;
            bool exempt = _aiController.IsDodgeRollActive || _aiController.IsDodgeStrikeActive || (_health != null && _health.IsDead);
            float speed = _measuredVelocity.magnitude;
            float legs = an.GetFloat(InputMagnitudeHash);
            if (speed > 0.4f && legs < 0.05f && !exempt)
            {
                _slideTime += Time.deltaTime;
                if (_slideTime > 0.15f && Time.time >= _slideNextLog)
                {
                    _slideNextLog = Time.time + 0.5f;
                    var ci = an.GetCurrentAnimatorClipInfo(0);
                    string clip = ci != null && ci.Length > 0 && ci[0].clip != null ? ci[0].clip.name : "none";
                    NavMeshAgentVelocity(out float navSpeed, out _);
                    string moverName = _aiController.DominantMover();
                    if (moverName == "none" && navSpeed > 0.3f) moverName = "NavAgent(" + navSpeed.ToString("0.00") + ")";
                    else if (moverName == "none" && _body != null && !_body.isKinematic && _body.linearVelocity.sqrMagnitude > 0.09f) moverName = "Rigidbody(" + _body.linearVelocity.magnitude.ToString("0.00") + ")";
                    else if (moverName == "none") moverName = "unknown(rootmotion/other)";
                    Debug.Log($"[SlideNoLegs] {name} speed={speed:0.00} legs={legs:0.00} mover={moverName.Replace(" ", "")} ctrlMag={(_controller != null ? _controller.inputMagnitude : -1f):0.00} animSpeed={an.speed:0.00} shouldTick={(ShouldTickAnimator() ? 1 : 0)} state={_aiController.CurrentStateName} blocked={(IsMotorBlocked ? 1 : 0)} stagger={(_ragdollBridge != null && _ragdollBridge.IsHitStaggerActive ? 1 : 0)} quick={(_aiController.IsQuickStepActive ? 1 : 0)} attacking={(_aiController.IsInAttackSequence ? 1 : 0)} suppress={(ShouldSuppressLocomotionAnimator() ? 1 : 0)} clip={clip}", this);
                }
            }
            else
            {
                _slideTime = 0f;
            }
        }

        private bool _stepWasQuick, _stepLagArmed; private float _stepEndTime = -1f;
        private static readonly int InputMagnitudeHash = Animator.StringToHash("InputMagnitude");
        private static readonly int InputHorizontalHash = Animator.StringToHash("InputHorizontal");
        private static readonly int InputVerticalHash = Animator.StringToHash("InputVertical");

        /// <summary>Raises Animator.speed while a quick step is faster than the fastest locomotion clip. Only touches the speed while it is exactly 1 (or our own value), same guard as the combo driver.</summary>
        private void ApplyStepPlaybackBoost(Animator animator, float ratio, DM_EnemyEngagementProfile p)
        {
            if (animator == null)
                return;

            float boost = p != null ? Mathf.Clamp(ratio, 1f, Mathf.Max(1f, p.stepAnimMaxPlaybackBoost)) : 1f;
            if (boost <= 1.001f)
            {
                ReleaseStepPlaybackBoost(animator);
                return;
            }

            if (_stepBoostValue > 0f)
            {
                if (Mathf.Abs(animator.speed - _stepBoostValue) < 0.001f || Mathf.Abs(animator.speed - 1f) < 0.01f)
                {
                    animator.speed = boost;
                    _stepBoostValue = boost;
                }
                else
                {
                    _stepBoostValue = 0f;
                }

                return;
            }

            if (Mathf.Abs(animator.speed - 1f) < 0.01f)
            {
                animator.speed = boost;
                _stepBoostValue = boost;
            }
        }

        private void ReleaseStepPlaybackBoost(Animator animator)
        {
            if (_stepBoostValue <= 0f)
                return;

            if (animator != null && Mathf.Abs(animator.speed - _stepBoostValue) < 0.001f)
                animator.speed = 1f;
            _stepBoostValue = 0f;
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
