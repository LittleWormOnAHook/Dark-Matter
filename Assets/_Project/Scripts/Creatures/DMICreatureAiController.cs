using Project.AI;
using Project.World;
using UnityEngine;
using UnityEngine.AI;
using MalbersAnimations.PathCreation;

namespace Project.Creatures
{
    /// <summary>
    /// RiggedNative creature AI: Idle / Wander / Patrol / Chase / Melee / Spit.
    /// Driven by <see cref="DMICreatureBrainProfile"/> + definition threat ranges.
    /// Patterned on <see cref="EnemyAiController"/> but slimmed for lifeforms.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public class DMICreatureAiController : MonoBehaviour
    {
        public enum CreatureAiState
        {
            Idle,
            Wander,
            Patrol,
            Chase,
            Melee,
            Spit
        }

        [Header("References")]
        [SerializeField] private DMICreatureBridge bridge;
        [SerializeField] private DMISulfurSpitAttack spitAttack;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private DMICreatureAnimationDriver animationDriver;
        [SerializeField] private DMICreatureEmissionDriver emissionDriver;
        [SerializeField] private DMICreatureAudioDriver audioDriver;
        [SerializeField] private DMICreatureBrainProfile brainProfile;

        [Header("Movement")]
        [SerializeField] private DMICreatureMovementMode movementMode = DMICreatureMovementMode.Wander;
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float runSpeed = 4.5f;
        [Tooltip("Legacy turn factor (≈ deg/sec × 18). Lower = slower, more natural yaw.")]
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float stopDistance = 0.4f;
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float idleDurationMin = 1.5f;
        [SerializeField] private float idleDurationMax = 3.5f;
        [SerializeField] private float idleDurationVariation = 0f;
        [SerializeField] private float wanderDuration = 0f;
        [SerializeField] private float wanderDurationVariation = 0f;
        [SerializeField] private float navMeshSampleRadius = 2.5f;

        [Header("Patrol")]
        [Tooltip("Path Creator (or Path Creator Variant) when Movement Mode is Patrol. Uses bezier anchors; overrides generated circle route.")]
        [SerializeField] private PathCreator patrolPath;
        [Tooltip("Optional explicit provider. If empty, resolved from Patrol Path.")]
        [SerializeField] private DMIPathFollowProvider patrolPathProvider;
        [SerializeField] private DMICreaturePatrolMode patrolMode = DMICreaturePatrolMode.Loop;
        [SerializeField] private int patrolPointCount = 4;
        [SerializeField] private float patrolRadius = 6f;
        [SerializeField] private float patrolWaitDuration = 2f;
        [SerializeField] private Vector3[] patrolPoints;

        [Header("Combat")]
        [SerializeField] private bool allowChase = true;
        [SerializeField] private bool allowMelee = true;
        [SerializeField] private bool allowRangedSpit = true;
        [SerializeField] private float engageRange = 9f;
        [SerializeField] private float leashRange = 12.6f;
        [SerializeField] private float meleeRange = 2.75f;
        [SerializeField] private float loseTargetDelay = 2.5f;
        [SerializeField] private float meleeHitInterval = 1.1f;
        [SerializeField] private float meleeHitIntervalVariation = 0f;

        [Header("AI Senses")]
        [SerializeField] private bool senseHearingEnabled = true;
        [SerializeField] private float hearingRange = 14f;
        [SerializeField] [Range(0f, 1f)] private float hearingAggroChance = 0.55f;
        [SerializeField] private float hearingCooldown = 0.8f;
        [SerializeField] private bool aggroOnDamaged = true;
        [SerializeField] private bool aggroOnHeardHit = true;

        private CreatureAiState state = CreatureAiState.Idle;
        private Vector3 homePosition;
        private Vector3 moveTarget;
        private float stateTimer;
        private float lostTargetTimer;
        private float nextMeleeHitTime;
        private float currentSpeed;
        private bool isDead;
        private int patrolIndex;
        private int patrolDirection = 1;
        private float nextHearingAggroTime;

        public CreatureAiState State => state;
        public float CurrentSpeed => currentSpeed;
        public bool IsDead => isDead;

        private void Awake()
        {
            CacheReferences();
            if (bridge != null && bridge.Definition != null)
                ConfigureFromDefinition(bridge.Definition);
            else if (brainProfile != null)
                ConfigureFromBrainProfile(brainProfile);

            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.stoppingDistance = stopDistance;
                agent.updateRotation = true;
            }

            TryBindAssignedPatrolPath();
        }

        private void OnEnable()
        {
            CacheReferences();
            homePosition = transform.position;
            isDead = false;
            TryBindAssignedPatrolPath();
            BuildPatrolRouteIfNeeded();
            if (health != null)
            {
                health.Died += HandleDeath;
                // Melee and ranged both flow through EnemyHealth.TakeDamage → DamagedWithSource.
                health.DamagedWithSource += HandleDamagedWithSource;
            }

            EnemyNoiseEvents.OnNoise += HandleNoise;

            EnterState(CreatureAiState.Idle);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
                health.DamagedWithSource -= HandleDamagedWithSource;
            }

            if (patrolPathProvider != null)
                patrolPathProvider.UnregisterCreature(this);

            EnemyNoiseEvents.OnNoise -= HandleNoise;
        }

        private void Update()
        {
            if (isDead)
                return;

            RefreshThreat();
            switch (state)
            {
                case CreatureAiState.Idle:
                    UpdateIdle();
                    break;
                case CreatureAiState.Wander:
                    UpdateWander();
                    break;
                case CreatureAiState.Patrol:
                    UpdatePatrol();
                    break;
                case CreatureAiState.Chase:
                    UpdateChase();
                    break;
                case CreatureAiState.Melee:
                    UpdateMelee();
                    break;
                case CreatureAiState.Spit:
                    UpdateSpit();
                    break;
            }

            animationDriver?.Tick(this);
            audioDriver?.Tick(this);
        }

        public void ConfigureFromDefinition(DMICreatureDefinition definition)
        {
            if (definition == null)
                return;

            engageRange = definition.threatSenseRange;
            leashRange = definition.threatSenseRange * Mathf.Max(1f, definition.threatLeashMultiplier);
            loseTargetDelay = definition.loseTargetDelay;
            meleeRange = definition.meleeEngageRange;

            senseHearingEnabled = definition.senseHearingEnabled;
            hearingRange = Mathf.Max(0f, definition.hearingRange);
            hearingAggroChance = Mathf.Clamp01(definition.hearingAggroChance);
            hearingCooldown = Mathf.Max(0f, definition.hearingCooldown);
            aggroOnDamaged = definition.aggroOnDamaged;
            aggroOnHeardHit = definition.aggroOnHeardHit;

            if (definition.brainProfile != null)
                ConfigureFromBrainProfile(definition.brainProfile);

            // CM definition cooldowns override brain profile / baked component values.
            meleeHitInterval = Mathf.Max(0.05f, definition.meleeAttackCooldown);
            meleeHitIntervalVariation = Mathf.Clamp(definition.meleeIntervalVariation, 0f, 10f);
            idleDurationMin = Mathf.Max(0f, definition.idleDuration);
            idleDurationMax = idleDurationMin;
            idleDurationVariation = Mathf.Clamp(definition.idleDurationVariation, 0f, 10f);
            wanderDuration = Mathf.Max(0f, definition.wanderDuration);
            wanderDurationVariation = Mathf.Clamp(definition.wanderDurationVariation, 0f, 10f);

            if (!definition.enableRangedParticleAttack)
                allowRangedSpit = false;

            spitAttack?.ConfigureFromDefinition(definition);
            audioDriver?.ConfigureFromDefinition(definition);
            emissionDriver?.ConfigureFromDefinition(definition);
            if (animationDriver != null)
                emissionDriver?.ConfigureAttackPulseDuration(animationDriver.AttackLockDuration);
        }

        public void ConfigureFromBrainProfile(DMICreatureBrainProfile profile)
        {
            if (profile == null)
                return;

            brainProfile = profile;
            movementMode = profile.movementMode;
            walkSpeed = profile.walkSpeed;
            runSpeed = profile.runSpeed;
            turnSpeed = profile.turnSpeed;
            stopDistance = profile.stopDistance;
            wanderRadius = profile.wanderRadius;
            idleDurationMin = profile.idleDurationMin;
            idleDurationMax = profile.idleDurationMax;
            idleDurationVariation = Mathf.Clamp(profile.idleDurationVariation, 0f, 10f);
            wanderDuration = Mathf.Max(0f, profile.wanderDuration);
            wanderDurationVariation = Mathf.Clamp(profile.wanderDurationVariation, 0f, 10f);
            navMeshSampleRadius = profile.navMeshSampleRadius;
            patrolMode = profile.patrolMode;
            patrolPointCount = Mathf.Max(2, profile.patrolPointCount);
            patrolRadius = profile.patrolRadius;
            patrolWaitDuration = profile.patrolWaitDuration;
            allowChase = profile.allowChase;
            allowMelee = profile.allowMelee;
            allowRangedSpit = profile.allowRangedSpit;
            meleeHitInterval = profile.meleeHitInterval;

            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.stoppingDistance = stopDistance;
                agent.angularSpeed = profile.agentAngularSpeed > 0.01f
                    ? Mathf.Min(profile.agentAngularSpeed, 200f)
                    : DMILocomotionFacing.ToAgentAngularSpeed(turnSpeed);
            }

            animationDriver?.ConfigureAttackLock(profile.meleeAttackLockDuration);
            emissionDriver?.ConfigureAttackPulseDuration(profile.meleeAttackLockDuration);
        }

        /// <summary>Optional world patrol route from encounter systems (like enemies).</summary>
        public void SetPatrolRoute(Vector3[] worldPoints, DMICreaturePatrolMode mode)
        {
            patrolPoints = worldPoints;
            patrolMode = mode;
            patrolIndex = 0;
            patrolDirection = 1;
            if (patrolPoints != null && patrolPoints.Length > 0)
                movementMode = DMICreatureMovementMode.Patrol;
        }

        /// <summary>Assign Path Creator for Patrol and register with bezier anchors.</summary>
        public void SetPatrolPath(PathCreator path, DMIPathFollowProvider provider = null)
        {
            patrolPath = path;
            patrolPathProvider = provider;
            if (path != null || provider != null)
                movementMode = DMICreatureMovementMode.Patrol;
            TryBindAssignedPatrolPath();
        }

        public PathCreator PatrolPath => patrolPath;
        public DMIPathFollowProvider PatrolPathProvider => patrolPathProvider;

        private void TryBindAssignedPatrolPath()
        {
            if (movementMode != DMICreatureMovementMode.Patrol)
                return;

            DMIPathFollowProvider provider = patrolPathProvider;
            if (provider == null)
                provider = DMIPathFollowBinding.Resolve((Object)patrolPath ?? patrolPathProvider);

            if (provider == null)
                return;

            patrolPathProvider = provider;
            if (patrolPath == null)
                patrolPath = provider.PathCreator;

            provider.TryAssignCreature(this);
        }

        private void CacheReferences()
        {
            if (bridge == null)
                bridge = GetComponent<DMICreatureBridge>();
            if (spitAttack == null)
                spitAttack = GetComponent<DMISulfurSpitAttack>();
            if (health == null)
                health = GetComponent<EnemyHealth>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>() ?? GetComponentInChildren<NavMeshAgent>(true);
            if (animationDriver == null)
                animationDriver = GetComponent<DMICreatureAnimationDriver>()
                                 ?? GetComponentInChildren<DMICreatureAnimationDriver>(true);
            if (emissionDriver == null)
                emissionDriver = GetComponent<DMICreatureEmissionDriver>()
                                 ?? GetComponentInChildren<DMICreatureEmissionDriver>(true);
            if (audioDriver == null)
                audioDriver = GetComponent<DMICreatureAudioDriver>()
                              ?? GetComponentInChildren<DMICreatureAudioDriver>(true);
        }

        private void BuildPatrolRouteIfNeeded()
        {
            if (movementMode != DMICreatureMovementMode.Patrol)
                return;
            if (patrolPoints != null && patrolPoints.Length >= 2)
                return;

            int count = Mathf.Max(2, patrolPointCount);
            patrolPoints = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                Vector3 candidate = homePosition
                                   + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patrolRadius;
                if (agent != null
                    && NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                    patrolPoints[i] = hit.position;
                else
                    patrolPoints[i] = candidate;
            }

            patrolIndex = 0;
            patrolDirection = 1;
        }

        private void RefreshThreat()
        {
            if (bridge == null || !allowChase)
                return;

            Transform threat = bridge.CurrentThreat;
            if (threat == null)
            {
                if (state == CreatureAiState.Chase || state == CreatureAiState.Melee || state == CreatureAiState.Spit)
                {
                    lostTargetTimer += Time.deltaTime;
                    if (lostTargetTimer >= loseTargetDelay)
                        EnterState(CreatureAiState.Idle);
                }

                return;
            }

            float dist = HorizontalDistance(transform.position, threat.position);

            // Outside leash: keep chasing during loseTargetDelay grace (ranged damage often lands
            // beyond leash). Instant clear used to wipe aggro before combat could start.
            if (dist > leashRange)
            {
                lostTargetTimer += Time.deltaTime;
                if (lostTargetTimer >= loseTargetDelay)
                {
                    bridge.ClearThreatTarget();
                    EnterState(CreatureAiState.Idle);
                    return;
                }
            }
            else
            {
                lostTargetTimer = 0f;
            }

            // Threat already acquired (proximity or damage) — chase regardless of engageRange.
            // engageRange only gates passive sense acquisition on the bridge, not combat reaction.
            if (state == CreatureAiState.Idle
                || state == CreatureAiState.Wander
                || state == CreatureAiState.Patrol)
            {
                EnterState(CreatureAiState.Chase);
            }
        }

        /// <summary>
        /// Any damage (melee or ranged) that hits <see cref="EnemyHealth"/> should pull this
        /// creature into Chase when allowChase — same idea as <see cref="EnemyAiController"/> damage aggro.
        /// </summary>
        private void HandleDamagedWithSource(float damage, GameObject source, bool isCritical)
        {
            if (!aggroOnDamaged || isDead || !allowChase || damage <= 0f || source == null || bridge == null)
                return;

            if (health != null && health.IsDead)
                return;

            Transform attacker = EnemyThreatSourceResolver.ResolveThreatRoot(source);
            if (attacker == null)
                attacker = source.transform;

            if (!DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(bridge, attacker))
                return;

            bridge.SetThreatTarget(attacker, moveToTarget: true);
            lostTargetTimer = 0f;

            if (state != CreatureAiState.Chase
                && state != CreatureAiState.Melee
                && state != CreatureAiState.Spit)
            {
                EnterState(CreatureAiState.Chase);
            }
        }

        private void HandleNoise(EnemyNoiseEvents.NoiseEvent noiseEvent)
        {
            if (!senseHearingEnabled || !aggroOnHeardHit || isDead || !allowChase || bridge == null)
                return;

            if (health != null && health.IsDead)
                return;

            if (noiseEvent.Kind != EnemyNoiseKind.CombatImpact)
                return;

            float distance = Vector3.Distance(transform.position, noiseEvent.Position);
            if (distance > hearingRange + noiseEvent.Radius)
                return;

            if (Time.time < nextHearingAggroTime)
                return;

            nextHearingAggroTime = Time.time + Mathf.Max(0.05f, hearingCooldown);

            if (hearingAggroChance <= 0f || UnityEngine.Random.value > hearingAggroChance)
                return;

            Transform attacker = EnemyThreatSourceResolver.ResolveThreatRoot(noiseEvent.Source);
            if (attacker == null || !DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(bridge, attacker))
                return;

            bridge.SetThreatTarget(attacker, moveToTarget: true);
            lostTargetTimer = 0f;

            if (state != CreatureAiState.Chase
                && state != CreatureAiState.Melee
                && state != CreatureAiState.Spit)
            {
                EnterState(CreatureAiState.Chase);
            }
        }

        private void UpdateIdle()
        {
            SetSpeed(0f);
            StopAgent();
            if (movementMode == DMICreatureMovementMode.Stationary)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            if (movementMode == DMICreatureMovementMode.Patrol)
                EnterState(CreatureAiState.Patrol);
            else
                EnterState(CreatureAiState.Wander);
        }

        private void UpdateWander()
        {
            SetSpeed(walkSpeed);

            // Optional wander timeout (CM wanderDuration + variation). 0 = walk until arrival only.
            if (stateTimer > 0f)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    EnterState(CreatureAiState.Idle);
                    return;
                }
            }

            if (HasArrived(moveTarget))
            {
                EnterState(CreatureAiState.Idle);
                return;
            }

            MoveToward(moveTarget, walkSpeed);
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                EnterState(CreatureAiState.Idle);
                return;
            }

            if (stateTimer > 0f)
            {
                SetSpeed(0f);
                StopAgent();
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    AdvancePatrolIndex();
                return;
            }

            moveTarget = patrolPoints[Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1)];
            SetSpeed(walkSpeed);
            if (HasArrived(moveTarget))
            {
                stateTimer = patrolWaitDuration;
                StopAgent();
                SetSpeed(0f);
                return;
            }

            MoveToward(moveTarget, walkSpeed);
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (patrolMode == DMICreaturePatrolMode.PingPong)
            {
                int next = patrolIndex + patrolDirection;
                if (next < 0 || next >= patrolPoints.Length)
                {
                    patrolDirection *= -1;
                    next = patrolIndex + patrolDirection;
                }

                patrolIndex = Mathf.Clamp(next, 0, patrolPoints.Length - 1);
            }
            else
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
        }

        private void UpdateChase()
        {
            Transform threat = bridge != null ? bridge.CurrentThreat : null;
            if (threat == null || !allowChase)
            {
                EnterState(CreatureAiState.Idle);
                return;
            }

            float dist = HorizontalDistance(transform.position, threat.position);
            if (allowMelee && dist <= meleeRange)
            {
                EnterState(CreatureAiState.Melee);
                return;
            }

            if (TryBeginSpit(threat, dist))
                return;

            SetSpeed(runSpeed);
            MoveToward(threat.position, runSpeed);
        }

        private void UpdateMelee()
        {
            Transform threat = bridge != null ? bridge.CurrentThreat : null;
            if (threat == null || !allowMelee)
            {
                EnterState(CreatureAiState.Idle);
                return;
            }

            float dist = HorizontalDistance(transform.position, threat.position);
            if (dist > meleeRange * 1.35f)
            {
                EnterState(allowChase ? CreatureAiState.Chase : CreatureAiState.Idle);
                return;
            }

            if (TryBeginSpit(threat, dist))
                return;

            SetSpeed(0f);
            StopAgent();
            FaceToward(threat.position);

            if (Time.time >= nextMeleeHitTime)
            {
                nextMeleeHitTime = Time.time + SampleMeleeInterval();
                animationDriver?.PlayAttack();
                emissionDriver?.NotifyAttack(animationDriver != null
                    ? animationDriver.AttackLockDuration
                    : -1f);
                audioDriver?.PlayMeleeAttack();
                bridge?.TryDealMeleeToThreat();
            }
        }

        private float SampleMeleeInterval()
        {
            float interval = Mathf.Max(0.05f, meleeHitInterval);
            float variation = Mathf.Clamp(meleeHitIntervalVariation, 0f, 10f);
            return variation > 0f ? interval + Random.Range(0f, variation) : interval;
        }

        private float SampleIdleDuration()
        {
            float duration = Mathf.Max(0f, idleDurationMin);
            float variation = Mathf.Clamp(idleDurationVariation, 0f, 10f);
            if (variation > 0f)
                return duration + Random.Range(0f, variation);
            // Legacy brain profiles: idleDurationMax > min with no variation field.
            if (idleDurationMax > idleDurationMin + 0.001f)
                return Random.Range(idleDurationMin, idleDurationMax);
            return duration;
        }

        private float SampleWanderDuration()
        {
            float duration = Mathf.Max(0f, wanderDuration);
            float variation = Mathf.Clamp(wanderDurationVariation, 0f, 10f);
            if (variation > 0f)
                return duration + Random.Range(0f, variation);
            return duration; // 0 = no wander timeout
        }

        private void UpdateSpit()
        {
            Transform threat = bridge != null ? bridge.CurrentThreat : null;
            SetSpeed(0f);
            StopAgent();
            if (threat != null)
                FaceToward(threat.position);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                EnterState(threat != null && allowChase ? CreatureAiState.Chase : CreatureAiState.Idle);
        }

        private bool TryBeginSpit(Transform threat, float dist)
        {
            if (!allowRangedSpit || spitAttack == null || !spitAttack.IsReady || threat == null)
                return false;

            // Stay out of point-blank melee; otherwise fire on cooldown (definition spitCooldown).
            if (dist > spitAttack.Range || dist <= meleeRange * 0.85f)
                return false;

            // RiggedNative cadence is spitCooldown. Chance rolls are for Malbers brain tasks only —
            // per-frame RollSpitChance here made CM "4s" feel like a fuzzy floor, not the fire rate.
            if (!spitAttack.TryFire(threat))
                return false;

            animationDriver?.PlayAttack();
            emissionDriver?.NotifyAttack(animationDriver != null
                ? animationDriver.AttackLockDuration
                : -1f);
            audioDriver?.PlayRangedAttack();
            EnterState(CreatureAiState.Spit);
            stateTimer = 0.85f;
            return true;
        }

        private void EnterState(CreatureAiState next)
        {
            state = next;
            switch (next)
            {
                case CreatureAiState.Idle:
                    stateTimer = SampleIdleDuration();
                    StopAgent();
                    SetSpeed(0f);
                    break;
                case CreatureAiState.Wander:
                    moveTarget = PickWanderPoint();
                    stateTimer = SampleWanderDuration(); // 0 = no timeout
                    SetSpeed(walkSpeed);
                    break;
                case CreatureAiState.Patrol:
                    BuildPatrolRouteIfNeeded();
                    stateTimer = 0f;
                    SetSpeed(walkSpeed);
                    break;
                case CreatureAiState.Chase:
                    SetSpeed(runSpeed);
                    break;
                case CreatureAiState.Melee:
                    StopAgent();
                    SetSpeed(0f);
                    break;
                case CreatureAiState.Spit:
                    StopAgent();
                    SetSpeed(0f);
                    break;
            }
        }

        private Vector3 PickWanderPoint()
        {
            Vector3 random = homePosition + Random.insideUnitSphere * wanderRadius;
            random.y = homePosition.y;
            if (agent != null && NavMesh.SamplePosition(random, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return hit.position;
            return random;
        }

        private void MoveToward(Vector3 worldTarget, float speed)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                float moveScale = 1f;
                if (agent.desiredVelocity.sqrMagnitude > 0.01f)
                    moveScale = DMILocomotionFacing.FacingMoveScale(transform, agent.desiredVelocity);

                agent.speed = speed * moveScale;
                agent.angularSpeed = DMILocomotionFacing.ToAgentAngularSpeed(turnSpeed);
                if ((agent.destination - worldTarget).sqrMagnitude > 0.25f)
                    agent.SetDestination(worldTarget);
                return;
            }

            Vector3 flat = worldTarget;
            flat.y = transform.position.y;
            Vector3 delta = flat - transform.position;
            if (delta.sqrMagnitude < 0.0001f)
                return;

            float moveScaleFallback = DMILocomotionFacing.FacingMoveScale(transform, delta);
            Vector3 step = delta.normalized * (speed * moveScaleFallback * Time.deltaTime);
            transform.position += step;
            FaceToward(flat);
        }

        private void FaceToward(Vector3 worldTarget)
        {
            DMILocomotionFacing.FaceToward(transform, worldTarget, turnSpeed);
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.isStopped = true;
            agent.ResetPath();
        }

        private bool HasArrived(Vector3 target)
        {
            return HorizontalDistance(transform.position, target) <= stopDistance + 0.35f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void SetSpeed(float speed)
        {
            currentSpeed = speed;
        }

        private void HandleDeath()
        {
            isDead = true;
            StopAgent();
            SetSpeed(0f);
            if (agent != null)
                agent.enabled = false;
            animationDriver?.PlayDeath();
            emissionDriver?.NotifyDeath();
            audioDriver?.NotifyDeath();
            enabled = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 home = Application.isPlaying ? homePosition : transform.position;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            if (movementMode == DMICreatureMovementMode.Wander && wanderRadius > 0f)
                Gizmos.DrawWireSphere(home, wanderRadius);
            if (movementMode == DMICreatureMovementMode.Patrol && patrolRadius > 0f)
                Gizmos.DrawWireSphere(home, patrolRadius);

            // Sense / leash mirrors (damage aggro can pull from beyond engageRange).
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.25f);
            if (engageRange > 0f)
                Gizmos.DrawWireSphere(transform.position, engageRange);
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.2f);
            if (leashRange > 0f)
                Gizmos.DrawWireSphere(transform.position, leashRange);
            if (senseHearingEnabled && hearingRange > 0f)
            {
                Gizmos.color = new Color(0.85f, 0.2f, 0.95f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, hearingRange);
            }

            if (patrolPoints == null)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.DrawSphere(patrolPoints[i], 0.2f);
                Vector3 next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (patrolMode == DMICreaturePatrolMode.Loop || i + 1 < patrolPoints.Length)
                    Gizmos.DrawLine(patrolPoints[i], next);
            }
        }
#endif
    }
}
