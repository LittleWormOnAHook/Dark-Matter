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
    // Break-away combat engagement: while fighting, the companion anchors to the enemy instead of
    // the player's formation slot, holding a comfort ring (ranged standoff band or melee strike
    // range) around it with facing always locked onto the enemy. Split out of
    // CompanionFollowController.cs.
    public partial class CompanionFollowController
    {
        private void FaceCombatTarget(Vector3 enemyPos)
        {
            Vector3 toEnemy = enemyPos - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.01f)
                return;

            DMILocomotionFacing.FaceToward(transform, enemyPos, turnSpeed * 1.6f);
        }

        /// <summary>
        /// Break-away combat movement: hold a comfort ring around the enemy with a wide
        /// deadband so the companion stands its ground instead of oscillating between the
        /// enemy and the moving formation slot (the source of the side-to-side jitter).
        /// Facing is always toward the enemy — never toward movement or the player.
        /// </summary>
        private bool TryUpdateCombatEngagement()
        {
            if (combatEngageTarget == null || !combatEngageTarget.gameObject.activeInHierarchy)
                return false;

            looseFollowActive = false;
            Vector3 enemyPos = combatEngageTarget.position;
            float distance = HorizontalDistance(transform.position, enemyPos);
            float preferred = combatEngagePreferredDistance;
            float strikeRange = Mathf.Max(preferred, combatEngageMaxStrikeRange);

            if (combatEngageIsRanged)
                return TryUpdateRangedCombatEngagement(enemyPos, distance, preferred, strikeRange);

            return TryUpdateMeleeCombatEngagement(enemyPos, distance, preferred, strikeRange);
        }

        private bool TryUpdateRangedCombatEngagement(Vector3 enemyPos, float distance, float preferred, float strikeRange)
        {
            float loseFireRange = strikeRange * 0.88f;
            float comfortOuter = preferred * 1.18f;
            float comfortInner = preferred * 0.82f;

            // Enemy fled beyond effective fire range — run the standoff ring back into band.
            if (distance > loseFireRange)
            {
                Vector3 ringPoint = ComputeCombatRingPoint(enemyPos, preferred);
                float speed = distance > preferred * 1.75f ? runSpeed * 1.08f : runSpeed;
                MoveTowards(ringPoint, speed, allowIdleRest: false, faceMovement: false);
                FaceCombatTarget(enemyPos);
                return true;
            }

            // Still in range but drifting too far for reliable shots — walk back to preferred standoff.
            if (distance > comfortOuter)
            {
                Vector3 ringPoint = ComputeCombatRingPoint(enemyPos, preferred);
                MoveTowards(ringPoint, walkSpeed * 1.12f, allowIdleRest: false, faceMovement: false);
                FaceCombatTarget(enemyPos);
                return true;
            }

            if (distance < comfortInner)
            {
                Vector3 away = transform.position - enemyPos;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f)
                    away = -transform.forward;

                Vector3 backPoint = enemyPos + away.normalized * preferred;
                backPoint.y = SampleTerrainHeight(backPoint);
                MoveTowards(backPoint, walkSpeed * 0.95f, allowIdleRest: false, faceMovement: false);
                FaceCombatTarget(enemyPos);
                return true;
            }

            HoldCombatFacing(enemyPos);
            return true;
        }

        private bool TryUpdateMeleeCombatEngagement(Vector3 enemyPos, float distance, float preferred, float strikeRange)
        {
            float minSeparation = preferred * 0.72f;

            if (distance > strikeRange * 0.98f)
            {
                Vector3 ringPoint = ComputeCombatRingPoint(enemyPos, preferred);
                float speed = distance > preferred * 2.5f ? runSpeed : walkSpeed * 1.05f;
                MoveTowards(ringPoint, speed, allowIdleRest: false, faceMovement: false);
                FaceCombatTarget(enemyPos);
                return true;
            }

            if (distance < minSeparation)
            {
                Vector3 away = transform.position - enemyPos;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f)
                    away = -transform.forward;

                Vector3 backPoint = enemyPos + away.normalized * preferred;
                backPoint.y = SampleTerrainHeight(backPoint);
                MoveTowards(backPoint, walkSpeed * 0.9f, allowIdleRest: false, faceMovement: false);
                FaceCombatTarget(enemyPos);
                return true;
            }

            HoldCombatFacing(enemyPos);
            return true;
        }

        private void HoldCombatFacing(Vector3 enemyPos)
        {
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            catchUpActive = false;
            isWandering = false;
            FaceCombatTarget(enemyPos);
        }

        private Vector3 ComputeCombatRingPoint(Vector3 enemyPos, float preferred)
        {
            Vector3 fromEnemy = transform.position - enemyPos;
            fromEnemy.y = 0f;
            if (fromEnemy.sqrMagnitude < 0.01f)
                fromEnemy = -transform.forward;

            fromEnemy.Normalize();

            // Small per-pioneer lateral bias so multiple companions fan out around the target.
            if (combatOrbitSign == 0f)
                combatOrbitSign = (pioneerSeed.GetHashCode() & 1) == 0 ? 1f : -1f;

            Vector3 lateral = Vector3.Cross(Vector3.up, fromEnemy) * (combatOrbitSign * (formationSlot * 0.55f));
            Vector3 point = enemyPos + (fromEnemy * preferred) + lateral;
            point.y = SampleTerrainHeight(point);
            return point;
        }
    }
}
