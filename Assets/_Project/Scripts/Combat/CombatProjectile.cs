using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Physical flying projectile shared by the player, companions, and enemies. Sweeps for hits
    /// every frame (kinematic, not Rigidbody-driven, so it stays lightweight),
    /// optionally arcs under gravity, and resolves damage/splash/status-effects/VFX through
    /// CombatHitResolver so every ammo type behaves consistently regardless of who fired it.
    /// Pooled via PoolManager (see CombatProjectileSpawner) instead of Instantiate/Destroy — Launch()
    /// performs the full per-shot state reset, and OnReturnedToPool()/DetachAndDestroyTracer() make
    /// sure nothing (tracer child, travel audio) leaks or duplicates across reuse cycles.
    /// </summary>
    public class CombatProjectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 85f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private GameObject owner;
        private AmmoType ammoType;
        private ItemData ammoItem;
        private ItemData weapon;
        private float damage;
        private bool isCritical;
        private float gravityScale;
        private Vector3 velocity;
        private Vector3 previousPosition;
        private float spawnTime;
        private bool hasHit;
        private bool launched;
        private GameObject tracerInstance;
        private AudioSource travelAudioSource;

        public void Launch(
            GameObject ownerRoot,
            Vector3 direction,
            float damageAmount,
            AmmoType type,
            ItemData ammoItemData = null,
            float speedOverride = 0f,
            bool critical = false,
            ItemData weaponItemData = null)
        {
            owner = ownerRoot;
            ammoType = type;
            ammoItem = ammoItemData;
            weapon = weaponItemData;
            damage = damageAmount;
            isCritical = critical;
            gravityScale = ammoItemData != null ? ammoItemData.projectileGravityScale : 0f;
            speed = speedOverride > 0f ? speedOverride : speed;
            velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.forward * speed;
            previousPosition = transform.position;
            spawnTime = Time.time;
            hasHit = false;
            launched = true;

            SpawnTracer();
            SpawnTravelAudio();
            EnsureProjectileVisible();
        }

        private void SpawnTracer()
        {
            // Reuse pooling can call Launch() again before OnReturnedToPool() ever ran (e.g. Launch
            // called directly without going through PoolManager) — guard against stacking a second
            // tracer child on top of a still-attached one.
            DetachAndDestroyTracer();

            GameObject tracerPrefab = CombatVfxUtility.ResolveTracerPrefab(ammoItem, weapon);
            if (tracerPrefab == null)
                return;

            tracerInstance = Instantiate(tracerPrefab, transform.position, transform.rotation, transform);
            CombatVfxUtility.PlayParticleSystemsRecursive(tracerInstance);
        }

        /// <summary>Detaches the tracer (so it can finish playing independently) and schedules its
        /// destruction. Safe to call multiple times / when there is no tracer.</summary>
        private void DetachAndDestroyTracer()
        {
            if (tracerInstance == null)
                return;

            tracerInstance.transform.SetParent(null, true);
            Destroy(tracerInstance, 2f);
            tracerInstance = null;
        }

        private void EnsureProjectileVisible()
        {
            // A pooled instance can be reused across launches where whether a tracer is present
            // varies (same projectile prefab shared by ammo types with/without a tracerPrefab) — so
            // always re-enable body renderers first instead of assuming they're still in whatever
            // state a previous launch left them in.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
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

        /// <summary>
        /// Looping sound that rides along with the flying projectile (parented to it, so it moves
        /// in step) and stops the instant the projectile hits or expires.
        /// </summary>
        private void SpawnTravelAudio()
        {
            AudioClip clip = ammoItem != null ? ammoItem.projectileTravelSound : null;
            if (clip == null)
                return;

            // Reuse the same child AudioSource across pooled reuse cycles instead of creating a new
            // "TravelAudio" GameObject every Launch() — otherwise disabled leftovers pile up as
            // children on a pooled instance that gets launched many times over its lifetime.
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
                DetachAndDestroyTracer();
                PoolManager.Release(gameObject);
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
            Vector3 delta = transform.position - previousPosition;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;

            if (Physics.SphereCast(
                    previousPosition,
                    radius,
                    delta.normalized,
                    out RaycastHit hit,
                    distance,
                    hitLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (CombatHitResolver.IsOwnerCollider(owner, hit.collider))
                    return;

                ResolveHit(hit.collider, hit.point, hit.normal);
            }
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

            // Use the actual surface normal from the sphere-cast hit, not the bullet's reversed
            // travel direction — otherwise impact decals orient toward wherever the shot came from
            // (often roughly facing the player) instead of lying flat against the surface they hit.
            Vector3 impactNormal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal : -velocity.normalized;
            CombatHitResolver.SpawnImpactVfx(ammoItem, weapon, hitPoint, impactNormal);

            if (ammoItem != null)
                CombatStatusEffect.Apply(ammoItem, collider.gameObject, owner);
            else
                CombatStatusEffect.Apply(ammoType, collider.gameObject, owner);

            DetachAndDestroyTracer();
            PoolManager.Release(gameObject);
        }

        /// <summary>IPoolable — Launch() performs the real per-shot reset; this is just a safety net.</summary>
        public void OnSpawnedFromPool()
        {
            hasHit = false;
        }

        /// <summary>IPoolable — runs whenever the pool takes this instance back, including on the
        /// normal hit/expiry paths above (which already clean up before releasing) and on any
        /// future forced-release path that might skip that cleanup.</summary>
        public void OnReturnedToPool()
        {
            launched = false;
            StopTravelAudio();
            DetachAndDestroyTracer();
        }
    }
}
