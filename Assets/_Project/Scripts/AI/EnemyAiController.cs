using System.Collections.Generic;
using MalbersAnimations.PathCreation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
using Project.Companions;
using Project.Survival;
using Project.World;

namespace Project.AI
{
    // Core: fields/config, the enum + threat-ledger struct, lifecycle (Awake/OnEnable/OnDisable),
    // the main Update()/LateUpdate() dispatch loop, and state-transition bookkeeping (EnterState/
    // EnterCalmState). Split across partials by responsibility — see EnemyAiController.Threat.cs
    // (aggro/threat-ledger/player-target-legality), .States.cs (the per-AiState Update* behaviors),
    // .CombatPositioning.cs (combat ring/standoff/enemy-separation math + pioneer retarget scans),
    // .Movement.cs (NavMeshAgent plumbing + raw transform locomotion). Purely a mechanical
    // reorganization (partial class split) — no behavior changed by the split.
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyCombat))]
    public partial class EnemyAiController : MonoBehaviour
    {
        private enum AiState
        {
            Idle,
            Wander,
            Patrol,
            Investigate,
            ReturnHome,
            Chase,
            Defensive,
            Attack,
            Search
        }

        [Header("Movement Mode")]
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Wander;
        [SerializeField] private EnemyPatrolMode patrolMode = EnemyPatrolMode.Loop;
        [SerializeField] private bool investigateNoise = true;
        [SerializeField] private bool chasePlayer = true;
        [SerializeField] private bool returnToHomeAfterSearch = true;
        [FormerlySerializedAs("homeLeashRadius")]
        [Tooltip("Max horizontal distance from spawn/home to pursue the player. 0 = unlimited.")]
        [SerializeField] private float chaseRadius = 0f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 4.8f;
        [Tooltip("Patrol-only walk speed. Defaults to walkSpeed when zero.")]
        [SerializeField] private float patrolWalkSpeed;
        [Tooltip("Explicit chase speed. 0 = runSpeed * chaseSpeedMultiplier.")]
        [SerializeField] private float chaseSpeed;
        [SerializeField] [Range(0.5f, 1.25f)] private float chaseSpeedMultiplier = 0.88f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float groundOffset = 0f;

        [Header("NavMesh")]
        [Tooltip("Use NavMeshAgent for Wander and Chase only. Other states keep transform movement.")]
        [SerializeField] private bool useNavMeshForChaseAndWander = false;
        [SerializeField] private float navMeshSampleRadius = 2.5f;
        [SerializeField] private float navDestinationRepathThreshold = 0.5f;

        [Header("Wander")]
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float wanderPauseMin = 2f;
        [SerializeField] private float wanderPauseMax = 5f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private PathCreator patrolPath;
        [SerializeField] private DMIPathFollowProvider patrolPathProvider;
        [SerializeField] private float patrolWaitDuration = 2f;
        [SerializeField] private float idleDuration = 3f;

        [Header("Aggro Triggers")]
        [SerializeField] private bool aggroOnDamaged = true;
        [SerializeField] private bool aggroOnHeardHit = true;
        [SerializeField] [Range(0f, 1f)] private float hearingAggroChance = 0.45f;
        [SerializeField] private float hearingCooldown = 0.75f;

        [Header("Behavior")]
        [SerializeField] private float loseTargetDelay = 2.5f;
        [SerializeField] private float maxChaseDuration = 6f;
        [SerializeField] private float investigateArriveDistance = 1.2f;
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 4f;

        [Header("Chase Stamina")]
        [SerializeField] private float chaseStaminaPauseMin = 0.5f;
        [SerializeField] private float chaseStaminaPauseMax = 0.9f;
        [SerializeField] private float chaseStaminaRollIntervalMin = 2.4f;
        [SerializeField] private float chaseStaminaRollIntervalMax = 5.2f;
        [SerializeField] [Range(0f, 1f)] private float chaseStaminaPauseChance = 0.38f;

        [Header("Pioneer Retarget")]
        [SerializeField] private float pioneerRetargetChanceMin = 0.10f;
        [SerializeField] private float pioneerRetargetChanceMax = 0.20f;
        [SerializeField] private float pioneerRetargetRadius = 4f;
        [SerializeField] private float pioneerRetargetRollIntervalMin = 0.75f;
        [SerializeField] private float pioneerRetargetRollIntervalMax = 1.35f;

        [Header("Player Threat")]
        [Tooltip("Unprovoked players closer than this are treated as a melee threat. Visible players beyond this are ignored.")]
        [SerializeField] private float playerThreatRange = 3f;

        [Header("Combat Spacing")]
        [Tooltip("Minimum horizontal gap kept between this enemy and its melee target — prevents body-shoving the player or pioneers.")]
        [SerializeField] private float minCombatSeparation = 0.95f;
        [SerializeField] [Range(0.45f, 0.95f)] private float attackStandoffFraction = 0.62f;
        [SerializeField] private float playerStandoffBonus = 0.15f;
        [SerializeField] private float pioneerChaseRangeMultiplier = 1.45f;

        [Header("Defensive Engage")]
        [SerializeField] private float defensivePauseMin = 0.35f;
        [SerializeField] private float defensivePauseMax = 0.75f;
        [SerializeField] private float defensiveBlockDuration = 1.2f;
        [SerializeField] [Range(0f, 1f)] private float defensiveAttackWeight = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float defensiveBlockWeight = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float defensiveRollWeight = 0.20f;

        [Header("Threat Ledger")]
        [SerializeField] [Range(0f, 1f)] private float threatSwitchLeadFraction = 0.15f;
        [SerializeField] private bool debugAggro;

        [Header("Crowd")]
        [Tooltip("Keep a small buffer between nearby enemies while chasing or brawling.")]
        [SerializeField] private float enemyAvoidanceRadius = 1.45f;
        [SerializeField] private float enemyAvoidanceStrength = 0.85f;
        [Tooltip("Spreads enemies around the combat ring so they do not stack on one slot.")]
        [SerializeField] private float combatRingSlotSpread = 42f;

        private struct ThreatEntry
        {
            public Transform Root;
            public float TotalDamage;
            public float LastHitTime;
        }

        private readonly Dictionary<Transform, ThreatEntry> threatLedger = new Dictionary<Transform, ThreatEntry>();

        private EnemySenses senses;
        private EnemyHealth health;
        private EnemyCombat combat;
        private EnemyInvectorCombatBridge combatBridge;

        private AiState state = AiState.Idle;
        private Vector3 homePosition;
        private Vector3 moveTarget;
        private Vector3 lastKnownPlayerPosition;
        private float stateTimer;
        private float lostTargetTimer;
        private int patrolIndex;
        private int patrolDirection = 1;
        private bool hasPatrolRoute;
        /// <summary>Runtime world anchors from path providers (takes precedence over Transform patrolPoints).</summary>
        private Vector3[] patrolWorldPoints;
        private float nextHearingAggroTime;
        private float currentLocomotionSpeed;
        private Vector3 currentLocalMoveDirection;
        private float chaseStaminaPauseUntil;
        private float nextChaseStaminaRollTime;
        private float nextPioneerRetargetRollTime;
        private Transform playerTarget;
        private Transform aggroTarget;
        private Transform firstBloodTarget;
        private float aggroUntil;
        private float chaseStartedTime;
        private float defensiveActionUntil;
        private bool defensiveActionPending;
        private SurvivalStats playerSurvivalStats;
        private NavMeshAgent navAgent;
        private bool navMeshReady;
        private Vector3 lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        private float chaseSpeedMultiplierJitter = 1f;
        private float combatRingSlotAngle;
        private int perfPhase;
        private Vector3 cachedSeparationOffset;
        private static readonly Collider[] AvoidanceHits = new Collider[12];
        private const float AggroDuration = 8f;
        private bool locomotionPaused;

        public float CurrentLocomotionSpeed => currentLocomotionSpeed;
        public Vector3 CurrentLocalMoveDirection => currentLocalMoveDirection;
        public float RunSpeed => runSpeed;
        public bool IsWalkOnlyLocomotion => state == AiState.Patrol;
        public bool IsDefensiveActionActive => defensiveActionPending || Time.time < defensiveActionUntil;

        public float ResolveChaseSpeed()
        {
            float speed;
            if (chaseSpeed > 0f)
                speed = chaseSpeed;
            else
                speed = runSpeed * Mathf.Max(0.5f, chaseSpeedMultiplier);

            return speed * chaseSpeedMultiplierJitter;
        }

        public bool IsEngagedWithTarget =>
            state == AiState.Attack || state == AiState.Chase || state == AiState.Defensive;

        /// <summary>
        /// True when the enemy is in Attack state with a ranged weapon and the target is within
        /// ranged engage range. Used by the combat bridge to drive the aim stance every frame.
        /// </summary>
        public bool IsInRangedEngagement
        {
            get
            {
                if (state != AiState.Attack) return false;
                if (combatBridge == null || !combatBridge.IsArmedRangedPreferred()) return false;
                Transform t = combat?.CurrentTarget;
                if (t == null) return false;
                return HorizontalDistance(transform.position, t.position) <= combatBridge.RangedEngageRange;
            }
        }

        public void SetLocomotionPaused(bool paused)
        {
            locomotionPaused = paused;
            if (paused)
            {
                ClearLocomotion();
                if (navAgent != null && navAgent.enabled)
                    navAgent.isStopped = true;
                return;
            }

            if (navAgent != null && navAgent.enabled)
                navAgent.isStopped = false;
        }

        /// <summary>
        /// Assigns a Transform patrol route after spawn (used by surface encounter zones).
        /// </summary>
        public void ConfigurePatrolRoute(Transform[] points, EnemyPatrolMode mode)
        {
            patrolWorldPoints = null;
            patrolPoints = points;
            patrolMode = mode;
            RefreshPatrolRouteFlag();
            if (!hasPatrolRoute)
                return;

            movementMode = EnemyMovementMode.Patrol;
            patrolIndex = 0;
            patrolDirection = 1;
        }

        /// <summary>
        /// Assigns world-space patrol anchors (used by <c>DMIPathFollowProvider</c>).
        /// </summary>
        public void ConfigurePatrolRoute(Vector3[] worldPoints, EnemyPatrolMode mode)
        {
            patrolPoints = null;
            patrolWorldPoints = worldPoints != null && worldPoints.Length > 0
                ? (Vector3[])worldPoints.Clone()
                : null;
            patrolMode = mode;
            RefreshPatrolRouteFlag();
            if (!hasPatrolRoute)
                return;

            movementMode = EnemyMovementMode.Patrol;
            patrolIndex = 0;
            patrolDirection = 1;
        }

        /// <summary>Assign Path Creator for Patrol and register with bezier anchors.</summary>
        public void SetPatrolPath(PathCreator path, DMIPathFollowProvider provider = null)
        {
            patrolPath = path;
            patrolPathProvider = provider;
            if (path != null || provider != null)
                movementMode = EnemyMovementMode.Patrol;
            TryBindAssignedPatrolPath();
        }

        public PathCreator PatrolPath => patrolPath;
        public DMIPathFollowProvider PatrolPathProvider => patrolPathProvider;

        private void TryBindAssignedPatrolPath()
        {
            if (movementMode != EnemyMovementMode.Patrol)
                return;

            DMIPathFollowProvider provider = patrolPathProvider;
            if (provider == null)
                provider = DMIPathFollowBinding.Resolve((Object)patrolPath ?? patrolPathProvider);

            if (provider == null)
                return;

            patrolPathProvider = provider;
            if (patrolPath == null)
                patrolPath = provider.PathCreator;

            provider.TryAssignEnemy(this);
        }

        private void RefreshPatrolRouteFlag()
        {
            hasPatrolRoute = (patrolWorldPoints != null && patrolWorldPoints.Length > 0) ||
                             (patrolPoints != null && patrolPoints.Length > 0);
        }

        private int PatrolPointCount
        {
            get
            {
                if (patrolWorldPoints != null && patrolWorldPoints.Length > 0)
                    return patrolWorldPoints.Length;
                return patrolPoints != null ? patrolPoints.Length : 0;
            }
        }

        private bool TryGetPatrolWorldPoint(int index, out Vector3 point)
        {
            if (patrolWorldPoints != null && patrolWorldPoints.Length > 0)
            {
                if (index < 0 || index >= patrolWorldPoints.Length)
                {
                    point = default;
                    return false;
                }

                point = patrolWorldPoints[index];
                return true;
            }

            if (patrolPoints == null || index < 0 || index >= patrolPoints.Length)
            {
                point = default;
                return false;
            }

            Transform anchor = patrolPoints[index];
            if (anchor == null)
            {
                point = default;
                return false;
            }

            point = anchor.position;
            return true;
        }

        private bool IsStationary => movementMode == EnemyMovementMode.Stationary;

        private bool AllowsTranslation =>
            !IsStationary || state == AiState.Chase;

        private void Awake()
        {
            senses = GetComponent<EnemySenses>();
            health = GetComponent<EnemyHealth>();
            combat = GetComponent<EnemyCombat>();
            combatBridge = GetComponent<EnemyInvectorCombatBridge>();
            RefreshPatrolRouteFlag();
            ConfigureNavMeshAgent();
            InitializeCrowdProfile();
            TryBindAssignedPatrolPath();
        }

        private void InitializeCrowdProfile()
        {
            int hash = Mathf.Abs(gameObject.GetEntityId().GetHashCode());
            chaseSpeedMultiplierJitter = 0.88f + (hash % 1000) / 1000f * 0.22f;
            combatRingSlotAngle = ((hash % 360) - 180f) / 180f * combatRingSlotSpread;
            perfPhase = hash % 3;
        }

        private void OnEnable()
        {
            homePosition = transform.position;
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;

            if (health != null)
            {
                health.Died += HandleDeath;
                health.DamagedWithSource += HandleDamagedWithSource;
            }

            ClearThreatLedger();

            ConfigureNavMeshAgent();
            TryBindAssignedPatrolPath();

            EnterCalmState();
            TrySubscribePlayerEvents();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
                health.DamagedWithSource -= HandleDamagedWithSource;
            }

            if (patrolPathProvider != null)
                patrolPathProvider.UnregisterEnemy(this);

            UnsubscribePlayerEvents();
        }

        private void LateUpdate()
        {
            if (IsStationary)
                return;

            // NavMeshAgent owns vertical placement while active on the mesh.
            if (navMeshReady && navAgent != null && navAgent.enabled)
                return;

            if (((Time.frameCount + perfPhase) & 1) != 0)
                return;

            SnapToGround();
        }

        private void Update()
        {
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;

            if (health != null && health.IsDead)
                return;

            if (locomotionPaused)
                return;

            TrySubscribePlayerEvents();

            Transform visiblePlayer = senses.GetVisiblePlayerTarget();
            if (visiblePlayer != null)
                lastKnownPlayerPosition = visiblePlayer.position;

            if (HasActiveAggroTarget())
            {
                playerTarget = null;
                UpdateAggroCombat();
            }
            else if (TryPickVisibleThreat(visiblePlayer, out Transform visibleThreat))
            {
                bool threatIsPlayer = IsCombatTargetPlayer(visibleThreat);
                playerTarget = threatIsPlayer ? visibleThreat : null;
                lostTargetTimer = 0f;

                if (!threatIsPlayer || !IsTargetingLivingPioneer())
                    combat.SetTarget(visibleThreat);

                if (combat.IsTargetInEffectiveRange())
                {
                    if (state != AiState.Defensive && state != AiState.Attack)
                    {
                        float dist = HorizontalDistance(transform.position, visibleThreat.position);
                        EnterState(ResolveAttackEntryState(visibleThreat, dist));
                    }
                }
                else if (chasePlayer && CanChaseTarget(visibleThreat.position))
                {
                    if (state != AiState.Chase)
                        EnterState(AiState.Chase);
                }
                else if (state == AiState.Chase || state == AiState.Attack)
                {
                    GiveUpChaseAndReturnHome();
                }
            }
            else
            {
                playerTarget = null;
                TryCorrectIllegalPlayerTarget();

                if (IsCombatTargetPlayer(combat.CurrentTarget))
                    combat.SetTarget(null);

                if (state == AiState.Chase || state == AiState.Attack)
                {
                    lostTargetTimer += Time.deltaTime;
                    if (lostTargetTimer >= loseTargetDelay)
                        GiveUpChaseAndReturnHome();
                }
            }

            if (state == AiState.Attack || state == AiState.Chase)
                TryCorrectIllegalPlayerTarget();

            switch (state)
            {
                case AiState.Idle:
                    UpdateIdle();
                    break;
                case AiState.Wander:
                    UpdateWander();
                    break;
                case AiState.Patrol:
                    UpdatePatrol();
                    break;
                case AiState.Investigate:
                    UpdateInvestigate();
                    break;
                case AiState.ReturnHome:
                    UpdateReturnHome();
                    break;
                case AiState.Chase:
                    UpdateChase();
                    break;
                case AiState.Defensive:
                    UpdateDefensive();
                    break;
                case AiState.Attack:
                    UpdateAttack();
                    break;
                case AiState.Search:
                    UpdateSearch();
                    break;
            }
        }

        private void EnterCalmState()
        {
            switch (movementMode)
            {
                case EnemyMovementMode.Stationary:
                case EnemyMovementMode.Idle:
                    EnterState(AiState.Idle);
                    break;
                case EnemyMovementMode.Wander:
                    EnterState(AiState.Wander);
                    break;
                case EnemyMovementMode.Patrol:
                    EnterState(hasPatrolRoute ? AiState.Patrol : AiState.Wander);
                    break;
            }
        }

        private void EnterState(AiState newState)
        {
            if (IsStationary && IsRelocationState(newState))
                return;

            bool wasNavState = state == AiState.Wander || state == AiState.Chase;
            bool willNavState = newState == AiState.Wander || newState == AiState.Chase;
            if (wasNavState && !willNavState)
                StopNavMeshMovement();

            state = newState;

            switch (newState)
            {
                case AiState.Idle:
                    stateTimer = idleDuration;
                    break;
                case AiState.Wander:
                    moveTarget = PickRandomGroundPoint(homePosition, wanderRadius);
                    stateTimer = Random.Range(wanderPauseMin, wanderPauseMax);
                    lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    break;
                case AiState.Patrol:
                    stateTimer = patrolWaitDuration;
                    if (hasPatrolRoute && TryGetPatrolWorldPoint(patrolIndex, out Vector3 patrolPoint))
                        moveTarget = patrolPoint;
                    break;
                case AiState.Investigate:
                    moveTarget = senses.TryGetHeardNoise(out Vector3 noisePosition)
                        ? noisePosition
                        : transform.position;
                    break;
                case AiState.ReturnHome:
                    moveTarget = homePosition;
                    break;
                case AiState.Chase:
                    moveTarget = lastKnownPlayerPosition;
                    chaseStartedTime = Time.time;
                    chaseStaminaPauseUntil = 0f;
                    ScheduleNextChaseStaminaRoll();
                    lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    break;
                case AiState.Defensive:
                    StopNavMeshMovement();
                    defensiveActionPending = false;
                    defensiveActionUntil = 0f;
                    stateTimer = Random.Range(defensivePauseMin, defensivePauseMax);
                    break;
                case AiState.Attack:
                    StopNavMeshMovement();
                    break;
                case AiState.Search:
                    stateTimer = searchDuration;
                    moveTarget = lastKnownPlayerPosition + Random.insideUnitSphere * searchRadius;
                    moveTarget.y = lastKnownPlayerPosition.y;
                    if (TrySampleGround(moveTarget, out float searchGroundY))
                        moveTarget.y = searchGroundY;
                    break;
            }
        }

        private static bool IsRelocationState(AiState aiState)
        {
            return aiState == AiState.Wander ||
                   aiState == AiState.Patrol ||
                   aiState == AiState.Investigate ||
                   aiState == AiState.ReturnHome ||
                   aiState == AiState.Chase ||
                   aiState == AiState.Defensive ||
                   aiState == AiState.Search;
        }

        private void HandleDeath()
        {
            ClearThreatLedger();
            ClearLocomotion();
            StopNavMeshMovement();
            if (navAgent != null)
                navAgent.enabled = false;

            enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 home = Application.isPlaying ? homePosition : transform.position;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            if (movementMode == EnemyMovementMode.Wander && wanderRadius > 0f)
                Gizmos.DrawWireSphere(home, wanderRadius);

            if (chaseRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
                Gizmos.DrawWireSphere(home, chaseRadius);
            }

            int count = PatrolPointCount;
            if (count == 0)
                return;

            Gizmos.color = Color.green;
            for (int i = 0; i < count; i++)
            {
                if (!TryGetPatrolWorldPoint(i, out Vector3 point))
                    continue;

                Gizmos.DrawWireSphere(point, 0.35f);
                if (TryGetPatrolWorldPoint((i + 1) % count, out Vector3 next))
                    Gizmos.DrawLine(point, next);
            }
        }
    }
}
