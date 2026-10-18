using Project.AI;
using Project.Combat;
using Project.Survival;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Auto: rest + silent target polling; engage when a valid target is in track range.
    /// Non-auto may visually sweep via <see cref="TickScan"/>. Player seat disables this brain.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DMTurretRuntime))]
    public sealed class DMTurretBrain : MonoBehaviour
    {
        private DMTurretRuntime runtime;
        private float nextScanPoseTime;
        private float nextTargetScanTime;
        private Vector3 scanAimPoint;
        private Transform currentTarget;

        private float noEngageableTargetSince = -1f;
        private float targetLastSeenTime = -1f;
        private int consecutiveLosFailures;

        private const float TargetRescanSeconds = 0.35f;
        private const float LongRestTargetRescanSeconds = 1.2f;

        private void Awake()
        {
            runtime = GetComponent<DMTurretRuntime>();
            nextTargetScanTime = Time.time + Random.Range(0f, TargetRescanSeconds);
        }

        private void Update()
        {
            if (runtime == null || runtime.IsDestroyed)
                return;
            if (!enabled)
                return;
            if (DMTurretPlayerControl.IsControlling(runtime))
                return;

            if (!DMTurretCombatGate.ShouldRunAutoBrain())
            {
                if (runtime.IsFiring)
                    runtime.StopFire();
                else
                    runtime.StopStuckSustainedForgeFx();
                runtime.HoldAutoIdle();
                SuspendForNonCombat();
                return;
            }

            if (runtime.ControlMode != DMTurretControlMode.Auto)
            {
                if (runtime.IsFiring)
                    runtime.StopFire();
                runtime.BrainMode = DMTurretBrainMode.Scan;
                TickScan();
                return;
            }

            currentTarget = ResolveAutoTarget();
            if (TryTickAutoEngaged())
                return;

            if (runtime.IsFiring)
                runtime.StopFire();
            runtime.BrainMode = DMTurretBrainMode.Scan;
            TickAutoRest();
        }

        private bool TryTickAutoEngaged()
        {
            if (currentTarget == null || !IsTargetStillValid(currentTarget))
                return false;

            float distance = runtime.HorizontalDistanceTo(currentTarget.position);
            if (distance > runtime.TrackRange || distance > runtime.ScanRange)
                return false;

            noEngageableTargetSince = -1f;

            Vector3 aim = AimPoint(currentTarget);
            runtime.AimAt(aim);

            if (distance > runtime.FireRange)
            {
                if (runtime.IsFiring)
                    runtime.StopFire();
                runtime.BrainMode = DMTurretBrainMode.Track;
                return true;
            }

            runtime.BrainMode = DMTurretBrainMode.Fire;
            if (runtime.IsAimedEnough() && HasFireLineOfSight(currentTarget))
                runtime.BeginFire();
            else if (runtime.IsFiring)
                runtime.StopFire();
            return true;
        }

        private void TickAutoRest()
        {
            if (noEngageableTargetSince < 0f)
                noEngageableTargetSince = Time.time;

            runtime.HoldAutoIdle();
        }

        private bool IsLongAutoRest()
        {
            if (noEngageableTargetSince < 0f)
                return false;

            DMTurretProfile profile = DMTurretProfile.Live;
            float threshold = profile != null ? profile.AutoRestAfterNoTargetSeconds : 15f;
            return Time.time - noEngageableTargetSince >= threshold;
        }

        private float ResolveTargetRescanInterval()
        {
            if (runtime.ControlMode != DMTurretControlMode.Auto)
                return TargetRescanSeconds;
            return IsLongAutoRest() ? LongRestTargetRescanSeconds : TargetRescanSeconds;
        }

        private void TickScan()
        {
            DMTurretProfile profile = DMTurretProfile.Live;
            float interval = profile != null ? profile.ScanRetargetSeconds : 2.4f;
            if (Time.time >= nextScanPoseTime || scanAimPoint == Vector3.zero)
            {
                nextScanPoseTime = Time.time + interval;
                scanAimPoint = PickScanPoint(profile);
            }

            runtime.AimAt(scanAimPoint);
        }

        public void ClearScanState()
        {
            nextScanPoseTime = 0f;
            nextTargetScanTime = 0f;
            scanAimPoint = Vector3.zero;
            currentTarget = null;
            noEngageableTargetSince = -1f;
            targetLastSeenTime = -1f;
            consecutiveLosFailures = 0;
            if (runtime != null)
                runtime.CurrentAutoTarget = null;
        }

        private Transform ResolveAutoTarget()
        {
            if (currentTarget != null)
            {
                if (IsTargetStillValid(currentTarget))
                {
                    runtime.CurrentAutoTarget = currentTarget;
                    return currentTarget;
                }

                ClearTargetLock();
            }

            if (Time.time < nextTargetScanTime)
            {
                runtime.CurrentAutoTarget = null;
                return null;
            }

            nextTargetScanTime = Time.time + ResolveTargetRescanInterval();
            currentTarget = runtime.FindTarget();
            if (currentTarget != null)
            {
                targetLastSeenTime = Time.time;
                consecutiveLosFailures = 0;
            }

            runtime.CurrentAutoTarget = currentTarget;
            return currentTarget;
        }

        private void ClearTargetLock()
        {
            currentTarget = null;
            runtime.CurrentAutoTarget = null;
            runtime.StopFireAndCleanupForgeFx();
            targetLastSeenTime = -1f;
            consecutiveLosFailures = 0;
            nextTargetScanTime = 0f;
        }

        /// <summary>Main menu / paused gameplay — drop lock so turrets do not snap-fire on session resume.</summary>
        private void SuspendForNonCombat()
        {
            if (currentTarget == null && consecutiveLosFailures == 0 && targetLastSeenTime < 0f)
                return;

            currentTarget = null;
            runtime.CurrentAutoTarget = null;
            targetLastSeenTime = -1f;
            consecutiveLosFailures = 0;
            noEngageableTargetSince = -1f;
        }

        private bool IsTargetStillValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return false;

            if (DMTurretPlayerControl.IsSeatedPlayer(target))
                return false;

            if (runtime.Faction == DMTurretFaction.Player)
            {
                EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead)
                    return false;
            }
            else if (runtime.Faction == DMTurretFaction.Enemy)
            {
                SurvivalStats stats = target.GetComponentInParent<SurvivalStats>();
                if (stats == null || stats.IsDead)
                    return false;
            }

            float distance = runtime.HorizontalDistanceTo(target.position);
            if (distance > runtime.ScanRange)
                return false;

            DMTurretProfile profile = DMTurretProfile.Live;
            float grace = profile != null ? profile.TargetLosGraceSeconds : 0.65f;
            int maxFailures = profile != null ? profile.TargetLosFailuresBeforeDrop : 4;

            if (runtime.HasLineOfSight(target))
            {
                consecutiveLosFailures = 0;
                targetLastSeenTime = Time.time;
                return true;
            }

            consecutiveLosFailures++;
            if (targetLastSeenTime >= 0f
                && Time.time - targetLastSeenTime <= grace
                && consecutiveLosFailures < maxFailures)
                return true;

            return false;
        }

        private bool HasFireLineOfSight(Transform target)
        {
            if (target == null)
                return false;
            if (runtime.HasLineOfSight(target))
                return true;

            DMTurretProfile profile = DMTurretProfile.Live;
            float grace = profile != null ? profile.TargetLosGraceSeconds : 0.65f;
            return targetLastSeenTime >= 0f && Time.time - targetLastSeenTime <= grace;
        }

        private Vector3 PickScanPoint(DMTurretProfile profile)
        {
            Vector2 headingLimit = profile != null ? profile.headingLimit : new Vector2(-180f, 180f);
            Vector2 elevation = profile != null ? profile.scanElevationDegrees : new Vector2(-8f, 12f);
            if (runtime.ForgeTurret != null)
            {
                headingLimit = runtime.ForgeTurret.HeadingLimit;
                Vector2 el = runtime.ForgeTurret.ElevationLimit;
                elevation.x = Mathf.Max(elevation.x, el.x);
                elevation.y = Mathf.Min(elevation.y, el.y);
            }

            float yaw = Random.Range(headingLimit.x, headingLimit.y);
            float pitch = Random.Range(elevation.x, elevation.y);
            Quaternion rot = transform.rotation * Quaternion.Euler(-pitch, yaw, 0f);
            return runtime.Barrel.position + rot * Vector3.forward * 40f;
        }

        private static Vector3 AimPoint(Transform target)
        {
            return DMTurretLos.AimPoint(target);
        }
    }
}
