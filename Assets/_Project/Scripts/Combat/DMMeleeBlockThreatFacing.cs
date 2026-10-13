using Invector.vCamera;
using Project.AI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Block and attack assist: rotate toward the nearest living melee threat when they sit outside the frontal cone.
    /// Nearest-target selection is throttled; yaw still tracks the cached threat every frame.
    /// </summary>
    public static class DMMeleeBlockThreatFacing
    {
        private const float NearestThreatRescanSeconds = 0.2f;

        private static Transform _cachedNearestThreat;
        private static int _cachedPlayerId;
        private static float _nearestThreatExpiresUnscaled;
        private static float _threatLockUntilUnscaled;

        public static bool TryGetNearestThreat(Transform player, float maxDistance, out Transform threat)
        {
            threat = null;
            if (player == null || maxDistance <= 0f)
                return false;

            int playerId = player.gameObject.GetEntityId().GetHashCode();
            float now = Time.unscaledTime;

            bool cachedValid = _cachedPlayerId == playerId && IsLivingThreatInRange(_cachedNearestThreat, player, maxDistance);
            if (cachedValid && (now < _nearestThreatExpiresUnscaled || now < _threatLockUntilUnscaled))
            {
                threat = _cachedNearestThreat;
                return true;
            }

            DM_CombatCoreProfile prof = DM_CombatCoreProfile.Live;
            if (cachedValid && TryFindNearestThreat(player, maxDistance, out Transform candidate) && candidate != _cachedNearestThreat)
            {
                // 2026-10-13 hysteresis: keep the current target unless the new one is clearly closer.
                float frac = prof != null ? prof.meleeFacingSwitchCloserFraction : 0.7f;
                float cur = (_cachedNearestThreat.position - player.position).sqrMagnitude;
                float nw = (candidate.position - player.position).sqrMagnitude;
                if (nw >= cur * frac * frac)
                {
                    _nearestThreatExpiresUnscaled = now + NearestThreatRescanSeconds;
                    threat = _cachedNearestThreat;
                    return true;
                }
                _threatLockUntilUnscaled = now + (prof != null ? prof.meleeFacingTargetLockSeconds : 1.5f);
                _cachedNearestThreat = candidate;
                _nearestThreatExpiresUnscaled = now + NearestThreatRescanSeconds;
                threat = candidate;
                return true;
            }
            if (cachedValid)
            {
                _nearestThreatExpiresUnscaled = now + NearestThreatRescanSeconds;
                threat = _cachedNearestThreat;
                return true;
            }

            if (!TryFindNearestThreat(player, maxDistance, out threat))
            {
                _cachedNearestThreat = null;
                _cachedPlayerId = playerId;
                _nearestThreatExpiresUnscaled = now + NearestThreatRescanSeconds;
                return false;
            }

            _cachedNearestThreat = threat;
            _cachedPlayerId = playerId;
            _nearestThreatExpiresUnscaled = now + NearestThreatRescanSeconds;
            _threatLockUntilUnscaled = now + (prof != null ? prof.meleeFacingTargetLockSeconds : 1.5f);
            return true;
        }

        public static void ApplyBlockFacing(Transform player, DM_CombatCoreProfile profile)
        {
            if (player == null)
                return;

            float maxDistance = profile != null ? profile.meleeBlockAutoFaceMaxDistance : 4.5f;
            float assistAfterDegrees = profile != null ? profile.meleeBlockAutoFaceHalfAngle : 30f;
            float turnSpeed = profile != null ? profile.meleeBlockAutoFaceTurnSpeed : 540f;
            ApplyFacingTowardNearestThreat(player, profile, maxDistance, assistAfterDegrees, turnSpeed);
        }

        public static void ApplyAttackFacing(Transform player, DM_CombatCoreProfile profile)
        {
            if (player == null)
                return;

            float maxDistance = profile != null ? profile.meleeBlockAutoFaceMaxDistance : 4.5f;
            float assistAfterDegrees = profile != null ? profile.meleeAttackAutoFaceHalfAngle : 16f;
            float turnSpeed = profile != null ? profile.meleeBlockAutoFaceTurnSpeed : 540f;
            ApplyFacingTowardNearestThreat(player, profile, maxDistance, assistAfterDegrees, turnSpeed);
        }

        /// <summary>Block assist — prefer <see cref="ApplyBlockFacing"/>.</summary>
        public static void ApplyFacing(Transform player, DM_CombatCoreProfile profile) =>
            ApplyBlockFacing(player, profile);

        private static void ApplyFacingTowardNearestThreat(
            Transform player,
            DM_CombatCoreProfile profile,
            float maxDistance,
            float assistAfterDegrees,
            float turnSpeed)
        {
            if (!TryGetNearestThreat(player, maxDistance, out Transform threat))
                return;

            Vector3 toThreat = threat.position - player.position;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 0.0025f)
                return;

            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();
            Vector3 dir = toThreat.normalized;
            float signedAngle = Vector3.SignedAngle(forward, dir, Vector3.up);
            if (Mathf.Abs(signedAngle) <= assistAfterDegrees)
                return;

            float maxStep = turnSpeed * Time.deltaTime;
            float step = Mathf.Clamp(signedAngle, -maxStep, maxStep);
            if (Mathf.Abs(step) < 0.001f)
                return;

            player.Rotate(0f, step, 0f, Space.World);

            vThirdPersonCamera tpCamera = vThirdPersonCamera.instance;
            if (tpCamera != null)
                DMCombatCameraThreatSnap.ApplyYawBehindPlayer(player, tpCamera, profile);
        }

        private static bool TryFindNearestThreat(Transform player, float maxDistance, out Transform threat)
        {
            threat = null;
            EnemyHealthSceneRegistry.FindNearestLiving(player.position, maxDistance, out EnemyHealth nearest);
            if (nearest == null)
                return false;

            threat = nearest.transform;
            return true;
        }

        private static bool IsLivingThreatInRange(Transform threat, Transform player, float maxDistance)
        {
            if (threat == null || player == null)
                return false;

            EnemyHealth health = threat.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead)
                return false;

            Vector3 delta = threat.position - player.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= maxDistance * maxDistance;
        }
    }
}
