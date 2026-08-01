using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Camera frustum helpers for spit view-boost and brain decisions.
    /// </summary>
    public static class DMICreatureViewUtility
    {
        public static bool IsInPlayerCameraView(Transform target)
        {
            if (target == null)
                return false;

            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Vector3 viewport = camera.WorldToViewportPoint(target.position);
            if (viewport.z <= 0f)
                return false;

            return viewport.x >= 0f && viewport.x <= 1f
                   && viewport.y >= 0f && viewport.y <= 1f;
        }

        public static bool IsBoundsInPlayerCameraView(Bounds bounds)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }
    }
}
