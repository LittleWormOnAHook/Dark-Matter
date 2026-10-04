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
        private static int _enemyLayer = int.MinValue;
        private static int _playerLayer = int.MinValue;
        private static int _companionLayer = int.MinValue;
        private static int _bodyPartLayer = int.MinValue;

        public static bool TryGetGroundY(Vector3 worldPosition, out float groundY, float groundOffset = 0f)
        {
            return TryGetGroundY(worldPosition, out groundY, groundOffset, null, float.NegativeInfinity);
        }

        public static bool TryGetGroundY(
            Vector3 worldPosition,
            out float groundY,
            float groundOffset,
            Transform ignoreRoot,
            float minAcceptedY)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + DefaultRaycastUp;
            if (TrySampleContainingTerrain(worldPosition, minAcceptedY, worldPosition.y, out float sampleY))
                originY = Mathf.Max(originY, sampleY + CreatureProbeUp);

            if (!TryPickClosestGroundY(worldPosition, originY, ignoreRoot, minAcceptedY, out float pickedY))
                return false;

            groundY = pickedY + groundOffset;
            return true;
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
            return SnapCreatureToGround(t, xz, float.NegativeInfinity);
        }

        public static Vector3 SnapCreatureToGround(Transform t, Vector3 xz, float minAcceptedY)
        {
            Vector3 snapped = ComputeCreatureGroundPosition(t, xz, minAcceptedY);
            if (t != null)
                t.position = snapped;
            return snapped;
        }

        /// <summary>Ground + foot-offset position without writing the transform (wander / patrol targets).</summary>
        public static Vector3 ComputeCreatureGroundPosition(Transform t, Vector3 xz)
        {
            return ComputeCreatureGroundPosition(t, xz, float.NegativeInfinity);
        }

        public static Vector3 ComputeCreatureGroundPosition(Transform t, Vector3 xz, float minAcceptedY)
        {
            Vector3 pos = xz;
            if (t == null)
                return SnapPositionToGround(pos);

            if (!TryGetCreatureGroundY(pos, t, minAcceptedY, out float groundY))
            {
                pos.y = Mathf.Max(t.position.y, minAcceptedY);
                return pos;
            }

            float footOffset = MeasureFootOffset(t);
            pos.y = Mathf.Max(minAcceptedY, groundY + footOffset + CreatureSkin);
            return pos;
        }

        public static bool TrySnapCreatureToGround(Transform t, Vector3 xz)
        {
            return TrySnapCreatureToGround(t, xz, float.NegativeInfinity);
        }

        public static bool TrySnapCreatureToGround(Transform t, Vector3 xz, float minAcceptedY)
        {
            if (t == null)
                return false;

            if (!TryGetCreatureGroundY(xz, t, minAcceptedY, out float groundY))
                return false;

            float footOffset = MeasureFootOffset(t);
            Vector3 pos = xz;
            pos.y = Mathf.Max(minAcceptedY, groundY + footOffset + CreatureSkin);
            t.position = pos;
            return true;
        }

        /// <summary>
        /// Pivot lift so the root capsule sits on the ground instead of a tiny 8cm skin
        /// that buries center-pivot humanoids.
        /// </summary>
        public static float ResolveCapsuleStandLift(GameObject instanceOrPrefab)
        {
            CapsuleCollider capsule = instanceOrPrefab != null
                ? instanceOrPrefab.GetComponent<CapsuleCollider>()
                : null;
            if (capsule == null)
                return 1.08f;

            float bottomLocal = capsule.center.y - capsule.height * 0.5f;
            return Mathf.Max(0.15f, -bottomLocal + 0.08f);
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

            CapsuleCollider rootCapsule = t.GetComponent<CapsuleCollider>();
            if (rootCapsule != null && rootCapsule.enabled && !rootCapsule.isTrigger)
                minY = Mathf.Min(minY, rootCapsule.bounds.min.y);

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

        private static bool TryGetCreatureGroundY(
            Vector3 worldPosition,
            Transform self,
            float minAcceptedY,
            out float groundY)
        {
            groundY = worldPosition.y;
            float originY = Mathf.Max(worldPosition.y + CreatureProbeUp, worldPosition.y + 2f);
            if (TrySampleContainingTerrain(worldPosition, minAcceptedY, worldPosition.y, out float sampleY))
                originY = Mathf.Max(originY, sampleY + CreatureProbeUp);

            return TryPickClosestGroundY(worldPosition, originY, self, minAcceptedY, out groundY);
        }

        private static bool TryPickClosestGroundY(
            Vector3 worldPosition,
            float originY,
            Transform ignoreRoot,
            float minAcceptedY,
            out float groundY)
        {
            groundY = worldPosition.y;
            float queryY = worldPosition.y;
            float bestY = float.NaN;
            float bestDelta = float.MaxValue;

            CollectRaycastGroundCandidates(
                worldPosition,
                originY,
                ignoreRoot,
                minAcceptedY,
                queryY,
                ref bestY,
                ref bestDelta);

            if (TrySampleContainingTerrain(worldPosition, minAcceptedY, queryY, out float sampleY))
                ConsiderCandidate(sampleY, minAcceptedY, queryY, ref bestY, ref bestDelta);

            if (float.IsNaN(bestY))
                return false;

            groundY = bestY;
            return true;
        }

        private static void CollectRaycastGroundCandidates(
            Vector3 worldPosition,
            float originY,
            Transform ignoreRoot,
            float minAcceptedY,
            float queryY,
            ref float bestY,
            ref float bestDelta)
        {
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

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                Collider col = hit.collider;
                if (col == null)
                    continue;
                if (ignoreRoot != null && col.transform != null && col.transform.IsChildOf(ignoreRoot))
                    continue;
                if (IsCharacterOrDummyCollider(col, ignoreRoot))
                    continue;
                if (!IsGroundCollider(col))
                    continue;

                ConsiderCandidate(hit.point.y, minAcceptedY, queryY, ref bestY, ref bestDelta);
            }
        }

        private static void ConsiderCandidate(
            float candidateY,
            float minAcceptedY,
            float queryY,
            ref float bestY,
            ref float bestDelta)
        {
            if (candidateY < minAcceptedY - 0.02f)
                return;

            float delta = Mathf.Abs(candidateY - queryY);
            if (delta >= bestDelta)
                return;

            bestDelta = delta;
            bestY = candidateY;
        }

        private static bool TrySampleContainingTerrain(
            Vector3 worldPosition,
            float minAcceptedY,
            float queryY,
            out float groundY)
        {
            groundY = worldPosition.y;
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return false;

            float bestY = float.NaN;
            float bestDelta = float.MaxValue;
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

                float sampleY = terrain.SampleHeight(worldPosition) + origin.y;
                if (sampleY < minAcceptedY - 0.02f)
                    continue;

                float delta = Mathf.Abs(sampleY - queryY);
                if (delta >= bestDelta)
                    continue;

                bestDelta = delta;
                bestY = sampleY;
            }

            if (float.IsNaN(bestY))
                return false;

            groundY = bestY;
            return true;
        }

        private static bool IsGroundCollider(Collider col)
        {
            if (col is TerrainCollider)
                return true;
            if (col.CompareTag("Terrain"))
                return true;

            int terrainLayer = TerrainLayer();
            if (terrainLayer >= 0 && col.gameObject.layer == terrainLayer)
                return true;

            // Combat sandbox / prototype floors are MeshCollider planes, not Terrain.
            return col is MeshCollider;
        }

        private static bool IsCharacterOrDummyCollider(Collider col, Transform ignoreRoot)
        {
            if (col == null)
                return true;

            Transform t = col.transform;
            if (ignoreRoot != null && t != null && t.IsChildOf(ignoreRoot))
                return true;

            int layer = col.gameObject.layer;
            if (layer == CachedLayer(ref _enemyLayer, "Enemy") && layer >= 0)
                return true;
            if (layer == CachedLayer(ref _playerLayer, "Player") && layer >= 0)
                return true;
            if (layer == CachedLayer(ref _companionLayer, "CompanionAI") && layer >= 0)
                return true;
            if (layer == CachedLayer(ref _bodyPartLayer, "BodyPart") && layer >= 0)
                return true;

            if (col.GetComponentInParent<EnemyHealth>() != null)
                return true;
            if (col.GetComponentInParent<CharacterController>() != null)
                return true;

            string n = col.gameObject.name;
            if (n.IndexOf("Dummy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (t != null && t.root != null &&
                t.root.name.IndexOf("Dummy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static bool IsTerrainHit(Collider col)
        {
            if (col == null)
                return false;
            if (col is TerrainCollider)
                return true;
            if (col.CompareTag("Terrain"))
                return true;
            int terrainLayer = TerrainLayer();
            return terrainLayer >= 0 && col.gameObject.layer == terrainLayer;
        }

        private static int TerrainLayer()
        {
            return CachedLayer(ref _terrainLayer, "Terrain");
        }

        private static int CachedLayer(ref int cache, string name)
        {
            if (cache == int.MinValue)
                cache = LayerMask.NameToLayer(name);
            return cache;
        }
    }
}
