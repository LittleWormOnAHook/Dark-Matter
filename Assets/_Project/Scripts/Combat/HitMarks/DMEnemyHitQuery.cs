using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Ranged hit queries against per-bone enemy hitboxes (layer DMHitbox, triggers) merged with the normal
    /// world query. Rules: the nearest valid result wins; an enemy's root capsule / ragdoll / weapon colliders are
    /// skipped while that enemy has an active <see cref="DMEnemyHitboxRig"/>. Static NonAlloc buffers, no GC.
    /// </summary>
    public static class DMEnemyHitQuery
    {
        public const int DefaultHitboxLayer = 26;
        public const string HitboxLayerName = "DMHitbox";
        private const int BufferSize = 16;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[BufferSize];
        private static readonly Collider[] OverlapBuffer = new Collider[BufferSize];
        private static int cachedLayer = -1;
        private static int cachedWantedMask;
        private static int lastSyncFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cachedLayer = -1;
            cachedWantedMask = 0;
            lastSyncFrame = -1;
        }

        public static int HitboxLayer
        {
            get
            {
                if (cachedLayer < 0)
                {
                    int named = LayerMask.NameToLayer(HitboxLayerName);
                    cachedLayer = named >= 0 ? named : DefaultHitboxLayer;
                }

                return cachedLayer;
            }
        }

        public static int HitboxMask => 1 << HitboxLayer;

        public const int PlayerLayerIndex = 8;

        /// <summary>
        /// Shared player / companion / enemy ranged mask: default raycast layers, player excluded,
        /// DMHitbox included so <see cref="MaskWantsHitboxes"/> merges per-bone queries.
        /// </summary>
        public static int SharedRangedLayerMask =>
            (Physics.DefaultRaycastLayers & ~(1 << PlayerLayerIndex)) | HitboxMask;

        /// <summary>True when any enemy has a live hitbox rig (otherwise callers can skip the second query).</summary>
        public static bool AnyRigActive => DMEnemyHitboxRig.ActiveRigCount > 0;

        public static float HitboxSweepRadius
        {
            get
            {
                DM_EnemyHitMarkProfile profile = DM_EnemyHitMarkProfile.LiveOrDefault;
                return profile != null ? Mathf.Max(0.001f, profile.hitboxSweepRadius) : 0.02f;
            }
        }

        /// <summary>
        /// True when <paramref name="collider"/> must not count as a ranged hit: it belongs to an enemy whose
        /// hitbox rig is active and it is not one of the hitboxes (root capsule, ragdoll bone, held weapon).
        /// </summary>
        public static bool IsSupersededByHitboxRig(Collider collider)
        {
            if (collider == null || !AnyRigActive)
                return false;
            if (collider.gameObject.layer == HitboxLayer && collider.isTrigger)
                return false;

            DMEnemyHitboxRig rig = collider.GetComponentInParent<DMEnemyHitboxRig>();
            return rig != null && rig.IsActive;
        }

        /// <summary>Hitbox behind this collider, when it is a live DM hitbox.</summary>
        public static bool TryGetHitbox(Collider collider, out DMEnemyHitbox hitbox)
        {
            hitbox = null;
            if (collider == null || collider.gameObject.layer != HitboxLayer)
                return false;

            return collider.TryGetComponent(out hitbox) && hitbox.IsLive;
        }

        /// <summary>Should the caller's ranged query also look at hitboxes for this layer mask?</summary>
        public static bool MaskWantsHitboxes(int mask)
        {
            if (!AnyRigActive)
                return false;

            if (cachedWantedMask == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                cachedWantedMask = HitboxMask;
                if (enemyLayer >= 0)
                    cachedWantedMask |= 1 << enemyLayer;
            }

            return (mask & cachedWantedMask) != 0;
        }

        /// <summary>
        /// Hitbox colliders ride animated bones and the project runs with auto sync transforms off, so sync once
        /// per frame before the first hitbox query so the query sees this frame's pose. (No longer reads the
        /// obsolete Physics.autoSyncTransforms; with auto sync on, an explicit sync is a cheap no-op.)
        /// </summary>
        private static void SyncOncePerFrame()
        {
            int frame = Time.frameCount;
            if (frame == lastSyncFrame)
                return;

            lastSyncFrame = frame;
            Physics.SyncTransforms();
        }

        public static bool RaycastHitboxes(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            GameObject owner,
            out RaycastHit best)
        {
            best = default;
            if (!AnyRigActive || maxDistance <= 0f)
                return false;

            SyncOncePerFrame();
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                HitBuffer,
                maxDistance,
                HitboxMask,
                QueryTriggerInteraction.Collide);
            return PickNearestHitbox(count, owner, out best);
        }

        public static bool SphereCastHitboxes(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float maxDistance,
            GameObject owner,
            out RaycastHit best)
        {
            best = default;
            if (!AnyRigActive || maxDistance <= 0f)
                return false;

            SyncOncePerFrame();
            int count = Physics.SphereCastNonAlloc(
                origin,
                Mathf.Max(0.001f, radius),
                direction,
                HitBuffer,
                maxDistance,
                HitboxMask,
                QueryTriggerInteraction.Collide);
            return PickNearestHitbox(count, owner, out best);
        }

        /// <summary>Closest live hitbox touching a small sphere (projectile spawn / overlap fallback).</summary>
        public static bool OverlapHitboxes(
            Vector3 center,
            float radius,
            GameObject owner,
            out Collider bestCollider,
            out Vector3 bestPoint,
            out float bestDistanceSq)
        {
            bestCollider = null;
            bestPoint = center;
            bestDistanceSq = float.MaxValue;
            if (!AnyRigActive)
                return false;

            SyncOncePerFrame();
            int count = Physics.OverlapSphereNonAlloc(
                center,
                Mathf.Max(0.001f, radius),
                OverlapBuffer,
                HitboxMask,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = OverlapBuffer[i];
                OverlapBuffer[i] = null;
                if (candidate == null || !TryGetHitbox(candidate, out _))
                    continue;
                if (CombatHitResolver.IsOwnerCollider(owner, candidate))
                    continue;

                Vector3 closest = candidate is MeshCollider mc && !mc.convex
                    ? candidate.bounds.ClosestPoint(center)
                    : candidate.ClosestPoint(center);
                float distSq = (closest - center).sqrMagnitude;
                if (distSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distSq;
                bestCollider = candidate;
                bestPoint = closest;
            }

            return bestCollider != null;
        }

        private static bool PickNearestHitbox(int count, GameObject owner, out RaycastHit best)
        {
            best = default;
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                Collider collider = hit.collider;
                if (collider == null || !TryGetHitbox(collider, out _))
                    continue;
                if (CombatHitResolver.IsOwnerCollider(owner, collider))
                    continue;
                // Initial overlaps report distance 0 / point zero; overlap probes own that case.
                if (hit.distance <= 0f && hit.point == Vector3.zero)
                    continue;
                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                best = hit;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// World contact point + outward normal for bone-parented HDRP decals. Prefers a raycast on the struck
        /// collider (hitboxes are triggers); falls back to resolver / travel hints, then shape exterior.
        /// </summary>
        public static void ResolveMarkSurface(
            Collider collider,
            Vector3 hitPoint,
            Vector3 incomingDirection,
            Vector3 hintNormal,
            out Vector3 surfacePoint,
            out Vector3 outwardNormal)
        {
            surfacePoint = hitPoint;
            outwardNormal = hintNormal.sqrMagnitude > 0.0001f ? hintNormal.normalized : Vector3.zero;

            if (collider == null)
            {
                if (outwardNormal == Vector3.zero)
                    outwardNormal = Vector3.up;
                return;
            }

            surfacePoint = ClosestPointOnCollider(collider, hitPoint);

            Vector3 incoming = incomingDirection.sqrMagnitude > 0.0001f
                ? incomingDirection.normalized
                : outwardNormal != Vector3.zero
                    ? -outwardNormal
                    : Vector3.forward;

            const float probe = 1.25f;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                float back = probe * (attempt + 1);
                Vector3 origin = surfacePoint + incoming * back;
                Ray ray = new Ray(origin, -incoming);
                if (collider.Raycast(ray, out RaycastHit hit, back + 0.25f))
                {
                    surfacePoint = hit.point;
                    outwardNormal = hit.normal.normalized;
                    EnsureOutwardNormalFacesIncoming(incomingDirection, ref outwardNormal);
                    return;
                }
            }

            if (outwardNormal == Vector3.zero)
            {
                Vector3 center = collider.bounds.center;
                outwardNormal = (surfacePoint - center).normalized;
                if (outwardNormal.sqrMagnitude < 0.0001f)
                    outwardNormal = collider.transform.up.sqrMagnitude > 0.0001f ? collider.transform.up : Vector3.up;
            }

            EnsureOutwardNormalFacesIncoming(incomingDirection, ref outwardNormal);
        }

        /// <summary>Decals project along -normal; flip when the resolver handed us an inward-facing normal.</summary>
        private static void EnsureOutwardNormalFacesIncoming(Vector3 incomingDirection, ref Vector3 outwardNormal)
        {
            if (outwardNormal.sqrMagnitude < 0.0001f || incomingDirection.sqrMagnitude < 0.0001f)
                return;

            if (Vector3.Dot(outwardNormal.normalized, incomingDirection.normalized) > 0.01f)
                outwardNormal = -outwardNormal;
        }

        private static Vector3 ClosestPointOnCollider(Collider collider, Vector3 point)
        {
            if (collider is BoxCollider
                || collider is SphereCollider
                || collider is CapsuleCollider
                || (collider is MeshCollider meshCollider && meshCollider.convex))
            {
                return collider.ClosestPoint(point);
            }

            return collider.bounds.ClosestPoint(point);
        }
    }
}
