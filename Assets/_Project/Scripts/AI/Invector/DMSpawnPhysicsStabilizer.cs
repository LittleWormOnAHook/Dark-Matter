using System.Collections;
using Invector.vCharacterController;
using Project.AI;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Keeps a living humanoid enemy rooted: kinematic, no gravity, no root motion.
    /// One short spawn snap, then only re-snap if the body launches.
    /// </summary>
    [DefaultExecutionOrder(-875)]
    [DisallowMultipleComponent]
    public sealed class DMSpawnPhysicsStabilizer : MonoBehaviour
    {
        private const float GroundedConfirmSeconds = 0f;
        private const float GroundedHeightSlack = 1.6f;
        private const float MaxSpawnGraceSeconds = 0.15f;

        [SerializeField] private float spawnGraceSeconds = 0.12f;
        [SerializeField] private float verticalSpikeThreshold = 1.25f;
        [SerializeField] private float horizontalSpikeThreshold = 6f;

        private Coroutine _routine;
        private EnemyAiController _ai;
        private EnemyInvectorBootstrap _bootstrap;
        private EnemyHealth _health;
        private Rigidbody _rootBody;
        private CapsuleCollider _capsule;
        private float _spawnTime;
        private float _groundedSince = -1f;
        private float _lastValidGroundY;
        private bool _hasLastValidGround;
        private bool _locomotionPaused;
        private bool _lockMovementReleased;
        private bool _initialSettleComplete;

        /// <summary>True only during the short spawn snap (not a combat delay).</summary>
        public bool IsSpawnSettleActive { get; private set; }

        /// <summary>True after feet are on ground, or when the short grace expires.</summary>
        public bool HasConfirmedGrounded { get; private set; }

        public static void EnsureOn(GameObject instance)
        {
            if (instance == null)
                return;

            DMSpawnPhysicsStabilizer stabilizer = instance.GetComponent<DMSpawnPhysicsStabilizer>();
            if (stabilizer == null)
                stabilizer = instance.AddComponent<DMSpawnPhysicsStabilizer>();

            stabilizer.Begin();
        }

        public static void KeepLivingRootKinematic(GameObject instance)
        {
            if (instance == null)
                return;

            DMSpawnPhysicsStabilizer stabilizer = instance.GetComponent<DMSpawnPhysicsStabilizer>();
            if (stabilizer != null)
            {
                stabilizer.ForceLivingRootKinematic();
                return;
            }

            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health != null && health.IsDead)
                return;

            Rigidbody body = instance.GetComponent<Rigidbody>();
            ApplyKinematicNoGravity(body);
        }

        private void Awake()
        {
            CacheRefs();
            Begin();
        }

        public void Begin()
        {
            CacheRefs();
            _spawnTime = Time.time;
            _groundedSince = -1f;
            HasConfirmedGrounded = false;
            _lockMovementReleased = false;
            _initialSettleComplete = false;
            IsSpawnSettleActive = true;
            if (spawnGraceSeconds > MaxSpawnGraceSeconds)
                spawnGraceSeconds = MaxSpawnGraceSeconds;
            CaptureLastValidGroundY();
            HumanoidPerformanceController.ForceSpawnVisible(gameObject);
            ApplyPass();

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(StabilizeRoutine());
        }

        private void CacheRefs()
        {
            if (_ai == null)
                _ai = GetComponent<EnemyAiController>();
            if (_bootstrap == null)
                _bootstrap = GetComponent<EnemyInvectorBootstrap>();
            if (_health == null)
                _health = GetComponent<EnemyHealth>();
            if (_rootBody == null)
                _rootBody = GetComponent<Rigidbody>();
            if (_capsule == null)
                _capsule = GetComponent<CapsuleCollider>();
        }

        private IEnumerator StabilizeRoutine()
        {
            PauseLocomotionBriefly();
            ApplyPass();
            UpdateGroundedConfirmation();

            float graceEnd = _spawnTime + Mathf.Max(0.05f, spawnGraceSeconds);
            while (Time.time < graceEnd && !HasConfirmedGrounded)
            {
                if (IsDead())
                    break;

                if (HasVerticalLaunch() || HasHorizontalLaunch())
                    ApplyPass();
                else
                    UpdateGroundedConfirmation();

                yield return new WaitForFixedUpdate();
            }

            if (!HasConfirmedGrounded)
                HasConfirmedGrounded = true;

            FinalizeSpawnSettle();
            ResumeLocomotionIfAlive();

            _initialSettleComplete = true;
            IsSpawnSettleActive = false;
            _routine = null;
        }

        private void LateUpdate()
        {
            if (IsDead())
                return;

            UpdateGroundedConfirmation();

            if (HasVerticalLaunch() || HasHorizontalLaunch())
                ApplyPass();
        }

        private void FixedUpdate()
        {
            if (IsDead())
                return;

            ForceLivingRootKinematic();
            ZeroAllVelocities();

            if (HasVerticalLaunch())
            {
                SnapFeetToGround();
                ClampMotorLaunch();
            }
        }

        public void ForceLivingRootKinematic()
        {
            if (IsDead())
                return;

            CacheRefs();
            ApplyKinematicNoGravity(_rootBody);
            DisableRootMotion();
            ClampMotorLaunch();
        }

        private void ApplyPass()
        {
            if (IsDead())
                return;

            DisableActiveRagdoll();
            SnapFeetToGround();
            StabilizeRigidbodies();
            ForceLivingRootKinematic();
            ZeroAllVelocities();
            LockMotorUntilGroundedConfirmed();
            Physics.SyncTransforms();
        }

        private void DisableActiveRagdoll()
        {
            vRagdoll ragdoll = GetComponent<vRagdoll>();
            if (ragdoll != null)
            {
                ragdoll.startRagdolled = false;
                ragdoll.keepRagdolled = false;
                ragdoll.CancelInvoke("ActivateRagdoll");

                vThirdPersonController controller = _bootstrap != null
                    ? _bootstrap.ThirdPersonController
                    : GetComponent<vThirdPersonController>();

                bool ragdolled = ragdoll.isActive ||
                                 ragdoll.state != vRagdoll.RagdollState.animated ||
                                 (controller != null && controller.ragdolled);

                if (ragdolled)
                    EnemyInvectorRagdollStateUtility.ClearRagdollWithoutResetRagdoll(gameObject);
            }

            EnemyInvectorRagdollBridge bridge = GetComponent<EnemyInvectorRagdollBridge>();
            if (bridge != null && bridge.HasActiveRagdoll && !IsDead())
                EnemyInvectorRagdollStateUtility.ClearRagdollWithoutResetRagdoll(gameObject);
        }

        private void SnapFeetToGround()
        {
            Vector3 xz = transform.position;
            float minY = _hasLastValidGround ? _lastValidGroundY : float.NegativeInfinity;
            EnemyGroundUtility.SnapCreatureToGround(transform, xz, minY);
            LiftCapsuleOutOfGround();
            ClampAboveLastValidGround();
        }

        private void LiftCapsuleOutOfGround()
        {
            if (_capsule == null)
                _capsule = GetComponent<CapsuleCollider>();

            float minY = _hasLastValidGround ? _lastValidGroundY : float.NegativeInfinity;
            if (_capsule == null ||
                !EnemyGroundUtility.TryGetGroundY(transform.position, out float groundY, 0f, transform, minY))
                return;

            RememberGroundY(groundY);

            Vector3 worldCenter = transform.TransformPoint(_capsule.center);
            float worldHeight = _capsule.height * Mathf.Abs(transform.lossyScale.y);
            float bottomY = worldCenter.y - worldHeight * 0.5f;
            float lift = (groundY + 0.04f) - bottomY;
            if (lift > 0.001f)
                transform.position += Vector3.up * lift;
        }

        private void CaptureLastValidGroundY()
        {
            if (EnemyGroundUtility.TryGetGroundY(transform.position, out float groundY, 0f, transform, float.NegativeInfinity))
            {
                _lastValidGroundY = groundY;
                _hasLastValidGround = true;
                return;
            }

            _lastValidGroundY = transform.position.y;
            _hasLastValidGround = true;
        }

        private void RememberGroundY(float groundY)
        {
            if (!_hasLastValidGround || groundY >= _lastValidGroundY - 0.02f)
            {
                _lastValidGroundY = _hasLastValidGround ? Mathf.Max(_lastValidGroundY, groundY) : groundY;
                _hasLastValidGround = true;
            }
        }

        private void ClampAboveLastValidGround()
        {
            if (!_hasLastValidGround)
                return;

            Vector3 pos = transform.position;
            if (pos.y + 0.001f >= _lastValidGroundY)
                return;

            pos.y = _lastValidGroundY + 0.08f;
            transform.position = pos;
        }

        private void StabilizeRigidbodies()
        {
            EnemyInvectorPhysicsCache cache = GetComponent<EnemyInvectorPhysicsCache>();
            if (cache == null)
                cache = gameObject.AddComponent<EnemyInvectorPhysicsCache>();

            cache.Refresh();
            cache.StabilizeAllRigidbodies();
            ZeroAllVelocities();

            _bootstrap?.EnsureInvectorPhysicsReady();
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);
            ForceLivingRootKinematic();
        }

        private void ZeroAllVelocities()
        {
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null)
                    continue;

                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }

        private void LockMotorUntilGroundedConfirmed()
        {
            vThirdPersonController controller = _bootstrap != null
                ? _bootstrap.ThirdPersonController
                : GetComponent<vThirdPersonController>();

            if (controller == null)
                return;

            controller.useRootMotion = false;
            controller.isGrounded = true;
            controller.disableCheckGround = true;
            controller.useSnapGround = false;
            controller.extraGravity = 0f;
            controller.verticalVelocity = 0f;

            bool combatReady = HasConfirmedGrounded;
            if (combatReady)
            {
                if (!_lockMovementReleased)
                {
                    controller.lockMovement = false;
                    _lockMovementReleased = true;
                }
            }
            else if (!_initialSettleComplete)
            {
                controller.lockMovement = true;
            }

            DisableRootMotion();
        }

        private void DisableRootMotion()
        {
            vThirdPersonController controller = _bootstrap != null
                ? _bootstrap.ThirdPersonController
                : GetComponent<vThirdPersonController>();

            Animator animator = controller != null ? controller.animator : GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void PauseLocomotionBriefly()
        {
            if (_locomotionPaused || _ai == null)
                return;

            _ai.SetLocomotionPaused(true);
            _locomotionPaused = true;
        }

        private void ResumeLocomotionIfAlive()
        {
            if (!_locomotionPaused || _ai == null)
                return;

            if (!IsDead())
                _ai.SetLocomotionPaused(false);

            _locomotionPaused = false;
        }

        private void FinalizeSpawnSettle()
        {
            if (IsDead())
                return;

            ApplyPass();
            _bootstrap?.EnsureInvectorPhysicsReady();
            ForceLivingRootKinematic();
        }

        private void UpdateGroundedConfirmation()
        {
            if (IsDead())
                return;

            if (IsFeetNearGround() && !HasVerticalLaunch())
            {
                if (_groundedSince < 0f)
                    _groundedSince = Time.time;

                if (Time.time - _groundedSince >= GroundedConfirmSeconds)
                    HasConfirmedGrounded = true;
            }
            else
            {
                _groundedSince = -1f;
                if (HasVerticalLaunch())
                {
                    if (!_initialSettleComplete)
                    {
                        HasConfirmedGrounded = false;
                        IsSpawnSettleActive = true;
                    }
                }
                else if (Time.time >= _spawnTime + spawnGraceSeconds &&
                         !EnemyGroundUtility.TryGetGroundY(transform.position, out _))
                {
                    HasConfirmedGrounded = true;
                }
                else if (_initialSettleComplete && Time.time >= _spawnTime + spawnGraceSeconds)
                {
                    HasConfirmedGrounded = true;
                }
            }
        }

        private bool IsFeetNearGround()
        {
            float minY = _hasLastValidGround ? _lastValidGroundY : float.NegativeInfinity;
            if (!EnemyGroundUtility.TryGetGroundY(transform.position, out float groundY, 0f, transform, minY))
                return false;

            RememberGroundY(groundY);
            float delta = transform.position.y - groundY;
            return delta >= -0.15f && delta <= GroundedHeightSlack;
        }

        private bool HasVerticalLaunch()
        {
            if (_rootBody != null && !_rootBody.isKinematic &&
                Mathf.Abs(_rootBody.linearVelocity.y) >= verticalSpikeThreshold)
                return true;

            float minY = _hasLastValidGround ? _lastValidGroundY : float.NegativeInfinity;
            if (!EnemyGroundUtility.TryGetGroundY(transform.position, out float groundY, 0f, transform, minY))
                return false;

            return transform.position.y > groundY + GroundedHeightSlack + 0.75f;
        }

        private bool HasHorizontalLaunch()
        {
            if (_rootBody == null || _rootBody.isKinematic)
                return false;

            Vector3 v = _rootBody.linearVelocity;
            v.y = 0f;
            return v.sqrMagnitude >= horizontalSpikeThreshold * horizontalSpikeThreshold;
        }

        private void ClampMotorLaunch()
        {
            vThirdPersonController controller = _bootstrap != null
                ? _bootstrap.ThirdPersonController
                : GetComponent<vThirdPersonController>();

            if (controller == null)
                return;

            controller.verticalVelocity = 0f;
            controller.extraGravity = 0f;
            controller.useRootMotion = false;
        }

        private bool IsDead()
        {
            return _health != null && _health.IsDead;
        }

        private static void ApplyKinematicNoGravity(Rigidbody body)
        {
            if (body == null)
                return;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}
