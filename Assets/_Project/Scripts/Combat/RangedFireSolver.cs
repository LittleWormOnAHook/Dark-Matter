using Project.Data;
using Project.Progression;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Resolves the final projectile direction and effective cone spread for a ranged shot.
    /// HUD stays at screen center; every muzzle→reticle shot uses that same camera ray.
    /// Hip fire adds cone spread only — it does not pull the shot back toward the barrel.
    /// </summary>
    public static class RangedFireSolver
    {
        public const float DefaultHipMaxDeviationDegrees = 15f;

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

        /// <summary>Locked HUD / fire viewport (Unity bottom-left origin).</summary>
        public static Vector2 ResolveReticleViewport(Camera cam, float maxRange = 32f)
        {
            return new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Screen-center reticle ray → world aim point (hit or far point of maxRange).
        /// </summary>
        public static Vector3 ResolveReticleAimPoint(Camera cam, float maxRange, LayerMask? optionalMask = null)
        {
            if (cam == null)
                return Vector3.zero;

            float range = Mathf.Max(1f, maxRange);
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int mask = optionalMask ?? Physics.DefaultRaycastLayers;

            if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return ray.GetPoint(range);
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
