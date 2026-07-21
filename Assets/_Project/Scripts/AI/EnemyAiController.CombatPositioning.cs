using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
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
        internal float ResolveCombatStandoffFor(Transform target)
        {
            if (combat == null)
                return minCombatSeparation;

            float effectiveRange = combat.ResolveEffectiveAttackRange(target);
            float standoff = Mathf.Max(minCombatSeparation, combat.AttackRange * attackStandoffFraction);
            if (IsCombatTargetPlayer(target))
                standoff += playerStandoffBonus;

            // Never orbit farther than we can strike — that was the shove-without-attacking failure mode.
            return Mathf.Min(standoff, effectiveRange * 0.9f);
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

        private Vector3 ComputeEnemySeparationOffset()
        {
            if (enemyAvoidanceRadius <= 0.01f || enemyAvoidanceStrength <= 0.01f)
                return Vector3.zero;

            if (state != AiState.Chase && state != AiState.Attack && state != AiState.Defensive)
                return Vector3.zero;

            if (((Time.frameCount + perfPhase) % 3) != 0)
                return cachedSeparationOffset;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int mask = enemyLayer >= 0 ? 1 << enemyLayer : Physics.AllLayers;
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                enemyAvoidanceRadius,
                AvoidanceHits,
                mask,
                QueryTriggerInteraction.Ignore);

            Vector3 push = Vector3.zero;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = AvoidanceHits[i];
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 away = transform.position - hit.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance <= 0.01f || distance >= enemyAvoidanceRadius)
                    continue;

                push += away.normalized * (1f - distance / enemyAvoidanceRadius);
            }

            if (push.sqrMagnitude < 0.0001f)
            {
                cachedSeparationOffset = Vector3.zero;
                return cachedSeparationOffset;
            }

            cachedSeparationOffset = push.normalized * enemyAvoidanceStrength;
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
    }
}
