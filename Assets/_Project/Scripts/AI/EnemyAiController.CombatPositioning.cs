using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
using Project.Combat;
using Project.Companions;
using Project.Survival;

namespace Project.AI
{
    // Combat-ring standoff/positioning math (keeping personal space from the current target while
    // still being close enough to strike), the enemy-vs-enemy separation/avoidance push, the
    // combat-step clamps that prevent shoving through targets/bystanders, and the nearby-pioneer
    // scans used for opportunistic retargeting. Split out of EnemyAiController.cs.
    public partial class EnemyAiController
    {
        /// <summary>
        /// Ranged hold rule: an enemy whose active weapon is ranged-preferred keeps well beyond melee reach of the player. It holds at
        /// <see cref="EnemyInvectorCombatBridge.RangedHoldDistance"/> (EnemyDefinition.rangedHoldDistance, 0 = auto 80% of rangedEngageRange)
        /// and backs off once the player is closer than the retreat distance (kiting archetypes retreat sooner).
        /// </summary>
        internal bool UsesRangedHold(Transform target)
        {
            return combatBridge != null && target != null && IsCombatTargetPlayer(target) && combatBridge.IsArmedRangedPreferred();
        }

        /// <summary>Distance below which a ranged-holding enemy retreats from the player.</summary>
        internal float ResolveRangedRetreatDistance()
        {
            float hold = combatBridge.RangedHoldDistance;
            float frac = combatBridge.RangedRetreatFraction;
            if (brain != null && brain.Archetype == DMEnemyArchetype.Kiter)
                frac = Mathf.Min(0.95f, frac * combatBridge.KiterRetreatBoost);
            return hold * frac;
        }

        private float crowdRoll = float.NaN;

        /// <summary>Per-instance back-off / crowding variety: definition multiplier x (1 + profile spread x this enemy's fixed random roll).</summary>
        internal float CrowdingVariationFactor()
        {
            if (float.IsNaN(crowdRoll)) crowdRoll = UnityEngine.Random.Range(-1f, 1f);
            DM_EnemyEngagementProfile p = EngagementProfile;
            float spread = p != null ? p.crowdingVariationSpread : 0f;
            EnemyDefinition def = AppliedDefinition;
            float defMult = def != null ? def.backoffDistanceMultiplier : 1f;
            return Mathf.Max(0.3f, defMult * (1f + spread * crowdRoll));
        }

        internal float ResolveMinCombatSeparation(Transform target)
        {
            float minSep = minCombatSeparation;
            DM_EnemyEngagementProfile crowd = EngagementProfile;
            if (crowd != null && crowd.applyCrowdingToAllEnemies && crowd.minCombatSeparation > 0.01f)
                minSep = crowd.minCombatSeparation;
            if (target == null || !IsCombatTargetPlayer(target))
                return minSep * CrowdingVariationFactor();

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float result = profile != null
                ? profile.ResolveMinCombatSeparationVsPlayer(minSep)
                : minSep;
            if (UsesRangedHold(target))
                result = Mathf.Max(result, ResolveRangedRetreatDistance());
            return result * CrowdingVariationFactor();
        }

        internal float ResolveCombatStandoffFor(Transform target)
        {
            float minSep = ResolveMinCombatSeparation(target);
            if (combat == null)
                return minSep;
            if (UsesRangedHold(target))
                return Mathf.Max(minSep, combatBridge.RangedHoldDistance);

            float effectiveRange = combat.ResolveEffectiveAttackRange(target);
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float standoffFraction = attackStandoffFraction;
            if (profile != null && profile.enemyMeleeStandoffFraction > 0.001f)
                standoffFraction = profile.enemyMeleeStandoffFraction;

            float standoff = Mathf.Max(minSep, combat.AttackRange * standoffFraction);
            if (IsCombatTargetPlayer(target))
                standoff += playerStandoffBonus;

            // Prefer a contact band inside weapon reach so Meshy-proportion swings (shorter arms)
            // still connect; never orbit farther than we can strike.
            float orbitFactor = profile != null ? profile.enemyMeleeMaxOrbitFactor : 0.86f;
            float maxOrbit = Mathf.Max(minSep, effectiveRange * orbitFactor);
            return Mathf.Min(standoff, maxOrbit);
        }

        private float ResolveCombatStandoff()
        {
            return ResolveCombatStandoffFor(combat != null ? combat.CurrentTarget : null);
        }

        /// <summary>
        /// Chase destination on the combat ring — never path to the target root directly.
        /// </summary>
        private Vector3 ResolveCombatChasePoint(Transform chaseTarget, Vector3 fallback)
        {
            if (chaseTarget == null)
                return fallback;

            float ringDistance = ResolveCombatStandoffFor(chaseTarget);
            Vector3 ringDirection = ResolveCombatRingDirection(chaseTarget);
            Vector3 ringPoint = chaseTarget.position + ringDirection * ringDistance;
            if (TrySampleGround(ringPoint, out float groundY))
                ringPoint.y = groundY;

            return ringPoint;
        }

        private Vector3 ResolveCombatRingDirection(Transform target)
        {
            if (target == null)
                return -transform.forward;

            Vector3 toSelf = transform.position - target.position;
            toSelf.y = 0f;
            if (toSelf.sqrMagnitude < 0.01f)
                toSelf = -transform.forward;
            else
                toSelf.Normalize();

            // Phase 3 spacing: the per-enemy slot rotation re-derived from the live bearing every frame was the
            // main cause of the constant circling (Spacing plan §2.1). Legacy only, behind the profile switch.
            DM_EnemyEngagementProfile engagementProfile = DM_EnemyEngagementProfile.Live;
            if (engagementProfile != null && engagementProfile.legacyRingSlotRotation)
                toSelf = Quaternion.AngleAxis(combatRingSlotAngle, Vector3.up) * toSelf;

            Vector3 separation = ComputeEnemySeparationOffset();
            if (separation.sqrMagnitude > 0.0001f)
            {
                toSelf += separation;
                toSelf.y = 0f;
                if (toSelf.sqrMagnitude > 0.01f)
                    toSelf.Normalize();
            }

            return toSelf;
        }

        private Vector3 cachedSeparationDir;
        private float cachedSeparationDepth;

        private float CrowdingDepthScale(float depth)
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            float gain = p != null ? p.crowdingDepthGain : 0f;
            return 1f + gain * Mathf.Clamp01(depth);
        }

        /// <summary>
        /// Hard minimum separation (DM_EnemyEngagementProfile.hardEnemySeparation): any two enemies closer than this are pushed apart every frame
        /// at a speed that keeps the legs animating (measured velocity drives locomotion). Skipped during rolls, staggers, swings and dodge strikes.
        /// </summary>
        private void TickHardSeparation()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            if (p == null || p.hardEnemySeparation <= 0.01f || IsStationary || dodgeRollUntil > 0f || dodgeStrikeUntil > 0f)
                return;
            if (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.IsCorpseRagdolled))
                return;
            bool swinging = IsInAttackSequence;

            Vector3 me = transform.position;
            Vector3 push = Vector3.zero;
            float deepest = 0f;
            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                EnemyAiController o = ActiveEnemies[i];
                if (o == null || o == this || !o.isActiveAndEnabled || o.IsStationary)
                    continue;
                if (o.health != null && o.health.IsDead)
                    continue;

                Vector3 d = me - o.transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist >= p.hardEnemySeparation)
                    continue;

                d = dist < 0.01f ? transform.right * ((GetHashCode() & 1) == 0 ? 1f : -1f) : d / dist;
                float overlap = p.hardEnemySeparation - dist;
                push += d * overlap;
                deepest = Mathf.Max(deepest, overlap);
            }

            if (deepest <= 0.001f || push.sqrMagnitude < 0.000001f)
                return;
            if (swinging && deepest < p.hardEnemySeparation * 0.5f)
                return; // a swinging enemy only gives way when the overlap is deep

            float speed = Mathf.Clamp(deepest * 6f, 0.9f, p.hardSeparationMaxSpeed);
            Vector3 step = push.normalized * Mathf.Min(speed * Time.deltaTime, deepest);
            step = ClampStepAwayFromNonTargetPlayer(step);
            transform.position += step;
            LastPushDirection = step;
            NoteMover("HardSeparation", step.magnitude);
        }

        /// <summary>Waiting enemies (Hold / Holder, or standing between swings) get nudged apart directly; the push grows with overlap depth.</summary>
        private void TickCrowdingDrift()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            if (p == null || !p.applyCrowdingToAllEnemies || p.waitingCrowdingStrength <= 0.01f || IsStationary)
                return;

            if (ragdollBridge != null && ragdollBridge.IsHitStaggerActive)
                return; // a staggered body is not animated by locomotion: no legs, so no sliding pushes

            bool waiting = state == AiState.Hold;
            bool betweenSwings = (state == AiState.Attack || state == AiState.Defensive) && combat != null && !combat.IsAttacking;
            if (!waiting && !betweenSwings)
                return;

            ComputeEnemySeparationOffset();
            if (cachedSeparationDepth <= 0.001f || cachedSeparationDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 step = cachedSeparationDir * (p.waitingCrowdingStrength * CrowdingDepthScale(cachedSeparationDepth) * Time.deltaTime);
            step.y = 0f;
            transform.position += step;
            LastPushDirection = step;
            NoteMover("CrowdPush", step.magnitude);
        }

        private Vector3 ComputeEnemySeparationOffset()
        {
            float avoidRadius = enemyAvoidanceRadius, avoidStrength = enemyAvoidanceStrength;
            DM_EnemyEngagementProfile crowd = EngagementProfile;
            if (crowd != null && crowd.applyCrowdingToAllEnemies) { avoidRadius = crowd.enemyAvoidanceRadius; avoidStrength = crowd.enemyAvoidanceStrength; }
            avoidRadius *= CrowdingVariationFactor();
            if (avoidRadius <= 0.01f || avoidStrength <= 0.01f)
                return Vector3.zero;

            if (state != AiState.Chase && state != AiState.Attack && state != AiState.Defensive && state != AiState.Hold)
                return Vector3.zero;

            if (((Time.frameCount + perfPhase) % 3) != 0)
                return cachedSeparationOffset;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int mask = enemyLayer >= 0 ? 1 << enemyLayer : Physics.AllLayers;
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                avoidRadius,
                AvoidanceHits,
                mask,
                QueryTriggerInteraction.Ignore);

            Vector3 push = Vector3.zero;
            float maxDepth = 0f;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = AvoidanceHits[i];
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 away = transform.position - hit.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance <= 0.01f || distance >= avoidRadius)
                    continue;

                float depth = 1f - distance / avoidRadius;
                maxDepth = Mathf.Max(maxDepth, depth);
                push += away.normalized * depth;
            }

            if (push.sqrMagnitude < 0.0001f)
            {
                cachedSeparationOffset = Vector3.zero;
                cachedSeparationDir = Vector3.zero;
                cachedSeparationDepth = 0f;
                return cachedSeparationOffset;
            }

            cachedSeparationDir = push.normalized;
            cachedSeparationDepth = maxDepth;
            cachedSeparationOffset = cachedSeparationDir * (avoidStrength * CrowdingDepthScale(maxDepth));
            return cachedSeparationOffset;
        }

        /// <summary>
        /// Move toward a ring point at <paramref name="ringDistance"/> from the target,
        /// preserving a personal-space buffer instead of walking into the target root.
        /// </summary>
        private void MoveTowardsCombatRing(Transform target, float speed, float ringDistance)
        {
            if (target == null)
                return;

            Vector3 targetPos = target.position;
            float currentDistance = HorizontalDistance(transform.position, targetPos);
            if (currentDistance <= ringDistance * 1.05f)
            {
                ClearLocomotion();
                return;
            }

            Vector3 ringDirection = ResolveCombatRingDirection(target);
            Vector3 ringPoint = targetPos + ringDirection * ringDistance;
            MoveTowards(ringPoint, speed, 0.18f);
        }

        /// <summary>
        /// Blocks chase/attack steps that would shrink distance below the combat buffer.
        /// </summary>
        private Vector3 ClampCombatStepTowardTarget(Vector3 step)
        {
            if (step.sqrMagnitude < 0.000001f || combat == null || !combat.HasLivingTarget())
                return step;

            if (state != AiState.Attack && state != AiState.Chase)
                return step;

            Transform liveTarget = combat.CurrentTarget;
            if (liveTarget == null)
                return step;

            float minSep = ResolveCombatStandoffFor(liveTarget);
            float currentDist = HorizontalDistance(transform.position, liveTarget.position);
            float distAfter = HorizontalDistance(transform.position + step, liveTarget.position);
            if (distAfter < minSep * 0.98f && distAfter < currentDist - 0.001f)
                step = Vector3.zero;

            step = ApplyEnemySeparationToStep(step);
            return ClampStepAwayFromNonTargetPlayer(step);
        }

        private Vector3 ApplyEnemySeparationToStep(Vector3 step)
        {
            if (step.sqrMagnitude < 0.000001f)
                return step;

            if (state != AiState.Chase && state != AiState.Attack && state != AiState.Defensive)
                return step;

            Vector3 separation = ComputeEnemySeparationOffset();
            if (separation.sqrMagnitude < 0.0001f)
                return step;

            Vector3 adjusted = step + separation * Time.deltaTime;
            adjusted.y = 0f;
            return adjusted.sqrMagnitude > 0.000001f ? adjusted : step;
        }

        /// <summary>
        /// While brawling a pioneer, do not walk through the bystander player capsule.
        /// </summary>
        private Vector3 ClampStepAwayFromNonTargetPlayer(Vector3 step)
        {
            if (step.sqrMagnitude < 0.000001f || combat == null)
                return step;

            Transform liveTarget = combat.CurrentTarget;
            if (liveTarget != null && IsCombatTargetPlayer(liveTarget))
                return step;

            Transform player = playerTarget;
            if (player == null && senses != null)
                player = senses.GetVisiblePlayerTarget();

            if (player == null)
                return step;

            SurvivalStats stats = player.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.IsDead || stats.HasEnemyCombatImmunity)
                return step;

            float playerBuffer = ResolveCombatStandoffFor(player);
            float currentDist = HorizontalDistance(transform.position, player.position);
            float distAfter = HorizontalDistance(transform.position + step, player.position);
            if (distAfter < playerBuffer * 0.98f && distAfter < currentDist - 0.001f)
                return Vector3.zero;

            return step;
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
            CompanionRosterBridge bridge = ResolveCompanionRosterBridge();
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

                CompanionHealth health = agent.Health;
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

            // Player is the higher-priority threat when provoked or when allowed and closer than pioneers.
            bool playerProvoked = HasActivePlayerAggro();
            if (playerTarget != null && AllowsCombatTarget(playerTarget) && !HasPioneerDamageAggro())
            {
                SurvivalStats playerStats = playerTarget.GetComponent<SurvivalStats>();
                if (playerStats == null || !playerStats.IsDead)
                {
                    float distance = HorizontalDistance(transform.position, playerTarget.position);
                    if (playerProvoked || ShouldEngagePlayer(playerTarget))
                    {
                        if (distance <= closestDistance)
                        {
                            closest = playerTarget;
                            closestDistance = distance;
                        }
                    }
                }
            }

            CompanionRosterBridge bridge = ResolveCompanionRosterBridge();
            IReadOnlyList<PioneerCompanionAgent> companions = bridge != null ? bridge.ActiveCompanions : null;
            if (companions != null)
            {
                for (int i = 0; i < companions.Count; i++)
                {
                    PioneerCompanionAgent agent = companions[i];
                    if (agent == null)
                        continue;

                    CompanionHealth health = agent.Health;
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
            CompanionRosterBridge bridge = ResolveCompanionRosterBridge();
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

                CompanionHealth health = agent.Health;
                if (health != null && health.IsDead)
                    continue;

                float distance = HorizontalDistance(transform.position, agent.transform.position);
                if (distance > pioneerRetargetRadius)
                    continue;

                return agent.transform;
            }

            return null;
        }

        private CompanionRosterBridge ResolveCompanionRosterBridge()
        {
            if (companionRosterBridge != null)
                return companionRosterBridge;

            companionRosterBridge = CompanionRosterBridge.Instance;
            return companionRosterBridge;
        }
    }
}
