using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Physical flying projectile shared by the player, companions, and enemies.
    /// Pooled via PoolManager; tracers are also pooled (no Instantiate/Destroy per shot).
    /// </summary>
    public class CombatProjectile : MonoBehaviour, IPoolable
    {
        private const int OverlapBufferSize = 16;
        private const int SweepBufferSize = 16;

        [SerializeField] private float speed = 85f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private float defaultSpeed;
        private GameObject owner;
        private AmmoType ammoType;
        private ItemData ammoItem;
        private ItemData weapon;
        private GameObject impactVfxOverride;
        private float damage;
        private bool isCritical;
        private float gravityScale;
        private Vector3 velocity;
        private Vector3 previousPosition;
        private float spawnTime;
        private bool hasHit;
        private bool launched;
        private bool deferMotionOneFrame;
        private GameObject tracerInstance;
        private GameObject tracerPrefabUsed;
        private AudioSource travelAudioSource;
        private Renderer[] cachedBodyRenderers;
        private static readonly Collider[] OverlapBuffer = new Collider[OverlapBufferSize];
        private static readonly RaycastHit[] SweepBuffer = new RaycastHit[SweepBufferSize];

        private void Awake()
        {
            defaultSpeed = speed;
            CacheBodyRenderers();
        }

        public void Launch(
            GameObject ownerRoot,
            Vector3 direction,
            float damageAmount,
            AmmoType type,
            ItemData ammoItemData = null,
            float speedOverride = 0f,
            bool critical = false,
            ItemData weaponItemData = null,
            GameObject impactVfxPrefabOverride = null)
        {
            owner = ownerRoot;
            ammoType = type;
            ammoItem = ammoItemData;
            weapon = weaponItemData;
            impactVfxOverride = impactVfxPrefabOverride;
            damage = damageAmount;
            isCritical = critical;
            gravityScale = ammoItemData != null ? ammoItemData.projectileGravityScale : 0f;
            float launchSpeed = speedOverride > 0f ? speedOverride : defaultSpeed;
            speed = launchSpeed;
            velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * launchSpeed : Vector3.forward * launchSpeed;
            previousPosition = transform.position;
            spawnTime = Time.time;
            hasHit = false;
            launched = true;
            deferMotionOneFrame = true;

            SpawnTracer();
            SpawnTravelAudio();
            EnsureProjectileVisible();
            TryResolveOverlapHit();
        }

        private void CacheBodyRenderers()
        {
            cachedBodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void SpawnTracer()
        {
            ReleaseTracerToPool();

            GameObject tracerPrefab = CombatVfxUtility.ResolveTracerPrefab(ammoItem, weapon);
            if (tracerPrefab == null)
                return;

            tracerPrefabUsed = tracerPrefab;
            tracerInstance = PoolManager.Spawn(tracerPrefab, transform.position, transform.rotation, transform);
            if (tracerInstance == null)
                return;

            CombatVfxUtility.PrepareAttachedTracer(tracerInstance, transform.position, velocity);
        }

        private void ReleaseTracerToPool()
        {
            if (tracerInstance == null)
                return;

            GameObject tracer = tracerInstance;
            tracerInstance = null;
            tracerPrefabUsed = null;

            // Detach so pool reparent does not fight an active projectile hierarchy.
            tracer.transform.SetParent(null, true);
            CombatVfxUtility.PreparePooledOneShotVfx(tracer, 2f);
        }

        private void StickProjectileVisualsAtImpact(Vector3 hitPoint, Vector3 impactNormal, Transform attach)
        {
            Quaternion rotation = impactNormal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(impactNormal, Vector3.up)
                : transform.rotation;

            if (tracerInstance != null)
            {
                GameObject tracer = tracerInstance;
                tracerInstance = null;
                tracerPrefabUsed = null;
                DMCombatFx.StickTracerAtImpact(tracer, hitPoint, impactNormal, attach);
                return;
            }

            GameObject stuckPrefab = CombatVfxUtility.ResolveTracerPrefab(ammoItem, weapon);
            if (stuckPrefab == null)
                stuckPrefab = DMCombatFx.ResolveProjectile(ammoItem, weapon);

            if (stuckPrefab == null)
                return;

            GameObject stuck = PoolManager.Spawn(stuckPrefab, hitPoint, rotation, attach);
            if (stuck == null)
                return;

            CombatVfxUtility.DisableVendorAutoReleaseBehaviours(stuck);
            CombatVfxUtility.StickPooledVfxAtImpact(stuck, hitPoint, rotation, attach, 4f, freezeProjectileVisual: true);
        }

        private void EnsureProjectileVisible()
        {
            if (cachedBodyRenderers == null || cachedBodyRenderers.Length == 0)
                CacheBodyRenderers();

            Renderer[] renderers = cachedBodyRenderers;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null)
                    renderer.enabled = true;
            }

            if (tracerInstance != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || renderer.transform.IsChildOf(tracerInstance.transform))
                        continue;

                    renderer.enabled = false;
                }

                return;
            }

            Transform visual = transform.Find("Visual");
            if (visual != null && visual.localScale.sqrMagnitude < 0.05f)
                visual.localScale = Vector3.one * 0.35f;
        }

        private void SpawnTravelAudio()
        {
            AudioClip clip = ammoItem != null ? ammoItem.projectileTravelSound : null;
            if (clip == null)
                return;

            if (travelAudioSource == null)
            {
                GameObject audioObject = new GameObject("TravelAudio");
                audioObject.transform.SetParent(transform, false);

                travelAudioSource = audioObject.AddComponent<AudioSource>();
                travelAudioSource.loop = true;
                travelAudioSource.playOnAwake = false;
                travelAudioSource.spatialBlend = 1f;
            }

            travelAudioSource.enabled = true;
            travelAudioSource.clip = clip;
            travelAudioSource.Play();
        }

        private void StopTravelAudio()
        {
            if (travelAudioSource == null)
                return;

            travelAudioSource.Stop();
            travelAudioSource.enabled = false;
        }

        private void Update()
        {
            if (!launched || hasHit)
                return;

            if (Time.time - spawnTime > maxLifetime)
            {
                StopTravelAudio();
                ReleaseTracerToPool();
                PoolManager.Release(gameObject);
                return;
            }

            if (deferMotionOneFrame)
            {
                deferMotionOneFrame = false;
                previousPosition = transform.position;
                TryResolveOverlapHit();
                return;
            }

            previousPosition = transform.position;

            if (gravityScale > 0f)
                velocity += Physics.gravity * gravityScale * Time.deltaTime;

            transform.position += velocity * Time.deltaTime;
            if (velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);

            SweepForHit();
        }

        private void SweepForHit()
        {
            if (TryResolveOverlapHit())
                return;

            Vector3 delta = transform.position - previousPosition;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;

            Vector3 origin = previousPosition;
            Vector3 direction = delta.normalized;

            // World sweep (triggers ignored). Owner colliders, and enemy capsules / ragdoll / weapon colliders
            // superseded by an active DM hitbox rig, are skipped so the shot continues to what is behind them.
            bool worldHit = TrySweepWorld(origin, direction, distance, out RaycastHit hit);

            // Per-bone enemy hitboxes (layer DMHitbox, triggers) with a thin sweep: the nearest valid hit wins.
            if (DMEnemyHitQuery.MaskWantsHitboxes(hitLayers)
                && DMEnemyHitQuery.SphereCastHitboxes(
                    origin,
                    DMEnemyHitQuery.HitboxSweepRadius,
                    direction,
                    distance,
                    owner,
                    out RaycastHit boxHit)
                && (!worldHit || boxHit.distance <= hit.distance))
            {
                ResolveHit(boxHit.collider, boxHit.point, boxHit.normal);
                return;
            }

            if (worldHit)
                ResolveHit(hit.collider, hit.point, hit.normal);
        }

        private bool TrySweepWorld(Vector3 origin, Vector3 direction, float distance, out RaycastHit best)
        {
            best = default;
            int count = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                SweepBuffer,
                distance,
                hitLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = SweepBuffer[i];
                Collider collider = candidate.collider;
                if (collider == null)
                    continue;
                // Starting overlaps (distance 0, point zero) are owned by TryResolveOverlapHit.
                if (candidate.distance <= 0f && candidate.point == Vector3.zero)
                    continue;
                if (CombatHitResolver.IsOwnerCollider(owner, collider))
                    continue;
                if (DMEnemyHitQuery.IsSupersededByHitboxRig(collider))
                    continue;
                if (candidate.distance >= bestDistance)
                    continue;

                bestDistance = candidate.distance;
                best = candidate;
                found = true;
            }

            return found;
        }

        private bool TryResolveOverlapHit()
        {
            if (hasHit)
                return false;

            float probeRadius = Mathf.Max(0.05f, radius);
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                probeRadius,
                OverlapBuffer,
                hitLayers,
                QueryTriggerInteraction.Ignore);
            if (count <= 0 && !DMEnemyHitQuery.AnyRigActive)
                return false;

            Collider best = null;
            float bestDistSq = float.MaxValue;
            Vector3 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = OverlapBuffer[i];
                if (candidate == null || CombatHitResolver.IsOwnerCollider(owner, candidate))
                    continue;
                if (DMEnemyHitQuery.IsSupersededByHitboxRig(candidate))
                    continue;

                Vector3 closest = GetClosestPointSafe(candidate, origin);
                float distSq = (closest - origin).sqrMagnitude;
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = candidate;
            }

            Vector3 hitPoint = best != null ? GetClosestPointSafe(best, origin) : origin;
            if (DMEnemyHitQuery.MaskWantsHitboxes(hitLayers)
                && DMEnemyHitQuery.OverlapHitboxes(
                    origin,
                    Mathf.Max(0.03f, DMEnemyHitQuery.HitboxSweepRadius),
                    owner,
                    out Collider boxCollider,
                    out Vector3 boxPoint,
                    out float boxDistSq)
                && boxDistSq <= bestDistSq)
            {
                best = boxCollider;
                hitPoint = boxPoint;
            }

            if (best == null)
                return false;
            Vector3 normal = origin - hitPoint;
            if (normal.sqrMagnitude < 0.0001f)
                normal = -velocity.normalized;
            else
                normal.Normalize();

            ResolveHit(best, hitPoint, normal);
            return true;
        }

        private static Vector3 GetClosestPointSafe(Collider collider, Vector3 point)
        {
            if (collider == null)
                return point;

            if (collider is BoxCollider
                || collider is SphereCollider
                || collider is CapsuleCollider
                || (collider is MeshCollider meshCollider && meshCollider.convex))
            {
                return collider.ClosestPoint(point);
            }

            return collider.bounds.ClosestPoint(point);
        }

        private void ResolveHit(Collider collider, Vector3 hitPoint, Vector3 surfaceNormal)
        {
            hasHit = true;
            launched = false;
            Vector3 travelDirection = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward;
            velocity = Vector3.zero;
            transform.position = hitPoint;
            StopTravelAudio();

            float appliedDamage = damage;
            if (ammoType == AmmoType.ResonanceStabilizer)
            {
                EchoStabilizeReceiver echo = collider.GetComponentInParent<EchoStabilizeReceiver>();
                if (echo != null)
                    appliedDamage = Mathf.Max(1f, damage * 0.15f);
            }

            DMCombatRangedResolver.ApplyHitOutcome(
                collider,
                hitPoint,
                surfaceNormal,
                travelDirection,
                appliedDamage,
                isCritical,
                owner,
                ammoItem,
                weapon,
                playImpactAudio: true,
                impactVfxOverride: impactVfxOverride);

            Vector3 impactNormal = surfaceNormal.sqrMagnitude > 0.0001f
                ? surfaceNormal
                : (travelDirection.sqrMagnitude > 0.0001f ? -travelDirection : Vector3.up);

            Transform attach = collider != null ? collider.transform : null;
            GameObject vfxSource = tracerInstance != null ? tracerInstance : gameObject;
            CombatVfxUtility.SpawnVendorParticleCollisionEffects(vfxSource, hitPoint, impactNormal, attach);
            StickProjectileVisualsAtImpact(hitPoint, impactNormal, attach);
            PoolManager.Release(gameObject);
        }

        public void OnSpawnedFromPool()
        {
            hasHit = false;
        }

        public void OnReturnedToPool()
        {
            launched = false;
            deferMotionOneFrame = false;
            StopTravelAudio();
            ReleaseTracerToPool();
        }
    }
}
