using Invector.vCharacterController;
using Project.Features.Climb;
using Project.Features.Jetpack;
using Project.Survival;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Four regular-fall heights:
    /// 1) regular jump — Invector land, no lock
    /// 2) medium drop — hero / Jetpack Land
    /// 3) high drop — flop before contact, then get up
    /// 4) lethal drop — SurvivalStats death + Player_v7 ragdoll
    /// Still thrusting into the ground is hero. 20m+ after boost is released is lethal.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class DMLandingDirector : MonoBehaviour
    {
        private const float HeroLandSpeed = 2f;
        private const float HardFallApproach = 1.35f;
        private const float SoftImpactApproach = 1.0f;
        private const float GroundCommitSeconds = 0.16f;
        private const string BuildStamp = "DMLanding 0830-flop";
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
        private bool _mutedInvector;
        private bool _savedBlockFall;
        private float _fallTime;
        private bool _playedFallPose;

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

            bool walkable = OnWalkableGround(out float floorDist);
            if (!walkable)
                MuteInvectorFall();

            if (Time.unscaledTime < _ignoreLandsUntil)
                return;

            float vy = body != null ? body.linearVelocity.y : motor.verticalVelocity;
            float y = transform.position.y;
            if (!walkable && (body == null || !body.isKinematic))
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

            // Flop on real floor contact. Do not zero speed in the air.
            if (walkable || floorDist <= 0.55f)
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

            bool walkable = OnWalkableGround(out _);
            float vy = body != null ? body.linearVelocity.y : motor.verticalVelocity;
            if (walkable)
            {
                _groundedFor += Time.unscaledDeltaTime;
                _fallTime = 0f;
                _playedFallPose = false;
            }
            else
            {
                _groundedFor = 0f;
                if (vy < -2f)
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

        private FallLand ClassifyFall(float dropMeters, bool boosted, float verticalVelocity)
        {
            float heroMin = HeroMin;
            float lethalMin = LethalMin;
            bool stillBoosting = jetpack != null && jetpack.IsBoostingNow;
            // Still thrusting into the ground = hero. Ignore the 6s-after-release
            // lockout: a 20m fall is ~2s, so that window made every jetpack-this-air
            // tower drop a hero land.
            bool jetGrace = stillBoosting;
            bool lethalDrop = dropMeters >= lethalMin || _fallTime >= LethalFallTime;

            if (dropMeters < heroMin && !boosted && !lethalDrop)
                return FallLand.RegularJump;
            if (lethalDrop && !jetGrace)
                return FallLand.Lethal;
            if (boosted || dropMeters >= heroMin)
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
                bool thrusting = jetpack != null && jetpack.IsBoostingNow;
                if (dropMeters >= LethalMin && !thrusting)
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

        private void BeginLethalFall()
        {
            if (climb != null && climb.IsClimbing)
                return;

            if (_hardFalling)
            {
                EnsureRagdollKept();
                return;
            }

            _hardFalling = true;
            MuteInvectorFall();

            float drop = _airApexY - transform.position.y;
            Vector3 impact = body != null ? body.linearVelocity : Vector3.zero;
            if (motor != null && impact.y > motor.verticalVelocity)
                impact.y = motor.verticalVelocity;
            if (impact.y > -8f && drop > 1f)
            {
                float fromDrop = -Mathf.Sqrt(Mathf.Max(0f, 2f * 9.81f * drop));
                if (fromDrop < impact.y)
                    impact.y = fromDrop;
            }

            if (motor != null)
            {
                motor.isJumping = false;
                motor.input = Vector3.zero;
                motor.inputMagnitude = 0f;
            }

            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (ragdoll != null)
            {
                ragdoll.keepRagdolled = true;
                ragdoll.ignoreGetUpAnimation = true;
            }

            if (ragdoll != null && !ragdoll.isActive)
                ragdoll.ActivateRagdoll(null, 999f);
            else if (motor != null && motor.onActiveRagdoll != null)
                motor.onActiveRagdoll.Invoke(null);

            ApplyFallVelocityToBones(impact);
            EnsureRagdollKept();

            Debug.Log($"{BuildStamp} lethal drop={drop:F1} fallT={_fallTime:F2} vy={impact.y:F1} ragdoll={(ragdoll != null && ragdoll.isActive)}");

            SurvivalStats stats = ResolveSurvivalStats();
            if (stats != null && !stats.IsDead)
                stats.KillFromFall();
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
            if (jetpack != null && jetpack.IsBoostingNow)
                return;
            float drop = _airApexY - y;
            if (drop < 6f && vy > -10f)
                return;
            if (animator == null || !animator.HasState(0, FallingState))
                return;
            animator.CrossFadeInFixedTime(FallingState, 0.08f, 0);
            _playedFallPose = true;
        }

        private void ApplyFallVelocityToBones(Vector3 impact)
        {
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody rb = bodies[i];
                if (rb == null || rb == body || rb.isKinematic)
                    continue;
                rb.linearVelocity = impact;
            }
        }

        private void EnsureRagdollKept()
        {
            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (ragdoll == null)
                return;
            ragdoll.keepRagdolled = true;
            ragdoll.ignoreGetUpAnimation = true;
            if (!ragdoll.isActive)
                ragdoll.ActivateRagdoll(null, 999f);
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
