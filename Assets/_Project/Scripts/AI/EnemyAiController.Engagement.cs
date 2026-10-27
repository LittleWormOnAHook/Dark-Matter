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
        private float holderActionStart;
        private float holderActionTotal = 1f;
        private float holderSidestepSpeed = 3f;
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

        /// <summary>Every enabled enemy (registry for the global one-attacker cap and the hard separation).</summary>
        internal static readonly System.Collections.Generic.List<EnemyAiController> ActiveEnemies = new System.Collections.Generic.List<EnemyAiController>(16);

        // shakefix: which code path translated this enemy lately (audit [SlideNoLegs] names the dominant mover).
        private readonly System.Collections.Generic.Dictionary<string, float> moverDist = new System.Collections.Generic.Dictionary<string, float>(8);
        private float moverWindowStart;
        /// <summary>Time of the last crowd / separation push (the motor bridge snaps the leg blend to the push so pushes never slide).</summary>
        internal float LastPushTime = -10f;
        internal Vector3 LastPushDirection;
        internal string CurrentStateName => state.ToString();
        internal bool PushSnapsLegs { get { DM_EnemyEngagementProfile ep = EngagementProfile; return ep == null || ep.pushSnapsLegs; } }

        internal void NoteMover(string tag, float distance)
        {
            float now = Time.time;
            if (tag == "CrowdPush" || tag == "HardSeparation")
                LastPushTime = now;

            Project.Combat.DM_CombatCoreProfile cc = Project.Combat.DM_CombatCoreProfile.Live;
            if (cc == null || !cc.logInterruptsInPlay)
                return;

            if (now - moverWindowStart > 0.6f)
            {
                moverDist.Clear();
                moverWindowStart = now;
            }

            moverDist.TryGetValue(tag, out float d);
            moverDist[tag] = d + distance;
        }

        internal string DominantMover()
        {
            if (Time.time - moverWindowStart > 0.8f || moverDist.Count == 0)
                return "none";
            string best = "none";
            float bestD = 0f;
            var sb = new System.Text.StringBuilder();
            foreach (var kv in moverDist)
            {
                sb.Append(kv.Key).Append(kv.Value.ToString("0.00")).Append("/");
                if (kv.Value > bestD) { bestD = kv.Value; best = kv.Key; }
            }

            return best + "[" + sb.ToString() + "]";
        }

        /// <summary>One attacker at a time across ALL enemy types (melee, android, robot, ranged): false while another enemy on the same target is still in its attack sequence.</summary>
        public Transform CurrentCombatTarget => combat != null ? combat.CurrentTarget : null;

        public bool CanStartAttackGlobal(Transform target)
        {
            return DMEnemyEngagementDirector.CanStartAttackGlobal(this, target);
        }

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
            DM_EnemyBrainProfile brainProfile = EnemyDefinitionOverrides.EffectiveBrainProfile(this, definition);
            if (brainProfile == null) brainProfile = DM_EnemyBrainProfile.Live;
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
            var lb = GetComponent<Project.AI.Invector.EnemyInvectorLoadoutBridge>();
            if (lb != null && lb.ProfileOverrides != null && lb.ProfileOverrides.engagementProfileOverride != null)
                engagementProfileOverride = lb.ProfileOverrides.engagementProfileOverride; // instance first
        }

        public void SetEngagementProfileOverride(DM_EnemyEngagementProfile profile)
        {
            if (profile != null) engagementProfileOverride = profile;
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

            if (combatBridge != null && (combatBridge.IsArmedRangedPreferred() || combatBridge.WouldUseRangedFor(target))) // 2026-10-13 (g): a gun-at-range enemy never holds the melee ring token
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
            brain.Tick(role == DMEngagementRole.Engager, threatInReach, IsInAttackSequence, LastHitByTargetTime,
                combat != null && combat.AttackReadyIn > p.engagerMoveMinCooldown);
        }

        /// <summary>Director eligibility (§5.4 penalties). The current holder keeps the token through stagger.</summary>
        public bool IsTokenEligible(Transform target, DM_EnemyEngagementProfile p, bool asCurrentHolder)
        {
            if (!isActiveAndEnabled || health == null || health.IsDead || target == null)
                return false;

            if (combat == null || combat.CurrentTarget != target)
                return false;

            if (combatBridge != null && (combatBridge.IsArmedRangedPreferred() || combatBridge.WouldUseRangedFor(target))) // 2026-10-13 (g): a gun-at-range enemy never holds the melee ring token
                return false;

            if (ragdollBridge != null && ragdollBridge.IsCorpseRagdolled)
                return false;

            // Slice 2b: a knocked-down enemy gives up its melee token / attack slot (DM_CombatCoreProfile.knockdownReleasesAttackSlot).
            if (DMEnemyKnockdown.IsDown(gameObject))
            {
                Project.Combat.DM_CombatCoreProfile kdProfile = Project.Combat.DM_CombatCoreProfile.Live;
                if (kdProfile == null || kdProfile.knockdownReleasesAttackSlot)
                    return false;
            }

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
            ResetEngagerCooldownMove();
            coneBlockedSince = -1f;
            offscreenReadySince = -1f;
            if (brain != null)
                brain.CancelRetreat();
        }

        /// <summary>EnemyCombat.TryAttack gate: token, hand-off grace, facing cone, off-screen wind-up.</summary>
        public bool CanEngagementAttack(Transform target)
        {
            if (Project.Combat.Turrets.DMTurretHealth.IsTurretTransform(target))
                return true;

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

            // One attacker at a time (per the Phase 4 slot budget): never start a swing while the ring's budget is already mid-swing.
            return DMEnemyEngagementDirector.CanStartSwing(this);
        }

        public void NotifyEngagementAttackBegan()
        {
            coneBlockedSince = -1f;
            offscreenReadySince = -1f;
            if (engagementState.Ring != null)
                DMEnemyEngagementDirector.NotifyAttackBegan(this);
        }

        public void NotifyIntentionalMeleeMiss(float stepDuration)
        {
            brain?.NotifyIntentionalMeleeMiss(stepDuration);
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

            step = EnemyChaseWallSlide.ClampStepToGeometry(transform, step);
            if (step.sqrMagnitude < 0.000001f)
            {
                ClearLocomotion();
                return;
            }

            transform.position += step;
            NoteMover("StepFlat", step.magnitude);
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
                NoteDodgeStrikeSwing();
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

            float minSep = ResolveMinCombatSeparation(target);
            SampleSwingTarget(target);
            if (brain != null && TryBeginDodgeStrike(target, distance, effective, minSep))
                return;

            float standoff = ResolveCombatStandoffFor(target);
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (distance >= minSep && TickEngagerCooldownMove(target, distance, effective, minSep, p))
            {
                creeping = false;
                creepTimer = 0f;
                FaceTowardsRate(target.position, p.engagerTurnRate);
                combat.TryAttack();
                return;
            }

            if (distance < minSep)
            {
                creeping = false;
                creepTimer = 0f;
                StepFlat(-toTarget, walkSpeed * 0.6f, minSep - distance);
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

                float creepStop = Mathf.Max(standoff, minSep);

                // Close in before the swing: attack nearly ready but the target is outside the weapon's real reach -> creep now (no grace).
                float swingGate = ResolveSwingStartDistance(target);
                if (p.swingStartUseWeaponReach && combat.AttackReadyIn <= p.swingCloseInReadySeconds && distance > swingGate + 0.02f &&
                    (combatBridge == null || !combatBridge.HasRangedWeaponEquipped()))
                {
                    creeping = true;
                    creepStop = Mathf.Max(minSep, Mathf.Min(creepStop, swingGate - 0.05f));
                }

                bool missRecovery = combat.WantsForcedMissStep || (brain != null && brain.IsMissRecoveryActive);
                if (missRecovery)
                    creepStop = Mathf.Min(creepStop, combat.ResolveMissStepStopDistance(target));

                if ((creeping && distance > creepStop) || missRecovery)
                {
                    float speedMul = combat.WantsMeleeReposition || missRecovery
                        ? combat.ResolveMissStepSpeedMultiplier()
                        : p.creepSpeedFactor;
                    float speed = walkSpeed * speedMul;
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

        private float nextDodgeRollAllowed;
        private float nextBlockAllowed;

        /// <summary>Single owner of the enemy block: the brain picks it (defendBlockShare); cooldown / duration / per-definition scale come from DM_EnemyBrainProfile "Enemy block".</summary>
        private bool TryBrainBlock(out float blockDuration)
        {
            blockDuration = 0f;
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            if (combatBridge == null || !bp.enableEnemyBlock || Time.time < nextBlockAllowed)
                return false;

            float scale = AppliedDefinition != null ? AppliedDefinition.blockChanceMultiplier : 1f;
            if (scale < 0.999f && Random.value >= scale)
                return false;

            float wanted = bp.blockDurationSeconds > 0f ? bp.blockDurationSeconds : defensiveBlockDuration;
            if (!combatBridge.TryBeginBlock(wanted, out blockDuration))
                return false;

            nextBlockAllowed = Time.time + blockDuration + bp.blockCooldown;
            return true;
        }

        /// <summary>Reactive block (DMEnemyBlock): rolls chance x definition multiplier; front arc, cooldown, not mid-swing / staggered.</summary>
        public bool TryReactiveBlock(Transform attacker, float chance)
        {
            if (combatBridge == null || attacker == null || Time.time < nextBlockAllowed)
                return false;
            if (state == AiState.Defensive || IsInAttackSequence || (combat != null && combat.IsAttacking))
                return false;
            var rag = GetComponent<Project.AI.Invector.EnemyInvectorRagdollBridge>();
            if (rag != null && rag.IsHitStaggerActive)
                return false;
            Vector3 to = attacker.position - transform.position;
            to.y = 0f;
            float cos = Mathf.Cos(Mathf.Clamp(DM_EnemyBrainProfile.Live.blockArcDegrees, 0f, 360f) * 0.5f * Mathf.Deg2Rad);
            if (to.sqrMagnitude > 0.0001f && Vector3.Dot(transform.forward, to.normalized) < cos)
                return false;
            if (Random.value >= chance)
            {
                nextBlockAllowed = Time.time + 0.5f;
                return false;
            }
            if (!TryBrainBlock(out float blockDuration))
                return false;
            EnterState(AiState.Defensive);
            defensiveActionUntil = Time.time + blockDuration;
            return true;
        }

        /// <summary>Drops the guard now (a charged hit or poise break broke it); the Defensive state ends next frame.</summary>
        public void BreakGuard()
        {
            combatBridge?.EndBlock();
            if (state == AiState.Defensive)
                defensiveActionUntil = Time.time;
        }

        /// <summary>Single owner of the enemy dodge roll: chance / cooldown / direction weights come from DM_EnemyBrainProfile "Dodge roll".</summary>
        private bool TryBrainDodgeRoll(Transform target, out float rollDuration)
        {
            rollDuration = 0f;
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            if (combatBridge == null || Time.time < nextDodgeRollAllowed || Random.value >= bp.dodgeRollChance)
                return false;

            if (!combatBridge.TryBeginDodgeRoll(target, out rollDuration))
                return false;

            nextDodgeRollAllowed = Time.time + bp.dodgeRollCooldown;
            BeginDodgeRollMotion(target, combatBridge.LastRollDirection, rollDuration, bp, combatBridge.LastRollUsedBackClip, combatBridge.LastRollClipId);
            return true;
        }

        private void BeginBrainDefend(Transform target, bool preferBlock)
        {
            EnterState(AiState.Defensive);
            if (combatBridge != null)
            {
                if (preferBlock && TryBrainBlock(out float blockDuration))
                {
                    defensiveActionUntil = Time.time + blockDuration;
                    return;
                }

                if (TryBrainDodgeRoll(target, out float rollDuration))
                {
                    defensiveActionUntil = Time.time + rollDuration;
                    return;
                }

                if (!preferBlock && TryBrainBlock(out float fallbackBlock))
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

            if (TickRetreatStuck(target, p, now, anchorDistance)) // 2026-10-24a
            {
                toAnchor = engagementState.Anchor - transform.position;
                toAnchor.y = 0f;
                anchorDistance = toAnchor.magnitude;
            }

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

            if (anchorDistance > HolderArriveRadius)
                StepFlat(toAnchor, p.holderWalkSpeed, anchorDistance - 0.05f);
            else
                ClearLocomotion();

            FaceHolder(target, p);

            if (anchorDistance <= HolderArriveRadius + 0.1f)
                TickHolderIdle(target, p, now, distance);
        }

        private float ResolveHoldRadius(DM_EnemyEngagementProfile p)
        {
            float min = Mathf.Min(p.holdRingRadiusMin, p.holdRingRadiusMax);
            float max = Mathf.Max(p.holdRingRadiusMin, p.holdRingRadiusMax);
            float radius = (brain != null && brain.IsRetreating) || DMEnemyEngagementDirector.IsMoraleBroken(this) ? max : p.holdRingRadius; // 2026-10-25s morale
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
            bool atAnchor = toAnchor.magnitude <= HolderArriveRadius + 0.1f;

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
            // 2026-10-24a: radiusChanged no longer bypasses the cooldown, and a minimum re-pick interval applies to every re-anchor.
            if (now < nextReanchorTime && !farBehind)
                return;
            if (now - engagementState.AnchorTime < RetreatRepickInterval)
                return;

            holderOutsideBandTimer = 0f;
            SetHoldAnchor(target, p, radius, now, countDrift: !playerMoved);
        }

        private void SetHoldAnchor(Transform target, DM_EnemyEngagementProfile p, float radius, float now, bool countDrift, float bearingOffsetDeg = 0f)
        {
            Vector3 center = target.position;
            Vector3 fromTarget = transform.position - center;
            fromTarget.y = 0f;

            if (bearingOffsetDeg != 0f)
                fromTarget = Quaternion.AngleAxis(bearingOffsetDeg, Vector3.up) * fromTarget;

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

            holderSidestepSpeed = Mathf.Max(0.8f, p.sidestepSpeed * MoveVariation());
            float duration = stepDistance / holderSidestepSpeed + p.sidestepDurationPad;
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
            holderActionStart = now;
            holderActionStartPos = transform.position;
            holderActionTotal = stepDistance;
            holderActionUntil = now + duration;
            NoteQuickStep(duration);
            LogCooldownMove("HolderSidestep", sign * arc, 0f, p);
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
            // A sidestep that is still on its way to the anchor gets a short grace so the movement finishes with the step animation instead of stopping early.
            bool sidestepUnfinished = holderAction == DMHolderAction.Sidestep && anchorDistance > 0.1f && now < holderActionUntil + 0.6f;
            if (now >= holderActionUntil && !sidestepUnfinished)
            {
                CancelHolderAction("timeout");
                return false;
            }

            switch (holderAction)
            {
                case DMHolderAction.Sidestep:
                    if (anchorDistance <= 0.1f)
                    {
                        CancelHolderAction("complete");
                        return false;
                    }

                    {
                        float eased = EaseInOutFactor(now - holderActionStart, anchorDistance / Mathf.Max(0.05f, holderActionTotal), p.moveEaseSeconds);
                        StepFlat(toAnchor, holderSidestepSpeed * eased, anchorDistance);
                    }

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

        private Vector3 holderActionStartPos;

        private void CancelHolderAction(string reason = "cancel")
        {
            if (holderAction == DMHolderAction.Sidestep)
            {
                Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
                if (core != null && core.logInterruptsInPlay)
                {
                    Vector3 moved = transform.position - holderActionStartPos;
                    moved.y = 0f;
                    Debug.Log($"[CooldownMoveEnd] {name} kind=HolderSidestep moved={moved.magnitude:0.00} planned={holderActionTotal:0.00} elapsed={Time.time - holderActionStart:0.00} reason={reason}", this);
                }
            }

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
            if (engagementState.AttackSlot)
                report += " (attack slot)";
            if (DMEnemyEngagementDirector.TryGetSlotSummary(this, out int attackers, out int totalSlots, out int enemyCount))
                report += ", attackers " + attackers + "/" + totalSlots + " of " + enemyCount + " enemies";
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

            if (engagementState.AttackSlot)
                Gizmos.DrawWireCube(head + Vector3.up * 0.25f, Vector3.one * 0.2f);

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
