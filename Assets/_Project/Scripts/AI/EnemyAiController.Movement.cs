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

                step = ClampCombatStepTowardTarget(step);

                if (step.sqrMagnitude < 0.000001f)
                {
                    ClearLocomotion();
                    return;
                }

                transform.position += step;
                currentLocomotionSpeed = speed;
                currentLocalMoveDirection = transform.InverseTransformDirection(step.normalized);
            }
            else
            {
                ClearLocomotion();
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
            return EnemyGroundUtility.TryGetGroundY(worldPosition, out groundY, groundOffset);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
