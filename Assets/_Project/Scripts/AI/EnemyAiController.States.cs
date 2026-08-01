using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
using Project.Companions;
using Project.Survival;

namespace Project.AI
{
    // The per-AiState Update*()/Begin*() behaviors (Idle/Wander/Patrol/Investigate/ReturnHome/
    // Chase/Defensive/Attack/Search) plus the small helpers only those states use (chase-stamina
    // pausing, chase-leash checks, attack-entry-state resolution). Split out of EnemyAiController.cs.
    public partial class EnemyAiController
    {
        private float ResolvePatrolSpeed()
        {
            return patrolWalkSpeed > 0f ? patrolWalkSpeed : walkSpeed;
        }

        private float ResolveEffectiveEngageDistance(Transform candidate)
        {
            if (combatBridge != null && combatBridge.IsArmedRangedPreferred())
                return combatBridge.RangedEngageRange;

            return ResolveCombatStandoffFor(candidate);
        }

        // Returns which attack state to enter based on current distance.
        // Ranged enemy within ranged range but beyond melee → Attack directly.
        // Within melee standoff → Defensive. Anything else → Chase.
        private AiState ResolveAttackEntryState(Transform candidate, float distance)
        {
            if (distance <= ResolveCombatStandoffFor(candidate) * 1.05f)
                return AiState.Defensive;

            if (combatBridge != null
                && combatBridge.IsArmedRangedPreferred()
                && distance <= combatBridge.RangedEngageRange)
                return AiState.Attack;

            return AiState.Chase;
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

            if (!TryGetPatrolWorldPoint(patrolIndex, out Vector3 point))
            {
                AdvancePatrolIndex();
                return;
            }

            moveTarget = point;
            MoveTowards(moveTarget, ResolvePatrolSpeed());

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
            float chaseSpeedResolved = ResolveChaseSpeed();

            if (!CanContinueChase(chasePosition))
            {
                GiveUpChaseAndReturnHome();
                return;
            }

            if (maxChaseDuration > 0f && chaseStartedTime > 0f &&
                Time.time - chaseStartedTime >= maxChaseDuration)
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

            float giveUpDistance = stopDistance + 0.75f;
            bool atLastKnown = HasArrived(chasePosition, giveUpDistance, navMeshReady);
            if ((chaseTarget == null || !IsPioneer(chaseTarget)) && !senses.CanSeePlayer() && atLastKnown)
            {
                lostTargetTimer += Time.deltaTime;
                if (lostTargetTimer >= loseTargetDelay)
                {
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

            if (combat.HasLivingTarget() && chaseTarget != null)
            {
                float distance = HorizontalDistance(transform.position, chaseTarget.position);
                float engageDistance = ResolveEffectiveEngageDistance(chaseTarget);
                if (distance <= engageDistance * 1.05f)
                {
                    EnterState(ResolveAttackEntryState(chaseTarget, distance));
                    return;
                }
            }

            moveTarget = combat.HasLivingTarget() && chaseTarget != null
                ? ResolveCombatChasePoint(chaseTarget, chasePosition)
                : chasePosition;

            float arriveStandoff = combat.HasLivingTarget()
                ? ResolveCombatStandoffFor(chaseTarget)
                : stopDistance;

            if (chaseTarget != null)
            {
                MoveTowardsCombatRing(chaseTarget, chaseSpeedResolved, arriveStandoff);
                return;
            }

            if (!TryMoveWithNavMesh(moveTarget, chaseSpeedResolved, arriveStandoff))
                MoveTowards(moveTarget, chaseSpeedResolved, arriveStandoff);
        }

        private void UpdateDefensive()
        {
            Transform target = combat.CurrentTarget;
            if (target == null && HasActiveAggroTarget())
                target = aggroTarget;

            if (target == null || !IsLivingThreat(target))
            {
                GetComponent<Invector.EnemyInvectorCombatBridge>()?.EndBlock();
                EnterState(AiState.Chase);
                return;
            }

            if (defensiveActionUntil > 0f && Time.time >= defensiveActionUntil)
            {
                GetComponent<Invector.EnemyInvectorCombatBridge>()?.EndBlock();
                defensiveActionUntil = 0f;
                EnterState(combat.IsTargetInRange() ? AiState.Attack : AiState.Chase);
                return;
            }

            StopNavMeshMovement();
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
            FaceTowards(target.position);

            if (Time.time < defensiveActionUntil)
                return;

            if (!defensiveActionPending)
            {
                stateTimer = Random.Range(defensivePauseMin, defensivePauseMax);
                defensiveActionPending = true;
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            defensiveActionPending = false;
            BeginDefensiveAction(target);
        }

        private void BeginDefensiveAction(Transform target)
        {
            Invector.EnemyInvectorCombatBridge combatBridge = GetComponent<Invector.EnemyInvectorCombatBridge>();
            float roll = Random.value;
            float attackThreshold = defensiveAttackWeight;
            float blockThreshold = attackThreshold + defensiveBlockWeight;
            float rollThreshold = blockThreshold + defensiveRollWeight;

            if (combatBridge != null && combatBridge.HasRangedWeaponEquipped())
            {
                attackThreshold = 0.70f;
                blockThreshold = 0.85f;
                rollThreshold = 1f;
            }

            if (roll < attackThreshold)
            {
                EnterState(AiState.Attack);
                return;
            }

            if (roll < blockThreshold && combatBridge != null &&
                combatBridge.TryBeginBlock(defensiveBlockDuration, out float blockDuration))
            {
                defensiveActionUntil = Time.time + blockDuration;
                return;
            }

            if (roll < rollThreshold && combatBridge != null &&
                combatBridge.TryBeginDodgeRoll(target, out float rollDuration))
            {
                defensiveActionUntil = Time.time + rollDuration;
                if (!combat.IsTargetInRange())
                    EnterState(AiState.Chase);
                return;
            }

            EnterState(AiState.Attack);
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
            Transform primaryThreat = ResolvePrimaryThreat();
            if (primaryThreat != null && AllowsCombatTarget(primaryThreat))
                combat.SetTarget(primaryThreat);
            else if (HasActiveAggroTarget())
                combat.SetTarget(aggroTarget);
            else
            {
                Transform closestThreat = PickClosestLivingThreat(combat.AttackRange * 2.5f);
                if (closestThreat != null && AllowsCombatTarget(closestThreat))
                    combat.SetTarget(closestThreat);
            }

            TryRetargetToNearbyPioneer();

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

            float distanceToTarget = HorizontalDistance(transform.position, target.position);
            float standoff = ResolveCombatStandoffFor(target);
            bool inRangedEngagement = combatBridge != null
                && combatBridge.IsArmedRangedPreferred()
                && distanceToTarget <= combatBridge.RangedEngageRange;
            bool inStrikeRange = combat.IsInAttackRange(target) || inRangedEngagement;

            // Too close — back off instead of clipping through / pushing the target.
            if (distanceToTarget < standoff * 0.88f)
            {
                MoveTowardsCombatRing(target, walkSpeed * 0.75f, standoff);
                FaceTowards(target.position);
                return;
            }

            if (!inStrikeRange)
            {
                float chaseThreshold = IsPioneer(target)
                    ? combat.AttackRange * pioneerChaseRangeMultiplier
                    : combat.AttackRange * 1.55f;

                if (distanceToTarget > chaseThreshold && chasePlayer && CanChaseTarget(target.position))
                {
                    EnterState(AiState.Chase);
                    return;
                }

                if (IsPioneer(target))
                    MoveTowardsCombatRing(target, walkSpeed * 0.85f, standoff);
                else
                    MoveTowardsCombatRing(target, walkSpeed * 0.75f, standoff);

                FaceTowards(target.position);
                return;
            }

            StopNavMeshMovement();
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
            FaceTowards(target.position);
            combat.TryAttack();
        }

        private void UpdateAggroCombat()
        {
            if (!HasActiveAggroTarget())
                return;

            Transform primary = ResolvePrimaryThreat() ?? aggroTarget;
            if (primary != null && AllowsCombatTarget(primary))
            {
                aggroTarget = primary;
                combat.SetTarget(primary);
            }
            else if (!AllowsCombatTarget(aggroTarget))
            {
                aggroTarget = null;
                aggroUntil = 0f;
                if (state == AiState.Chase || state == AiState.Attack || state == AiState.Defensive)
                    GiveUpChaseAndReturnHome();
                return;
            }

            if (!IsTargetingLivingPioneer() || aggroTarget != combat.CurrentTarget)
                combat.SetTarget(aggroTarget);

            if (IsPioneer(aggroTarget))
                lostTargetTimer = 0f;

            if (combat.HasLivingTarget())
            {
                Transform target = combat.CurrentTarget;
                float distance = HorizontalDistance(transform.position, target.position);
                float engageDistance = ResolveEffectiveEngageDistance(target);
                if (distance <= engageDistance * 1.05f)
                {
                    if (state != AiState.Defensive && state != AiState.Attack)
                        EnterState(ResolveAttackEntryState(target, distance));
                    return;
                }
            }

            if (state != AiState.Chase && CanChaseTarget(aggroTarget.position))
                EnterState(AiState.Chase);
        }

        private void GiveUpChaseAndReturnHome()
        {
            lostTargetTimer = 0f;
            chaseStartedTime = 0f;
            defensiveActionPending = false;
            defensiveActionUntil = 0f;
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

        private bool ShouldInvestigateNoise()
        {
            return !IsStationary && investigateNoise && senses.HasRecentNoise && senses.NoiseAge < 1f;
        }

        private void AdvancePatrolIndex()
        {
            int count = PatrolPointCount;
            if (count == 0)
                return;

            if (patrolMode == EnemyPatrolMode.PingPong && count > 1)
            {
                patrolIndex += patrolDirection;
                if (patrolIndex >= count)
                {
                    patrolIndex = count - 2;
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
                patrolIndex = (patrolIndex + 1) % count;
            }
        }
    }
}
