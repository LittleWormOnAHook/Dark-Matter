using Invector.vCharacterController;
using Project.Features.Climb;
using Project.Features.Dash;
using Project.Features.Jetpack;
using Project.Survival;
using Project.Vehicles;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Base-layer Player_v7 landing:
    /// 1) short hop — LandLow
    /// 2) under lethal — LandHigh (50% health in the damage band)
    /// 3) lethal — Bounce, then ragdoll, then retry/end
    /// Jetpack grace still treats a lethal-height land as LandHigh.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(320)]
    public sealed class DMLandingDirector : MonoBehaviour
    {
        private const string BuildStamp = "DMLanding 0919-anyfall-gate";
        private const float TorsoTwistLimit = 18f;
        private const float TorsoSwing1Limit = 10f;
        private const float TorsoSwing2Limit = 8f;
        private const float TorsoJointSpring = 280f;
        private const float TorsoJointDamper = 36f;
        private const float TorsoProjectionAngle = 22f;
        private const float HipAngularDamping = 3.2f;
        private const float SpineAngularDamping = 2.4f;

        // Fallbacks if Resources Landing/DMLandingProfile is missing � Genesis Studio edits Live.
        private const float FallbackGroundCommitSeconds = 0.06f;
        private const float FallbackLandAnticipateMaxDist = 0.52f;
        private const float FallbackLandAnticipateMinFallSpeed = 1.35f;
        private const float FallbackLandCrossFadeSeconds = 0.08f;
        private const float FallbackWalkableDist = 0.45f;
        private const float FallbackWalkableNormalY = 0.55f;
        /// <summary>Allow Jump->Falling only this close to dirt (meters).</summary>
        private const float ShortHopNearGroundDist = 0.7f;

        [SerializeField] private DM_ClimbDashProfile climbProfile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private vRagdoll ragdoll;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider capsule;
        [SerializeField] private DMClimbController climb;
        [SerializeField] private DMDashController dash;

        private bool _landing;
        private bool _hardFalling;
        private bool _shortHopArmed;
        private bool _enteredLandState;
        private bool _pendingLethalRagdoll;
        private bool _lockDuringLand;
        private int _ownedLandState;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;
        private bool _heldBlockFallDamage;
        private bool _heldDisableAnimations;
        private float _savedAnimatorSpeed = 1f;
        private float _clipEndsAt = -1f;
        /// <summary>Hero LandHigh: cut soft clip tail + snap out of crouch (no exit cushion).</summary>
        private bool _ownedLandSharpExit;
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
        private Vector3 _flopImpact;
        private float _flopBoostUntil = -1f;
        private float _unmuteAt = -1f;
        private int _flopBoneCount;

        private bool _hasVerticalVelocity;
        private bool _hasJetpackLand;
        private bool _hasLandHigh;
        private bool _hasIsGrounded;
        private bool _hasGroundDistance;

        private static readonly RaycastHit[] ProbeHits = new RaycastHit[16];
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int JetpackLand = Animator.StringToHash("JetpackLand");
        private static readonly int JetpackLandState = Animator.StringToHash("Jetpack Land");
        private static readonly int LandHighTrigger = Animator.StringToHash("LandHigh");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int GroundDistance = Animator.StringToHash("GroundDistance");
        private static readonly int LandLowState = Animator.StringToHash("LandLow");
        private static readonly int LandHighState = Animator.StringToHash("LandHigh");
        private static readonly int BounceState = Animator.StringToHash("Bounce");
        private static readonly int Locomotion = Animator.StringToHash("Locomotion");
        private static readonly int JumpState = Animator.StringToHash("Jump");
        private static readonly int JumpMoveState = Animator.StringToHash("JumpMove");
        private static readonly int FallingState = Animator.StringToHash("Falling");
        private static readonly int[] StolenGetUpStates =
        {
            Animator.StringToHash("Falling"),
            Animator.StringToHash("StandUpFromBelly"),
            Animator.StringToHash("StandUpFromBack"),
            Animator.StringToHash("StandUp@FromBelly"),
            Animator.StringToHash("StandUp@FromBack"),
            Animator.StringToHash("GetUpFromBelly"),
            Animator.StringToHash("GetUpFromBack"),
            Animator.StringToHash("Jetpack Land"),
            Animator.StringToHash("Roll"),
        };

        public bool IsLandingLocked => _landing;
        public bool IsHardFalling => _hardFalling;

        public void ResetForRespawn()
        {
            _hardFalling = false;
            _landing = false;
            _enteredLandState = false;
            _pendingLethalRagdoll = false;
            _lockDuringLand = false;
            _ownedLandState = 0;
            _ownedLandSharpExit = false;
            _physAir = false;
            _fallTime = 0f;
            _flopBoostUntil = -1f;
            _flopBoneCount = 0;
            _clipEndsAt = -1f;
            _ignoreLandsUntil = 0f;
            _unmuteAt = -1f;
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
                motor.blockApplyFallDamage = false;
                motor.disableAnimations = false;
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

        /// <summary>Terrain rescue snapped us onto the surface — do not count that as a lethal land.</summary>
        public void NotifyTerrainRescue()
        {
            _fallTime = 0f;
            _physAir = false;
            _wasGrounded = true;
            _groundedFor = GroundCommitSeconds;
            _airApexY = transform.position.y;
            _airVerticalVelocity = 0f;
            IgnoreLandsFor(1.25f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
                player = GameObject.Find("Player_v7 Variant");
            if (player == null || player.GetComponent<DMLandingDirector>() != null)
                return;

            player.AddComponent<DMLandingDirector>();
        }

        private void Awake()
        {
            climbProfile = DM_ClimbDashProfile.Resolve(climbProfile);
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
            if (dash == null)
                dash = GetComponent<DMDashController>();
            CacheAnimatorParameters();
        }

        private bool IsDashing => dash != null && dash.IsDashing;

        /// <summary>Dash restores locomotion — cancel half-started lands and unstick motor/animator.</summary>
        public void OnDashEnded()
        {
            if (_landing && !_enteredLandState)
                EndLanding(restoreLocks: false);
            else if (_landing)
            {
                _heldLockMovement = false;
                _heldLockAnimMovement = false;
            }

            if (_landing)
                return;

            ReleaseMotorForLocomotion();
            RestoreAnimatorLocomotion();
        }

        private void ReleaseMotorForLocomotion()
        {
            if (motor == null)
                return;

            motor.lockMovement = false;
            motor.lockAnimMovement = false;
            motor.disableAnimations = false;
            motor.disableCheckGround = false;
        }

        private void RestoreAnimatorLocomotion()
        {
            if (animator == null)
                return;

            animator.enabled = true;
            if (animator.speed <= 0.01f)
                animator.speed = 1f;

            if (animator.HasState(0, Locomotion))
                animator.CrossFadeInFixedTime(Locomotion, 0.1f, 0, 0f);
        }

        private void Start()
        {
            if (_loggedBuild)
                return;
            _loggedBuild = true;
            // Startup stamp silenced.
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
            _hasGroundDistance = false;
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
                else if (hash == GroundDistance)
                    _hasGroundDistance = true;
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
            if (Project.Core.GameplayWorldSimulation.IsFrozen)
                return;

            if (motor == null)
                return;

            if (climb != null && climb.IsClimbing)
                return;

            if (IsDashing)
                return;

            if (ClearMountedAir())
                return;

            TrackShortHopSession();
            ProtectShortHopApex();

            bool walkable = OnWalkableGround(out _);
            // Regular jumps must stay on Invector � muting here was fighting the hop.
            if ((!walkable || !motor.isGrounded) && !IsShortHopAir())
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

            if (_landing && _lockDuringLand)
            {
                SuppressInvectorLand();
                HoldOwnedLandState();
                KickStolenGetUpStates();
            }
            else if (_physAir && !IsShortHopAir())
            {
                SuppressAirborneFall();
                if (!_landing && !_hardFalling && Time.unscaledTime >= _ignoreLandsUntil)
                    TryAnticipateLanding();
            }
        }

        private void Update()
        {
            if (Project.Core.GameplayWorldSimulation.IsFrozen)
                return;

            if (motor == null || animator == null)
                return;

            if (climb != null && climb.IsClimbing)
            {
                _fallTime = 0f;
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
            }
            else
            {
                _groundedFor = 0f;
                if (jetpack != null && jetpack.IsBoostingNow)
                    _fallTime = 0f;
                else if (vy < -2f)
                    _fallTime += Time.unscaledDeltaTime;
                if (!IsShortHopAir())
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
            }
            else if (_groundedFor >= GroundCommitSeconds)
            {
                if (_physAir && !IsDashing)
                {
                    float drop = _airApexY - transform.position.y;
                    // Regular jump: OnWalkableGround probe goes true early and was CrossFading LandLow
                    // while Jump was still playing (mid-air pop / fast shift). Wait for real grounded.
                    bool shortHop = !JetpackHeroThisAir()
                        && (_shortHopArmed || IsInvectorRegularJump());
                    bool commitOk = !shortHop || (motor.isGrounded && vy <= 0.05f);
                    if (commitOk
                        && !_landing
                        && !_hardFalling
                        && Time.unscaledTime >= _ignoreLandsUntil)
                        BeginLanding(drop, ReadFallVelocity());

                    // Keep apex while short-hop is still airborne over a false walkable probe.
                    if (!shortHop || motor.isGrounded || _landing)
                        ResetAirTracking();
                }
                else
                    ResetAirTracking();
            }

            if (_physAir && !_landing && !_hardFalling && !IsDashing)
                TryAnticipateLanding();

            _wasGrounded = walkable;

            if (_hardFalling)
            {
                EnsureRagdollKept();
                return;
            }

            if (_unmuteAt > 0f && Time.unscaledTime >= _unmuteAt && !_landing && !_physAir)
            {
                UnmuteInvectorFall();
                _unmuteAt = -1f;
            }
        }

        private void LateUpdate()
        {
            if (motor == null || animator == null)
                return;
            if (climb != null && climb.IsClimbing)
                return;

            TrackShortHopSession();
            ProtectShortHopApex();
            // Soft bounce sets _landing but must not mute/zero VerticalVelocity — that fought LandLow on regular jumps.
            bool hardLandOrAir = _physAir || (_landing && _lockDuringLand) || _unmuteAt > Time.unscaledTime;
            if (!IsShortHopAir() && hardLandOrAir)
            {
                MuteInvectorFall();
                if (_hasVerticalVelocity)
                    animator.SetFloat(VerticalVelocity, 0f);
            }

            if (_physAir && !_landing && !IsShortHopAir())
                SuppressAirborneFall();

            if (!_landing)
                return;

            if (_lockDuringLand)
            {
                ApplyLock();
                // Settle velocity only after feet are close � keeps hero crouch from floating then dropping.
                if (_ownedLandSharpExit && ProbeGround(out float lockDist) && lockDist <= SoftenNearGroundDist)
                {
                    SoftenImpact();
                    SnapFeetToGround();
                }
                SuppressInvectorLand();
                HoldOwnedLandState();
                KickStolenGetUpStates();
            }
            // Soft bounce: one CrossFade only � no per-frame Hold/Suppress fight with Invector.
            TickOwnedLand();
        }

        [SerializeField] private float heroDropMeters = 2.6f;
        [SerializeField] private float lethalDropMeters = 100f;
        [SerializeField] private float fallDamageStartMeters = 40f;
        [SerializeField] private float fallDamageLethalPercent = 0.3f;
        [SerializeField] private float fallDamageHealthFraction = 0.5f;
        [SerializeField] private float jetpackLethalDelay = 6f;

        private DM_ClimbDashProfile LiveClimb
        {
            get
            {
                climbProfile = DM_ClimbDashProfile.Resolve(climbProfile);
                return climbProfile;
            }
        }

        private DMLandingProfile LiveLanding => DMLandingProfile.Live;

        private float GroundCommitSeconds =>
            LiveLanding != null ? LiveLanding.groundCommitSeconds : FallbackGroundCommitSeconds;

        private float LandAnticipateMaxDist =>
            LiveLanding != null ? LiveLanding.landAnticipateMaxDist : FallbackLandAnticipateMaxDist;

        private float LandAnticipateMinFallSpeed =>
            LiveLanding != null ? LiveLanding.landAnticipateMinFallSpeed : FallbackLandAnticipateMinFallSpeed;

        private float LandCrossFadeSeconds =>
            LiveLanding != null ? LiveLanding.landCrossFadeSeconds : FallbackLandCrossFadeSeconds;

        private float WalkableDist =>
            LiveLanding != null ? LiveLanding.walkableDist : FallbackWalkableDist;

        private float WalkableNormalY =>
            LiveLanding != null ? LiveLanding.walkableNormalY : FallbackWalkableNormalY;

        private float LandLowDuration =>
            LiveLanding != null ? LiveLanding.landLowDuration : 0.45f;

        private float LandHighDuration =>
            LiveLanding != null ? LiveLanding.landHighDuration : 1f;

        private float JetpackLandDuration =>
            LiveLanding != null ? LiveLanding.jetpackLandDuration : 1.25f;

        private bool PreferJetpackLandState =>
            LiveLanding == null || LiveLanding.preferJetpackLandState;

        private bool OwnedBounceOnRegularJump =>
            LiveLanding == null || LiveLanding.ownedBounceOnRegularJump;

        private bool PreferBounceStateForShortLand =>
            LiveLanding == null || LiveLanding.preferBounceStateForShortLand;

        /// <summary>Short bounce ceiling: Studio shortBounceMaxDropMeters, else ClimbDash heroDropMeters.</summary>
        private float BounceCeilingMeters =>
            LiveLanding != null && LiveLanding.shortBounceMaxDropMeters > 0.01f
                ? LiveLanding.shortBounceMaxDropMeters
                : HeroMin;

        private float BounceLandDuration =>
            LiveLanding != null ? LiveLanding.bounceLandDuration : 0.65f;

        private float MinDropMetersToLand =>
            LiveLanding != null ? LiveLanding.minDropMetersToLand : 0.2f;

        private float MinFallSpeedYToLand =>
            LiveLanding != null ? LiveLanding.minFallSpeedYToLand : -2f;

        private float SoftenNearGroundDist =>
            LiveLanding != null
                ? Mathf.Max(0.12f, LiveLanding.landAnticipateMaxDist + 0.08f)
                : 0.22f;

        private float HeroMin => LiveClimb != null ? LiveClimb.heroDropMeters : heroDropMeters;
        private float LethalMin => LiveClimb != null ? LiveClimb.lethalDropMeters : lethalDropMeters;
        private float DamageStartMeters => LiveClimb != null ? LiveClimb.fallDamageStartMeters : fallDamageStartMeters;
        private float DamageLethalPercent => LiveClimb != null ? LiveClimb.fallDamageLethalPercent : fallDamageLethalPercent;
        private float DamageHealthFraction => LiveClimb != null ? LiveClimb.fallDamageHealthFraction : fallDamageHealthFraction;
        private float JetDelay => LiveClimb != null ? LiveClimb.jetpackLethalDelay : jetpackLethalDelay;

        /// <summary>No damage below 40m, or below 30% less than lethal if that band is larger.</summary>
        private float NoDamageMax => Mathf.Max(DamageStartMeters, LethalMin * (1f - DamageLethalPercent));

        private enum FallLand
        {
            LandLow,
            LandHigh,
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

        private float ClampDropToImpact(float dropMeters, float airVelocity)
        {
            DMLandingProfile profile = LiveLanding;
            float epsilon = profile != null ? profile.clampDropSpeedEpsilon : 0.5f;
            float shortCap = profile != null ? profile.clampDropShortCap : 1.5f;
            float slack = profile != null ? profile.clampDropPhysicsSlack : 6f;

            float speed = Mathf.Abs(airVelocity);
            // Soft touch: keep measured apex drop so Studio hero/lethal bands still tier correctly.
            // Only clamp tiny / noisy apex values.
            if (speed < epsilon)
                return dropMeters;

            float physicsDrop = (speed * speed) / (2f * 9.81f);
            if (dropMeters > physicsDrop + slack)
                return physicsDrop;
            return dropMeters;
        }

        private void TryAnticipateLanding()
        {
            if (_landing || _hardFalling || !_physAir)
                return;
            if (IsDashing)
                return;

            if (Time.unscaledTime < _ignoreLandsUntil)
                return;
            if (climb != null && climb.IsClimbing)
                return;
            if (PlayerVehicleState.IsMounted)
                return;
            if (jetpack != null && jetpack.IsBoostingNow)
                return;
            if (IsShortHopAir())
                return;
            // Jetpack / hero-armed air: wait for real ground commit. Anticipating played the crouch in empty air.
            if (JetpackHeroThisAir())
                return;

            if (!ProbeGround(out float groundDist))
                return;

            if (groundDist > LandAnticipateMaxDist)
                return;

            float vy = ReadFallVelocity();
            if (vy > -LandAnticipateMinFallSpeed)
                return;

            float drop = Mathf.Max(_airApexY - transform.position.y, groundDist);
            BeginLanding(drop, vy);
        }

        private void ResetAirTracking()
        {
            _physAir = false;
            _airApexY = transform.position.y;
            _airVerticalVelocity = 0f;
            _fallTime = 0f;
        }

        private FallLand ClassifyFall(float dropMeters, bool boosted, float verticalVelocity)
        {
            float lethalMin = LethalMin;
            bool jetGrace = JetpackGraceActive();
            bool lethalDrop = dropMeters >= lethalMin;

            // Studio Jump/Landing: BounceCeiling (heroDropMeters or override) = bounce -> hero.
            if (lethalDrop && !jetGrace)
                return FallLand.Lethal;
            if (dropMeters < BounceCeilingMeters)
                return FallLand.LandLow;
            return FallLand.LandHigh;
        }

        private void BeginLanding(float dropMeters, float airVelocity)
        {
            if (IsDashing)
                return;

            dropMeters = ClampDropToImpact(dropMeters, airVelocity);
            bool boosted = JetpackHeroThisAir();

            // Regular-jump soft bounce must not start in empty air (probe can see ground early).
            if (!boosted
                && OwnedBounceOnRegularJump
                && (motor == null || !motor.isGrounded))
                return;

            // Micro-contact skip is for tiny foot taps only. When Studio wants owned regular-jump bounce, keep going.
            // Jetpack touchdowns always get an owned absorb.
            if (!boosted
                && !OwnedBounceOnRegularJump
                && dropMeters < MinDropMetersToLand
                && airVelocity > MinFallSpeedYToLand)
                return;

            FallLand kind = ClassifyFall(dropMeters, boosted, airVelocity);
            if (kind == FallLand.Lethal)
            {
                BeginBounceThenRagdoll();
                return;
            }

            int state;
            float duration;
            bool lockMove;
            bool sharpExit;

            if (kind == FallLand.LandLow)
            {
                // Regular jump can opt out in Studio (ownedBounceOnRegularJump).
                if (!boosted && !OwnedBounceOnRegularJump)
                {
                    UnmuteInvectorFall();
                    return;
                }

                // Regular jump + short jetpack: nice bounce (Studio landLowDuration / Bounce / Jetpack Land).
                state = ResolveShortBounceState(boosted);
                duration = LandLowDuration;
                lockMove = false;
                sharpExit = false;
            }
            else
            {
                // Mid / high: LandHigh hero crouch. Damage when drop is in Studio damage band.
                state = LandHighState;
                duration = LandHighDuration;
                lockMove = true;
                sharpExit = true;
            }

            StartOwnedLand(state, duration, lockMove, sharpExit);
            if (kind == FallLand.LandHigh)
                ApplyFallDamageIfNeeded(dropMeters);
        }

        private int ResolveShortBounceState(bool boosted)
        {
            // Studio: LandLow first; Bounce if preferred/missing LandLow; Jetpack Land fallback.
            if (animator != null && animator.HasState(0, LandLowState))
                return LandLowState;
            if (PreferBounceStateForShortLand && animator != null && animator.HasState(0, BounceState))
                return BounceState;
            if (boosted && PreferJetpackLandState && animator != null)
            {
                if (animator.HasState(0, JetpackLandState))
                    return JetpackLandState;
                if (animator.HasState(0, JetpackLand))
                    return JetpackLand;
            }
            if (animator != null && animator.HasState(0, BounceState))
                return BounceState;
            return LandHighState;
        }

        private void BeginBounceThenRagdoll()
        {
            int state = animator != null && animator.HasState(0, BounceState)
                ? BounceState
                : LandHighState;
            StartOwnedLand(state, BounceLandDuration, lockMove: true);
            _pendingLethalRagdoll = true;
        }

        private void StartOwnedLand(int stateHash, float duration, bool lockMove, bool sharpExit = false)
        {
            if (animator == null || IsDashing)
                return;

            MuteInvectorFall();
            // Hero lands: settle feet. Soft bounce (regular jump): skip snap/soften � that popped the hop.
            if (lockMove && ProbeGround(out float softDist) && softDist <= SoftenNearGroundDist)
            {
                SoftenImpact();
                SnapFeetToGround();
            }
            _landing = true;
            _enteredLandState = false;
            _pendingLethalRagdoll = false;
            _lockDuringLand = lockMove;
            _ownedLandSharpExit = sharpExit;
            _ownedLandState = stateHash;
            _shortHopArmed = false;
            if (motor != null)
            {
                _heldLockMovement = motor.lockMovement && !IsDashing;
                _heldLockAnimMovement = motor.lockAnimMovement && !IsDashing;
                _heldBlockFallDamage = motor.blockApplyFallDamage;
                _heldDisableAnimations = motor.disableAnimations;
                motor.blockApplyFallDamage = true;
                // Soft bounce must leave Invector anim ownership alone � disableAnimations made regular jumps look broken.
                if (lockMove)
                    motor.disableAnimations = true;
            }

            _savedAnimatorSpeed = animator.speed;
            animator.speed = 1f;
            if (lockMove)
                ApplyLock();
            else if (motor != null)
            {
                motor.isJumping = false;
                if (motor.verticalVelocity < 0f)
                    motor.verticalVelocity = 0f;
            }

            if (lockMove)
                SuppressInvectorLand();
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasJetpackLand)
                animator.ResetTrigger(JetpackLand);
            if (animator.HasState(0, stateHash))
                animator.CrossFadeInFixedTime(stateHash, LandCrossFadeSeconds, 0, 0f);
            float minHold = lockMove ? 0.2f : 0.28f;
            _clipEndsAt = Time.unscaledTime + Mathf.Max(minHold, duration);
        }

        private void ApplyFallDamageIfNeeded(float dropMeters)
        {
            if (dropMeters < NoDamageMax)
                return;

            SurvivalStats stats = ResolveSurvivalStats();
            if (stats == null || stats.IsDead)
                return;

            float damage = stats.maxHealth * Mathf.Clamp01(DamageHealthFraction);
            if (damage <= 0f)
                return;

            stats.ApplyDamage(damage, "fall");
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
            _physAir = false;
            _wasGrounded = true;
            _groundedFor = GroundCommitSeconds;
            _airApexY = transform.position.y;
            _airVerticalVelocity = 0f;
            if (!_loggedMountedGate)
            {
                _loggedMountedGate = true;
                // silenced 0831 BuildStamp log
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
            // silenced 0831 BuildStamp log

            SurvivalStats stats = ResolveSurvivalStats();
            if (stats != null && !stats.IsDead)
                stats.KillFromFall();

            ResetAirTracking();
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


        private void SnapFeetToGround()
        {
            DMLandingProfile profile = LiveLanding;
            float skin = profile != null ? profile.landFeetGroundSkin : 0.03f;
            float maxSnap = profile != null ? profile.landMaxFeetSnap : 0.85f;
            if (!ProbeGround(out float dist))
                return;
            if (dist <= skin || dist > maxSnap)
                return;

            float delta = dist - skin;
            if (delta <= 0.001f)
                return;

            Vector3 p = transform.position;
            p.y -= Mathf.Min(delta, maxSnap);
            transform.position = p;
            if (body != null && !body.isKinematic)
            {
                Vector3 v = body.linearVelocity;
                if (v.y < 0f)
                    v.y = 0f;
                body.linearVelocity = v;
            }
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
            DMLandingProfile probeProfile = LiveLanding;
            float originUp = probeProfile != null ? probeProfile.probeRayOriginUp : 0.2f;
            float rayMax = probeProfile != null ? probeProfile.probeRayMaxDown : 5f;
            float belowReject = probeProfile != null ? probeProfile.probePivotBelowReject : -0.25f;
            float validMax = probeProfile != null ? probeProfile.probeValidMaxDist : 3.5f;
            Vector3 origin = transform.position + Vector3.up * originUp;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, ProbeHits, rayMax, ~0, QueryTriggerInteraction.Ignore);
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
                if (d < belowReject)
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
            return distance < validMax;
        }

        private bool OnWalkableGround(out float distance)
        {
            if (!ProbeGround(out distance))
                return false;
            if (distance > WalkableDist)
                return false;
            return motor == null || motor.isGrounded || distance <= 0.2f;
        }

        /// <summary>Invector standing / move jump — do not steal the animator or land clip.</summary>
        
        private void TrackShortHopSession()
        {
            if (motor == null)
                return;

            if (motor.isJumping && !JetpackHeroThisAir())
            {
                if (!_shortHopArmed)
                    _shortHopArmed = true;
            }
            else if (_shortHopArmed && motor.isGrounded && !_physAir)
            {
                _shortHopArmed = false;
            }
        }




        /// <summary>
        /// Falling used to Any-State interrupt Jump at GroundDistance > 0.25.
        /// Controller now gates Any-State Falling at 1.5m; Jump exits muted.
        /// </summary>
        private void ProtectShortHopApex()
        {
            // Intentionally empty � Falling gate is on Base Layer Any State.
        }



        private bool IsShortHopSessionActive()
        {
            if (_shortHopArmed && (_physAir || (motor != null && motor.isJumping)))
                return true;
            return IsShortHopAir();
        }

        private bool IsInvectorRegularJump()
        {
            return motor != null && motor.isJumping && !JetpackHeroThisAir();
        }

        private bool IsShortHopAir()
        {
            if (_landing || _hardFalling || JetpackHeroThisAir())
                return false;
            if (IsInvectorRegularJump())
                return true;
            // Keep protecting after Invector clears isJumping (~0.3s) until we leave short-hop height.
            // Do not treat random short falls as short hops - that fought Invector and glitched regular jumps.
            if (_shortHopArmed && _physAir)
                return (_airApexY - transform.position.y) < HeroMin;
            return false;
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

        private void TickOwnedLand()
        {
            if (_pendingLethalRagdoll)
            {
                if (_clipEndsAt > 0f && Time.unscaledTime >= _clipEndsAt)
                {
                    _landing = false;
                    _pendingLethalRagdoll = false;
                    BeginLethalFall();
                }
                return;
            }

            bool inState = _ownedLandState != 0 && StateMatches(_ownedLandState);
            if (inState)
                _enteredLandState = true;
            else if (_enteredLandState)
            {
                EndLanding(restoreLocks: true);
                return;
            }

            // Hero LandHigh clip has a soft rise at the end - cut before that cushion plays.
            if (_ownedLandSharpExit && inState && animator != null && !animator.IsInTransition(0))
            {
                AnimatorStateInfo landInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (landInfo.normalizedTime >= 0.62f)
                {
                    EndLanding(restoreLocks: true);
                    return;
                }
            }

            if (_clipEndsAt > 0f && Time.unscaledTime >= _clipEndsAt)
                EndLanding(restoreLocks: true);
        }

        private void HoldOwnedLandState()
        {
            if (animator == null || _ownedLandState == 0)
                return;

            if (StateMatches(_ownedLandState))
                return;

            AnimatorStateInfo next = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : default;
            if (animator.IsInTransition(0) && next.shortNameHash == _ownedLandState)
                return;

            animator.CrossFadeInFixedTime(_ownedLandState, LandCrossFadeSeconds, 0);
        }

        private void KickStolenGetUpStates()
        {
            if (animator == null || _ownedLandState == 0)
                return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : current;

            if (IsStolenGetUpHash(current.shortNameHash) || IsStolenGetUpHash(next.shortNameHash))
            {
                if (animator.HasState(0, _ownedLandState))
                    animator.CrossFadeInFixedTime(_ownedLandState, LandCrossFadeSeconds, 0);
            }
        }

        private static bool IsStolenGetUpHash(int hash)
        {
            for (int i = 0; i < StolenGetUpStates.Length; i++)
            {
                if (StolenGetUpStates[i] == hash)
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

        private void SuppressAirborneFall()
        {
            if (animator == null)
                return;
            if (jetpack != null && jetpack.IsBoostingNow)
                return;

            MuteInvectorFall();
            if (_hasVerticalVelocity)
                animator.SetFloat(VerticalVelocity, 0f);
            if (_hasGroundDistance)
                animator.SetFloat(GroundDistance, 0.1f);
            if (_hasIsGrounded)
                animator.SetBool(IsGrounded, false);
        }

        private void SuppressInvectorLand()
        {
            if (animator == null)
                return;

            if (motor != null)
                motor.verticalVelocity = 0f;

            if (_hasVerticalVelocity)
                animator.SetFloat(VerticalVelocity, 0f);
            if (_hasGroundDistance)
                animator.SetFloat(GroundDistance, 0.05f);
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasJetpackLand)
                animator.ResetTrigger(JetpackLand);
            // Do not push IsGrounded while we own the land clip — that feeds Invector GetUp.
        }

        private void EndLanding(bool restoreLocks)
        {
            if (!_landing)
                return;

            _landing = false;
            _enteredLandState = false;
            _pendingLethalRagdoll = false;
            _lockDuringLand = false;
            _ownedLandState = 0;
            _clipEndsAt = -1f;
            bool sharpExit = _ownedLandSharpExit;
            _ownedLandSharpExit = false;
            SuppressInvectorLand();

            if (jetpack != null && motor != null && motor.isGrounded && _groundedFor >= GroundCommitSeconds)
                jetpack.NotifyLanded();

            if (animator != null)
            {
                animator.speed = _savedAnimatorSpeed;
                if (animator.HasState(0, Locomotion))
                {
                    // Hero: snap out - the 0.35 blend was the tail cushion. Soft jetpack absorb keeps a light blend.
                    float exitBlend = sharpExit ? LandCrossFadeSeconds : 0.18f;
                    animator.CrossFadeInFixedTime(Locomotion, exitBlend, 0);
                }
            }

            if (restoreLocks && motor != null)
            {
                motor.lockMovement = _heldLockMovement;
                motor.lockAnimMovement = _heldLockAnimMovement;
                motor.blockApplyFallDamage = _heldBlockFallDamage;
                motor.disableAnimations = _heldDisableAnimations;
            }
            else if (motor != null)
            {
                motor.lockMovement = false;
                motor.lockAnimMovement = false;
                motor.disableAnimations = false;
                motor.blockApplyFallDamage = _heldBlockFallDamage;
            }

            ResetAirTracking();
            float endGrace = LiveLanding != null ? LiveLanding.landGroundedEndGraceSeconds : 0.25f;
            _ignoreLandsUntil = Mathf.Max(_ignoreLandsUntil, Time.unscaledTime + endGrace);
            // Soft bounce: do not keep muting Invector after � that made regular-jump recovery look wrong.
            if (sharpExit)
            {
                _unmuteAt = Time.unscaledTime + 0.4f;
                SuppressInvectorLand();
            }
            else
            {
                _unmuteAt = -1f;
                UnmuteInvectorFall();
            }
        }
    }
}