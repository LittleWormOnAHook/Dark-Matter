using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
using Project.Companions;
using Project.Survival;

namespace Project.AI
{
    // NavMeshAgent plumbing (enable/disable/configure/sample/path) and the raw transform-based
    // locomotion fallback (MoveTowards/FaceTowards/ground snapping) used by every state's Update
    // method. Split out of EnemyAiController.cs.
    public partial class EnemyAiController
    {
        private void ClearLocomotion()
        {
            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
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

        private void ConfigureNavMeshAgent()
        {
            navMeshReady = false;
            if (!useNavMeshForChaseAndWander)
            {
                RemoveNavMeshAgentComponent();
                return;
            }

            EnsureNavMeshAgent();
        }

        private void RemoveNavMeshAgentComponent()
        {
            navMeshReady = false;
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();

            if (navAgent == null)
                return;

            SafeStopAgent();
            Destroy(navAgent);
            navAgent = null;
        }

        private void EnsureNavMeshAgent()
        {
            navMeshReady = false;
            if (!useNavMeshForChaseAndWander)
            {
                RemoveNavMeshAgentComponent();
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
            navAgent.angularSpeed = DMILocomotionFacing.ToAgentAngularSpeed(turnSpeed);
            navAgent.acceleration = 8f;
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

            float moveScale = 1f;
            if (navAgent.desiredVelocity.sqrMagnitude > 0.01f)
                moveScale = DMILocomotionFacing.FacingMoveScale(transform, navAgent.desiredVelocity, ResolveFacingMoveScaleFloor());

            navAgent.speed = speed * moveScale;
            navAgent.angularSpeed = Mathf.Max(DMILocomotionFacing.ToAgentAngularSpeed(turnSpeed), ResolveTurnDegreesPerSecond() > LegacyTurnDegreesPerSecond() + 0.01f ? ResolveTurnDegreesPerSecond() : 0f);
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

        // Editor audit observations; no steering authority or per-frame allocation.
#if UNITY_EDITOR
        private Vector3 auditDesiredStep, auditResolvedStep, auditRootAfterStep;
        private int auditStepFrame = -1;
#endif
        private Vector3 wallRouteGoal, wallRoutePrevious;
        private float wallRouteBestDistance, wallRoutePath, wallRouteNoProgress, wallRouteLastSample;
        private float wallRouteRecoveryCooldown;
        internal bool WallRouteRecoveryRequested;
        internal int WallRouteRecoveryCount;
        private readonly List<Vector3> wallEscapePoints = new List<Vector3>(4);
        private Vector3 wallEscapeGoal;
        private float wallEscapeUntil;

        private void ResetWallRouteProgress()
        {
            wallRouteLastSample = -1f;
            WallRouteRecoveryRequested = false;
            wallEscapePoints.Clear();
        }

        private void TrackWallRouteProgress(Vector3 goal)
        {
            if (wallEscapePoints.Count > 0) return;
            float distance = HorizontalDistance(transform.position, goal);
            // Moving combat ring points are new routes, not evidence of circling.
            if (wallRouteLastSample < 0f || Time.time - wallRouteLastSample > 0.25f ||
                HorizontalDistance(goal, wallRouteGoal) > 0.75f)
            {
                wallRouteGoal = goal;
                wallRoutePrevious = transform.position;
                wallRouteBestDistance = distance;
                wallRoutePath = wallRouteNoProgress = 0f;
                WallRouteRecoveryRequested = false;
            }
            else
            {
                wallRoutePath += HorizontalDistance(transform.position, wallRoutePrevious);
                if (distance < wallRouteBestDistance - 0.35f)
                {
                    wallRouteBestDistance = distance;
                    wallRouteNoProgress = wallRoutePath = 0f;
                    WallRouteRecoveryRequested = false;
                }
                else wallRouteNoProgress += Time.deltaTime;

                var profile = DM_EnemyBrainProfile.Live;
                float patience = Mathf.Max(4f, (profile != null ? profile.chaseWallBufferStuckSeconds : 1.5f) * 3f);
                if (wallRouteNoProgress >= patience && wallRoutePath > Mathf.Max(6f, distance) &&
                    Time.time >= wallRouteRecoveryCooldown && ChaseWallFollowSideSign != 0)
                    WallRouteRecoveryRequested = true;
            }
            wallRoutePrevious = transform.position;
            wallRouteLastSample = Time.time;
        }

        internal void NoteWallRouteRecovery()
        {
            WallRouteRecoveryRequested = false;
            WallRouteRecoveryCount++;
            wallRoutePath = wallRouteNoProgress = 0f;
            wallRouteBestDistance = HorizontalDistance(transform.position, wallRouteGoal);
            wallRouteRecoveryCooldown = Time.time + 8f;
        }

        private void ApplyWallRouteEscape(ref Vector3 target)
        {
            if (wallEscapePoints.Count > 0 && (Time.time >= wallEscapeUntil ||
                HorizontalDistance(target, wallEscapeGoal) > 0.75f))
            {
                wallEscapePoints.Clear();
                ResetWallRouteProgress();
            }
            if (WallRouteRecoveryRequested && ChaseWallSurface != null && wallEscapePoints.Count == 0)
            {
                if (EnemyChaseWallSlide.TryBuildLocalEscape(transform, ChaseWallSurface, target,
                    DM_EnemyBrainProfile.Live, wallEscapePoints))
                {
                    wallEscapeGoal = target;
                    wallEscapeUntil = Time.time + 12f;
                    NoteWallRouteRecovery();
                    ChaseWallFollowSideSign = 0;
                    chaseWallLastContactNormal = Vector3.zero;
                    ChaseWallSurfaceNormal = Vector3.zero;
                }
            }
            while (wallEscapePoints.Count > 0 && HorizontalDistance(transform.position, wallEscapePoints[0]) < 0.55f)
                wallEscapePoints.RemoveAt(0);
            if (wallEscapePoints.Count > 0) target = wallEscapePoints[0];
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

            TickChaseStuckRecovery(target, arriveDistance);
            TrackWallRouteProgress(target);
            ApplyWallRouteEscape(ref target);
            if (wallEscapePoints.Count > 0) arriveDistance = Mathf.Min(arriveDistance, 0.2f);
            TryApplyWallRetreatSidestepTarget(ref target);

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
                float moveScale = RecentWallBufferContact
                    ? 1f
                    : DMILocomotionFacing.FacingMoveScale(transform, toTarget, ResolveFacingMoveScaleFloor());
                float scaledSpeed = speed * moveScale;
                Vector3 step = toTarget.normalized * (scaledSpeed * Time.deltaTime);
                float maxStep = distance - arriveDistance;
                if (step.magnitude > maxStep)
                    step = toTarget.normalized * maxStep;

                step = ClampCombatStepTowardTarget(step);
#if UNITY_EDITOR
                auditDesiredStep = step;
                auditStepFrame = Time.frameCount;
#endif
                DM_EnemyBrainProfile wallProfile = DM_EnemyBrainProfile.Live;
                float minWallProgress = wallProfile != null
                    ? Mathf.Max(0.0005f, wallProfile.chaseWallMinProgress)
                    : 0.002f;
                EnemyChaseWallSlide.AdjustResult wallMove = EnemyChaseWallSlide.AdjustStep(
                    transform,
                    step,
                    flatTarget,
                    wallProfile,
                    this);
                step = wallMove.Step;
                // Ease into the resolved avoidance direction, rather than maintaining
                // full speed sideways while the body is still turning toward it.
                // Do not slow large depenetration/clearance corrections.
                if (wallMove.InWallBufferContact && step.sqrMagnitude > 0.000001f &&
                    step.magnitude <= speed * Time.deltaTime * 1.1f)
                    step *= DMILocomotionFacing.FacingMoveScale(transform, step, ResolveFacingMoveScaleFloor());
                step = LimitWanderStepToTerritory(step);
#if UNITY_EDITOR
                auditResolvedStep = step;
                auditRootAfterStep = transform.position + step;
#endif
                RecentWallBufferContact = wallMove.InWallBufferContact;
                if (wallMove.InWallBufferContact && UsesChaseWallSlideLocomotion())
                {
                    float ringLock = wallProfile != null ? wallProfile.chaseWallRingLockSeconds : 1.05f;
                    combatRingPointLockUntil = Time.time + ringLock;
                    cachedCombatRingPoint = flatTarget;
                    combatRingDirLockUntil = Time.time + ringLock;
                }

                UpdateVirtualSteerSnapshot(flatTarget, toTarget);
                float minProgressSq = minWallProgress * minWallProgress;
                TickWallBufferStuckRetreat(wallMove, step.sqrMagnitude, minWallProgress);

                if (step.sqrMagnitude < minProgressSq)
                {
                    ClearLocomotion();
                    bool faceSlide = wallProfile != null && wallProfile.chaseWallFollowFaceSlide;
                    if (faceSlide && wallMove.PreferFaceSlideDirection && wallMove.FaceDirection.sqrMagnitude > 0.0001f)
                        ApplyWallFollowFacing(wallMove.FaceDirection, fastTurn: false);
                    else if (toTarget.sqrMagnitude > 0.01f && !wallMove.InWallBufferContact)
                        FaceTowards(flatTarget);
                    return;
                }

                transform.position += step;
                NoteMover("ChaseMove", step.magnitude);
                currentLocomotionSpeed = step.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                currentLocalMoveDirection = transform.InverseTransformDirection(step.normalized);
                if (wallMove.InWallBufferContact && Mathf.Abs(currentLocalMoveDirection.z) < 0.2f)
                    currentLocalMoveDirection.x = 0f;

                if (wallMove.PreferFaceSlideDirection && wallMove.FaceDirection.sqrMagnitude > 0.0001f)
                    ApplyWallFollowFacing(wallMove.FaceDirection, fastTurn: true);
                else if (toTarget.sqrMagnitude > 0.01f && !wallMove.InWallBufferContact)
                    FaceTowards(flatTarget);
                return;
            }
            else
            {
                chaseWallBlockedFrames = 0;
                RecentWallBufferContact = false;
                ClearLocomotion();
                if (EnemyChaseWallSlide.TryExtractFromOverlap(transform, DM_EnemyBrainProfile.Live, out Vector3 unstick, out Vector3 unstickNormal))
                {
                    transform.position += unstick;
                    RecentWallBufferContact = true;
                    chaseWallLastContactNormal = unstickNormal;
                }
            }

            if (toTarget.sqrMagnitude > 0.01f)
                FaceTowards(flatTarget);
        }

        // 24ac: generic chase stall detector (rock wedge, slope push-back, wall-follow oscillation): position-based, not step-based.
        private Vector3 chaseStuckSamplePos;
        private float chaseStuckSampleTime = -1f;
        private int chaseStuckSide = 1;
        private int chaseStuckFails;

        private void TickChaseStuckRecovery(Vector3 target, float arriveDistance)
        {
            DM_EnemyBrainProfile p = DM_EnemyBrainProfile.Live;
            if (p == null || !p.enableChaseStuckRecovery || !UsesChaseWallSlideLocomotion() || Time.time < wallRetreatSidestepUntil)
            {
                chaseStuckSampleTime = -1f;
                return;
            }
            Vector3 toGoal = target - transform.position; toGoal.y = 0f;
            if (toGoal.magnitude <= arriveDistance + 1f) { chaseStuckSampleTime = -1f; return; }
            if (chaseStuckSampleTime < 0f || Time.time - chaseStuckSampleTime > p.chaseStuckSeconds * 3f)
            {
                chaseStuckSamplePos = transform.position; chaseStuckSampleTime = Time.time; return;
            }
            if (Time.time - chaseStuckSampleTime < p.chaseStuckSeconds) return;
            float moved = HorizontalDistance(transform.position, chaseStuckSamplePos);
            chaseStuckSamplePos = transform.position; chaseStuckSampleTime = Time.time;
            if (moved >= p.chaseStuckMinMove) { chaseStuckFails = 0; return; }
            chaseStuckFails++;
            if (p.chaseStuckHardAfter > 0 && chaseStuckFails > p.chaseStuckHardAfter)
            {
                string blockers = EnemyChaseWallSlide.DescribeOverlaps(transform, p);
                if (EnemyChaseWallSlide.TryFindFreeSpot(transform, p, toGoal, p.chaseStuckHardMaxRadius,
                        q => TrySampleGround(q, out float gy) ? (true, gy) : (false, 0f), out Vector3 free))
                {
                    if (p.chaseStuckLog)
                        Debug.Log("[EnemyStuck] '" + name + "' HARD unstick after " + chaseStuckFails + " fails, moved " + HorizontalDistance(transform.position, free).ToString("F2") + "m; overlapping: " + blockers, this);
                    transform.position = free;
                    chaseStuckSamplePos = free;
                    chaseStuckFails = 0;
                    ResetWallRouteProgress();
                    RecentWallBufferContact = false;
                    return;
                }
                if (p.chaseStuckLog)
                    Debug.Log("[EnemyStuck] '" + name + "' no free spot within " + p.chaseStuckHardMaxRadius + "m; overlapping: " + blockers, this);
            }
            Vector3 fwd = toGoal.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, fwd) * chaseStuckSide;
            chaseStuckSide = -chaseStuckSide; // alternate if it stalls again
            Vector3 nudge = side * p.chaseStuckSidestepMeters + fwd * 0.4f;
            if (p.chaseStuckLog)
                Debug.Log("[EnemyStuck] '" + name + "' state=" + state + " moved=" + moved.ToString("F2") + "m in " + p.chaseStuckSeconds.ToString("F1") + "s, goal " + toGoal.magnitude.ToString("F1") + "m -> sidestep " + (chaseStuckSide < 0 ? "right" : "left") + " (wallContact=" + RecentWallBufferContact + ") overlapping: " + EnemyChaseWallSlide.DescribeOverlaps(transform, p), this);
            BeginWallRetreatSidestep(nudge);
            wallRetreatCooldownUntil = Time.time + p.chaseStuckSeconds;
        }

        private void ApplyWallFollowFacing(Vector3 worldDirection, bool fastTurn)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            float turnRate = profile != null ? profile.chaseWallBlockedTurnRate : 240f;
            if (!fastTurn)
                turnRate = Mathf.Min(turnRate, 200f);

            Vector3 wishForward = worldDirection.normalized;
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f && Vector3.Dot(flatForward.normalized, wishForward) > 0.992f)
                return;

            DMILocomotionFacing.FaceTowardDegrees(transform, transform.position + wishForward, turnRate);
        }

        private Vector3 LimitWanderStepToTerritory(Vector3 step)
        {
            if (state != AiState.Wander || step.sqrMagnitude < 0.000001f) return step;
            Vector3 from = transform.position - homePosition; from.y = 0f;
            float radius = Mathf.Max(0.5f, wanderRadius);
            if ((from + step).sqrMagnitude <= radius * radius) return step;

            // Limit along the already collision-checked segment; projecting the end
            // onto the circle would introduce an unchecked sideways displacement.
            float a = step.sqrMagnitude;
            float b = Vector3.Dot(from, step);
            float c = from.sqrMagnitude - radius * radius;
            float fraction = c > 0f ? (b < 0f ? 1f : 0f)
                : Mathf.Clamp01((-b + Mathf.Sqrt(Mathf.Max(0f, b * b - a * c))) / a);
            CommitNewWanderDestination();
            ChaseWallFollowSideSign = 0;
            ChaseWallFollowSideLockUntil = 0f;
            ChaseWallSurface = null;
            ChaseWallSurfaceNormal = Vector3.zero;
            return step * fraction;
        }

        private bool UsesChaseWallSlideLocomotion()
        {
            return state == AiState.Chase || state == AiState.Investigate || state == AiState.Search;
        }

        private bool TryApplyWallRetreatSidestepTarget(ref Vector3 target)
        {
            if (Time.time > wallRetreatSidestepUntil)
                return false;

            target = wallRetreatSidestepTarget;
            return true;
        }

        private void BeginWallRetreatSidestep(Vector3 nudge)
        {
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            float seconds = profile != null ? profile.chaseWallRetreatSidestepSeconds : 0.65f;
            nudge.y = 0f;
            if (nudge.sqrMagnitude < 0.0001f || seconds <= 0.02f)
                return;

            wallRetreatSidestepTarget = transform.position + nudge.normalized * (nudge.magnitude + 0.35f);
            if (TrySampleGround(wallRetreatSidestepTarget, out float groundY))
                wallRetreatSidestepTarget.y = groundY;
            wallRetreatSidestepUntil = Time.time + seconds;
        }

        private void TickWallBufferStuckRetreat(
            EnemyChaseWallSlide.AdjustResult wallMove,
            float stepSqrMag,
            float minProgress)
        {
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            if (profile == null || !profile.enableChaseWallSlide ||
                (!UsesChaseWallSlideLocomotion() && state != AiState.Wander && state != AiState.Patrol && state != AiState.ReturnHome))
                return;

            bool blocked = stepSqrMag < minProgress * minProgress;
            // Touching a wall while making progress is normal wall-follow, not a stall.
            if (blocked)
                chaseWallBufferStuckTimer += Time.deltaTime;
            else
                chaseWallBufferStuckTimer = Mathf.Max(0f, chaseWallBufferStuckTimer - Time.deltaTime * 2f);

            float limit = profile.chaseWallBufferStuckSeconds;
            if (limit <= 0.05f || chaseWallBufferStuckTimer < limit)
                return;

            if (Time.time < wallRetreatSidestepUntil || Time.time < wallRetreatCooldownUntil)
                return;

            if (EnemyChaseWallSlide.TryBuildRetreatNudge(transform, profile, this, out Vector3 nudge))
            {
                BeginWallRetreatSidestep(nudge);
                wallRetreatCooldownUntil = Time.time + Mathf.Max(limit * 1.5f, 0.85f);
            }

            chaseWallBufferStuckTimer = 0f;
            chaseWallBlockedFrames = 0;
            ChaseWallStutterFrames = 0;
        }

        /// <summary>
        /// Turn rate outside the Attack state: while chasing / defending / searching with a combat target and the engagement director on,
        /// use the engagement profile's nonAttackTurnRate (a player who moves behind the enemy is faced in under ~1 s). Otherwise legacy turnSpeed.
        /// </summary>
        private float ResolveTurnDegreesPerSecond()
        {
            float legacy = LegacyTurnDegreesPerSecond();
            if ((state == AiState.Chase || state == AiState.Defensive || state == AiState.Search) &&
                DMEnemyEngagementDirector.IsEnabled && combat != null && combat.HasLivingTarget())
            {
                DM_EnemyEngagementProfile p = EngagementProfile;
                if (p != null)
                    return Mathf.Max(legacy, p.nonAttackTurnRate);
            }

            return legacy;
        }

        private void FaceTowards(Vector3 worldPosition)
        {
            float rate = ResolveTurnDegreesPerSecond();
            if (rate > LegacyTurnDegreesPerSecond() + 0.01f)
                DMILocomotionFacing.FaceTowardDegrees(transform, worldPosition, rate);
            else
                DMILocomotionFacing.FaceTowardDegrees(transform, worldPosition, LegacyTurnDegreesPerSecond());
        }

        /// <summary>Legacy turnSpeed -> deg/s using the engagement profile's clamp (default 360, was hard-coded 220).</summary>
        private float LegacyTurnDegreesPerSecond()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            float clampMax = p != null ? Mathf.Max(220f, p.turnRateClampMax) : 220f;
            return DMILocomotionFacing.ToDegreesPerSecond(turnSpeed, clampMax);
        }

        /// <summary>Lowest move-speed fraction while turned away from the move direction (profile, was 0.18).</summary>
        private float ResolveFacingMoveScaleFloor()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            return p != null ? Mathf.Clamp(p.facingMoveScaleFloor, 0.1f, 1f) : 0.18f;
        }

        /// <summary>
        /// Standing calm/combat states skip ground snap so terrain ray noise and foot-offset
        /// fighting do not bob the root every other LateUpdate frame (idle jitter).
        /// </summary>
        private bool ShouldApplyGroundSnap()
        {
            if (currentLocomotionSpeed > 0.05f)
                return true;

            switch (state)
            {
                case AiState.Wander:
                case AiState.Patrol:
                case AiState.Chase:
                case AiState.Investigate:
                case AiState.Search:
                case AiState.ReturnHome:
                    return true;
                default:
                    return false;
            }
        }

        private void SnapToGround()
        {
            if (!TrySampleGround(transform.position, out float groundY))
                return;

            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.y - groundY) < 0.045f)
                return;

            pos.y = groundY;
            transform.position = pos;
        }

        private bool TrySampleGround(Vector3 worldPosition, out float groundY)
        {
            return EnemyGroundUtility.TryGetGroundY(worldPosition, out groundY, groundOffset);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// Virtual intent polyline (memory only): root → fan / wish hint → move target. Caller supplies list.
        /// </summary>
        public int FillVirtualSteerPolyline(List<Vector3> buffer)
        {
            if (buffer == null)
                return 0;

            buffer.Clear();
            Vector3 root = transform.position;
            buffer.Add(root);

            if (!VirtualSteerActive)
                return 1;

            if ((VirtualSteerHint - root).sqrMagnitude > 0.01f)
                buffer.Add(VirtualSteerHint);

            if ((VirtualSteerMoveTarget - root).sqrMagnitude > 0.01f &&
                (VirtualSteerMoveTarget - VirtualSteerHint).sqrMagnitude > 0.01f)
                buffer.Add(VirtualSteerMoveTarget);

            return buffer.Count;
        }

        private void UpdateVirtualSteerSnapshot(Vector3 flatTarget, Vector3 toTarget)
        {
            VirtualSteerMoveTarget = flatTarget;
            VirtualSteerActive = UsesChaseWallSlideLocomotion() ||
                                 state == AiState.Patrol ||
                                 state == AiState.Wander;

            Vector3 root = transform.position;
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            float lookAhead = 0.6f;
            if (profile != null)
                lookAhead = Mathf.Max(0.25f, profile.chaseWallProbeDistance * profile.chaseWallFanLookAheadScale);

            if (profile != null &&
                profile.enableChaseWallPredictiveFan &&
                ChaseFanLatchedDirection.sqrMagnitude > 0.0001f)
            {
                VirtualSteerHint = root + ChaseFanLatchedDirection.normalized * lookAhead;
                return;
            }

            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                float dist = Mathf.Min(toTarget.magnitude, lookAhead);
                VirtualSteerHint = root + toTarget.normalized * dist;
            }
            else
                VirtualSteerHint = root;
        }
    }
}
