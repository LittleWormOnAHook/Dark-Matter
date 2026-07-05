using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.Companions;
using Project.Survival;

namespace Project.AI
{
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyCombat))]
    public class EnemyAiController : MonoBehaviour
    {
        private enum AiState
        {
            Idle,
            Wander,
            Patrol,
            Investigate,
            ReturnHome,
            Chase,
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
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private float groundProbeHeight = 40f;
        [SerializeField] private float groundProbeDistance = 80f;

        [Header("NavMesh")]
        [Tooltip("Use NavMeshAgent for Wander and Chase only. Other states keep transform movement.")]
        [SerializeField] private bool useNavMeshForChaseAndWander = true;
        [SerializeField] private float navMeshSampleRadius = 2.5f;
        [SerializeField] private float navDestinationRepathThreshold = 0.5f;

        [Header("Wander")]
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float wanderPauseMin = 2f;
        [SerializeField] private float wanderPauseMax = 5f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitDuration = 2f;
        [SerializeField] private float idleDuration = 3f;

        [Header("Behavior")]
        [SerializeField] private float loseTargetDelay = 4f;
        [SerializeField] private float investigateArriveDistance = 1.2f;
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 4f;

        [Header("Chase Stamina")]
        [SerializeField] private float chaseStaminaPauseMin = 0.35f;
        [SerializeField] private float chaseStaminaPauseMax = 1.15f;
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

        private EnemySenses senses;
        private EnemyHealth health;
        private EnemyCombat combat;

        private AiState state = AiState.Idle;
        private Vector3 homePosition;
        private Vector3 moveTarget;
        private Vector3 lastKnownPlayerPosition;
        private float stateTimer;
        private float lostTargetTimer;
        private int patrolIndex;
        private int patrolDirection = 1;
        private bool hasPatrolRoute;
        private float currentLocomotionSpeed;
        private Vector3 currentLocalMoveDirection;
        private float chaseStaminaPauseUntil;
        private float nextChaseStaminaRollTime;
        private float nextPioneerRetargetRollTime;
        private Transform playerTarget;
        private Transform aggroTarget;
        private float aggroUntil;
        private SurvivalStats playerSurvivalStats;
        private NavMeshAgent navAgent;
        private bool navMeshReady;
        private Vector3 lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        private const float AggroDuration = 12f;

        public float CurrentLocomotionSpeed => currentLocomotionSpeed;
        public Vector3 CurrentLocalMoveDirection => currentLocalMoveDirection;

        public bool IsEngagedWithTarget =>
            state == AiState.Attack || state == AiState.Chase;

        private bool IsStationary => movementMode == EnemyMovementMode.Stationary;

        private bool AllowsTranslation =>
            !IsStationary || state == AiState.Chase;

        private void Awake()
        {
            senses = GetComponent<EnemySenses>();
            health = GetComponent<EnemyHealth>();
            combat = GetComponent<EnemyCombat>();
            hasPatrolRoute = patrolPoints != null && patrolPoints.Length > 0;
            EnsureNavMeshAgent();
        }

        private void OnEnable()
        {
            homePosition = transform.position;
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;

            if (health != null)
            {
                health.Died += HandleDeath;
                health.DamagedBy += HandleDamagedBy;
            }

            EnsureNavMeshAgent();
            if (navMeshReady && TrySampleNavMesh(transform.position, out Vector3 navHome))
                homePosition = navHome;

            EnterCalmState();
            TrySubscribePlayerEvents();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
                health.DamagedBy -= HandleDamagedBy;
            }

            UnsubscribePlayerEvents();
        }

        private void TrySubscribePlayerEvents()
        {
            if (playerSurvivalStats != null)
                return;

            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
                return;

            playerSurvivalStats = playerObject.GetComponent<SurvivalStats>();
            if (playerSurvivalStats == null)
                return;

            playerSurvivalStats.PlayerDied += HandlePlayerDied;
            playerSurvivalStats.PlayerRevived += HandlePlayerRevived;
        }

        private void UnsubscribePlayerEvents()
        {
            if (playerSurvivalStats == null)
                return;

            playerSurvivalStats.PlayerDied -= HandlePlayerDied;
            playerSurvivalStats.PlayerRevived -= HandlePlayerRevived;
            playerSurvivalStats = null;
        }

        private void HandlePlayerDied()
        {
            ClearPlayerThreat();
        }

        private void HandlePlayerRevived()
        {
            ClearPlayerThreat();
        }

        /// <summary>
        /// Drop the player as a combat target on death/respawn so enemies do not keep pounding
        /// a corpse or instantly re-aggro a freshly respawned player.
        /// </summary>
        private void ClearPlayerThreat()
        {
            if (IsCombatTargetPlayer(combat.CurrentTarget))
                combat.SetTarget(null);

            if (aggroTarget != null && !IsPioneer(aggroTarget))
            {
                aggroTarget = null;
                aggroUntil = 0f;
            }

            playerTarget = null;

            if ((state == AiState.Chase || state == AiState.Attack) && !HasActiveAggroTarget() && !IsTargetingLivingPioneer())
                GiveUpChaseAndReturnHome();
        }

        /// <summary>
        /// Aggro on whoever hurt us — companions are otherwise invisible to EnemySenses
        /// (it only tracks the Player), so an enemy attacked by pioneers would never fight back.
        /// </summary>
        private void HandleDamagedBy(GameObject source)
        {
            if (source == null || health == null || health.IsDead)
                return;

            Transform attacker = ResolveThreatRoot(source);
            if (attacker == null)
                return;

            aggroTarget = attacker;
            aggroUntil = Time.time + AggroDuration;
            lastKnownPlayerPosition = attacker.position;

            Debug.Log($"[EnemyAggro] {name} aggro -> {attacker.name} (source={source.name})");

            // Always snap combat onto whoever just hurt us — do not keep swinging at the
            // player after a pioneer lands a hit.
            combat.SetTarget(attacker);

            if (state != AiState.Attack && state != AiState.Chase)
                EnterState(combat.IsTargetInRange() ? AiState.Attack : AiState.Chase);
        }

        private static Transform ResolveThreatRoot(GameObject source)
        {
            if (source == null)
                return null;

            // Companions must win over the player — shared Invector prefab bones can otherwise
            // mis-attribute pioneer hits to the nearby player root.
            CompanionHealth companionHealth = source.GetComponentInParent<CompanionHealth>();
            if (companionHealth != null)
                return companionHealth.transform;

            PioneerCompanionAgent companion = source.GetComponentInParent<PioneerCompanionAgent>();
            if (companion != null)
                return companion.transform;

            SurvivalStats player = source.GetComponentInParent<SurvivalStats>();
            return player != null ? player.transform : null;
        }

        /// <summary>
        /// Central gate for whether this enemy is allowed to acquire or keep the player as
        /// a melee target. Blocks bystanders while pioneers are actively fighting nearby.
        /// </summary>
        public bool AllowsCombatTarget(Transform candidate)
        {
            if (candidate == null)
                return false;

            if (!IsCombatTargetPlayer(candidate))
                return true;

            SurvivalStats stats = candidate.GetComponent<SurvivalStats>();
            if (stats != null && (stats.IsDead || stats.HasEnemyCombatImmunity))
                return false;

            // Pioneer holds aggro — player is never a legal target until that window expires.
            if (HasActiveAggroTarget() && IsPioneer(aggroTarget))
                return false;

            // Player who personally provoked us may be struck even with pioneers nearby.
            if (HasActivePlayerAggro() && aggroTarget == candidate)
                return true;

            // Pioneers are the front line. A bystander player in the melee scrum is not
            // fair game — that was the "death by unknown" pattern (enemy aggro on pioneer,
            // but still pounding the player standing next to them).
            if (HasNearbyLivingPioneer(pioneerRetargetRadius * 1.75f))
                return false;

            // Hostile on sight: a visible lone player within threat/vision range is fair game.
            if (HorizontalDistance(transform.position, candidate.position) <= playerThreatRange)
                return true;

            return senses.CanSeeThreat(candidate);
        }

        private bool HasActivePlayerAggro()
        {
            return HasActiveAggroTarget() && !IsPioneer(aggroTarget);
        }

        /// <summary>
        /// Picks the closest visible threat this enemy is allowed to engage — pioneers by
        /// sight (they used to be invisible to senses), the player only when legal.
        /// </summary>
        private bool TryPickVisibleThreat(Transform visiblePlayer, out Transform threat)
        {
            Transform visiblePioneer = senses.GetVisiblePioneerTarget();
            bool playerAllowed = visiblePlayer != null && AllowsCombatTarget(visiblePlayer);

            if (visiblePioneer != null && playerAllowed)
            {
                threat = HorizontalDistance(transform.position, visiblePioneer.position) <=
                         HorizontalDistance(transform.position, visiblePlayer.position)
                    ? visiblePioneer
                    : visiblePlayer;
                return true;
            }

            threat = visiblePioneer != null ? visiblePioneer : (playerAllowed ? visiblePlayer : null);
            return threat != null;
        }

        private bool HasNearbyLivingPioneer(float maxRange)
        {
            return PickClosestNearbyPioneerWithin(maxRange) != null;
        }

        private void TryCorrectIllegalPlayerTarget()
        {
            Transform current = combat.CurrentTarget;
            if (!IsCombatTargetPlayer(current) || AllowsCombatTarget(current))
                return;

            Transform pioneer = PickClosestNearbyPioneerWithin(pioneerRetargetRadius * 1.75f);
            combat.SetTarget(pioneer);
        }

        private void LateUpdate()
        {
            if (IsStationary)
                return;

            // NavMeshAgent owns vertical placement while active on the mesh.
            if (navMeshReady && navAgent != null && navAgent.enabled)
                return;

            SnapToGround();
        }

        private void Update()
        {
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;

            if (health != null && health.IsDead)
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

                if (combat.IsTargetInRange())
                {
                    if (state != AiState.Attack)
                        EnterState(AiState.Attack);
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
                case AiState.Attack:
                    UpdateAttack();
                    break;
                case AiState.Search:
                    UpdateSearch();
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            if (movementMode == EnemyMovementMode.Stationary)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            EnterCalmState();
        }

        private void UpdateWander()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            float arriveDistance = stopDistance + 0.5f;
            bool usingNav = TryMoveWithNavMesh(moveTarget, walkSpeed, arriveDistance);
            if (!usingNav)
                MoveTowards(moveTarget, walkSpeed);

            if (!HasArrived(moveTarget, arriveDistance, usingNav))
                return;

            StopNavMeshMovement();

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            moveTarget = PickRandomGroundPoint(homePosition, wanderRadius);
            stateTimer = Random.Range(wanderPauseMin, wanderPauseMax);
        }

        private void UpdatePatrol()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            if (!hasPatrolRoute)
            {
                EnterCalmState();
                return;
            }

            Transform point = patrolPoints[patrolIndex];
            if (point == null)
            {
                AdvancePatrolIndex();
                return;
            }

            moveTarget = point.position;
            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, moveTarget) <= stopDistance + 0.5f)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    AdvancePatrolIndex();
                    stateTimer = patrolWaitDuration;
                }
            }
        }

        private void UpdateInvestigate()
        {
            if (senses.TryGetHeardNoise(out Vector3 noisePosition))
                moveTarget = noisePosition;

            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, moveTarget) <= investigateArriveDistance)
                EnterState(AiState.Search);
        }

        private void UpdateReturnHome()
        {
            moveTarget = homePosition;
            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, homePosition) <= stopDistance + 0.5f)
                EnterCalmState();
        }

        private void UpdateChase()
        {
            Transform chaseTarget = combat.CurrentTarget;
            Vector3 chasePosition = ResolveChasePosition(chaseTarget);

            if (!CanContinueChase(chasePosition))
            {
                GiveUpChaseAndReturnHome();
                return;
            }

            if (Time.time < chaseStaminaPauseUntil)
            {
                StopNavMeshMovement();
                currentLocomotionSpeed = 0f;
                currentLocalMoveDirection = Vector3.zero;
                FaceTowards(chasePosition);
                return;
            }

            if (Time.time >= nextChaseStaminaRollTime)
                TryStartChaseStaminaPause();

            // Arrived at the last-known player spot with nobody there — give up quickly
            // instead of camping the stale position for the rest of the aggro window.
            // Also covers a null combat target (target was disallowed mid-chase).
            float giveUpDistance = stopDistance + 0.75f;
            bool atLastKnown = HasArrived(chasePosition, giveUpDistance, navMeshReady);
            if ((chaseTarget == null || !IsPioneer(chaseTarget)) && !senses.CanSeePlayer() && atLastKnown)
            {
                lostTargetTimer += Time.deltaTime;
                if (lostTargetTimer >= loseTargetDelay)
                {
                    // Clear aggro too — otherwise the next frame's dispatch sees the aggro
                    // window still open and immediately re-enters Chase, undoing the give-up.
                    if (aggroTarget == chaseTarget)
                    {
                        aggroTarget = null;
                        aggroUntil = 0f;
                    }

                    GiveUpChaseAndReturnHome();
                    return;
                }
            }
            else
            {
                lostTargetTimer = 0f;
            }

            moveTarget = chasePosition;

            // Stop at striking distance and let the Attack state land the hit —
            // running to point-blank body-shoves the target's rigidbody around.
            float standoff = combat.HasLivingTarget()
                ? Mathf.Max(stopDistance, combat.AttackRange * 0.85f)
                : stopDistance;

            if (!TryMoveWithNavMesh(moveTarget, runSpeed, standoff))
                MoveTowards(moveTarget, runSpeed, standoff);
        }

        /// <summary>
        /// Companions are treated as engaged brawlers (live tracking expected). The player,
        /// once provoked, is only "known" at the position they were last actually sensed —
        /// otherwise the enemy would chase a perfectly up-to-date position through walls and
        /// across the map for the whole aggro window, making retreat impossible.
        /// </summary>
        private Vector3 ResolveChasePosition(Transform chaseTarget)
        {
            if (chaseTarget == null)
                return lastKnownPlayerPosition;

            if (IsPioneer(chaseTarget))
                return chaseTarget.position;

            if (senses.CanSeePlayer())
            {
                lastKnownPlayerPosition = chaseTarget.position;
                return chaseTarget.position;
            }

            return lastKnownPlayerPosition;
        }

        private void TryStartChaseStaminaPause()
        {
            ScheduleNextChaseStaminaRoll();
            if (Random.value > chaseStaminaPauseChance)
                return;

            chaseStaminaPauseUntil = Time.time + Random.Range(chaseStaminaPauseMin, chaseStaminaPauseMax);
        }

        private void ScheduleNextChaseStaminaRoll()
        {
            nextChaseStaminaRollTime = Time.time + Random.Range(chaseStaminaRollIntervalMin, chaseStaminaRollIntervalMax);
        }

        private void UpdateAttack()
        {
            // Active aggro is sticky: never peel off a pioneer mid-swing to slap the player.
            if (HasActiveAggroTarget())
            {
                combat.SetTarget(aggroTarget);
            }
            else
            {
                Transform closestThreat = PickClosestLivingThreat(combat.AttackRange);
                if (closestThreat != null && AllowsCombatTarget(closestThreat))
                    combat.SetTarget(closestThreat);
            }

            if (!combat.HasLivingTarget())
            {
                if (HasActiveAggroTarget())
                    combat.SetTarget(aggroTarget);
                else if (playerTarget != null && AllowsCombatTarget(playerTarget))
                    combat.SetTarget(playerTarget);
                return;
            }

            Transform target = combat.CurrentTarget;
            if (target == null)
                return;

            if (!AllowsCombatTarget(target))
            {
                Transform pioneer = PickClosestNearbyPioneerWithin(pioneerRetargetRadius * 1.75f);
                combat.SetTarget(pioneer != null ? pioneer : null);
                return;
            }

            if (!combat.IsTargetInRange() && chasePlayer && CanChaseTarget(target.position))
            {
                EnterState(AiState.Chase);
                return;
            }

            FaceTowards(target.position);
            combat.TryAttack();
        }

        private void UpdateAggroCombat()
        {
            if (!HasActiveAggroTarget())
                return;

            // Never chase someone we're not allowed to strike — that produces the
            // "enemy shoves the player around without attacking" failure mode.
            if (!AllowsCombatTarget(aggroTarget))
            {
                aggroTarget = null;
                aggroUntil = 0f;
                if (state == AiState.Chase || state == AiState.Attack)
                    GiveUpChaseAndReturnHome();
                return;
            }

            if (!IsTargetingLivingPioneer() || aggroTarget != combat.CurrentTarget)
                combat.SetTarget(aggroTarget);

            // Pioneers are actively brawling, so contact is continuous — safe to keep alive.
            // The player's lostTargetTimer is owned by UpdateChase's stale-position give-up
            // check below; resetting it here would erase that "can't find them" countdown.
            if (IsPioneer(aggroTarget))
                lostTargetTimer = 0f;

            if (combat.IsTargetInRange())
            {
                if (state != AiState.Attack)
                    EnterState(AiState.Attack);
            }
            else if (state != AiState.Chase && CanChaseTarget(aggroTarget.position))
            {
                EnterState(AiState.Chase);
            }
        }

        /// <summary>
        /// Visible players are not auto-combat targets. Only engage if they damaged us,
        /// are in melee threat range, or are not immune after respawn.
        /// </summary>
        private bool ShouldEngagePlayer(Transform player)
        {
            if (player == null)
                return false;

            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            if (stats != null && (stats.IsDead || stats.HasEnemyCombatImmunity))
                return false;

            if (HasActiveAggroTarget() && !IsPioneer(aggroTarget))
                return true;

            if (HasActiveAggroTarget() && IsPioneer(aggroTarget))
                return false;

            return HorizontalDistance(transform.position, player.position) <= playerThreatRange;
        }

        private static bool IsCombatTargetPlayer(Transform candidate)
        {
            return candidate != null && candidate.GetComponent<SurvivalStats>() != null;
        }

        private bool HasActiveAggroTarget()
        {
            if (aggroTarget == null || Time.time >= aggroUntil)
                return false;

            CompanionHealth companionHealth = aggroTarget.GetComponent<CompanionHealth>();
            if (companionHealth != null)
                return !companionHealth.IsDead;

            SurvivalStats stats = aggroTarget.GetComponent<SurvivalStats>();
            return stats == null || !stats.IsDead;
        }

        private static bool IsPioneer(Transform candidate)
        {
            return candidate != null && candidate.GetComponent<PioneerCompanionAgent>() != null;
        }

        private bool IsTargetingLivingPioneer()
        {
            Transform current = combat.CurrentTarget;
            if (current == null)
                return false;

            if (current.GetComponent<CompanionHealth>() is { IsDead: false })
                return true;

            return current.GetComponent<PioneerCompanionAgent>() != null && combat.HasLivingTarget();
        }

        private void TryRetargetToNearbyPioneer()
        {
            if (playerTarget == null)
                return;

            Transform closestPioneer = PickClosestNearbyPioneer();
            if (closestPioneer != null && combat.IsInAttackRange(closestPioneer))
            {
                Transform current = combat.CurrentTarget;
                float pioneerDistance = HorizontalDistance(transform.position, closestPioneer.position);
                float currentDistance = current != null
                    ? HorizontalDistance(transform.position, current.position)
                    : float.MaxValue;

                if (pioneerDistance + 0.15f < currentDistance)
                {
                    combat.SetTarget(closestPioneer);
                    return;
                }
            }

            if (Time.time < nextPioneerRetargetRollTime)
                return;

            nextPioneerRetargetRollTime = Time.time + Random.Range(pioneerRetargetRollIntervalMin, pioneerRetargetRollIntervalMax);

            float chance = Random.Range(pioneerRetargetChanceMin, pioneerRetargetChanceMax);
            if (Random.value > chance)
                return;

            Transform pioneer = PickRandomNearbyPioneer();
            if (pioneer != null)
                combat.SetTarget(pioneer);
        }

        private Transform PickClosestNearbyPioneer()
        {
            return PickClosestNearbyPioneerWithin(pioneerRetargetRadius);
        }

        private Transform PickClosestNearbyPioneerWithin(float maxRange)
        {
            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge == null)
                return null;

            IReadOnlyList<PioneerCompanionAgent> companions = bridge.ActiveCompanions;
            if (companions == null || companions.Count == 0)
                return null;

            Transform closest = null;
            float closestDistance = maxRange;

            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent agent = companions[i];
                if (agent == null)
                    continue;

                CompanionHealth health = agent.GetComponent<CompanionHealth>();
                if (health != null && health.IsDead)
                    continue;

                float distance = HorizontalDistance(transform.position, agent.transform.position);
                if (distance > maxRange || distance >= closestDistance)
                    continue;

                closest = agent.transform;
                closestDistance = distance;
            }

            return closest;
        }

        private Transform PickClosestLivingThreat(float maxRange)
        {
            Transform closest = null;
            float closestDistance = maxRange;

            // The player only competes for attack priority when they actually provoked us;
            // a bystanding player watching their pioneers fight is not the primary threat.
            bool playerProvoked = HasActivePlayerAggro();
            if (playerTarget != null && playerProvoked && AllowsCombatTarget(playerTarget))
            {
                SurvivalStats playerStats = playerTarget.GetComponent<SurvivalStats>();
                if (playerStats == null || !playerStats.IsDead)
                {
                    float distance = HorizontalDistance(transform.position, playerTarget.position);
                    if (distance <= closestDistance)
                    {
                        closest = playerTarget;
                        closestDistance = distance;
                    }
                }
            }

            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            IReadOnlyList<PioneerCompanionAgent> companions = bridge != null ? bridge.ActiveCompanions : null;
            if (companions != null)
            {
                for (int i = 0; i < companions.Count; i++)
                {
                    PioneerCompanionAgent agent = companions[i];
                    if (agent == null)
                        continue;

                    CompanionHealth health = agent.GetComponent<CompanionHealth>();
                    if (health != null && health.IsDead)
                        continue;

                    float distance = HorizontalDistance(transform.position, agent.transform.position);
                    if (distance > closestDistance)
                        continue;

                    closest = agent.transform;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private Transform PickRandomNearbyPioneer()
        {
            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge == null)
                return null;

            IReadOnlyList<PioneerCompanionAgent> companions = bridge.ActiveCompanions;
            if (companions == null || companions.Count == 0)
                return null;

            int startIndex = Random.Range(0, companions.Count);
            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent agent = companions[(startIndex + i) % companions.Count];
                if (agent == null)
                    continue;

                CompanionHealth health = agent.GetComponent<CompanionHealth>();
                if (health != null && health.IsDead)
                    continue;

                float distance = HorizontalDistance(transform.position, agent.transform.position);
                if (distance > pioneerRetargetRadius)
                    continue;

                return agent.transform;
            }

            return null;
        }

        private void GiveUpChaseAndReturnHome()
        {
            lostTargetTimer = 0f;
            combat.SetTarget(null);

            if (IsStationary || !returnToHomeAfterSearch)
            {
                EnterCalmState();
                return;
            }

            EnterState(AiState.ReturnHome);
        }

        private bool CanChaseTarget(Vector3 targetPosition)
        {
            if (chaseRadius <= 0f)
                return true;

            return HorizontalDistance(homePosition, targetPosition) <= chaseRadius;
        }

        private bool CanContinueChase(Vector3 targetPosition)
        {
            if (chaseRadius <= 0f)
                return true;

            return HorizontalDistance(homePosition, transform.position) <= chaseRadius &&
                   HorizontalDistance(homePosition, targetPosition) <= chaseRadius;
        }

        private void UpdateSearch()
        {
            stateTimer -= Time.deltaTime;
            MoveTowards(moveTarget, walkSpeed * 0.85f);

            if (HorizontalDistance(transform.position, moveTarget) <= stopDistance + 0.4f)
                moveTarget = lastKnownPlayerPosition + Random.insideUnitSphere * searchRadius;

            if (stateTimer <= 0f)
            {
                if (returnToHomeAfterSearch)
                    EnterState(AiState.ReturnHome);
                else
                    EnterCalmState();
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
                    if (hasPatrolRoute)
                        moveTarget = patrolPoints[patrolIndex].position;
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
                    chaseStaminaPauseUntil = 0f;
                    ScheduleNextChaseStaminaRoll();
                    lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
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

        private bool ShouldInvestigateNoise()
        {
            return !IsStationary && investigateNoise && senses.HasRecentNoise && senses.NoiseAge < 1f;
        }

        private static bool IsRelocationState(AiState aiState)
        {
            return aiState == AiState.Wander ||
                   aiState == AiState.Patrol ||
                   aiState == AiState.Investigate ||
                   aiState == AiState.ReturnHome ||
                   aiState == AiState.Chase ||
                   aiState == AiState.Search;
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (patrolMode == EnemyPatrolMode.PingPong && patrolPoints.Length > 1)
            {
                patrolIndex += patrolDirection;
                if (patrolIndex >= patrolPoints.Length)
                {
                    patrolIndex = patrolPoints.Length - 2;
                    patrolDirection = -1;
                }
                else if (patrolIndex < 0)
                {
                    patrolIndex = 1;
                    patrolDirection = 1;
                }
            }
            else
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
        }

        private Vector3 PickRandomGroundPoint(Vector3 origin, float radius)
        {
            if (navMeshReady)
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Vector2 offset = Random.insideUnitCircle * radius;
                    Vector3 candidate = origin + new Vector3(offset.x, 0f, offset.y);
                    if (TrySampleNavMesh(candidate, out Vector3 navPoint))
                        return navPoint;
                }

                if (TrySampleNavMesh(origin, out Vector3 originNav))
                    return originNav;
            }

            Vector2 fallbackOffset = Random.insideUnitCircle * radius;
            Vector3 target = origin + new Vector3(fallbackOffset.x, 0f, fallbackOffset.y);
            if (TrySampleGround(target, out float groundY))
                target.y = groundY;
            return target;
        }

        private void HandleDeath()
        {
            StopNavMeshMovement();
            if (navAgent != null)
                navAgent.enabled = false;

            enabled = false;
        }

        private void EnsureNavMeshAgent()
        {
            navMeshReady = false;
            if (!useNavMeshForChaseAndWander)
            {
                DisableNavMeshAgent();
                return;
            }

            // Place on the mesh *before* enabling the agent to avoid
            // "Failed to create agent because it is not close enough to the NavMesh".
            float spawnSampleRadius = Mathf.Max(navMeshSampleRadius, 10f);
            if (!TrySampleNavMesh(transform.position, out Vector3 navPos, spawnSampleRadius))
            {
                DisableNavMeshAgent();
                return;
            }

            transform.position = navPos;

            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
                navAgent = gameObject.AddComponent<NavMeshAgent>();

            navAgent.enabled = false;
            navAgent.speed = walkSpeed;
            navAgent.angularSpeed = Mathf.Max(120f, turnSpeed * 45f);
            navAgent.acceleration = 14f;
            navAgent.stoppingDistance = stopDistance;
            navAgent.autoBraking = true;
            navAgent.updateRotation = true;
            navAgent.updatePosition = true;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            navAgent.enabled = true;
            if (!navAgent.isOnNavMesh)
            {
                if (!navAgent.Warp(navPos) || !navAgent.isOnNavMesh)
                {
                    DisableNavMeshAgent();
                    return;
                }
            }

            SafeStopAgent();
            navMeshReady = true;
            lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }

        private void DisableNavMeshAgent()
        {
            navMeshReady = false;
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();

            if (navAgent == null)
                return;

            SafeStopAgent();
            navAgent.enabled = false;
        }

        private bool TrySampleNavMesh(Vector3 worldPosition, out Vector3 navPosition)
        {
            return TrySampleNavMesh(worldPosition, out navPosition, navMeshSampleRadius);
        }

        private static bool TrySampleNavMesh(Vector3 worldPosition, out Vector3 navPosition, float sampleRadius)
        {
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                navPosition = hit.position;
                return true;
            }

            navPosition = worldPosition;
            return false;
        }

        private bool IsAgentUsable()
        {
            return navMeshReady &&
                   navAgent != null &&
                   navAgent.enabled &&
                   navAgent.isActiveAndEnabled &&
                   navAgent.isOnNavMesh;
        }

        private bool TryMoveWithNavMesh(Vector3 target, float speed, float arriveDistance)
        {
            if (!navMeshReady || navAgent == null || !navAgent.enabled)
                return false;

            if (!navAgent.isOnNavMesh)
            {
                if (!TrySampleNavMesh(transform.position, out Vector3 recover, Mathf.Max(navMeshSampleRadius, 10f)) ||
                    !navAgent.Warp(recover) ||
                    !navAgent.isOnNavMesh)
                {
                    navMeshReady = false;
                    return false;
                }
            }

            if (IsStationary && state != AiState.Chase)
                return false;

            if (!TrySampleNavMesh(target, out Vector3 navTarget))
                return false;

            navAgent.speed = speed;
            navAgent.stoppingDistance = Mathf.Max(0.05f, arriveDistance);

            float repathThresholdSq = navDestinationRepathThreshold * navDestinationRepathThreshold;
            bool needsRepath = (navTarget - lastNavDestination).sqrMagnitude > repathThresholdSq ||
                               !navAgent.hasPath ||
                               navAgent.pathStatus == NavMeshPathStatus.PathInvalid;

            if (needsRepath)
            {
                if (!navAgent.SetDestination(navTarget))
                    return false;

                lastNavDestination = navTarget;
            }

            if (navAgent.isStopped)
                navAgent.isStopped = false;

            SyncLocomotionFromAgent();
            return true;
        }

        private void StopNavMeshMovement()
        {
            SafeStopAgent();
            lastNavDestination = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
        }

        private void SafeStopAgent()
        {
            if (navAgent == null || !navAgent.enabled)
                return;

            if (!navAgent.isOnNavMesh)
                return;

            navAgent.isStopped = true;
            if (navAgent.hasPath)
                navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }

        private void SyncLocomotionFromAgent()
        {
            if (!IsAgentUsable())
                return;

            Vector3 velocity = navAgent.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            currentLocomotionSpeed = speed;
            currentLocalMoveDirection = speed > 0.05f
                ? transform.InverseTransformDirection(velocity.normalized)
                : Vector3.zero;
        }

        private bool HasArrived(Vector3 destination, float arriveDistance, bool preferNavMesh)
        {
            if (preferNavMesh && IsAgentUsable() && !navAgent.pathPending)
            {
                float remaining = navAgent.remainingDistance;
                if (!float.IsInfinity(remaining) &&
                    remaining <= arriveDistance &&
                    (!navAgent.hasPath || navAgent.velocity.sqrMagnitude < 0.05f))
                    return true;
            }

            return HorizontalDistance(transform.position, destination) <= arriveDistance;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            MoveTowards(target, speed, stopDistance);
        }

        private void MoveTowards(Vector3 target, float speed, float arriveDistance)
        {
            if (!AllowsTranslation &&
                state != AiState.Chase &&
                state != AiState.Investigate &&
                state != AiState.ReturnHome &&
                state != AiState.Search)
            {
                return;
            }

            if (IsStationary)
                return;

            Vector3 flatTarget = target;
            if (TrySampleGround(flatTarget, out float groundY))
                flatTarget.y = groundY;
            else
                flatTarget.y = transform.position.y;

            Vector3 toTarget = flatTarget - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance > arriveDistance)
            {
                Vector3 step = toTarget.normalized * (speed * Time.deltaTime);
                float maxStep = distance - arriveDistance;
                if (step.magnitude > maxStep)
                    step = toTarget.normalized * maxStep;

                transform.position += step;
                currentLocomotionSpeed = speed;
                currentLocalMoveDirection = transform.InverseTransformDirection(step.normalized);
            }
            else
            {
                currentLocomotionSpeed = 0f;
                currentLocalMoveDirection = Vector3.zero;
            }

            if (toTarget.sqrMagnitude > 0.01f)
                FaceTowards(flatTarget);
        }

        private void FaceTowards(Vector3 worldPosition)
        {
            Vector3 toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.01f)
                return;

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        private void SnapToGround()
        {
            if (TrySampleGround(transform.position, out float groundY))
            {
                Vector3 pos = transform.position;
                pos.y = groundY;
                transform.position = pos;
            }
        }

        private bool TrySampleGround(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;

            Vector3 origin = new Vector3(worldPosition.x, worldPosition.y + groundProbeHeight, worldPosition.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundProbeDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
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

            if (patrolPoints == null)
                return;

            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Transform point = patrolPoints[i];
                if (point == null)
                    continue;

                Gizmos.DrawWireSphere(point.position, 0.35f);
                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next != null)
                    Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
