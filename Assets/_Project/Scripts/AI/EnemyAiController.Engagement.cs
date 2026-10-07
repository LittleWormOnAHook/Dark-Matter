using Project.AI.Invector;
using Project.Core;
using UnityEngine;

namespace Project.AI
{
    // Combat Plan Phase 3 + Enemy Spacing plan Part A: engagement role (Engager / Holder) from
    // DMEnemyEngagementDirector, the Hold state (world-anchored hold points, holder idle actions with the
    // anti-circling drift cap), the shared range-hysteresis transition, a single facing resolve per frame,
    // the attack facing cone / hand-off grace / off-screen wind-up gate, per-swing tracking windows, and the
    // utility brain hook (DMEnemyBrain). Melee enemies only; ranged-preferred and Stationary enemies keep
    // the legacy state machine. DM_EnemyEngagementProfile.enableEngagementDirector = false rolls it all back.
    public partial class EnemyAiController
    {
        private readonly DMEngagementAgentState engagementState = new DMEngagementAgentState();
        [Tooltip("Optional per-type engagement profile (set from EnemyDefinition). Empty = global DM_EnemyEngagementProfile.")]
        [SerializeField] private DM_EnemyEngagementProfile engagementProfileOverride;
        private EnemyDefinition appliedDefinition;
        private DMEnemyBrain brain;
        private DMEnemyMeleeComboDriver comboDriver;
        private EnemyInvectorRagdollBridge ragdollBridge;
        private DMSpawnPhysicsStabilizer spawnStabilizer;

        private bool engagementActiveThisFrame;
        private float nextBrainTickTime;
        private float engageOutTimer;
        private float creepTimer;
        private bool creeping;
        private float coneBlockedSince = -1f;
        private float offscreenReadySince = -1f;
        private bool wasInAttackSequence;
        private Transform lastDamagedBy;
        private float lastDamagedTime;

        // Holder state.
        private bool holderTurning;
        private float holderOutsideBandTimer;
        private float nextReanchorTime;
        private float anchorRadius;
        private DMHolderAction holderAction = DMHolderAction.None;
        private float holderActionUntil;
        private Vector3 holderFeintPoint;
        private int holderFeintPhase;
        private bool holderTauntBlocking;
        private float nextHolderActionRoll;
        private float nextSidestepAllowed;
        private float nextTauntAllowed;
        private int lastSidestepSign;
        private readonly float[] driftTimes = { -999f, -999f, -999f, -999f, -999f, -999f, -999f, -999f };
        private readonly float[] driftAngles = new float[8];
        private int driftIndex;

        public DMEngagementAgentState EngagementState => engagementState;
        public DMEngagementRole EngagementRole => engagementState.Role;
        public DMEnemyBrain Brain => brain;

        public bool IsInAttackSequence =>
            (combat != null && combat.IsAttacking) || (comboDriver != null && comboDriver.IsSequenceActive);

        public bool HasPunishPriority => comboDriver != null && comboDriver.HasPunishArmed;

        public float BrainTokenBias => brain != null ? brain.TokenBias : 0f;

        /// <summary>Time the engagement target last damaged this enemy (0 = never / different attacker).</summary>
        public float LastHitByTargetTime
        {
            get
            {
                if (lastDamagedBy == null)
                    return 0f;

                Transform ringTarget = engagementState.Ring != null ? engagementState.Ring.Target : null;
                if (lastDamagedBy == ringTarget || (combat != null && lastDamagedBy == combat.CurrentTarget))
                    return lastDamagedTime;
                return 0f;
            }
        }

        /// <summary>Awareness (§9), derived from the state machine. Debug / future icon only.</summary>
        public DMEnemyAwareness Awareness
        {
            get
            {
                switch (state)
                {
                    case AiState.Chase:
                    case AiState.Attack:
                    case AiState.Defensive:
                    case AiState.Hold:
                        return DMEnemyAwareness.Combat;
                    case AiState.Search:
                        return DMEnemyAwareness.Alert;
                    case AiState.Investigate:
                        return DMEnemyAwareness.Suspicious;
                    default:
                        return DMEnemyAwareness.Unaware;
                }
            }
        }

        private void InitializeEngagement()
        {
            comboDriver = GetComponent<DMEnemyMeleeComboDriver>();
            ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            spawnStabilizer = GetComponent<DMSpawnPhysicsStabilizer>();
            brain = GetComponent<DMEnemyBrain>();
            EnemyDefinition definition = ResolveAppliedDefinition();
            DM_EnemyBrainProfile brainProfile = definition != null && definition.brainProfileOverride != null
                ? definition.brainProfileOverride
                : DM_EnemyBrainProfile.Live;
            if (brain == null && brainProfile != null && brainProfile.autoAttachBrain)
            {
                brain = gameObject.AddComponent<DMEnemyBrain>();
                EnemyDefinitionOverrides.ApplyBrain(brain, definition, isNewBrain: true);
            }
            else if (brain != null && definition != null)
            {
                EnemyDefinitionOverrides.ApplyBrain(brain, definition, isNewBrain: false);
            }
        }

        /// <summary>Per-type engagement profile when the EnemyDefinition sets one, else the global profile.</summary>
        public DM_EnemyEngagementProfile EngagementProfile =>
            engagementProfileOverride != null ? engagementProfileOverride : DM_EnemyEngagementProfile.Live;

        /// <summary>Definition applied by EnemyInvectorGameplaySetup (spawner override or prefab-baked).</summary>
        public EnemyDefinition AppliedDefinition => ResolveAppliedDefinition();

        public void ApplyDefinitionOverrides(EnemyDefinition definition)
        {
            appliedDefinition = definition;
            engagementProfileOverride = definition != null ? definition.engagementProfileOverride : null;
        }

        private EnemyDefinition ResolveAppliedDefinition()
        {
            if (appliedDefinition != null)
                return appliedDefinition;

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            return bootstrap != null ? bootstrap.Definition : null;
        }

        private void RecordDamageForEngagement(GameObject source, float damage)
        {
            if (source == null || damage <= 0f)
                return;

            Transform attacker = EnemyThreatSourceResolver.ResolveThreatRoot(source);
            if (attacker == null)
                return;

            lastDamagedBy = attacker;
            lastDamagedTime = Time.time;
        }

        private bool UsesEngagement(Transform target)
        {
            if (target == null || combat == null || IsStationary)
                return false;

            if (!DMEnemyEngagementDirector.IsEnabled)
                return false;

            if (combatBridge != null && combatBridge.IsArmedRangedPreferred())
                return false;

            return combat.CurrentTarget == target && combat.HasLivingTarget();
        }

        private void LeaveEngagement()
        {
            CancelHolderAction();
            if (engagementState.Ring != null)
                DMEnemyEngagementDirector.Leave(this);
        }

        /// <summary>
        /// Shared transition for engaged melee enemies (replaces the visible-threat and aggro-path
        /// transitions). Returns false when this enemy should use the legacy transitions instead.
        /// </summary>
        private bool RunEngagementTransitions(Transform target)
        {
            engagementActiveThisFrame = false;
            if (!UsesEngagement(target))
            {
                if (engagementState.Ring != null)
                    LeaveEngagement();
                return false;
            }

            DMEngagementRole role = DMEnemyEngagementDirector.UpdateMembership(this, target);
            if (role == DMEngagementRole.None)
                return false;

            engagementActiveThisFrame = true;
            DM_EnemyEngagementProfile p = EngagementProfile;
            float distance = HorizontalDistance(transform.position, target.position);
            float effective = combat.ResolveEffectiveAttackRange(target);
            TickBrain(role, distance <= effective * 1.1f);

            if (role == DMEngagementRole.Holder)
            {
                if (state != AiState.Hold)
                    EnterState(AiState.Hold);
                return true;
            }

            // Engager.
            switch (state)
            {
                case AiState.Chase:
                    engageOutTimer = 0f;
                    if (distance <= effective * p.engageInFactor)
                        EnterState(AiState.Attack);
                    else if (!CanChaseTarget(target.position))
                        GiveUpChaseAndReturnHome();
                    break;
                case AiState.Attack:
                    if (distance > effective * p.engageOutFactor && !IsInAttackSequence)
                    {
                        engageOutTimer += Time.deltaTime;
                        if (engageOutTimer >= p.engageOutGrace)
                        {
                            engageOutTimer = 0f;
                            if (CanChaseTarget(target.position))
                                EnterState(AiState.Chase);
                            else
                                GiveUpChaseAndReturnHome();
                        }
                    }
                    else
                    {
                        engageOutTimer = 0f;
                    }

                    break;
                case AiState.Defensive:
                    break;
                default:
                    engageOutTimer = 0f;
                    EnterState(distance <= effective * p.engageInFactor ? AiState.Attack : AiState.Chase);
                    break;
            }

            return true;
        }

        private void TickBrain(DMEngagementRole role, bool threatInReach)
        {
            if (brain == null)
                return;

            float now = Time.time;
            if (now < nextBrainTickTime)
                return;

            DM_EnemyEngagementProfile p = EngagementProfile;
            nextBrainTickTime = now + 1f / Mathf.Max(1f, p.agentRoleTickHz);
            brain.Tick(role == DMEngagementRole.Engager, threatInReach, IsInAttackSequence, LastHitByTargetTime);
        }

        /// <summary>Director eligibility (§5.4 penalties). The current holder keeps the token through stagger.</summary>
        public bool IsTokenEligible(Transform target, DM_EnemyEngagementProfile p, bool asCurrentHolder)
        {
            if (!isActiveAndEnabled || health == null || health.IsDead || target == null)
                return false;

            if (combat == null || combat.CurrentTarget != target)
                return false;

            if (combatBridge != null && combatBridge.IsArmedRangedPreferred())
                return false;

            if (ragdollBridge != null && ragdollBridge.IsCorpseRagdolled)
                return false;

            if (brain != null && brain.IsRetreating)
                return false;

            float maxDistance = p.candidateMaxDistance * (asCurrentHolder ? 1.5f : 1f);
            if (HorizontalDistance(transform.position, target.position) > maxDistance)
                return false;

            if (!AllowsCombatTarget(target))
                return false;

            if (asCurrentHolder)
                return true;

            if (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.IsKnockdownActive))
                return false;

            if (spawnStabilizer != null && spawnStabilizer.IsSpawnSettleActive)
                return false;

            return !IsDefensiveActionActive;
        }

        public void OnEngagementTokenAwarded(bool withGrace)
        {
            CancelHolderAction();
            engageOutTimer = 0f;
            creepTimer = 0f;
            creeping = false;
            coneBlockedSince = -1f;
            offscreenReadySince = -1f;
            if (brain != null)
                brain.CancelRetreat();
        }

        /// <summary>EnemyCombat.TryAttack gate: token, hand-off grace, facing cone, off-screen wind-up.</summary>
        public bool CanEngagementAttack(Transform target)
        {
            if (engagementState.Ring == null || engagementState.Role == DMEngagementRole.None)
                return true;

            if (engagementState.Role != DMEngagementRole.Engager)
                return false;

            if (DMEnemyEngagementDirector.IsHandoffGraceActive(this))
                return false;

            if (target == null)
                return false;

            DM_EnemyEngagementProfile p = EngagementProfile;
            float now = Time.time;
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            float angle = toTarget.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f
                ? Vector3.Angle(forward, toTarget)
                : 0f;

            if (angle > p.attackFacingConeHalfAngle)
            {
                if (coneBlockedSince < 0f)
                    coneBlockedSince = now;
                if (now - coneBlockedSince < p.attackFacingMaxTurnSeconds)
                    return false;
            }
            else
            {
                coneBlockedSince = -1f;
            }

            if (p.offscreenAttackExtraWindup > 0f && IsOffscreenForPlayer(target))
            {
                if (offscreenReadySince < 0f)
                    offscreenReadySince = now;
                if (now - offscreenReadySince < p.offscreenAttackExtraWindup)
                    return false;
            }
            else
            {
                offscreenReadySince = -1f;
            }

            return true;
        }

        public void NotifyEngagementAttackBegan()
        {
            coneBlockedSince = -1f;
            offscreenReadySince = -1f;
            if (engagementState.Ring != null)
                DMEnemyEngagementDirector.NotifyAttackBegan(this);
        }

        private bool IsOffscreenForPlayer(Transform target)
        {
            if (target == null || PlayerReference.Transform != target)
                return false;

            Camera cam = PlayerReference.Camera;
            if (cam == null)
                return false;

            Vector3 viewport = cam.WorldToViewportPoint(transform.position + Vector3.up * 1.2f);
            return viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
        }

        private void TrackAttackSequenceEdge()
        {
            bool inSequence = IsInAttackSequence;
            if (wasInAttackSequence && !inSequence)
                engagementState.LastSequenceEndTime = Time.time;
            wasInAttackSequence = inSequence;
        }

        private void LateEngagementBookkeeping()
        {
            TrackAttackSequenceEdge();
            if (engagementState.Ring == null)
                return;

            bool combatState = state == AiState.Chase || state == AiState.Attack ||
                               state == AiState.Defensive || state == AiState.Hold;
            if (!combatState)
                LeaveEngagement();
        }

        // ── Facing ───────────────────────────────────────────────────────────────────────────────

        private void FaceTowardsRate(Vector3 worldPosition, float degreesPerSecond)
        {
            DMILocomotionFacing.FaceTowardDegrees(transform, worldPosition, degreesPerSecond);
        }

        /// <summary>Per-swing tracking window: track until the swing's normalized time passes trackUntil, then lock.</summary>
        private void ApplySwingTracking(Transform target, DM_EnemyEngagementProfile p)
        {
            if (target == null)
                return;

            if (comboDriver != null && comboDriver.TryGetSwingProgress(out float normalized, out bool charged))
            {
                float rate = charged ? p.chargedTrackRate : p.lightTrackRate;
                float until = charged ? p.chargedTrackUntil : p.lightTrackUntil;
                if (normalized < until && rate > 0f)
                    FaceTowardsRate(target.position, rate);
                return;
            }

            float elapsed = combat != null ? combat.AttackElapsed : -1f;
            if (elapsed >= 0f && elapsed < 0.3f && p.lightTrackRate > 0f)
                FaceTowardsRate(target.position, p.lightTrackRate);
        }

        private void FaceHolder(Transform target, DM_EnemyEngagementProfile p)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            float angle = Vector3.Angle(forward, toTarget);
            if (!holderTurning && angle > p.holderFacingDeadZone)
                holderTurning = true;

            if (!holderTurning)
                return;

            FaceTowardsRate(target.position, p.holderTurnRate);
            if (angle <= p.holderFacingSettle)
                holderTurning = false;
        }

        // ── Movement helpers ─────────────────────────────────────────────────────────────────────

        /// <summary>Flat translation that keeps the current facing (strafe / backpedal clips via the motor bridge).</summary>
        private void StepFlat(Vector3 direction, float speed, float maxDistance)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f || speed <= 0f || maxDistance <= 0.001f || IsStationary)
            {
                ClearLocomotion();
                return;
            }

            direction.Normalize();
            float distance = Mathf.Min(speed * Time.deltaTime, maxDistance);
            Vector3 step = direction * distance;
            step = ClampStepAwayFromNonTargetPlayer(step);
            if (step.sqrMagnitude < 0.000001f)
            {
                ClearLocomotion();
                return;
            }

            transform.position += step;
            currentLocomotionSpeed = speed;
            currentLocalMoveDirection = transform.InverseTransformDirection(direction);
        }

        // ── Engager (Attack state) ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Engager in melee band: no ring rotation, single straight creep / back-off with hysteresis, one facing
        /// resolve, per-swing tracking, brain-driven defend.
        /// </summary>
        private void UpdateEngagerAttack(Transform target, float distance)
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            if (IsInAttackSequence)
            {
                ClearLocomotion();
                creeping = false;
                ApplySwingTracking(target, p);
                combat.TryAttack();
                return;
            }

            float effective = combat.ResolveEffectiveAttackRange(target);
            if (brain != null && brain.ConsumeDefendRequest(out bool preferBlock) && distance <= effective * 1.1f)
            {
                BeginBrainDefend(target, preferBlock);
                return;
            }

            float standoff = ResolveCombatStandoffFor(target);
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (distance < minCombatSeparation)
            {
                creeping = false;
                creepTimer = 0f;
                StepFlat(-toTarget, walkSpeed * 0.6f, minCombatSeparation - distance);
            }
            else
            {
                if (!creeping)
                {
                    if (distance > effective * p.creepStartFactor)
                    {
                        creepTimer += Time.deltaTime;
                        if (creepTimer >= p.creepGrace)
                            creeping = true;
                    }
                    else
                    {
                        creepTimer = 0f;
                    }
                }

                float creepStop = Mathf.Max(standoff, minCombatSeparation);
                if (creeping && distance > creepStop)
                {
                    float speed = walkSpeed * (combat.WantsMeleeReposition ? 0.92f : p.creepSpeedFactor);
                    StepFlat(toTarget, speed, distance - creepStop);
                }
                else
                {
                    creeping = false;
                    creepTimer = 0f;
                    ClearLocomotion();
                }
            }

            FaceTowardsRate(target.position, p.engagerTurnRate);
            combat.TryAttack();
        }

        private void BeginBrainDefend(Transform target, bool preferBlock)
        {
            EnterState(AiState.Defensive);
            if (combatBridge != null)
            {
                if (preferBlock && combatBridge.TryBeginBlock(defensiveBlockDuration, out float blockDuration))
                {
                    defensiveActionUntil = Time.time + blockDuration;
                    return;
                }

                if (combatBridge.TryBeginDodgeRoll(target, out float rollDuration))
                {
                    defensiveActionUntil = Time.time + rollDuration;
                    return;
                }

                if (!preferBlock && combatBridge.TryBeginBlock(defensiveBlockDuration, out float fallbackBlock))
                {
                    defensiveActionUntil = Time.time + fallbackBlock;
                    return;
                }
            }

            EnterState(AiState.Attack);
        }

        // ── Holder (Hold state) ─────────────────────────────────────────────────────────────────

        private void OnEnterHold()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            StopNavMeshMovement();
            holderTurning = false;
            holderOutsideBandTimer = 0f;
            holderAction = DMHolderAction.None;
            nextHolderActionRoll = Time.time + p.RandomRange(p.holderActionInterval) + perfPhase * 0.4f;
        }

        private void UpdateHold()
        {
            Transform target = combat != null ? combat.CurrentTarget : null;
            if (target == null || !combat.HasLivingTarget() || engagementState.Role != DMEngagementRole.Holder)
            {
                if (engagementState.Role != DMEngagementRole.Engager)
                    LeaveEngagement();
                EnterState(AiState.Chase);
                return;
            }

            DM_EnemyEngagementProfile p = EngagementProfile;
            float now = Time.time;
            float distance = HorizontalDistance(transform.position, target.position);

            MaintainHoldAnchor(target, p, distance, now);

            Vector3 toAnchor = engagementState.Anchor - transform.position;
            toAnchor.y = 0f;
            float anchorDistance = toAnchor.magnitude;

            if (holderAction != DMHolderAction.None && TickHolderAction(target, p, now, toAnchor, anchorDistance))
            {
                FaceHolder(target, p);
                return;
            }

            if (anchorDistance > p.holderRunToAnchorDistance)
            {
                // Far from the hold point (target ran off, or just joined): run there facing the move direction.
                MoveTowards(engagementState.Anchor, ResolveChaseSpeed(), 0.25f);
                return;
            }

            if (anchorDistance > 0.3f)
                StepFlat(toAnchor, p.holderWalkSpeed, anchorDistance - 0.05f);
            else
                ClearLocomotion();

            FaceHolder(target, p);

            if (anchorDistance <= 0.6f)
                TickHolderIdle(target, p, now, distance);
        }

        private float ResolveHoldRadius(DM_EnemyEngagementProfile p)
        {
            float min = Mathf.Min(p.holdRingRadiusMin, p.holdRingRadiusMax);
            float max = Mathf.Max(p.holdRingRadiusMin, p.holdRingRadiusMax);
            float radius = brain != null && brain.IsRetreating ? max : p.holdRingRadius;
            return Mathf.Clamp(radius, min, max);
        }

        private void MaintainHoldAnchor(Transform target, DM_EnemyEngagementProfile p, float distance, float now)
        {
            float radius = ResolveHoldRadius(p);
            if (!engagementState.HasAnchor)
            {
                SetHoldAnchor(target, p, radius, now, countDrift: false);
                return;
            }

            Vector3 originDelta = target.position - engagementState.AnchorOrigin;
            originDelta.y = 0f;
            bool playerMoved = originDelta.magnitude > p.holdReanchorPlayerMove;

            Vector3 toAnchor = engagementState.Anchor - transform.position;
            toAnchor.y = 0f;
            bool atAnchor = toAnchor.magnitude <= 0.6f;

            bool outsideBand = false;
            if (atAnchor && (distance < p.holdBandMin || distance > p.holdBandMax))
            {
                holderOutsideBandTimer += Time.deltaTime;
                outsideBand = holderOutsideBandTimer >= p.holdBandGrace;
            }
            else
            {
                holderOutsideBandTimer = 0f;
            }

            bool overlapping = atAnchor && DMEnemyEngagementDirector.IsHolderOverlapping(this, p.holderMinSpacing);

            bool inEngagerLine = false;
            EnemyAiController engager = DMEnemyEngagementDirector.GetEngager(this);
            if (engager != null && engager != this)
            {
                Vector3 toSelf = transform.position - target.position;
                Vector3 toEngager = engager.transform.position - target.position;
                toSelf.y = 0f;
                toEngager.y = 0f;
                if (toSelf.sqrMagnitude > 0.01f && toEngager.sqrMagnitude > 0.01f &&
                    toSelf.sqrMagnitude > toEngager.sqrMagnitude &&
                    Vector3.Angle(toSelf, toEngager) < p.holderEngagerLineHalfAngle)
                    inEngagerLine = true;
            }

            bool radiusChanged = Mathf.Abs(anchorRadius - radius) > 0.5f;
            bool wantsReanchor = playerMoved || outsideBand || overlapping || inEngagerLine || radiusChanged;
            if (!wantsReanchor)
                return;

            // Target ran well past the hold band: follow now instead of waiting out the re-anchor cooldown.
            bool farBehind = originDelta.magnitude > p.holdBandMax;
            if (now < nextReanchorTime && !radiusChanged && !farBehind)
                return;

            holderOutsideBandTimer = 0f;
            SetHoldAnchor(target, p, radius, now, countDrift: !playerMoved);
        }

        private void SetHoldAnchor(Transform target, DM_EnemyEngagementProfile p, float radius, float now, bool countDrift)
        {
            Vector3 center = target.position;
            Vector3 fromTarget = transform.position - center;
            fromTarget.y = 0f;

            Vector3 oldBearing = engagementState.HasAnchor ? engagementState.Anchor - center : Vector3.zero;
            oldBearing.y = 0f;

            Vector3 anchor = DMEnemyEngagementDirector.ResolveHoldAnchor(this, target, fromTarget, radius);
            if (TrySampleGround(anchor, out float groundY))
                anchor.y = groundY;
            else
                anchor.y = transform.position.y;

            if (countDrift && oldBearing.sqrMagnitude > 0.01f)
            {
                Vector3 newBearing = anchor - center;
                newBearing.y = 0f;
                if (newBearing.sqrMagnitude > 0.01f)
                    RecordDrift(Vector3.SignedAngle(oldBearing, newBearing, Vector3.up), now);
            }

            engagementState.Anchor = anchor;
            engagementState.AnchorOrigin = center;
            engagementState.HasAnchor = true;
            engagementState.AnchorTime = now;
            anchorRadius = radius;
            nextReanchorTime = now + p.RandomRange(p.holdReanchorCooldown);
        }

        private void RecordDrift(float signedDegrees, float now)
        {
            driftTimes[driftIndex] = now;
            driftAngles[driftIndex] = signedDegrees;
            driftIndex = (driftIndex + 1) % driftTimes.Length;
        }

        private float DriftLast10Seconds(float now)
        {
            float sum = 0f;
            for (int i = 0; i < driftTimes.Length; i++)
            {
                if (now - driftTimes[i] <= 10f)
                    sum += driftAngles[i];
            }

            return sum;
        }

        private void TickHolderIdle(Transform target, DM_EnemyEngagementProfile p, float now, float distance)
        {
            if (now < nextHolderActionRoll)
                return;

            nextHolderActionRoll = now + p.RandomRange(p.holderActionInterval);

            float sideMul = 1f, tauntMul = 1f, feintMul = 1f;
            if (brain != null)
                brain.GetHolderActionMultipliers(out sideMul, out tauntMul, out feintMul);

            bool sidestepOk = now >= nextSidestepAllowed &&
                              DMEnemyEngagementDirector.GetTargetSpeed(this) <= p.noSidestepPlayerSpeed &&
                              now - engagementState.AnchorTime >= p.noSidestepAfterReanchor;
            float wSide = sidestepOk ? Mathf.Max(0f, p.holderWeightSidestep * sideMul) : 0f;
            float wTaunt = now >= nextTauntAllowed ? Mathf.Max(0f, p.holderWeightTaunt * tauntMul) : 0f;
            float wFeint = distance > p.feintMinDistance ? Mathf.Max(0f, p.holderWeightFeint * feintMul) : 0f;
            float wNone = Mathf.Max(0f, p.holderWeightNone);
            float total = wSide + wTaunt + wFeint + wNone;
            if (total <= 0.0001f)
                return;

            float roll = Random.value * total;
            DMHolderAction pick;
            if (roll < wSide)
                pick = DMHolderAction.Sidestep;
            else if (roll < wSide + wTaunt)
                pick = DMHolderAction.Taunt;
            else if (roll < wSide + wTaunt + wFeint)
                pick = DMHolderAction.Feint;
            else
                return;

            switch (pick)
            {
                case DMHolderAction.Sidestep:
                    TryBeginSidestep(target, p, now);
                    break;
                case DMHolderAction.Taunt:
                    TryBeginTaunt(p, now);
                    break;
                case DMHolderAction.Feint:
                    TryBeginFeint(target, p, now);
                    break;
            }
        }

        private void TryBeginSidestep(Transform target, DM_EnemyEngagementProfile p, float now)
        {
            Vector3 center = engagementState.AnchorOrigin;
            Vector3 fromCenter = engagementState.Anchor - center;
            fromCenter.y = 0f;
            float radius = fromCenter.magnitude;
            if (radius < 0.5f)
                return;

            float stepDistance = p.RandomRange(p.sidestepDistance);
            float arc = Mathf.Min(p.sidestepMaxArc, stepDistance / radius * Mathf.Rad2Deg);
            if (arc < 1f)
                return;

            // Prefer the side away from the nearest other ring member; never the same side twice in a row.
            int sign = Random.value < 0.5f ? 1 : -1;
            EnemyAiController engager = DMEnemyEngagementDirector.GetEngager(this);
            if (engager != null && engager != this)
            {
                Vector3 toEngager = engager.transform.position - center;
                toEngager.y = 0f;
                float side = Vector3.SignedAngle(fromCenter, toEngager, Vector3.up);
                sign = side > 0f ? -1 : 1;
            }

            if (sign == lastSidestepSign)
                sign = -sign;

            float drift = DriftLast10Seconds(now);
            if (Mathf.Abs(drift + sign * arc) > p.holderMaxDriftDegPer10s)
            {
                sign = -sign;
                if (Mathf.Abs(drift + sign * arc) > p.holderMaxDriftDegPer10s)
                    return;
            }

            float duration = stepDistance / Mathf.Max(0.2f, p.holderWalkSpeed) + 0.6f;
            if (!DMEnemyEngagementDirector.TryBeginHolderAction(this, duration))
            {
                nextHolderActionRoll = now + 0.5f;
                return;
            }

            Vector3 rotated = Quaternion.AngleAxis(sign * arc, Vector3.up) * fromCenter;
            Vector3 anchor = center + rotated;
            if (TrySampleGround(anchor, out float groundY))
                anchor.y = groundY;
            else
                anchor.y = engagementState.Anchor.y;

            engagementState.Anchor = anchor;
            RecordDrift(sign * arc, now);
            lastSidestepSign = sign;
            nextSidestepAllowed = now + p.sidestepCooldown;
            holderAction = DMHolderAction.Sidestep;
            holderActionUntil = now + duration;
        }

        private void TryBeginTaunt(DM_EnemyEngagementProfile p, float now)
        {
            float duration = p.RandomRange(p.tauntDuration);
            if (!DMEnemyEngagementDirector.TryBeginHolderAction(this, duration))
            {
                nextHolderActionRoll = now + 0.5f;
                return;
            }

            // Guard raise / weapon posture as the taunt read (no dedicated taunt clips yet).
            holderTauntBlocking = combatBridge != null && combatBridge.TryBeginBlock(duration, out duration);
            nextTauntAllowed = now + p.tauntCooldown;
            holderAction = DMHolderAction.Taunt;
            holderActionUntil = now + duration;
            holderTurning = true;
        }

        private void TryBeginFeint(Transform target, DM_EnemyEngagementProfile p, float now)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            float duration = 2f * p.feintDistance / Mathf.Max(0.2f, p.holderWalkSpeed * 1.3f) + 0.6f;
            if (!DMEnemyEngagementDirector.TryBeginHolderAction(this, duration))
            {
                nextHolderActionRoll = now + 0.5f;
                return;
            }

            holderFeintPoint = transform.position + toTarget.normalized * p.feintDistance;
            holderFeintPhase = 0;
            holderAction = DMHolderAction.Feint;
            holderActionUntil = now + duration;
        }

        /// <summary>Returns true while a holder action still owns movement this frame.</summary>
        private bool TickHolderAction(Transform target, DM_EnemyEngagementProfile p, float now, Vector3 toAnchor, float anchorDistance)
        {
            if (now >= holderActionUntil)
            {
                CancelHolderAction();
                return false;
            }

            switch (holderAction)
            {
                case DMHolderAction.Sidestep:
                    if (anchorDistance <= 0.1f)
                    {
                        CancelHolderAction();
                        return false;
                    }

                    StepFlat(toAnchor, p.holderWalkSpeed, anchorDistance);
                    return true;

                case DMHolderAction.Taunt:
                    ClearLocomotion();
                    return true;

                case DMHolderAction.Feint:
                    if (holderFeintPhase == 0)
                    {
                        Vector3 toPoint = holderFeintPoint - transform.position;
                        toPoint.y = 0f;
                        if (toPoint.magnitude <= 0.08f)
                            holderFeintPhase = 1;
                        else
                            StepFlat(toPoint, p.holderWalkSpeed * 1.3f, toPoint.magnitude);
                        return true;
                    }

                    if (anchorDistance <= 0.1f)
                    {
                        CancelHolderAction();
                        return false;
                    }

                    StepFlat(toAnchor, p.holderWalkSpeed, anchorDistance);
                    return true;
            }

            CancelHolderAction();
            return false;
        }

        private void CancelHolderAction()
        {
            if (holderTauntBlocking)
            {
                holderTauntBlocking = false;
                combatBridge?.EndBlock();
            }

            holderAction = DMHolderAction.None;
            holderActionUntil = 0f;
        }

        // ── Debug ───────────────────────────────────────────────────────────────────────────────

        public string BuildEngagementDebugReport()
        {
            string report = "AI " + name + " — state " + state + ", awareness " + Awareness + ", role " + engagementState.Role;
            if (engagementState.Ring != null && engagementState.Ring.Target != null)
                report += ", target " + engagementState.Ring.Target.name;
            if (engagementState.Role == DMEngagementRole.Holder)
                report += ", holder action " + holderAction + ", drift(10s) " + DriftLast10Seconds(Time.time).ToString("0") + "°";
            if (brain != null)
                report += " | " + brain.BuildDebugReport();
            return report;
        }

        [ContextMenu("DM/Log Brain + Engagement Report")]
        private void LogEngagementReport()
        {
            Debug.Log(BuildEngagementDebugReport(), this);
        }

        private void DrawEngagementGizmos()
        {
            if (!Application.isPlaying || engagementState.Ring == null)
                return;

            Vector3 head = transform.position + Vector3.up * 2.3f;
            Gizmos.color = engagementState.Role == DMEngagementRole.Engager ? Color.red : new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawSphere(head, 0.12f);

            if (engagementState.Role == DMEngagementRole.Holder && engagementState.HasAnchor)
            {
                Gizmos.DrawWireSphere(engagementState.Anchor, 0.3f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, engagementState.Anchor + Vector3.up * 0.1f);
            }

            Transform target = engagementState.Ring.Target;
            if (target != null)
            {
                DM_EnemyEngagementProfile p = EngagementProfile;
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
                Gizmos.DrawWireSphere(target.position, p.holdRingRadius);
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
                Vector3 f = transform.forward;
                Gizmos.DrawRay(transform.position + Vector3.up, Quaternion.AngleAxis(p.attackFacingConeHalfAngle, Vector3.up) * f * 2f);
                Gizmos.DrawRay(transform.position + Vector3.up, Quaternion.AngleAxis(-p.attackFacingConeHalfAngle, Vector3.up) * f * 2f);
            }
        }
    }
}
