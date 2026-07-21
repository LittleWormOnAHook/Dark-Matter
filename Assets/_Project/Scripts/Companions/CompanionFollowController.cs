using ECM2;
using Invector.vCharacterController;
using Project.AI;
using Project.Companions.Invector;
using Project.Crafting;
using Project.Data;
using Project.Interaction;
using Project.Pet;
using Project.Pioneers;
using Project.Player;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Formation follow with smoothed wander, companion spacing, terrain grounding, and obstacle sliding.
    /// Split across partials by responsibility — see CompanionFollowController.Follow.cs (loose-leash
    /// formation following, hold/defend positioning), .Combat.cs (break-away combat engagement ring),
    /// .Idle.cs (world-ambient idle/wander/ping-pong behaviors), .Movement.cs (capsule collision,
    /// step-up, terrain grounding, stuck recovery — the raw locomotion engine every other partial
    /// calls into via MoveTowards/SampleTerrainHeight/etc). This file keeps fields/config, lifecycle,
    /// the public Initialize/behavior-profile API, and the main Update() dispatch. Purely a
    /// mechanical reorganization (partial class split) — no behavior changed by the split.
    /// </summary>
    public partial class CompanionFollowController : MonoBehaviour
    {
        private static readonly Vector3[] FormationOffsets =
        {
            new Vector3(-3.8f, 0f, -3.4f),
            new Vector3(3.8f, 0f, -3.4f),
            new Vector3(0f, 0f, -5.2f)
        };

        private static readonly Vector3[] IdleAnchorOffsets =
        {
            new Vector3(-2.5f, 0f, -0.8f),
            new Vector3(2.5f, 0f, -0.8f),
            new Vector3(0f, 0f, -3.4f),
            new Vector3(-1.9f, 0f, 1.5f),
            new Vector3(1.9f, 0f, 1.5f),
            new Vector3(0.8f, 0f, 2.1f),
            new Vector3(-0.8f, 0f, 2.1f)
        };

        private static readonly System.Collections.Generic.List<CompanionFollowController> ActiveCompanions =
            new System.Collections.Generic.List<CompanionFollowController>(4);

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.4f;
        [SerializeField] private float runSpeed = 8.5f;
        [SerializeField] private float catchUpSpeed = 8.5f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float stopDistance = 0.45f;
        [SerializeField] private float maxFollowDistance = 14f;
        [SerializeField] private float catchUpDistance = 5.5f;
        [SerializeField] private float teleportCatchUpDistance = 20f;
        [SerializeField] private float groundOffset = 0.05f;
        [SerializeField] private float bodyRadius = 0.42f;
        [SerializeField] private float bodyHeight = 1.75f;

        [Header("Individual Behavior")]
        [SerializeField] private float wanderRadius = 0.65f;
        [Tooltip("Scales walkSpeed while meandering near formation (1 = same as player walk).")]
        [SerializeField] [Range(0.1f, 1f)] private float wanderPaceScale = 0.3f;
        [SerializeField] private float wanderRetargetMin = 2.8f;
        [SerializeField] private float wanderRetargetMax = 4.6f;
        [SerializeField] private float wanderSmoothTime = 1.35f;
        [SerializeField] private float restFacingSpeed = 1.1f;
        [SerializeField] private float formationDriftDegreesPerSecond = 2.8f;
        [Tooltip("Formation/idle slots use travel direction, not owner body yaw (avoids orbiting when the player turns camera).")]
        [SerializeField] private float formationHeadingSmoothTime = 0.45f;
        [SerializeField] private float minOwnerSpeedForHeadingUpdate = 0.3f;

        [Header("Loose Follow")]
        [Tooltip("Start walking toward the player only when farther than this (hysteresis with stop).")]
        [SerializeField] private float looseLeashStart = 5.5f;
        [Tooltip("Stop following once within this distance — ignore further player motion until leash breaks.")]
        [SerializeField] private float looseLeashStop = 3.2f;
        [SerializeField] private float looseFollowBackDistance = 3.5f;
        [SerializeField] private float looseSlotSpacing = 2.2f;
        [SerializeField] private float looseTargetSmoothTime = 1.5f;
        [Tooltip("Only update travel heading when the player is translating at least this fast (ignores spin-in-place).")]
        [SerializeField] private float travelHeadingMinSpeed = 1.15f;
        [SerializeField] private float travelHeadingSmoothTime = 2.0f;

        [Header("Follow Delay")]
        [Tooltip("Seconds pioneers wait after the player starts moving before they begin following.")]
        [SerializeField] private float followMovementDelayMin = 0.12f;
        [SerializeField] private float followMovementDelayMax = 0.12f;

        [Header("Avoidance")]
        [SerializeField] private float playerAvoidRadius = 2.75f;
        [SerializeField] private float playerAvoidStrength = 1.15f;
        [SerializeField] private float companionAvoidRadius = 2.4f;
        [SerializeField] private float companionAvoidStrength = 0.7f;
        [SerializeField] private float petAvoidRadius = 2.4f;
        [SerializeField] private float petAvoidStrength = 0.65f;

        [Header("Idle Positions")]
        [SerializeField] [Range(0f, 1f)] private float idleProbability = 0.9f;
        [SerializeField] private float idleWanderRange = 10f;
        [Tooltip("Scales walkSpeed while wandering away from idle anchor when the owner is still.")]
        [SerializeField] [Range(0.1f, 1f)] private float idleWanderPaceScale = 0.3f;
        [SerializeField] private float idleRestDurationMin = 12f;
        [SerializeField] private float idleRestDurationMax = 28f;
        [SerializeField] private float idleWanderDurationMin = 3f;
        [SerializeField] private float idleWanderDurationMax = 6f;
        [SerializeField] private float idleAnchorChangeMin = 4.2f;
        [SerializeField] private float idleAnchorChangeMax = 7.5f;

        [Header("World Ambient")]
        [SerializeField] private CompanionFollowBehaviorMode behaviorMode = CompanionFollowBehaviorMode.Follow;
        [SerializeField] private float pingPongPatrolRadius = 4f;
        [SerializeField] private float pingPongPauseMin = 1.5f;
        [SerializeField] private float pingPongPauseMax = 3.5f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstructionLayers = 1;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private int movementSlideIterations = 4;
        [SerializeField] private float collisionSkin = 0.03f;
        [Tooltip("Max height above Unity terrain sample pioneers may stand outdoors (prevents roof placement).")]
        [SerializeField] private float maxHeightAboveTerrain = 0.35f;
        [Tooltip("Max height above terrain for tagged/named interior walkables (ramps, stairs, floors).")]
        [SerializeField] private float maxInteriorHeightAboveTerrain = 6f;
        [Tooltip("Max ledge height pioneers can step onto. Kept at or above the player's ECM2 stepOffset.")]
        [SerializeField] private float stepOffset = 0.75f;
        [Tooltip("Extra step height added on top of the player's ECM2 stepOffset when syncing locomotion limits.")]
        [SerializeField] private float stepOffsetBonus = 0.3f;
        [Tooltip("Max walkable ground slope in degrees (matches player ECM2 CharacterMovement slopeLimit).")]
        [SerializeField] private float slopeLimit = 45f;

        [Header("Stuck Recovery")]
        [SerializeField] private float stuckSampleInterval = 0.3f;
        [SerializeField] private float stuckMinProgress = 0.08f;
        [SerializeField] private float stuckRecoverySidestep = 0.9f;
        [SerializeField] private int maxTrailAttemptsBeforeSidestep = 5;
        [SerializeField] private float trailRecoveryMinLookback = 6f;
        [SerializeField] private float trailRecoveryMaxLookback = 48f;
        [SerializeField] private float trailRecoveryMaxDuration = 8f;
        [SerializeField] private float trailRecoveryArrivalDistance = 0.65f;

        [Header("Trail Following")]
        [Tooltip("When the direct path to the follow target is blocked, steer toward the player's recent path.")]
        [SerializeField] private bool useTrailWhenPathBlocked = true;
        [SerializeField] private float trailFollowMinLookahead = 3f;
        [SerializeField] private float trailFollowMaxLookahead = 22f;
        [SerializeField] private float groundProbeHeight = 2.5f;
        [SerializeField] private float groundProbeDistance = 10f;

        private CapsuleCollider bodyCollider;
        private static readonly Collider[] OverlapBuffer = new Collider[16];
        private Transform owner;
        private Character ownerCharacter;
        private CompanionTaskQueue taskQueue;
        private int formationSlot;
        private string pioneerSeed;
        private float currentSpeed;
        private float wanderPhase;
        private float formationDriftAngle;
        private float formationHeadingYaw;
        private float idleRestYaw;
        private float nextWanderRetargetTime;
        private Vector3 wanderTargetOffset;
        private Vector3 smoothedWanderOffset;
        private Vector3 wanderVelocity;
        private Vector3 stepBackTarget;
        private float stepBackUntil;
        private Vector3 currentMoveDirection;
        private Vector3 lastOwnerPosition;
        private Vector3 ownerTravelDelta;
        private float ownerMotionSpeed;
        private bool isNearFormation;
        private bool isWandering;
        private bool wasOwnerMoving;
        private int[] idlePositionOrder;
        private int idleOrderIndex;
        private Vector3 currentIdleAnchorLocal;
        private float nextIdleAnchorChangeTime;
        private Vector3 idleWanderWorldTarget;
        private bool idleWanderPhaseActive;
        private float idlePhaseEndsAt;
        private bool catchUpActive;
        private PioneerBehaviorProfile activeProfile = new PioneerBehaviorProfile();
        private PioneerFollowMode followMode = PioneerFollowMode.FollowPlayer;
        private SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        private Vector3 worldAnchor;
        private Vector3 pingPongPointA;
        private Vector3 pingPongPointB;
        private bool pingPongMovingToB = true;
        private float pingPongPauseUntil;
        private bool worldAmbientInitialized;
        private PioneerWorldIdleJob assignedWorldJob = PioneerWorldIdleJob.None;
        private CompanionCombatController combatController;
        private float holdFacingYaw;
        private float maintainDistanceUntil;
        private Vector3 maintainDistanceTarget;
        private float maintainDistancePreferred;
        private bool maintainDistanceChase;
        private float allowFollowMovementAt;
        private float scheduledFollowMovementDelay;
        private Vector3 lastStuckSamplePosition;
        private float nextStuckSampleTime;
        private int stuckSidestepSign = 1;
        private Vector3 trailRecoveryTarget;
        private float trailRecoveryUntil;
        private Transform combatEngageTarget;
        private float combatEngagePreferredDistance = 2.4f;
        private float combatEngageMaxStrikeRange = 2.4f;
        private bool combatEngageIsRanged;
        private float combatOrbitSign;
        private int consecutiveStuckCount;
        private int trailAttemptsThisEpisode;
        private bool looseFollowActive;
        private Vector3 looseFollowSmoothedTarget;
        private Vector3 looseFollowVelocity;
        private Vector3 travelForward = Vector3.forward;
#if UNITY_EDITOR
        [SerializeField] private bool drawTrailRecoveryGizmos;
#endif
        private static int itemLayer = -1;
        private static int resourceLayer = -1;

        public float CurrentSpeed => currentSpeed;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public int FormationSlot => formationSlot;
        public Vector3 CurrentMoveDirection => currentMoveDirection;
        public bool IsNearFormation => isNearFormation;
        public bool IsWandering => isWandering;
        public PioneerFollowMode FollowMode => followMode;
        public CompanionFollowBehaviorMode BehaviorMode => behaviorMode;
        public PioneerWorldIdleJob AssignedWorldJob => assignedWorldJob;

        private void Awake()
        {
            EnsureBodyCollider();
            CacheWorldItemLayers();
        }

        public void Initialize(Transform followTarget, CompanionTaskQueue queue, int slotIndex, string pioneerId = null)
        {
            EnsureBodyCollider();
            owner = followTarget;
            ownerCharacter = followTarget != null ? followTarget.GetComponent<Character>() : null;
            lastOwnerPosition = followTarget != null ? followTarget.position : Vector3.zero;
            taskQueue = queue;
            formationSlot = Mathf.Clamp(slotIndex, 0, FormationOffsets.Length - 1);
            pioneerSeed = string.IsNullOrEmpty(pioneerId) ? name : pioneerId;
            combatController = GetComponent<CompanionCombatController>();
            formationHeadingYaw = followTarget != null ? followTarget.eulerAngles.y : 0f;
            if (followTarget != null)
            {
                Vector3 fwd = followTarget.forward;
                fwd.y = 0f;
                travelForward = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            }

            looseFollowActive = false;
            looseFollowSmoothedTarget = followTarget != null ? followTarget.position : Vector3.zero;
            SyncLocomotionLimitsFromOwner();

            int hash = pioneerSeed.GetHashCode();
            wanderPhase = (hash & 0xFFFF) / 65535f * Mathf.PI * 2f;
            formationDriftAngle = ((hash >> 16) & 0xFF) / 255f * 120f;
            idleRestYaw = ((hash >> 8) & 0xFF) / 255f * 50f - 25f;
            scheduledFollowMovementDelay = ResolveFollowMovementDelay();
            lastStuckSamplePosition = transform.position;
            nextStuckSampleTime = Time.time + stuckSampleInterval;
            nextWanderRetargetTime = Time.time + Random.Range(wanderRetargetMin, wanderRetargetMax);
            PickNewWanderTarget();
            RepickIdlePositionRoutine();
            BeginIdlePhase();
            behaviorMode = CompanionFollowBehaviorMode.Follow;
            worldAmbientInitialized = false;
        }

        /// <summary>
        /// World Echo/recruit ambient setup — no player follow target until recruited.
        /// </summary>
        public void InitializeWorldAmbient(
            Vector3 anchor,
            CompanionFollowBehaviorMode mode,
            string seedId = null,
            float patrolRadius = -1f,
            PioneerWorldIdleJob worldIdleJob = PioneerWorldIdleJob.None)
        {
            EnsureBodyCollider();
            owner = null;
            ownerCharacter = null;
            taskQueue = null;
            pioneerSeed = string.IsNullOrEmpty(seedId) ? name : seedId;
            behaviorMode = mode;
            assignedWorldJob = worldIdleJob;
            worldAnchor = anchor;
            worldAmbientInitialized = true;

            if (patrolRadius > 0f)
                pingPongPatrolRadius = patrolRadius;

            int hash = pioneerSeed.GetHashCode();
            idleRestYaw = ((hash >> 8) & 0xFF) / 255f * 50f - 25f;
            lastStuckSamplePosition = transform.position;
            nextStuckSampleTime = Time.time + stuckSampleInterval;
            SetupPingPongPoints();
            BeginIdlePhase();
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile, SkilledPioneerClass skilledClass)
        {
            pioneerClass = skilledClass;
            ApplyBehaviorProfile(profile);
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile)
        {
            activeProfile = profile != null ? profile.Clone() : new PioneerBehaviorProfile();
            followMode = activeProfile.followMode;
            if (owner != null)
                behaviorMode = activeProfile.followBehaviorMode;
            walkSpeed = activeProfile.walkSpeed;
            runSpeed = activeProfile.runSpeed;
            catchUpSpeed = activeProfile.catchUpSpeed;
            catchUpDistance = activeProfile.catchUpDistance;
            maxFollowDistance = activeProfile.maxFollowDistance;
            stopDistance = activeProfile.stopDistance;
            wanderPaceScale = activeProfile.wanderPaceScale;
            idleWanderPaceScale = activeProfile.wanderPaceScale;
            formationDriftDegreesPerSecond = activeProfile.formationDriftDegreesPerSecond;
            formationHeadingSmoothTime = activeProfile.formationHeadingSmoothTime;
        }

        public void SetFollowMode(PioneerFollowMode mode)
        {
            followMode = mode;
            if (activeProfile != null)
                activeProfile.followMode = mode;
        }

        public void SetBehaviorMode(CompanionFollowBehaviorMode mode)
        {
            behaviorMode = mode;
            if (activeProfile != null)
                activeProfile.followBehaviorMode = mode;

            if (mode == CompanionFollowBehaviorMode.Follow)
                return;

            worldAnchor = transform.position;
            worldAmbientInitialized = true;
            SetupPingPongPoints();
        }

        /// <summary>Future hook: assign a world idle job (bench, crafting, repairs).</summary>
        public void SetWorldIdleJob(PioneerWorldIdleJob job)
        {
            assignedWorldJob = job;
            if (activeProfile != null)
                activeProfile.worldIdleJob = job;
        }

        /// <summary>
        /// Enters break-away combat mode: the companion anchors to the enemy instead of the
        /// player's formation slot, holding a comfort ring around it (TLOU/DA:I buddy-AI pattern).
        /// The combat tether leash back to the player is the only thing that overrides it.
        /// </summary>
        public void SetCombatEngagement(Transform target, float preferredDistance, float maxStrikeRange, bool isRangedEngagement)
        {
            combatEngageTarget = target;
            combatEngagePreferredDistance = Mathf.Max(1f, preferredDistance);
            combatEngageMaxStrikeRange = Mathf.Max(combatEngagePreferredDistance, maxStrikeRange);
            combatEngageIsRanged = isRangedEngagement;
        }

        public void ClearCombatEngagement()
        {
            combatEngageTarget = null;
            combatEngageIsRanged = false;
        }

        public void RequestCombatChase(Vector3 targetWorld, float preferredDistance, float duration)
        {
            maintainDistanceTarget = targetWorld;
            maintainDistanceUntil = Time.time + duration;
            maintainDistancePreferred = preferredDistance;
            maintainDistanceChase = true;
        }

        public void RequestCombatMaintainDistance(Vector3 worldPosition, float preferredDistance, float duration)
        {
            Vector3 away = transform.position - worldPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = transform.forward;

            away.Normalize();
            maintainDistanceTarget = worldPosition + away * preferredDistance;
            maintainDistanceUntil = Time.time + duration;
            maintainDistancePreferred = preferredDistance;
            maintainDistanceChase = false;
        }

        public void BeginIdlePhase()
        {
            idleWanderPhaseActive = false;
            idlePhaseEndsAt = Time.time + Random.Range(idleRestDurationMin, idleRestDurationMax);
            smoothedWanderOffset = Vector3.zero;
            wanderVelocity = Vector3.zero;
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            isWandering = false;
            catchUpActive = false;
        }

        private void OnEnable()
        {
            EnsureBodyCollider();
            if (!ActiveCompanions.Contains(this))
                ActiveCompanions.Add(this);
        }

        private void OnDisable()
        {
            if (bodyCollider != null)
                FollowerCollisionUtility.Unregister(bodyCollider);

            ActiveCompanions.Remove(this);
        }

        public void RequestCombatStepBack(Vector3 awayFromWorld, float distance, float duration)
        {
            Vector3 away = transform.position - awayFromWorld;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;

            away.Normalize();
            stepBackTarget = transform.position + away * distance;
            stepBackUntil = Time.time + duration;
        }

        private void LateUpdate()
        {
            SnapToTerrain();
        }

        private void Update()
        {
            currentMoveDirection = Vector3.zero;
            isNearFormation = false;
            isWandering = false;

            if (behaviorMode != CompanionFollowBehaviorMode.Follow)
            {
                UpdateWorldAmbientBehavior();
                return;
            }

            if (owner == null || taskQueue == null)
            {
                currentSpeed = 0f;
                return;
            }

            if (Time.time < stepBackUntil)
            {
                MoveTowards(stepBackTarget, walkSpeed * 1.35f, allowIdleRest: false);
                return;
            }

            if (trailRecoveryUntil > 0f)
            {
                if (Time.time < trailRecoveryUntil)
                {
                    float distanceToRecovery = HorizontalDistance(transform.position, trailRecoveryTarget);
                    if (distanceToRecovery <= trailRecoveryArrivalDistance)
                    {
                        EndTrailRecovery(resumeFollow: true);
                    }
                    else
                    {
                        MoveTowards(trailRecoveryTarget, walkSpeed * 1.2f, allowIdleRest: false);
                        return;
                    }
                }
                else if (trailAttemptsThisEpisode < maxTrailAttemptsBeforeSidestep && TryBeginTrailRecovery())
                {
                    MoveTowards(trailRecoveryTarget, walkSpeed * 1.2f, allowIdleRest: false);
                    return;
                }
                else
                {
                    EndTrailRecovery(resumeFollow: false);
                }
            }

            if (Time.time < maintainDistanceUntil)
            {
                if (maintainDistanceChase)
                {
                    Vector3 toTarget = maintainDistanceTarget - transform.position;
                    toTarget.y = 0f;
                    float distance = toTarget.magnitude;
                    if (distance <= maintainDistancePreferred)
                    {
                        maintainDistanceUntil = 0f;
                        currentSpeed = 0f;
                        return;
                    }

                    Vector3 chasePoint = maintainDistanceTarget - toTarget.normalized * maintainDistancePreferred;
                    chasePoint.y = SampleTerrainHeight(chasePoint);
                    MoveTowards(chasePoint, runSpeed, allowIdleRest: false);
                }
                else
                {
                    MoveTowards(maintainDistanceTarget, walkSpeed, allowIdleRest: false);
                }

                return;
            }

            SyncHoldFromTaskQueue();

            if (taskQueue.ShouldHold)
            {
                UpdateHoldBehavior();
                return;
            }

            if (TryCombatTetherReturn())
                return;

            if (TryUpdateCombatEngagement())
                return;

            if (!taskQueue.ShouldFollow)
            {
                currentSpeed = 0f;
                return;
            }

            // Loose leash: pioneers only move when the player pulls the leash.
            // Player spin / small circles do not rotate a formation frame around them.
            UpdateLooseFollow();
        }
    }
}
