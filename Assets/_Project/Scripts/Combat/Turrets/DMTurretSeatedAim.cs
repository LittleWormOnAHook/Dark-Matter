using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Screen-center reticle ray for seated player turret aim (matches UITK crosshair).</summary>
    public static class DMTurretSeatedAim
    {
        public static Vector3 ResolveScreenPoint(DMTurretProfile profile)
        {
            float bias = profile != null ? profile.PlayerReticleScreenBias : 0.06f;
            return new Vector3(Screen.width * 0.5f, Screen.height * (0.5f - bias), 0f);
        }

        public static Ray BuildAimRay(Camera camera, DMTurretProfile profile)
        {
            if (camera == null)
                return default;
            return camera.ScreenPointToRay(ResolveScreenPoint(profile));
        }

        public static bool TryResolveAimPoint(
            Camera camera,
            DMTurretRuntime runtime,
            out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            if (camera == null || runtime == null)
                return false;

            DMTurretProfile profile = DMTurretProfile.Live;
            Ray ray = BuildAimRay(camera, profile);
            float range = runtime.ScanRange;
            worldPoint = ray.GetPoint(range);

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    range,
                    DMTurretLos.ShotMask,
                    QueryTriggerInteraction.Ignore))
            {
                worldPoint = hit.point;
            }

            return true;
        }
    }
}
