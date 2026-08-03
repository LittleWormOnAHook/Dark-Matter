using ECM2;
using Invector.vCharacterController;
using Project.AI;
using Project.Companions.Invector;
using Project.Crafting;
using Project.Data;
using Project.Interaction;
using Project.Pet;
using Project.Pioneers;
using Project.Player;
using UnityEngine;

namespace Project.Companions
{
    // World-ambient behaviors for un-recruited/world-placed companions (Idle/PingPong modes), plus
    // the near-formation idle-anchor rotation and small wander-offset used while actively following.
    // Split out of CompanionFollowController.cs.
    public partial class CompanionFollowController
    {
        private void UpdateWorldAmbientBehavior()
        {
            if (!worldAmbientInitialized)
            {
                worldAnchor = transform.position;
                SetupPingPongPoints();
                worldAmbientInitialized = true;
            }

            switch (behaviorMode)
            {
                case CompanionFollowBehaviorMode.Idle:
                    UpdateWorldIdle();
                    break;
                case CompanionFollowBehaviorMode.PingPong:
                    UpdateWorldPingPong();
                    break;
            }
        }

        private void UpdateWorldIdle()
        {
            // PioneerWorldIdleJob hook: future systems can drive animations here.
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            isNearFormation = true;
            ApplyIdleRestFacing();
        }

        private void UpdateWorldPingPong()
        {
            if (Time.time < pingPongPauseUntil)
            {
                currentSpeed = 0f;
                currentMoveDirection = Vector3.zero;
                ApplyIdleRestFacing();
                return;
            }

            Vector3 target = pingPongMovingToB ? pingPongPointB : pingPongPointA;
            float distance = HorizontalDistance(transform.position, target);
            if (distance <= stopDistance + 0.2f)
            {
                pingPongMovingToB = !pingPongMovingToB;
                pingPongPauseUntil = Time.time + Random.Range(pingPongPauseMin, pingPongPauseMax);
                currentSpeed = 0f;
                currentMoveDirection = Vector3.zero;
                return;
            }

            isWandering = true;
            MoveTowards(target, walkSpeed * wanderPaceScale, allowIdleRest: false);
        }

        private void SetupPingPongPoints()
        {
            int hash = string.IsNullOrEmpty(pioneerSeed) ? name.GetHashCode() : pioneerSeed.GetHashCode();
            float angle = (hash & 0xFF) / 255f * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * pingPongPatrolRadius;
            pingPongPointA = worldAnchor;
            pingPongPointB = worldAnchor + offset;
            pingPongPointA.y = SampleTerrainHeight(pingPongPointA);
            pingPongPointB.y = SampleTerrainHeight(pingPongPointB);
            pingPongMovingToB = true;
            pingPongPauseUntil = 0f;
        }

        private void UpdateIdleAnchorBehavior()
        {
            if (owner == null)
            {
                currentSpeed = 0f;
                return;
            }

            if (Time.time >= nextIdleAnchorChangeTime)
                AdvanceIdleAnchor();

            if (Time.time >= idlePhaseEndsAt)
                RollIdlePhase();

            Vector3 anchorWorld = GetIdleAnchorWorld();

            if (!idleWanderPhaseActive)
            {
                isWandering = false;
                float distanceToAnchor = HorizontalDistance(transform.position, anchorWorld);
                isNearFormation = distanceToAnchor <= stopDistance + 0.35f;

                if (distanceToAnchor > stopDistance + 0.25f)
                {
                    MoveTowards(anchorWorld, walkSpeed * 0.75f, allowIdleRest: true);
                    return;
                }

                currentSpeed = 0f;
                currentMoveDirection = Vector3.zero;
                ApplyIdleRestFacing();
                return;
            }

            isWandering = true;
            float distanceToWanderTarget = HorizontalDistance(transform.position, idleWanderWorldTarget);
            isNearFormation = distanceToWanderTarget <= stopDistance + 0.35f;

            if (distanceToWanderTarget <= stopDistance + 0.2f)
            {
                idlePhaseEndsAt = Mathf.Min(idlePhaseEndsAt, Time.time + 0.35f);
                currentSpeed = 0f;
                ApplyIdleRestFacing();
                return;
            }

            MoveTowards(idleWanderWorldTarget, walkSpeed * idleWanderPaceScale, allowIdleRest: false);
        }

        private Vector3 GetIdleAnchorWorld()
        {
            Quaternion frame = Quaternion.Euler(0f, formationHeadingYaw, 0f);
            return owner.position + frame * currentIdleAnchorLocal;
        }

        private void RollIdlePhase()
        {
            idleWanderPhaseActive = Random.value > idleProbability;

            if (idleWanderPhaseActive)
            {
                PickIdleWanderDestination();
                idlePhaseEndsAt = Time.time + Random.Range(idleWanderDurationMin, idleWanderDurationMax);
                return;
            }

            idlePhaseEndsAt = Time.time + Random.Range(idleRestDurationMin, idleRestDurationMax);
        }

        private void PickIdleWanderDestination()
        {
            Vector3 origin = owner != null ? owner.position : transform.position;
            Vector2 offset = Random.insideUnitCircle * idleWanderRange;
            idleWanderWorldTarget = origin + new Vector3(offset.x, 0f, offset.y);
            idleWanderWorldTarget.y = SampleTerrainHeight(idleWanderWorldTarget);
        }

        private void RepickIdlePositionRoutine()
        {
            if (IdleAnchorOffsets.Length == 0)
                return;

            if (idlePositionOrder == null || idlePositionOrder.Length != IdleAnchorOffsets.Length)
                idlePositionOrder = new int[IdleAnchorOffsets.Length];

            for (int i = 0; i < idlePositionOrder.Length; i++)
                idlePositionOrder[i] = i;

            for (int i = idlePositionOrder.Length - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (idlePositionOrder[i], idlePositionOrder[swapIndex]) = (idlePositionOrder[swapIndex], idlePositionOrder[i]);
            }

            idleOrderIndex = 0;
            ApplyCurrentIdleAnchor();
        }

        private void AdvanceIdleAnchor()
        {
            idleOrderIndex = (idleOrderIndex + 1) % idlePositionOrder.Length;
            ApplyCurrentIdleAnchor();
        }

        private void ApplyCurrentIdleAnchor()
        {
            if (idlePositionOrder == null || idlePositionOrder.Length == 0)
                RepickIdlePositionRoutine();

            int anchorIndex = idlePositionOrder[Mathf.Clamp(idleOrderIndex, 0, idlePositionOrder.Length - 1)];
            currentIdleAnchorLocal = IdleAnchorOffsets[Mathf.Clamp(anchorIndex, 0, IdleAnchorOffsets.Length - 1)];
            nextIdleAnchorChangeTime = Time.time + Random.Range(idleAnchorChangeMin, idleAnchorChangeMax);
        }

        private void PickNewWanderTarget()
        {
            float radius = wanderRadius * (0.65f + GetPersonalityFactor() * 0.35f);
            wanderPhase += Random.Range(0.8f, 1.6f);
            wanderTargetOffset = new Vector3(
                Mathf.Cos(wanderPhase) * radius,
                0f,
                Mathf.Sin(wanderPhase * 0.81f) * radius);
            nextWanderRetargetTime = Time.time + Random.Range(wanderRetargetMin, wanderRetargetMax);
        }

        private void UpdateWanderOffset()
        {
            if (Time.time >= nextWanderRetargetTime)
                PickNewWanderTarget();

            smoothedWanderOffset = Vector3.SmoothDamp(
                smoothedWanderOffset,
                wanderTargetOffset,
                ref wanderVelocity,
                wanderSmoothTime);
        }

        private void ApplyIdleRestFacing()
        {
            float targetYaw = formationHeadingYaw + idleRestYaw;
            Quaternion restRotation = Quaternion.Euler(0f, targetYaw, 0f);
            float maxDegrees = DMILocomotionFacing.ToDegreesPerSecond(restFacingSpeed) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, restRotation, maxDegrees);
        }

        private float GetPersonalityFactor()
        {
            return (Mathf.Abs(pioneerSeed.GetHashCode()) % 1000) / 1000f;
        }

        private float GetDriftSign()
        {
            return (pioneerSeed.GetHashCode() & 1) == 0 ? 1f : -1f;
        }
    }
}
