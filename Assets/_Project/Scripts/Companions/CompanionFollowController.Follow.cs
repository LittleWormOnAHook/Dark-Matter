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
    // Loose-leash formation following (only walk when the player pulls the leash far enough),
    // hold-point/defend positioning, formation-slot math, and the follow-movement start delay.
    // Split out of CompanionFollowController.cs.
    public partial class CompanionFollowController
    {
        private void SyncHoldFromTaskQueue()
        {
            if (taskQueue == null || !taskQueue.HasHoldPoint)
                return;

            holdFacingYaw = taskQueue.HoldFacingYaw;
        }

        private void UpdateHoldBehavior()
        {
            if (taskQueue == null || !taskQueue.HasHoldPoint)
            {
                currentSpeed = 0f;
                ApplyHoldFacing();
                return;
            }

            Vector3 holdPoint = taskQueue.HoldPosition;
            holdPoint.y = SampleTerrainHeight(holdPoint);
            float distance = HorizontalDistance(transform.position, holdPoint);

            if (distance > stopDistance + 0.15f)
            {
                MoveTowards(holdPoint, walkSpeed, allowIdleRest: false);
                return;
            }

            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            ApplyHoldFacing();
        }

        private void ApplyHoldFacing()
        {
            Quaternion holdRotation = Quaternion.Euler(0f, holdFacingYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, holdRotation, restFacingSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Soft leash follow: only walk when the player is far enough, toward a lagging point
        /// behind their travel direction. Player body yaw and spin-in-place are ignored.
        /// </summary>
        private void UpdateLooseFollow()
        {
            UpdateOwnerMotionSpeed();
            UpdateTravelHeadingSlow();

            float distanceToOwner = HorizontalDistance(transform.position, owner.position);

            if (TryTeleportCatchUp(distanceToOwner, distanceToOwner))
                return;

            if (distanceToOwner > maxFollowDistance)
            {
                looseFollowActive = true;
                catchUpActive = true;
                Vector3 catchPoint = ComputeLooseFollowPoint();
                looseFollowSmoothedTarget = catchPoint;
                MoveTowards(catchPoint, runSpeed, allowIdleRest: false, faceMovement: true);
                return;
            }

            if (looseFollowActive)
            {
                if (distanceToOwner <= looseLeashStop)
                {
                    looseFollowActive = false;
                    catchUpActive = false;
                    currentSpeed = 0f;
                    currentMoveDirection = Vector3.zero;
                    isNearFormation = true;
                    return;
                }
            }
            else if (distanceToOwner >= looseLeashStart)
            {
                looseFollowActive = true;
                Vector3 seed = ComputeLooseFollowPoint();
                if (looseFollowSmoothedTarget.sqrMagnitude < 0.01f)
                    looseFollowSmoothedTarget = seed;
            }

            if (!looseFollowActive)
            {
                // Inside the leash: stand still. Do not orbit or mirror player turns.
                catchUpActive = false;
                isNearFormation = true;
                isWandering = false;
                currentSpeed = 0f;
                currentMoveDirection = Vector3.zero;
                return;
            }

            catchUpActive = distanceToOwner > catchUpDistance;
            Vector3 desired = ComputeLooseFollowPoint();
            looseFollowSmoothedTarget = Vector3.SmoothDamp(
                looseFollowSmoothedTarget,
                desired,
                ref looseFollowVelocity,
                looseTargetSmoothTime);
            looseFollowSmoothedTarget.y = SampleTerrainHeight(looseFollowSmoothedTarget);

            float speed = distanceToOwner > catchUpDistance ? runSpeed : walkSpeed * 0.8f;
            MoveTowards(looseFollowSmoothedTarget, speed, allowIdleRest: false, faceMovement: true);
        }

        private Vector3 ComputeLooseFollowPoint()
        {
            Vector3 forward = travelForward.sqrMagnitude > 0.01f ? travelForward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.right;
            else
                right.Normalize();

            float lateral = (formationSlot - 1) * looseSlotSpacing;
            Vector3 point = owner.position - forward * looseFollowBackDistance + right * lateral;
            point.y = SampleTerrainHeight(point);
            return point;
        }

        private void UpdateTravelHeadingSlow()
        {
            if (ownerMotionSpeed < travelHeadingMinSpeed)
                return;

            if (ownerTravelDelta.sqrMagnitude < 0.0001f)
                return;

            Vector3 dir = ownerTravelDelta.normalized;
            float smooth = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.15f, travelHeadingSmoothTime));
            travelForward = Vector3.Slerp(travelForward, dir, smooth);
            travelForward.y = 0f;
            if (travelForward.sqrMagnitude > 0.0001f)
                travelForward.Normalize();
        }

        private bool TryCombatTetherReturn()
        {
            // Fully independent while engaged with an enemy — never yank back to the player.
            if (combatEngageTarget != null)
                return false;

            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator == null || !coordinator.IsCombatEngaged || activeProfile == null)
                return false;

            if (activeProfile.combatTetherRadius <= 0.01f)
                return false;

            Vector3 anchor = ResolveCombatAnchor();
            float distanceFromAnchor = HorizontalDistance(transform.position, anchor);
            if (distanceFromAnchor <= activeProfile.combatTetherRadius)
                return false;

            MoveTowards(anchor, catchUpSpeed * 0.9f, allowIdleRest: false, faceMovement: true);
            return true;
        }

        private Vector3 ResolveCombatAnchor()
        {
            if (taskQueue != null && taskQueue.ShouldHold && taskQueue.HasHoldPoint)
                return taskQueue.HoldPosition;

            // Player position only — not a rotating formation slot.
            return owner != null ? owner.position : transform.position;
        }

        private Vector3 ResolveFollowTarget(float driftForFormation)
        {
            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator != null && coordinator.IsCombatEngaged && followMode == PioneerFollowMode.DefendPlayer)
                return ResolveDefendPosition();

            if (coordinator != null && coordinator.IsCombatEngaged && activeProfile != null
                && activeProfile.PrefersRangedSpacing(pioneerClass))
            {
                EnemyHealth target = combatController != null ? combatController.CurrentTarget : null;
                if (target != null && owner != null)
                {
                    Vector3 toTarget = target.transform.position - owner.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        ItemData weapon = combatController != null ? combatController.EquippedWeapon : null;
                        float preferred = combatController != null
                            ? combatController.ResolveEngagementDistance(weapon)
                            : activeProfile.ResolvePreferredCombatDistance(pioneerClass);
                        Vector3 rangedPoint = owner.position + toTarget.normalized * preferred;
                        Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized)
                            * ((formationSlot - 1) * 0.85f);
                        rangedPoint += lateral;
                        rangedPoint.y = SampleTerrainHeight(rangedPoint);
                        return rangedPoint;
                    }
                }
            }

            return GetFormationPosition(driftForFormation);
        }

        private Vector3 ResolveDefendPosition()
        {
            if (owner == null)
                return transform.position;

            EnemyHealth threat = combatController != null ? combatController.CurrentTarget : null;
            if (threat == null)
                return GetFormationPosition();

            Vector3 playerPos = owner.position;
            Vector3 threatPos = threat.transform.position;

            if (pioneerClass == SkilledPioneerClass.CombatTactician)
            {
                float standoff = combatController != null
                    ? combatController.AttackRange * 0.88f
                    : activeProfile.preferredCombatDistance * 0.85f;

                Vector3 toPioneer = transform.position - threatPos;
                toPioneer.y = 0f;
                if (toPioneer.sqrMagnitude < 0.01f)
                {
                    Vector3 fallback = playerPos - threatPos;
                    fallback.y = 0f;
                    toPioneer = fallback.sqrMagnitude > 0.01f ? fallback : transform.forward;
                }

                toPioneer.Normalize();
                float slotSpread = (formationSlot - 1) * 0.65f;
                Vector3 lateral = Vector3.Cross(Vector3.up, toPioneer) * slotSpread;
                Vector3 chasePoint = threatPos + toPioneer * standoff + lateral;
                chasePoint.y = SampleTerrainHeight(chasePoint);
                return chasePoint;
            }

            Vector3 toThreat = threatPos - playerPos;
            toThreat.y = 0f;

            if (toThreat.sqrMagnitude < 0.01f)
                return GetFormationPosition();

            Vector3 defendDir = -toThreat.normalized;
            float slotSpreadDefend = (formationSlot - 1) * 1.15f;
            Vector3 lateralDefend = Vector3.Cross(Vector3.up, toThreat.normalized) * slotSpreadDefend;
            Vector3 defendPoint = playerPos + defendDir * activeProfile.preferredCombatDistance + lateralDefend;
            defendPoint.y = SampleTerrainHeight(defendPoint);
            return defendPoint;
        }

        private float ResolveFollowSpeed(float distanceToOwner, float distanceToTarget, bool nearFormation, bool wandering)
        {
            if (catchUpActive)
                return catchUpSpeed;

            if (ShouldRun(distanceToOwner, distanceToTarget))
                return runSpeed;

            if (nearFormation && wandering)
                return walkSpeed * wanderPaceScale;

            return walkSpeed;
        }

        private bool TryTeleportCatchUp(float distanceToOwner, float distanceToTarget)
        {
            if (distanceToOwner < teleportCatchUpDistance && distanceToTarget < teleportCatchUpDistance)
                return false;

            Vector3 formation = GetFormationPosition();
            formation.y = SampleTerrainHeight(formation);
            transform.position = formation;
            Depenetrate();
            SyncInvectorRigidbody();
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            catchUpActive = false;
            return true;
        }

        private void ScheduleFollowMovementDelay()
        {
            scheduledFollowMovementDelay = ResolveFollowMovementDelay();
            allowFollowMovementAt = Time.time + scheduledFollowMovementDelay;
        }

        private void ClearFollowMovementDelay()
        {
            allowFollowMovementAt = 0f;
        }

        private bool IsFollowMovementDelayed() =>
            allowFollowMovementAt > 0f && Time.time < allowFollowMovementAt;

        private float ResolveFollowMovementDelay()
        {
            float min = Mathf.Min(followMovementDelayMin, followMovementDelayMax);
            float max = Mathf.Max(followMovementDelayMin, followMovementDelayMax);
            return Random.Range(min, max);
        }

        private void ApplyFollowMovementDelayHold()
        {
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            isNearFormation = true;
            isWandering = false;
            catchUpActive = false;
            ApplyIdleRestFacing();
        }

        private void UpdateOwnerMotionSpeed()
        {
            if (owner == null)
            {
                ownerMotionSpeed = 0f;
                return;
            }

            Vector3 delta = owner.position - lastOwnerPosition;
            delta.y = 0f;
            ownerTravelDelta = delta;
            ownerMotionSpeed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
            lastOwnerPosition = owner.position;
        }

        private void UpdateFormationHeading()
        {
            if (owner == null || ownerMotionSpeed < minOwnerSpeedForHeadingUpdate)
                return;

            if (ownerTravelDelta.sqrMagnitude < 0.0001f)
                return;

            float travelYaw = Mathf.Atan2(ownerTravelDelta.x, ownerTravelDelta.z) * Mathf.Rad2Deg;
            float smooth = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, formationHeadingSmoothTime));
            formationHeadingYaw = Mathf.LerpAngle(formationHeadingYaw, travelYaw, smooth);
        }

        private bool IsOwnerStationary()
        {
            if (owner == null)
                return true;

            if (ownerCharacter != null)
                return ownerCharacter.GetSpeed() < 0.12f;

            return ownerMotionSpeed < 0.12f;
        }

        public Vector3 GetFormationPosition(float driftAngleOverride = float.NaN)
        {
            float drift = float.IsNaN(driftAngleOverride) ? formationDriftAngle : driftAngleOverride;
            Vector3 target = GetFormationPosition(owner, formationSlot, drift, formationHeadingYaw);
            target.y = SampleTerrainHeight(target);
            return target;
        }

        public static Vector3 GetFormationPosition(Transform ownerTransform, int slotIndex, float driftAngle = 0f)
        {
            float headingYaw = ownerTransform != null ? ownerTransform.eulerAngles.y : 0f;
            return GetFormationPosition(ownerTransform, slotIndex, driftAngle, headingYaw);
        }

        public static Vector3 GetFormationPosition(
            Transform ownerTransform,
            int slotIndex,
            float driftAngle,
            float headingYaw)
        {
            if (ownerTransform == null)
                return Vector3.zero;

            int slot = Mathf.Clamp(slotIndex, 0, FormationOffsets.Length - 1);
            Vector3 offset = FormationOffsets[slot];
            if (Mathf.Abs(driftAngle) > 0.01f)
                offset = Quaternion.Euler(0f, driftAngle, 0f) * offset;

            Quaternion frame = Quaternion.Euler(0f, headingYaw, 0f);
            return ownerTransform.position + frame * offset;
        }

        private bool ShouldRun(float distanceToOwner, float distanceToTarget)
        {
            return distanceToOwner > maxFollowDistance * 0.42f || distanceToTarget > 2.5f;
        }
    }
}
