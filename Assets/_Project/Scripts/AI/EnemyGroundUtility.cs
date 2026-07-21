using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Shared ground height sampling for transform-driven enemies on prototype terrain.
    /// </summary>
    public static class EnemyGroundUtility
    {
        private const float DefaultRaycastUp = 40f;
        private const float DefaultRaycastDown = 80f;

        public static bool TryGetGroundY(Vector3 worldPosition, out float groundY, float groundOffset = 0f)
        {
            groundY = worldPosition.y;

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                groundY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;
                return true;
            }

            Vector3 origin = new Vector3(worldPosition.x, worldPosition.y + DefaultRaycastUp, worldPosition.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, DefaultRaycastDown, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
        }

        public static Vector3 SnapPositionToGround(Vector3 worldPosition, float groundOffset = 0f)
        {
            if (TryGetGroundY(worldPosition, out float groundY, groundOffset))
            {
                worldPosition.y = groundY;
                return worldPosition;
            }

            return worldPosition;
        }

        public static bool IsAbnormallyHigh(Vector3 worldPosition, float maxAboveGround = 8f)
        {
            if (!TryGetGroundY(worldPosition, out float groundY))
                return false;

            return worldPosition.y > groundY + maxAboveGround;
        }
    }
}
