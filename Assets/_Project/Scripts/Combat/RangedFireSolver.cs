using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Resolves the final projectile direction for a ranged shot.
    /// ADS: camera aim wins (crosshair-accurate).
    /// Hip: camera aim clamped to a cone around barrel forward so bullets
    /// never visibly leave the muzzle at impossible angles.
    /// </summary>
    public static class RangedFireSolver
    {
        public const float DefaultHipMaxDeviationDegrees = 15f;

        public static Vector3 ResolveDirection(
            Vector3 cameraAimDirection,
            Vector3 muzzleForward,
            bool isAiming,
            float hipMaxDeviationDegrees)
        {
            if (cameraAimDirection.sqrMagnitude < 0.0001f)
                return muzzleForward.sqrMagnitude > 0.0001f ? muzzleForward.normalized : Vector3.forward;

            Vector3 aimDir = cameraAimDirection.normalized;
            if (isAiming)
                return aimDir;

            if (muzzleForward.sqrMagnitude < 0.0001f)
                return aimDir;

            Vector3 barrelDir = muzzleForward.normalized;
            float maxDeviation = hipMaxDeviationDegrees > 0.01f
                ? hipMaxDeviationDegrees
                : DefaultHipMaxDeviationDegrees;

            float angle = Vector3.Angle(barrelDir, aimDir);
            if (angle <= maxDeviation)
                return aimDir;

            return Vector3.RotateTowards(
                barrelDir,
                aimDir,
                maxDeviation * Mathf.Deg2Rad,
                0f).normalized;
        }
    }
}
