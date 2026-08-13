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
            PoolManager.ReleaseDelayed(tracer, 2f);
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
            float remaining = distance;

            // Skip owner colliders and continue the sweep so self-hits do not swallow the shot.
            const int maxSkips = 4;
            for (int skip = 0; skip < maxSkips && remaining > 0.0001f; skip++)
            {
                if (!Physics.SphereCast(
                        origin,
                        radius,
                        direction,
                        out RaycastHit hit,
                        remaining,
                        hitLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    return;
                }

                if (CombatHitResolver.IsOwnerCollider(owner, hit.collider))
                {
                    float advance = Mathf.Max(0.02f, hit.distance + 0.01f);
                    origin += direction * advance;
                    remaining -= advance;
                    continue;
                }

                ResolveHit(hit.collider, hit.point, hit.normal);
                return;
            }
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
            if (count <= 0)
                return false;

            Collider best = null;
            float bestDistSq = float.MaxValue;
            Vector3 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = OverlapBuffer[i];
                if (candidate == null || CombatHitResolver.IsOwnerCollider(owner, candidate))
                    continue;

                Vector3 closest = GetClosestPointSafe(candidate, origin);
                float distSq = (closest - origin).sqrMagnitude;
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = candidate;
            }

            if (best == null)
                return false;

            Vector3 hitPoint = GetClosestPointSafe(best, origin);
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
            StopTravelAudio();

            float appliedDamage = damage;
            if (ammoType == AmmoType.ResonanceStabilizer)
            {
                EchoStabilizeReceiver echo = collider.GetComponentInParent<EchoStabilizeReceiver>();
                if (echo != null)
                    appliedDamage = Mathf.Max(1f, damage * 0.15f);
            }

            CombatHitResolver.ApplyDirectHit(collider, hitPoint, velocity, appliedDamage, isCritical, owner);

            if (ammoItem != null && ammoItem.HasSplashDamage)
                CombatHitResolver.ApplySplash(ammoItem, hitPoint, appliedDamage, owner, collider);

            Vector3 impactNormal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal : -velocity.normalized;
            CombatHitResolver.HandleRangedWorldImpact(
                ammoItem,
                weapon,
                hitPoint,
                impactNormal,
                owner,
                playHitAudio: true,
                impactVfxOverride: impactVfxOverride);

            if (ammoItem != null)
                CombatStatusEffect.Apply(ammoItem, collider.gameObject, owner);
            else
                CombatStatusEffect.Apply(ammoType, collider.gameObject, owner);

            ReleaseTracerToPool();
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
