using Invector.vCamera;
using Project.Data;
using Project.Progression;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Resolves the final projectile direction and effective cone spread for a ranged shot.
    /// The HUD reticle and every muzzle→reticle shot share the same look-at aim point so
    /// over-shoulder zoom cannot slide the crosshair off the fire line.
    /// Hip fire adds cone spread only — it does not pull the shot back toward the barrel.
    /// </summary>
    public static class RangedFireSolver
    {
        public const float DefaultHipMaxDeviationDegrees = 15f;
        public const float DefaultLookAtConvergeDistance = 32f;
        private const int PlayerLayer = 8;

        /// <summary>ADS multiplies effective spread (matches RangedCombatHud crosshair shrink).</summary>
        public const float AdsSpreadScale = 0.75f;

        /// <summary>
        /// At accuracy 100, residual spread fraction after the accuracy curve.
        /// Formula: spread *= Lerp(1, MinSpreadFractionAtFullAccuracy, accuracy01).
        /// </summary>
        public const float MinSpreadFractionAtFullAccuracy = 0.15f;

        public static Vector3 ResolveDirection(
            Vector3 cameraAimDirection,
            Vector3 muzzleForward,
            bool isAiming,
            float hipMaxDeviationDegrees)
        {
            if (cameraAimDirection.sqrMagnitude < 0.0001f)
                return muzzleForward.sqrMagnitude > 0.0001f ? muzzleForward.normalized : Vector3.forward;

            // Always follow the HUD reticle. Hip fire uses extra spread, not a barrel clamp —
            // a 15° pull toward the muzzle walks shots off the crosshair when zoomed out.
            return cameraAimDirection.normalized;
        }

        /// <summary>
        /// Viewport of the shared look-at aim point (Unity bottom-left origin).
        /// Zoomed-in sits near screen center; zoomed-out stays on the player look line
        /// instead of drifting right with the over-shoulder camera.
        /// </summary>
        public static Vector2 ResolveReticleViewport(Camera cam, float maxRange = DefaultLookAtConvergeDistance)
        {
            if (cam == null)
                return new Vector2(0.5f, 0.5f);

            Vector3 aimPoint = ResolveReticleAimPoint(cam, maxRange);
            Vector3 vp = cam.WorldToViewportPoint(aimPoint);
            if (vp.z <= 0.05f)
                return new Vector2(0.5f, 0.5f);

            return new Vector2(
                Mathf.Clamp(vp.x, 0.12f, 0.88f),
                Mathf.Clamp(vp.y, 0.12f, 0.88f));
        }

        /// <summary>
        /// World aim point on the player look-at line (hit or far point).
        /// Used by the HUD crosshair and every muzzle→reticle shot.
        /// </summary>
        public static Vector3 ResolveReticleAimPoint(Camera cam, float maxRange, LayerMask? optionalMask = null)
        {
            if (cam == null)
                return Vector3.zero;

            float range = Mathf.Max(1f, maxRange);
            Vector3 pivot = ResolveAimPivot(cam);
            Vector3 dir = cam.transform.forward;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;
            else
                dir.Normalize();

            Vector3 origin = pivot + dir * 0.35f;
            int mask = optionalMask ?? Physics.DefaultRaycastLayers;
            mask &= ~(1 << PlayerLayer);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return origin + dir * range;
        }

        public static Vector3 ResolveAimPivot(Camera cam)
        {
            vThirdPersonCamera tp = vThirdPersonCamera.instance;
            if (tp != null && tp.currentTarget != null)
            {
                Vector3 pivot = tp.currentTarget.position + tp.currentTarget.up * tp.offSetPlayerPivot;
                if (tp.currentState != null)
                    pivot += tp.currentTarget.up * tp.currentState.height;
                return pivot;
            }

            if (cam != null)
                return cam.transform.position + cam.transform.forward * 0.75f;

            return Vector3.zero;
        }

        /// <summary>
        /// Direction from muzzle to the reticle aim point (true 3D aim, including vertical).
        /// </summary>
        public static Vector3 ResolveMuzzleToReticleDirection(
            Camera cam,
            Vector3 muzzlePosition,
            float maxRange,
            out float aimDistance,
            LayerMask? optionalMask = null)
        {
            aimDistance = 0f;
            if (cam == null)
                return Vector3.forward;

            Vector3 aimPoint = ResolveReticleAimPoint(cam, maxRange, optionalMask);
            Vector3 toAim = aimPoint - muzzlePosition;
            aimDistance = toAim.magnitude;
            if (aimDistance < 0.0001f)
            {
                Vector3 camForward = cam.transform.forward;
                return camForward.sqrMagnitude > 0.0001f ? camForward.normalized : Vector3.forward;
            }

            return toAim / aimDistance;
        }

        /// <summary>
        /// Builds the cone spread (degrees) passed to CombatProjectileSpawner after accuracy,
        /// skill, hip-fire, close-range, and ADS modifiers.
        /// </summary>
        public static float ResolveEffectiveSpreadDegrees(
            ItemData weapon,
            ItemData ammo,
            bool isAiming,
            float aimDistance,
            bool applyPlayerSkillBonus)
        {
            if (weapon == null)
                return 0f;

            float spread = ammo != null && ammo.projectileSpreadDegrees > 0f
                ? ammo.projectileSpreadDegrees
                : weapon.projectileSpreadDegrees;

            float accuracy = weapon.ResolveBaseAccuracy(ammo);
            if (applyPlayerSkillBonus)
                accuracy += PlayerSkillAllocator.GetWeaponAccuracyBonusPercent();

            accuracy = Mathf.Clamp(accuracy, 0f, 100f);
            float accuracy01 = accuracy * 0.01f;
            spread *= Mathf.Lerp(1f, MinSpreadFractionAtFullAccuracy, accuracy01);

            if (!isAiming)
            {
                float hipMul = weapon.hipFireSpreadMultiplier > 0.01f
                    ? weapon.hipFireSpreadMultiplier
                    : 1f;
                spread *= hipMul;
            }
            else
            {
                spread *= AdsSpreadScale;
            }

            float closeDist = weapon.closeRangeFullAccuracyDistance > 0.01f
                ? weapon.closeRangeFullAccuracyDistance
                : 12f;
            float closeScale = Mathf.Clamp01(weapon.closeRangeSpreadScale);
            float t = Mathf.Clamp01(aimDistance / closeDist);
            spread *= Mathf.Lerp(closeScale, 1f, t);

            return Mathf.Max(0f, spread);
        }
    }
}
