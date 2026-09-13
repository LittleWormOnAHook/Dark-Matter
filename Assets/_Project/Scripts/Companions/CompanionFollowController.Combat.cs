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
    // Break-away combat engagement: companions leave the player's formation slot and work a
    // comfort ring around the enemy. Ranged kits close in, take short backsteps, then ping-pong
    // along the buffer. Relocates face the walk; only holds face the enemy (no moonwalk).
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
        /// Combat ring: melee closes to strike range; ranged kits, backsteps, and orbits
        /// the buffer. Walks face the step so Invector does not moonwalk.
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

        private void ResetRangedKiteState()
        {
            rangedKiteManeuver = RangedKiteManeuver.Hold;
            rangedKiteUntil = 0f;
            rangedOrbitPauseUntil = 0f;
            rangedOrbitTowardB = combatOrbitSign >= 0f;
        }

        private bool TryUpdateRangedCombatEngagement(Vector3 enemyPos, float distance, float preferred, float strikeRange)
        {
            float loseFireRange = strikeRange * 0.88f;
            float comfortOuter = preferred * 1.22f;
            float comfortInner = preferred * 0.72f;

            if (distance > loseFireRange || distance > comfortOuter)
            {
                BeginRangedApproach(enemyPos, preferred);
                TickRangedRelocate(enemyPos, walkOrRun: distance > preferred * 1.6f);
                return true;
            }

            if (distance < comfortInner)
            {
                if (rangedKiteManeuver != RangedKiteManeuver.Backstep || Time.time >= rangedKiteUntil)
                    BeginRangedBackstep(enemyPos, preferred);
                TickRangedRelocate(enemyPos, walkOrRun: false);
                return true;
            }

            if (rangedKiteManeuver == RangedKiteManeuver.Backstep && Time.time < rangedKiteUntil)
            {
                TickRangedRelocate(enemyPos, walkOrRun: false);
                return true;
            }

            if (rangedKiteManeuver == RangedKiteManeuver.Approach)
            {
                if (HorizontalDistance(transform.position, rangedKiteTarget) > stopDistance + 0.25f)
                {
                    TickRangedRelocate(enemyPos, walkOrRun: false);
                    return true;
                }

                BeginRangedOrbitHold(enemyPos);
            }

            TickRangedOrbit(enemyPos, preferred);
            return true;
        }

        private void BeginRangedApproach(Vector3 enemyPos, float preferred)
        {
            rangedKiteManeuver = RangedKiteManeuver.Approach;
            rangedKiteTarget = ComputeCombatRingPoint(enemyPos, preferred);
            rangedKiteUntil = Time.time + 1.4f;
        }

        private void BeginRangedBackstep(Vector3 enemyPos, float preferred)
        {
            Vector3 away = transform.position - enemyPos;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;

            rangedKiteManeuver = RangedKiteManeuver.Backstep;
            rangedKiteTarget = enemyPos + away.normalized * preferred;
            rangedKiteTarget.y = SampleTerrainHeight(rangedKiteTarget);
            rangedKiteUntil = Time.time + 0.7f;
        }

        private void BeginRangedOrbitHold(Vector3 enemyPos)
        {
            rangedKiteManeuver = RangedKiteManeuver.Hold;
            rangedOrbitPauseUntil = Time.time + Random.Range(0.4f, 0.85f);
            HoldCombatFacing(enemyPos);
        }

        private void TickRangedRelocate(Vector3 enemyPos, bool walkOrRun)
        {
            isWandering = true;
            float speed = walkOrRun ? runSpeed : walkSpeed * 0.95f;
            MoveTowards(rangedKiteTarget, speed, allowIdleRest: false, faceMovement: true);
            if (currentSpeed <= 0.05f)
                FaceCombatTarget(enemyPos);
        }

        private void TickRangedOrbit(Vector3 enemyPos, float preferred)
        {
            if (rangedKiteManeuver == RangedKiteManeuver.Hold || Time.time < rangedOrbitPauseUntil)
            {
                if (Time.time >= rangedOrbitPauseUntil)
                    BeginRangedOrbitStep(enemyPos, preferred);
                else
                    HoldCombatFacing(enemyPos);
                return;
            }

            if (rangedKiteManeuver != RangedKiteManeuver.Orbit)
                BeginRangedOrbitStep(enemyPos, preferred);

            if (HorizontalDistance(transform.position, rangedKiteTarget) <= stopDistance + 0.3f
                || Time.time >= rangedKiteUntil)
            {
                rangedOrbitTowardB = !rangedOrbitTowardB;
                BeginRangedOrbitHold(enemyPos);
                return;
            }

            isWandering = true;
            MoveTowards(rangedKiteTarget, walkSpeed * 0.88f, allowIdleRest: false, faceMovement: true);
            if (currentSpeed <= 0.05f)
                FaceCombatTarget(enemyPos);
        }

        private void BeginRangedOrbitStep(Vector3 enemyPos, float preferred)
        {
            Vector3 radial = transform.position - enemyPos;
            radial.y = 0f;
            if (radial.sqrMagnitude < 0.01f)
                radial = -transform.forward;
            radial.Normalize();

            if (combatOrbitSign == 0f)
                combatOrbitSign = (pioneerSeed.GetHashCode() & 1) == 0 ? 1f : -1f;

            Vector3 lateral = Vector3.Cross(Vector3.up, radial) * combatOrbitSign;
            float side = rangedOrbitTowardB ? 1f : -1f;
            rangedKiteTarget = enemyPos + radial * preferred + lateral * (preferred * 0.42f * side);
            rangedKiteTarget.y = SampleTerrainHeight(rangedKiteTarget);
            rangedKiteManeuver = RangedKiteManeuver.Orbit;
            rangedKiteUntil = Time.time + 1.15f;
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
