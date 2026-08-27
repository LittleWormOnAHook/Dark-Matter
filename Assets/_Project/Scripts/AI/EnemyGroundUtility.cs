using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Shared ground height sampling for transform-driven enemies on prototype / Gaia terrain.
    /// </summary>
    public static class EnemyGroundUtility
    {
        private const float DefaultRaycastUp = 40f;
        private const float DefaultRaycastDown = 80f;
        private const float CreatureProbeUp = 8f;
        private const float CreatureSkin = 0.02f;
        private const int HitBufferSize = 24;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];
        private static int _terrainLayer = int.MinValue;

        public static bool TryGetGroundY(Vector3 worldPosition, out float groundY, float groundOffset = 0f)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + DefaultRaycastUp;
            if (TrySampleContainingTerrain(worldPosition, out float sampleY))
                originY = Mathf.Max(originY, sampleY + CreatureProbeUp);

            if (TryRaycastTerrain(worldPosition, originY, out float rayY))
            {
                groundY = rayY + groundOffset;
                return true;
            }

            if (TrySampleContainingTerrain(worldPosition, out sampleY))
            {
                groundY = sampleY + groundOffset;
                return true;
            }

            Vector3 origin = new Vector3(worldPosition.x, worldPosition.y + DefaultRaycastUp, worldPosition.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, DefaultRaycastDown, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
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

        /// <summary>
        /// Places <paramref name="t"/> so the lowest renderer/collider bound sits on the terrain
        /// (small skin), not the pivot. Needed when the mesh pivot is at center rather than feet.
        /// </summary>
        public static Vector3 SnapCreatureToGround(Transform t, Vector3 xz)
        {
            Vector3 snapped = ComputeCreatureGroundPosition(t, xz);
            if (t != null)
                t.position = snapped;
            return snapped;
        }

        /// <summary>Ground + foot-offset position without writing the transform (wander / patrol targets).</summary>
        public static Vector3 ComputeCreatureGroundPosition(Transform t, Vector3 xz)
        {
            Vector3 pos = xz;
            if (t == null)
                return SnapPositionToGround(pos);

            if (!TryGetCreatureGroundY(pos, t, out float groundY))
            {
                pos.y = t.position.y;
                return pos;
            }

            float footOffset = MeasureFootOffset(t);
            pos.y = groundY + footOffset + CreatureSkin;
            return pos;
        }

        public static bool TrySnapCreatureToGround(Transform t, Vector3 xz)
        {
            if (t == null)
                return false;

            if (!TryGetCreatureGroundY(xz, t, out float groundY))
                return false;

            float footOffset = MeasureFootOffset(t);
            Vector3 pos = xz;
            pos.y = groundY + footOffset + CreatureSkin;
            t.position = pos;
            return true;
        }

        /// <summary>
        /// World-space distance from transform origin down to the lowest renderer or
        /// non-trigger collider bound. 0 when the pivot is already at/below the feet.
        /// </summary>
        public static float MeasureFootOffset(Transform t)
        {
            if (t == null)
                return 0f;

            float minY = float.PositiveInfinity;

            CharacterController controller = t.GetComponent<CharacterController>();
            if (controller != null)
                minY = Mathf.Min(minY, controller.bounds.min.y);

            Collider[] colliders = t.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || col.isTrigger)
                    continue;
                minY = Mathf.Min(minY, col.bounds.min.y);
            }

            // Collider is the contact. Skinned localBounds is bind-pose AABB and often
            // hangs below the posed feet, which lifts the whole creature on snap.
            if (!float.IsPositiveInfinity(minY))
                return Mathf.Max(0f, t.position.y - minY);

            Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || !rend.enabled)
                    continue;
                if (rend is ParticleSystemRenderer)
                    continue;

                // Bind-pose / mesh local AABB so walk cycles do not bob the offset.
                if (rend is SkinnedMeshRenderer skinned)
                    minY = Mathf.Min(minY, LocalBoundsWorldMinY(skinned.transform, skinned.localBounds));
                else if (rend is MeshRenderer meshRenderer)
                {
                    MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        minY = Mathf.Min(minY, LocalBoundsWorldMinY(meshRenderer.transform, filter.sharedMesh.bounds));
                    else
                        minY = Mathf.Min(minY, rend.bounds.min.y);
                }
                else
                    minY = Mathf.Min(minY, rend.bounds.min.y);
            }

            if (float.IsPositiveInfinity(minY))
                return 0f;

            return Mathf.Max(0f, t.position.y - minY);
        }

        private static float LocalBoundsWorldMinY(Transform space, Bounds localBounds)
        {
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            float minY = float.PositiveInfinity;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 world = space.TransformPoint(c + new Vector3(e.x * x, e.y * y, e.z * z));
                        if (world.y < minY)
                            minY = world.y;
                    }
                }
            }

            return minY;
        }

        public static bool IsAbnormallyHigh(Vector3 worldPosition, float maxAboveGround = 8f)
        {
            if (!TryGetGroundY(worldPosition, out float groundY))
                return false;

            return worldPosition.y > groundY + maxAboveGround;
        }

        private static bool TryGetCreatureGroundY(Vector3 worldPosition, Transform self, out float groundY)
        {
            groundY = worldPosition.y;
            float originY = Mathf.Max(worldPosition.y + CreatureProbeUp, worldPosition.y + 2f);
            if (TrySampleContainingTerrain(worldPosition, out float sampleY))
                originY = Mathf.Max(originY, sampleY + CreatureProbeUp);

            if (TryRaycastTerrain(worldPosition, originY, out float rayY, self))
            {
                groundY = rayY;
                return true;
            }

            if (TrySampleContainingTerrain(worldPosition, out sampleY))
            {
                groundY = sampleY;
                return true;
            }

            // No Gaia tile / TerrainCollider yet — leave the creature where it is.
            return false;
        }

        private static bool TryRaycastTerrain(Vector3 worldPosition, float originY, out float groundY, Transform ignoreRoot = null)
        {
            groundY = worldPosition.y;
            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float distance = originY - worldPosition.y + DefaultRaycastDown;
            if (distance < 1f)
                distance = DefaultRaycastDown;

            int mask = Physics.DefaultRaycastLayers;
            int terrainLayer = TerrainLayer();
            if (terrainLayer >= 0)
                mask |= 1 << terrainLayer;

            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                HitBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float bestY = float.NaN;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                Collider col = hit.collider;
                if (col == null)
                    continue;
                if (ignoreRoot != null && col.transform != null && col.transform.IsChildOf(ignoreRoot))
                    continue;
                if (!IsTerrainHit(col))
                    continue;
                if (hit.distance >= bestDist)
                    continue;
                bestDist = hit.distance;
                bestY = hit.point.y;
            }

            if (float.IsNaN(bestY))
                return false;

            groundY = bestY;
            return true;
        }

        private static bool TrySampleContainingTerrain(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return false;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || !terrain.enabled || terrain.terrainData == null)
                    continue;

                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (worldPosition.x < origin.x || worldPosition.x > origin.x + size.x)
                    continue;
                if (worldPosition.z < origin.z || worldPosition.z > origin.z + size.z)
                    continue;

                groundY = terrain.SampleHeight(worldPosition) + origin.y;
                return true;
            }

            return false;
        }

        private static bool IsTerrainHit(Collider col)
        {
            if (col is TerrainCollider)
                return true;
            if (col.CompareTag("Terrain"))
                return true;
            int terrainLayer = TerrainLayer();
            return terrainLayer >= 0 && col.gameObject.layer == terrainLayer;
        }

        private static int TerrainLayer()
        {
            if (_terrainLayer == int.MinValue)
                _terrainLayer = LayerMask.NameToLayer("Terrain");
            return _terrainLayer;
        }
    }
}
