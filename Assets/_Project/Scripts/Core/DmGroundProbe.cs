using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Allocation-free downward ground sampling for trail recording and item drops.
    /// </summary>
    public static class DmGroundProbe
    {
        private const int HitBufferSize = 24;
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

        public static float SampleWalkableGroundY(
            Vector3 position,
            float probeHeight = 2.5f,
            float probeDistance = 10f,
            float groundOffset = 0.05f,
            float maxHeightAboveTerrain = 0.35f)
        {
            float baselineY = GetTerrainBaselineY(position, groundOffset);
            float originY = Mathf.Max(position.y + probeHeight, baselineY + probeHeight);
            Vector3 origin = new Vector3(position.x, originY, position.z);
            float rayLength = (originY - position.y) + probeDistance;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                HitBuffer,
                Mathf.Max(4f, rayLength),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float maxAllowedY = Mathf.Min(position.y + 0.5f, baselineY + maxHeightAboveTerrain);
            float bestScore = float.MaxValue;
            float bestY = baselineY;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = HitBuffer[i];
                Collider collider = hit.collider;
                if (collider == null || collider.isTrigger || !IsWalkableGroundCollider(collider))
                    continue;

                float candidateY = hit.point.y + groundOffset;
                if (candidateY > maxAllowedY + 0.01f)
                    continue;

                float score = Mathf.Abs(candidateY - baselineY);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestY = candidateY;
                found = true;
            }

            return found ? bestY : baselineY;
        }

        public static bool TrySampleDropGroundY(
            Vector3 worldPosition,
            Transform ignoreRoot,
            out float groundY)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + 3f;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                originY = Mathf.Max(originY, terrainY + 4f);
            }

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = originY - (worldPosition.y - 8f);

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                HitBuffer,
                Mathf.Max(4f, rayLength),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            bool foundGround = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = HitBuffer[i].collider;
                if (IsIgnorableDropSurface(hitCollider, ignoreRoot))
                    continue;

                float distance = HitBuffer[i].distance;
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                groundY = HitBuffer[i].point.y;
                foundGround = true;
            }

            if (foundGround)
                return true;

            if (terrain != null)
            {
                groundY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                return true;
            }

            return false;
        }

        private static float GetTerrainBaselineY(Vector3 worldPosition, float groundOffset)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return worldPosition.y;

            return terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;
        }

        private static bool IsWalkableGroundCollider(Collider collider)
        {
            if (collider is TerrainCollider)
                return true;

            if (collider.CompareTag("Dirt") || collider.CompareTag("Walkable"))
                return true;

            return IsWalkableGeometryName(collider.name);
        }

        private static bool IsWalkableGeometryName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            string lower = objectName.ToLowerInvariant();
            return lower.Contains("ramp")
                || lower.Contains("stair")
                || lower.Contains("step")
                || lower.Contains("floor")
                || lower.Contains("ground")
                || lower.Contains("terrain")
                || lower.Contains("platform")
                || lower.Contains("road")
                || lower.Contains("path");
        }

        private static bool IsIgnorableDropSurface(Collider hitCollider, Transform ignoreRoot)
        {
            if (hitCollider == null || hitCollider.isTrigger)
                return true;

            if (hitCollider.CompareTag("Player"))
                return true;

            Transform hitTransform = hitCollider.transform;
            if (ignoreRoot != null && (hitTransform == ignoreRoot || hitTransform.IsChildOf(ignoreRoot)))
                return true;

            return false;
        }
    }
}
