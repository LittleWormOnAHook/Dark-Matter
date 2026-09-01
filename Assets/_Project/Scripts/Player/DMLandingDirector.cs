using Invector.vCharacterController;
using Project.Features.Climb;
using Project.Features.Jetpack;
using Project.Survival;
using Project.Vehicles;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Four regular-fall heights:
    /// 1) regular jump — Invector land, no lock
    /// 2) medium drop — hero / Jetpack Land
    /// 3) high drop — flop before contact, then get up
    /// 4) lethal drop — SurvivalStats death + Player_v7 ragdoll
    /// Still thrusting into the ground is hero.
    /// Jetpack: land while boosting, or within jetpackLethalDelay after release, is hero.
    /// Unboosted 20m+ is lethal. Fall-time backup does not apply during a jetpack air.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class DMLandingDirector : MonoBehaviour
    {
        private const float HeroLandSpeed = 2f;
        private const float HardFallApproach = 1.35f;
        private const float SoftImpactApproach = 1.0f;
        private const float GroundCommitSeconds = 0.16f;
        private const string BuildStamp = "DMLanding 0831-boost";
        private const float TorsoTwistLimit = 18f;
        private const float TorsoSwing1Limit = 10f;
        private const float TorsoSwing2Limit = 8f;
        private const float TorsoJointSpring = 280f;
        private const float TorsoJointDamper = 36f;
        private const float TorsoProjectionAngle = 22f;
        private const float HipAngularDamping = 3.2f;
        private const float SpineAngularDamping = 2.4f;
        private const float LethalFallTime = 2f;
        private const float WalkableDist = 0.45f;
        private const float WalkableNormalY = 0.55f;

        [SerializeField] private DMClimbProfile climbProfile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private vRagdoll ragdoll;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider capsule;
        [SerializeField] private DMClimbController climb;

        private bool _landing;
        private bool _hardFalling;
        private bool _enteredHeroState;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;
        private bool _heldBlockFallDamage;
        private float _savedAnimatorSpeed = 1f;
        private float _clipEndsAt = -1f;
        private float _airApexY;
        private float _airVerticalVelocity;
        private bool _wasGrounded = true;
        private bool _physAir;
        private float _ignoreLandsUntil;
        private float _groundedFor;
        private bool _loggedBuild;
        private bool _loggedMountedGate;
        private bool _mutedInvector;
        private bool _savedBlockFall;
        private float _fallTime;
        private bool _playedFallPose;
        private Vector3 _flopImpact;
        private float _flopBoostUntil = -1f;
        private int _flopBoneCount;

        private bool _hasVerticalVelocity;
        private bool _hasJetpackLand;
        private bool _hasLandHigh;
        private bool _hasIsGrounded;

        private static readonly RaycastHit[] ProbeHits = new RaycastHit[16];
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int JetpackLand = Animator.StringToHash("JetpackLand");
        private static readonly int LandHighTrigger = Animator.StringToHash("LandHigh");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int JetpackLandState = Animator.StringToHash("Jetpack Land");
        private static readonly int LandHighState = Animator.StringToHash("LandHigh");
        private static readonly int Locomotion = Animator.StringToHash("Locomotion");
        private static readonly int FallingState = Animator.StringToHash("Falling");
        private static readonly int[] InvectorLandStates =
        {
            Animator.StringToHash("LandHigh"),
            Animator.StringToHash("LandLow"),
            Animator.StringToHash("Landing"),
            Animator.StringToHash("Falling"),
            Animator.StringToHash("StandUpFromBelly"),
            Animator.StringToHash("StandUpFromBack"),
            Animator.StringToHash("StandUp@FromBelly"),
            Animator.StringToHash("StandUp@FromBack"),
            Animator.StringToHash("GetUpFromBelly"),
            Animator.StringToHash("GetUpFromBack"),
            Animator.StringToHash("Roll"),
        };

        public bool IsLandingLocked => _landing;
        public bool IsHardFalling => _hardFalling;

        public void ResetForRespawn()
        {
            _hardFalling = false;
            _landing = false;
            _enteredHeroState = false;
            _physAir = false;
            _fallTime = 0f;
            _playedFallPose = false;
            _flopBoostUntil = -1f;
            _flopBoneCount = 0;
            _clipEndsAt = -1f;
            _ignoreLandsUntil = 0f;
            _groundedFor = 0f;

            if (ragdoll != null)
            {
                ragdoll.keepRagdolled = false;
                ragdoll.ignoreGetUpAnimation = false;
                ragdoll.removePhysicsAfterDie = false;
                ragdoll.RestoreRagdoll();
            }

            if (animator != null)
            {
                animator.enabled = true;
                animator.speed = _savedAnimatorSpeed > 0.01f ? _savedAnimatorSpeed : 1f;
                if (animator.HasState(0, Locomotion))
                    animator.CrossFadeInFixedTime(Locomotion, 0.08f, 0);
            }

            if (motor != null)
            {
                motor.lockMovement = false;
                motor.lockAnimMovement = false;
                if (motor.ragdolled)
                    motor.ResetRagdoll();
                motor.EnableGravityAndCollision();
            }

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (capsule != null)
            {
                capsule.enabled = true;
                capsule.isTrigger = false;
            }

            UnmuteInvectorFall();
        }

        public void IgnoreLandsFor(float seconds)
        {
            _ignoreLandsUntil = Time.unscaledTime + Mathf.Max(0.05f, seconds);
            if (_landing)
                EndLanding(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null || player.GetComponent<DMLandingDirector>() != null)
                return;

            player.AddComponent<DMLandingDirector>();
        }

        private void Awake()
        {
            if (climbProfile == null)
                climbProfile = Resources.Load<DMClimbProfile>(DMClimbController.ResourcesPath);
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (capsule == null)
                capsule = GetComponent<CapsuleCollider>();
            if (climb == null)
                climb = GetComponent<DMClimbController>();
            CacheAnimatorParameters();
        }

        private void Start()
        {
            if (_loggedBuild)
                return;
            _loggedBuild = true;
            Debug.Log(BuildStamp);
        }

        private void OnDisable()
        {
            EndLanding(restoreLocks: true);
        }

        private void CacheAnimatorParameters()
        {
            _hasVerticalVelocity = false;
            _hasJetpackLand = false;
            _hasLandHigh = false;
            _hasIsGrounded = false;
            if (animator == null)
                return;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                int hash = animator.GetParameter(i).nameHash;
                if (hash == VerticalVelocity)
                    _hasVerticalVelocity = true;
                else if (hash == JetpackLand)
                    _hasJetpackLand = true;
                else if (hash == LandHighTrigger)
                    _hasLandHigh = true;
                else if (hash == IsGrounded)
                    _hasIsGrounded = true;
            }
        }

        private bool JetpackHeroThisAir()
        {
            return jetpack != null && (
                jetpack.UsedJetpackThisAir ||
                jetpack.IsHeroLandArmed ||
                jetpack.HadJetpackFlightThisAirtime);
        }

        private void MuteInvectorFall()
        {
            if (motor == null || _mutedInvector)
                return;
            _savedBlockFall = motor.blockApplyFallDamage;
            motor.blockApplyFallDamage = true;
            _mutedInvector = true;
        }

        private void UnmuteInvectorFall()
        {
            if (motor == null || !_mutedInvector)
                return;
            motor.blockApplyFallDamage = _savedBlockFall;
            _mutedInvector = false;
        }

        private void FixedUpdate()
        {
            if (motor == null)
                return;

            if (climb != null && climb.IsClimbing)
                return;

            if (ClearMountedAir())
                return;

            bool walkable = OnWalkableGround(out float floorDist);
            if (!walkable)
                MuteInvectorFall();

            if (Time.unscaledTime < _ignoreLandsUntil)
                return;

            float vy = ReadFallVelocity();
            float y = transform.position.y;
            if (!walkable)
            {
                if (!_physAir)
                    _airApexY = y;
                else if (y > _airApexY)
                    _airApexY = y;
                _physAir = true;
                _airVerticalVelocity = vy;
            }

            if (_hardFalling)
            {
                EnsureRagdollKept();
                return;
            }

            bool boosted = JetpackHeroThisAir();
            float drop = _airApexY - y;
            FallLand kind = ClassifyFall(drop, boosted, vy);
            if (kind != FallLand.GetUp && kind != FallLand.Lethal)
                return;

            // Flop before bone colliders overlap the floor (disableColliders pop-on bounce).
            if (walkable || floorDist <= HardFallApproach)
                BeginLethalFall();
        }

        private void Update()
        {
            if (motor == null || animator == null)
                return;

            if (climb != null && climb.IsClimbing)
            {
                _fallTime = 0f;
                _playedFallPose = false;
                _wasGrounded = true;
                return;
            }

            if (ClearMountedAir())
                return;

            bool walkable = OnWalkableGround(out _);
            float vy = ReadFallVelocity();
            if (walkable)
            {
                _groundedFor += Time.unscaledDeltaTime;
                _fallTime = 0f;
                _playedFallPose = false;
            }
            else
            {
                _groundedFor = 0f;
                if (jetpack != null && jetpack.IsBoostingNow)
                    _fallTime = 0f;
                else if (vy < -2f)
                    _fallTime += Time.unscaledDeltaTime;
                MuteInvectorFall();
            }

            if (!walkable)
            {
                float y = transform.position.y;
                if (!_physAir)
                    _airApexY = y;
                else if (y > _airApexY)
                    _airApexY = y;
                _physAir = true;
                PlayFallPoseIfNeeded(y, vy);
            }
            else if (_physAir && _groundedFor >= GroundCommitSeconds)
            {
                float drop = _airApexY - transform.position.y;
                _physAir = false;
                if (!_landing && !_hardFalling && Time.unscaledTime >= _ignoreLandsUntil)
                    BeginLanding(drop, _airVerticalVelocity);
                _airApexY = transform.position.y;
            }

            _wasGrounded = walkable;

            if (_hardFalling)
            {
                EnsureRagdollKept();
                return;
            }

            if (_landing)
                SuppressInvectorLand();

            if (!_landing)
                return;

            ApplyLock();
            SuppressInvectorLand();
            KickInvectorLandStates();
            TickHero();
        }

        [SerializeField] private float heroDropMeters = 2.6f;
        [SerializeField] private float lethalDropMeters = 20f;
        [SerializeField] private float jetpackLethalDelay = 6f;

        private DMClimbProfile LiveClimb
        {
            get
            {
                if (climbProfile == null)
                    climbProfile = Resources.Load<DMClimbProfile>(DMClimbController.ResourcesPath);
                return climbProfile;
            }
        }

        private float HeroMin => LiveClimb != null ? LiveClimb.heroDropMeters : heroDropMeters;
        private float LethalMin => LiveClimb != null ? LiveClimb.lethalDropMeters : lethalDropMeters;
        private float JetDelay => LiveClimb != null ? LiveClimb.jetpackLethalDelay : jetpackLethalDelay;

        private enum FallLand
        {
            RegularJump,
            Hero,
            GetUp,
            Lethal,
        }

        private const float FlopMinSpeed = -9f;

        private bool JetpackGraceActive()
        {
            if (jetpack == null)
                return false;
            if (jetpack.IsBoostingNow)
                return true;
            if (!jetpack.UsedJetpackThisAir)
                return false;
            return jetpack.SecondsSinceBoostReleased <= JetDelay;
        }

        private FallLand ClassifyFall(float dropMeters, bool boosted, float verticalVelocity)
        {
            float heroMin = HeroMin;
            float lethalMin = LethalMin;
            bool jetGrace = JetpackGraceActive();
            // Height rule for unboosted falls. Do not treat fall-time as 20m during
            // a jetpack air — thrusting down still has negative vy and used to
            // arm lethal after 2s even while boosting back to the ground.
            bool lethalByHeight = dropMeters >= lethalMin;
            bool lethalByTime = !boosted && _fallTime >= LethalFallTime;
            bool lethalDrop = lethalByHeight || lethalByTime;

            if (dropMeters < heroMin && !boosted && !lethalDrop)
                return FallLand.RegularJump;
            if (lethalDrop && !jetGrace)
                return FallLand.Lethal;
            if (boosted || dropMeters >= heroMin || jetGrace)
                return FallLand.Hero;
            return FallLand.RegularJump;
        }

        private void BeginLanding(float dropMeters, float airVelocity)
        {
            if (dropMeters < 0.2f && airVelocity > -2f)
                return;

            bool boosted = JetpackHeroThisAir();
            FallLand kind = ClassifyFall(dropMeters, boosted, airVelocity);
            if (kind == FallLand.GetUp || kind == FallLand.Lethal)
            {
                BeginLethalFall();
                return;
            }

            if (kind != FallLand.Hero)
            {
                // Never unmute Invector on a 20m+ drop even if this classified as a hop.
                if (dropMeters >= LethalMin && !JetpackGraceActive())
                {
                    BeginLethalFall();
                    return;
                }

                UnmuteInvectorFall();
                return;
            }

            if (!animator.HasState(0, JetpackLandState) && !_hasJetpackLand)
            {
                Debug.LogWarning(BuildStamp + " no Jetpack Land state — hero skipped");
                return;
            }

            SoftenImpact();
            _landing = true;
            _enteredHeroState = false;
            if (motor != null)
            {
                _heldLockMovement = motor.lockMovement;
                _heldLockAnimMovement = motor.lockAnimMovement;
                _heldBlockFallDamage = motor.blockApplyFallDamage;
                motor.blockApplyFallDamage = true;
            }

            _savedAnimatorSpeed = animator.speed;
            animator.speed = HeroLandSpeed;
            ApplyLock();
            SuppressInvectorLand();
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasJetpackLand)
                animator.SetTrigger(JetpackLand);
            if (animator.HasState(0, JetpackLandState))
                animator.CrossFadeInFixedTime(JetpackLandState, 0.05f, 0);
            _clipEndsAt = Time.unscaledTime + 1.25f;
        }

        private void BeginHardFall()
        {
            BeginLethalFall();
        }

        private bool ClearMountedAir()
        {
            if (!PlayerVehicleState.IsMounted)
            {
                _loggedMountedGate = false;
                return false;
            }

            _fallTime = 0f;
            _playedFallPose = false;
            _physAir = false;
            _wasGrounded = true;
            _groundedFor = GroundCommitSeconds;
            _airApexY = transform.position.y;
            _airVerticalVelocity = 0f;
            if (!_loggedMountedGate)
            {
                _loggedMountedGate = true;
                Debug.Log(BuildStamp + " mounted — lethal-fall gated");
            }
            return true;
        }

        private void BeginLethalFall()
        {
            if (climb != null && climb.IsClimbing)
                return;
            if (ClearMountedAir())
                return;

            if (_hardFalling)
            {
                EnsureRagdollKept();
                return;
            }

            _hardFalling = true;
            MuteInvectorFall();

            // Snapshot BEFORE EnableRagdoll/StopCharacter zeroes the capsule.
            float drop = Mathf.Max(0f, _airApexY - transform.position.y);
            _flopImpact = SnapshotLethalImpact(drop);
            _flopBoostUntil = Time.unscaledTime + 0.45f;

            if (motor != null)
            {
                motor.isJumping = false;
                motor.input = Vector3.zero;
                motor.inputMagnitude = 0f;
            }

            PrepareRagdollForFlop();

            if (ragdoll != null && !ragdoll.isActive)
                ragdoll.ActivateRagdoll(null, 999f);
            else if (motor != null && motor.onActiveRagdoll != null)
                motor.onActiveRagdoll.Invoke(null);

            if (animator != null)
                animator.enabled = false;

            // Hips are reparented off the player by vRagdoll — apply onto the hip tree.
            ApplyFallVelocityToBones(_flopImpact);
            EnsureRagdollKept();

            float hipsVy = ReadHipsVelocityY();
            Debug.Log($"{BuildStamp} lethal drop={drop:F1} fallT={_fallTime:F2} vy={_flopImpact.y:F1} ragdoll={(ragdoll != null && ragdoll.isActive)} bones={_flopBoneCount} hipsVy={hipsVy:F1} anim={(animator != null && animator.enabled)} torsoClamp");

            SurvivalStats stats = ResolveSurvivalStats();
            if (stats != null && !stats.IsDead)
                stats.KillFromFall();
        }

        private Vector3 SnapshotLethalImpact(float drop)
        {
            Vector3 impact = Vector3.zero;
            if (body != null && !body.isKinematic)
                impact = body.linearVelocity;

            float vy = ReadFallVelocity();
            if (vy < impact.y)
                impact.y = vy;

            float fromDrop = -Mathf.Sqrt(Mathf.Max(0f, 2f * 9.81f * drop));
            if (fromDrop < impact.y)
                impact.y = fromDrop;
            if (impact.y > -12f)
                impact.y = -12f;
            return impact;
        }

        private float ReadFallVelocity()
        {
            float fromMotor = motor != null ? motor.verticalVelocity : 0f;
            float fromBody = 0f;
            if (body != null && !body.isKinematic)
                fromBody = body.linearVelocity.y;
            return fromBody < fromMotor ? fromBody : fromMotor;
        }

        private void PrepareRagdollForFlop()
        {
            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (ragdoll == null)
                return;

            ragdoll.keepRagdolled = true;
            ragdoll.ignoreGetUpAnimation = true;
            ragdoll.removePhysicsAfterDie = false;
            ragdoll.verticalMultiplier = 1f;
            ragdoll.horizontalMultiplier = 1f;
            // Prefab has disableColliders=1 (shooter). Start() already disabled bone
            // colliders; flipping the flag is not enough — re-enable them solid below.
            ragdoll.disableColliders = false;
        }

        private SurvivalStats ResolveSurvivalStats()
        {
            SurvivalStats stats = GetComponent<SurvivalStats>();
            if (stats == null)
                stats = GetComponentInParent<SurvivalStats>();
            if (stats == null)
                stats = FindAnyObjectByType<SurvivalStats>();
            return stats;
        }

        private void SoftenImpact()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                Vector3 v = body.linearVelocity;
                if (v.y < 0f)
                    v.y = 0f;
                body.linearVelocity = v;
                body.angularVelocity = Vector3.zero;
            }

            if (motor != null)
                motor.verticalVelocity = 0f;
        }

        private void KillImpact()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (motor != null)
                motor.verticalVelocity = 0f;
        }

        private bool ProbeGround(out float distance)
        {
            distance = 99f;
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, ProbeHits, 5f, ~0, QueryTriggerInteraction.Ignore);
            float best = 99f;
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit hit = ProbeHits[i];
                if (hit.collider == null)
                    continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    continue;
                if (hit.normal.y < WalkableNormalY)
                    continue;

                float d = transform.position.y - hit.point.y;
                if (d < -0.25f)
                    continue;
                if (!any || d < best)
                {
                    best = d;
                    any = true;
                }
            }

            if (!any)
                return false;

            distance = best;
            return distance < 3.5f;
        }

        private bool OnWalkableGround(out float distance)
        {
            if (!ProbeGround(out distance))
                return false;
            if (distance > WalkableDist)
                return false;
            return motor == null || motor.isGrounded || distance <= 0.2f;
        }

        private void PlayFallPoseIfNeeded(float y, float vy)
        {
            if (_playedFallPose || _landing || _hardFalling)
                return;
            if (jetpack != null && (jetpack.IsBoostingNow || jetpack.UsedJetpackThisAir))
                return;
            float drop = _airApexY - y;
            if (drop < 6f && vy > -10f)
                return;
            if (animator == null || !animator.HasState(0, FallingState))
                return;
            animator.CrossFadeInFixedTime(FallingState, 0.08f, 0);
            _playedFallPose = true;
        }

        private Transform ResolveHips()
        {
            if (ragdoll != null && ragdoll.characterHips != null)
                return ragdoll.characterHips;
            if (animator != null && animator.isHuman)
                return animator.GetBoneTransform(HumanBodyBones.Hips);
            return null;
        }

        private float ReadHipsVelocityY()
        {
            Transform hips = ResolveHips();
            if (hips == null)
                return 0f;
            Rigidbody hipBody = hips.GetComponent<Rigidbody>();
            return hipBody != null ? hipBody.linearVelocity.y : 0f;
        }

        private void ApplyFallVelocityToBones(Vector3 impact)
        {
            Transform hips = ResolveHips();
            Rigidbody[] bodies = hips != null
                ? hips.GetComponentsInChildren<Rigidbody>(true)
                : GetComponentsInChildren<Rigidbody>(true);

            _flopBoneCount = 0;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody rb = bodies[i];
                if (rb == null || rb == body)
                    continue;

                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
                // Limbs must be free to flop. Torso FreezeRotation stays off too
                // (that was the upright-statue bug) — joint limits hold the spine.
                rb.constraints = RigidbodyConstraints.None;
                Collider[] cols = rb.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] == null || cols[c] == capsule)
                        continue;
                    cols[c].enabled = true;
                    cols[c].isTrigger = false;
                }

                rb.linearVelocity = impact;
                rb.angularVelocity = Vector3.zero;
                _flopBoneCount++;
            }

            StiffenTorsoJoints(hips, bodies);
        }

        private void EnsureRagdollKept()
        {
            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (ragdoll == null)
                return;

            ragdoll.keepRagdolled = true;
            ragdoll.ignoreGetUpAnimation = true;
            ragdoll.verticalMultiplier = 1f;
            if (animator != null && animator.enabled)
                animator.enabled = false;
            if (!ragdoll.isActive)
                ragdoll.ActivateRagdoll(null, 999f);

            if (Time.unscaledTime > _flopBoostUntil)
                return;

            Transform hips = ResolveHips();
            Rigidbody hipBody = hips != null ? hips.GetComponent<Rigidbody>() : null;
            // Re-drive only if physics was stripped. Re-applying the reconstructed
            // -20..-28 after the hips have already slowed slams the torso through
            // the hip/spine joints and jackknifes the body.
            bool needsPush = hipBody == null
                || hipBody.isKinematic
                || _flopBoneCount <= 0;
            if (needsPush)
                ApplyFallVelocityToBones(_flopImpact);
            else
                StiffenTorsoJoints(hips, null);
        }

        private void StiffenTorsoJoints(Transform hips, Rigidbody[] bodies)
        {
            if (bodies == null)
            {
                bodies = hips != null
                    ? hips.GetComponentsInChildren<Rigidbody>(true)
                    : GetComponentsInChildren<Rigidbody>(true);
            }

            Transform hipBone = hips != null ? hips : ResolveHips();
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody rb = bodies[i];
                if (rb == null || rb == body)
                    continue;

                Transform t = rb.transform;
                if (!IsTorsoTransform(t, hipBone))
                    continue;

                rb.angularDamping = t == hipBone ? HipAngularDamping : SpineAngularDamping;

                CharacterJoint characterJoint = rb.GetComponent<CharacterJoint>();
                if (characterJoint != null)
                    ClampCharacterJoint(characterJoint);

                ConfigurableJoint configurable = rb.GetComponent<ConfigurableJoint>();
                if (configurable != null)
                    ClampConfigurableJoint(configurable);
            }

            // Thighs are hip sockets. Keep their authored swing so legs stay floppy,
            // but stop PhysX projectionAngle=180 from folding them onto the chest.
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody rb = bodies[i];
                if (rb == null || rb == body)
                    continue;
                if (IsTorsoTransform(rb.transform, hipBone))
                    continue;

                CharacterJoint characterJoint = rb.GetComponent<CharacterJoint>();
                if (characterJoint == null || characterJoint.connectedBody == null)
                    continue;
                if (!IsTorsoTransform(characterJoint.connectedBody.transform, hipBone))
                    continue;

                characterJoint.enableProjection = true;
                characterJoint.projectionAngle = Mathf.Min(characterJoint.projectionAngle, 45f);
                characterJoint.projectionDistance = Mathf.Min(characterJoint.projectionDistance, 0.08f);
                SoftJointLimitSpring swingSpring = characterJoint.swingLimitSpring;
                swingSpring.damper = Mathf.Max(swingSpring.damper, 12f);
                characterJoint.swingLimitSpring = swingSpring;
            }
        }

        private bool IsTorsoTransform(Transform t, Transform hipBone)
        {
            if (t == null)
                return false;
            if (t == hipBone)
                return true;

            if (animator != null && animator.isHuman)
            {
                if (t == animator.GetBoneTransform(HumanBodyBones.Hips))
                    return true;
                if (t == animator.GetBoneTransform(HumanBodyBones.Spine))
                    return true;
                if (t == animator.GetBoneTransform(HumanBodyBones.Chest))
                    return true;
                if (t == animator.GetBoneTransform(HumanBodyBones.UpperChest))
                    return true;
                if (t == animator.GetBoneTransform(HumanBodyBones.Neck))
                    return true;
            }

            string n = t.name;
            if (NameContains(n, "thigh") || NameContains(n, "calf") || NameContains(n, "upperarm")
                || NameContains(n, "forearm") || NameContains(n, "hand") || NameContains(n, "foot")
                || NameContains(n, "head") || NameContains(n, "leg") || NameContains(n, "arm"))
                return false;

            return NameContains(n, "hip") || NameContains(n, "pelvis") || NameContains(n, "spine")
                || NameContains(n, "chest") || NameContains(n, "neck") || NameContains(n, "torso");
        }

        private static bool NameContains(string name, string token)
        {
            return name != null && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ClampCharacterJoint(CharacterJoint joint)
        {
            SoftJointLimitSpring twistSpring = joint.twistLimitSpring;
            twistSpring.spring = Mathf.Max(twistSpring.spring, TorsoJointSpring);
            twistSpring.damper = Mathf.Max(twistSpring.damper, TorsoJointDamper);
            joint.twistLimitSpring = twistSpring;

            SoftJointLimitSpring swingSpring = joint.swingLimitSpring;
            swingSpring.spring = Mathf.Max(swingSpring.spring, TorsoJointSpring);
            swingSpring.damper = Mathf.Max(swingSpring.damper, TorsoJointDamper);
            joint.swingLimitSpring = swingSpring;

            SoftJointLimit low = joint.lowTwistLimit;
            low.limit = Mathf.Clamp(low.limit, -TorsoTwistLimit, 0f);
            low.bounciness = 0f;
            joint.lowTwistLimit = low;

            SoftJointLimit high = joint.highTwistLimit;
            high.limit = Mathf.Clamp(high.limit, 0f, TorsoTwistLimit);
            high.bounciness = 0f;
            joint.highTwistLimit = high;

            SoftJointLimit swing1 = joint.swing1Limit;
            swing1.limit = Mathf.Clamp(swing1.limit, 0f, TorsoSwing1Limit);
            swing1.bounciness = 0f;
            joint.swing1Limit = swing1;

            SoftJointLimit swing2 = joint.swing2Limit;
            swing2.limit = Mathf.Clamp(swing2.limit, 0f, TorsoSwing2Limit);
            swing2.bounciness = 0f;
            joint.swing2Limit = swing2;

            joint.enableProjection = true;
            joint.projectionAngle = Mathf.Min(joint.projectionAngle, TorsoProjectionAngle);
            joint.projectionDistance = Mathf.Min(joint.projectionDistance, 0.05f);
        }

        private static void ClampConfigurableJoint(ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit low = joint.lowAngularXLimit;
            low.limit = Mathf.Clamp(low.limit, -TorsoTwistLimit, 0f);
            low.bounciness = 0f;
            joint.lowAngularXLimit = low;

            SoftJointLimit high = joint.highAngularXLimit;
            high.limit = Mathf.Clamp(high.limit, 0f, TorsoTwistLimit);
            high.bounciness = 0f;
            joint.highAngularXLimit = high;

            SoftJointLimit y = joint.angularYLimit;
            y.limit = Mathf.Clamp(y.limit, 0f, TorsoSwing1Limit);
            y.bounciness = 0f;
            joint.angularYLimit = y;

            SoftJointLimit z = joint.angularZLimit;
            z.limit = Mathf.Clamp(z.limit, 0f, TorsoSwing2Limit);
            z.bounciness = 0f;
            joint.angularZLimit = z;

            SoftJointLimitSpring xSpring = joint.angularXLimitSpring;
            xSpring.spring = Mathf.Max(xSpring.spring, TorsoJointSpring);
            xSpring.damper = Mathf.Max(xSpring.damper, TorsoJointDamper);
            joint.angularXLimitSpring = xSpring;

            SoftJointLimitSpring yzSpring = joint.angularYZLimitSpring;
            yzSpring.spring = Mathf.Max(yzSpring.spring, TorsoJointSpring);
            yzSpring.damper = Mathf.Max(yzSpring.damper, TorsoJointDamper);
            joint.angularYZLimitSpring = yzSpring;

            JointDrive slerp = joint.slerpDrive;
            slerp.positionSpring = Mathf.Max(slerp.positionSpring, TorsoJointSpring);
            slerp.positionDamper = Mathf.Max(slerp.positionDamper, TorsoJointDamper);
            joint.slerpDrive = slerp;
            joint.rotationDriveMode = RotationDriveMode.Slerp;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionAngle = Mathf.Min(joint.projectionAngle, TorsoProjectionAngle);
            joint.projectionDistance = Mathf.Min(joint.projectionDistance, 0.05f);
        }

        private void TickHero()
        {
            bool inState = StateMatches(JetpackLandState);
            if (inState)
                _enteredHeroState = true;
            else if (_enteredHeroState)
            {
                EndLanding(restoreLocks: true);
                return;
            }

            if (_clipEndsAt > 0f && Time.unscaledTime >= _clipEndsAt)
                EndLanding(restoreLocks: true);
        }

        private void KickInvectorLandStates()
        {
            if (animator == null)
                return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : current;

            if (IsInvectorLandHash(current.shortNameHash) || IsInvectorLandHash(next.shortNameHash))
            {
                if (animator.HasState(0, JetpackLandState))
                    animator.CrossFadeInFixedTime(JetpackLandState, 0.04f, 0);
            }
        }

        private static bool IsInvectorLandHash(int hash)
        {
            for (int i = 0; i < InvectorLandStates.Length; i++)
            {
                if (InvectorLandStates[i] == hash)
                    return true;
            }

            return false;
        }

        private bool StateMatches(int hash)
        {
            if (animator == null)
                return false;

            if (animator.IsInTransition(0) &&
                animator.GetNextAnimatorStateInfo(0).shortNameHash == hash)
                return true;

            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
        }

        private void ApplyLock()
        {
            if (motor == null)
                return;

            motor.lockMovement = true;
            motor.lockAnimMovement = true;
            motor.input = Vector3.zero;
            motor.inputMagnitude = 0f;
            motor.isJumping = false;
            motor.verticalVelocity = 0f;
        }

        private void SuppressInvectorLand()
        {
            if (animator == null)
                return;

            if (motor != null)
                motor.verticalVelocity = 0f;

            if (_hasVerticalVelocity)
                animator.SetFloat(VerticalVelocity, 0f);
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasIsGrounded && (_landing || motor != null && motor.isGrounded))
                animator.SetBool(IsGrounded, true);
        }

        private void EndLanding(bool restoreLocks)
        {
            if (!_landing)
                return;

            _landing = false;
            _enteredHeroState = false;
            _clipEndsAt = -1f;
            SuppressInvectorLand();

            if (jetpack != null && motor != null && motor.isGrounded && _groundedFor >= GroundCommitSeconds)
                jetpack.NotifyLanded();

            if (animator != null)
            {
                animator.speed = _savedAnimatorSpeed;
                if (animator.HasState(0, Locomotion))
                    animator.CrossFadeInFixedTime(Locomotion, 0.12f, 0);
            }

            if (restoreLocks && motor != null)
            {
                motor.lockMovement = _heldLockMovement;
                motor.lockAnimMovement = _heldLockAnimMovement;
            }
            UnmuteInvectorFall();
        }
    }
}
