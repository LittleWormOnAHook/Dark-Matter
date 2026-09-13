using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Shared projectile VFX helpers for player, companion, and enemy fire.
    /// </summary>
    public static class CombatVfxUtility
    {
        public static ItemData ResolveAmmoItem(ItemData weapon, ItemData ammoItem)
        {
            if (ammoItem != null)
                return ammoItem;

            if (weapon == null)
                return null;

            if (weapon.defaultAmmoItem != null && weapon.defaultAmmoItem.CountsAsAmmo)
                return weapon.defaultAmmoItem;

            return null;
        }

        public static GameObject ResolveTracerPrefab(ItemData ammoItem, ItemData weapon)
        {
            return DMCombatFx.ResolveTracer(ammoItem, weapon);
        }

        public static void PlayParticleSystemsRecursive(GameObject root)
        {
            if (root == null)
                return;

            TracerVfxCache cache = root.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = root.AddComponent<TracerVfxCache>();
            cache.EnsureCached();

            ParticleSystem[] systems = cache.Particles;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                if (main.startDelay.mode == ParticleSystemCurveMode.Constant && main.startDelay.constant > 0f)
                    main.startDelay = 0f;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        /// <summary>
        /// Disables vendor auto-destroy drivers and schedules a pooled return after particles finish
        /// or <paramref name="maxLifeSeconds"/> (realtime), whichever comes first.
        /// </summary>
        public static void PreparePooledOneShotVfx(GameObject instance, float maxLifeSeconds, bool playParticles = true)
        {
            if (instance == null)
                return;

            TracerVfxCache cache = instance.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = instance.AddComponent<TracerVfxCache>();
            cache.EnsureCached();
            DisableVendorAutoReleaseBehaviours(cache);
            if (playParticles)
                PlayParticleSystemsRecursive(instance);

            SchedulePooledVfxRelease(instance, maxLifeSeconds, cache.Particles);
        }

        public static void SchedulePooledVfxRelease(
            GameObject instance,
            float maxLifeSeconds,
            ParticleSystem[] cachedParticles = null)
        {
            if (instance == null)
                return;

            PooledOneShotVfx lifetime = instance.GetComponent<PooledOneShotVfx>();
            if (lifetime == null)
                lifetime = instance.AddComponent<PooledOneShotVfx>();

            lifetime.Begin(maxLifeSeconds, cachedParticles);
        }

        /// <summary>
        /// Vendor VFX packs often ship Destroy-on-finish scripts that break pooling and leave orphans.
        /// Scans once per pooled instance via <see cref="TracerVfxCache"/>.
        /// </summary>
        public static void DisableVendorAutoReleaseBehaviours(TracerVfxCache cache)
        {
            if (cache == null)
                return;

            cache.EnsureCached();
            MonoBehaviour[] behaviours = cache.Behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                if (!IsVendorAutoReleaseBehaviour(behaviour))
                    continue;

                // OnEnable starts CheckIfAlive even when the component is disabled.
                // Leaving that coroutine running Destroy()s pooled WarFX after the first burst.
                behaviour.StopAllCoroutines();
                if (behaviour is CFX_AutoDestructShuriken cfx)
                {
                    cfx.OnlyDeactivate = true;
                    Object.Destroy(cfx);
                    continue;
                }

                behaviour.enabled = false;
            }

            cache.MarkVendorAutoReleaseDisabled();
        }

        private static bool IsVendorAutoReleaseBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour is CFX_AutoDestructShuriken)
                return true;

            string typeName = behaviour.GetType().Name;
            return typeName == "CFX_AutoStopLoopedEffect"
                || typeName == "CFX_Lifetime"
                || typeName == "AutoDestroy"
                || typeName == "DestroyAfterTime"
                || typeName == "DestroyAfterSeconds";
        }

        public static void DisableVendorAutoReleaseBehaviours(GameObject root)
        {
            if (root == null)
                return;

            TracerVfxCache cache = root.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = root.AddComponent<TracerVfxCache>();
            DisableVendorAutoReleaseBehaviours(cache);
        }

        /// <summary>
        /// Parents a pooled VFX instance to the struck collider (world pose preserved) and schedules pool return.
        /// </summary>
        public static void StickPooledVfxAtImpact(
            GameObject instance,
            Vector3 worldPoint,
            Quaternion worldRotation,
            Transform attach,
            float lifeSeconds,
            bool freezeProjectileVisual = false)
        {
            if (instance == null)
                return;

            Transform root = instance.transform;
            root.SetParent(null, true);
            if (attach != null)
            {
                root.SetParent(attach, true);
                root.SetPositionAndRotation(worldPoint, worldRotation);
            }
            else
            {
                root.SetPositionAndRotation(worldPoint, worldRotation);
            }

            if (freezeProjectileVisual)
            {
                HaltProjectileVfxAtImpact(instance);
                TracerVfxCache cache = instance.GetComponent<TracerVfxCache>();
                cache?.EnsureCached();
                DisableVendorAutoReleaseBehaviours(cache);
                SchedulePooledVfxRelease(instance, lifeSeconds, cache != null ? cache.Particles : null);
                return;
            }

            TracerVfxCache trailCache = instance.GetComponent<TracerVfxCache>();
            if (trailCache != null)
            {
                trailCache.EnsureCached();
                TrailRenderer[] trails = trailCache.Trails;
                for (int i = 0; i < trails.Length; i++)
                {
                    TrailRenderer trail = trails[i];
                    if (trail == null)
                        continue;

                    trail.emitting = false;
                }
            }

            PreparePooledOneShotVfx(instance, lifeSeconds);
        }

        /// <summary>
        /// Spawns Hovl <see cref="ParticleCollisionInstance"/> hit bursts when gameplay
        /// (<see cref="CombatProjectile"/>) resolves the impact — not when particles bounce.
        /// </summary>
        public static void SpawnVendorParticleCollisionEffects(
            GameObject vfxRoot,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Transform attach)
        {
            if (vfxRoot == null)
                return;

            ParticleCollisionInstance[] handlers = vfxRoot.GetComponentsInChildren<ParticleCollisionInstance>(true);
            if (handlers == null || handlers.Length == 0)
                return;

            Vector3 normal = hitNormal.sqrMagnitude > 0.0001f ? hitNormal.normalized : Vector3.up;

            for (int h = 0; h < handlers.Length; h++)
            {
                ParticleCollisionInstance handler = handlers[h];
                if (handler == null || handler.EffectsOnCollision == null)
                    continue;

                GameObject[] effects = handler.EffectsOnCollision;
                float offset = handler.Offset;
                float life = Mathf.Max(0.05f, handler.DestroyTimeDelay);
                Vector3 spawnPoint = hitPoint + normal * offset;
                Transform parent = handler.UseWorldSpacePosition ? attach : handler.transform;

                for (int e = 0; e < effects.Length; e++)
                {
                    GameObject prefab = effects[e];
                    if (prefab == null)
                        continue;

                    Quaternion rotation = ResolveVendorCollisionEffectRotation(handler, normal, spawnPoint);
                    GameObject instance = PoolManager.Spawn(prefab, spawnPoint, rotation, parent);
                    if (instance == null)
                        continue;

                    if (parent != null && handler.UseWorldSpacePosition)
                        instance.transform.SetPositionAndRotation(spawnPoint, rotation);

                    PreparePooledOneShotVfx(instance, life);
                }
            }
        }

        private static Quaternion ResolveVendorCollisionEffectRotation(
            ParticleCollisionInstance handler,
            Vector3 normal,
            Vector3 spawnPoint)
        {
            if (handler.UseFirePointRotation && handler.transform != null)
                return Quaternion.LookRotation(handler.transform.position - spawnPoint, Vector3.up);

            if (handler.useOnlyRotationOffset && handler.rotationOffset != Vector3.zero)
                return Quaternion.Euler(handler.rotationOffset);

            Quaternion look = Quaternion.LookRotation(normal, Vector3.up);
            if (handler.rotationOffset != Vector3.zero)
                look *= Quaternion.Euler(handler.rotationOffset);

            return look;
        }

        /// <summary>Freeze rider/tracer visuals on the surface after gameplay hit.</summary>
        public static void HaltProjectileVfxAtImpact(GameObject root)
        {
            if (root == null)
                return;

            TracerVfxCache cache = root.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = root.AddComponent<TracerVfxCache>();
            cache.EnsureCached();

            DisableVendorProjectileDrivers(cache);

            ParticleSystem[] systems = cache.Particles;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            TrailRenderer[] trails = cache.Trails;
            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                if (trail == null)
                    continue;

                trail.emitting = false;
                trail.Clear();
            }
        }

        public static Transform ResolveImpactAttachTransform(GameObject receiver)
        {
            return receiver != null ? receiver.transform : null;
        }

        public static void PrepareAttachedTracer(GameObject tracer, Vector3 muzzleWorldPosition, Vector3 fireDirection)
        {
            if (tracer == null)
                return;

            TracerVfxCache cache = tracer.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = tracer.AddComponent<TracerVfxCache>();
            cache.EnsureCached();

            DisableVendorProjectileDrivers(cache);
            DisableVendorParticleCollisionHandlers(cache);

            Transform root = tracer.transform;
            Vector3 forward = fireDirection.sqrMagnitude > 0.0001f
                ? fireDirection.normalized
                : (root.forward.sqrMagnitude > 0.0001f ? root.forward.normalized : Vector3.forward);

            if (root.parent != null)
            {
                root.localPosition = Vector3.zero;
                root.localRotation = Quaternion.identity;
            }
            else
            {
                root.position = muzzleWorldPosition;
                root.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            FlattenAuthoredDemoOffsets(cache, root);

            TrailRenderer[] trails = cache.Trails;
            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                if (trail == null)
                    continue;

                trail.emitting = true;
                trail.Clear();
                trail.AddPosition(muzzleWorldPosition);
                trail.AddPosition(muzzleWorldPosition + forward * 0.08f);
            }

            PlayParticleSystemsRecursive(tracer);
        }

        private static void DisableVendorProjectileDrivers(TracerVfxCache cache)
        {
            if (cache == null || cache.DriversDisabled)
                return;

            MonoBehaviour[] behaviours = cache.Behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "ProjectileMover" || typeName == "ProjectileMover2D")
                    behaviour.enabled = false;
            }

            Rigidbody[] bodies = cache.Bodies;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            Collider[] colliders = cache.Colliders;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            DisableVendorParticleCollisionHandlers(cache);

            cache.MarkDriversDisabled();
        }

        /// <summary>
        /// Gameplay owns impact spawns — keep vendor particle bounce, but block duplicate Instantiate paths.
        /// </summary>
        private static void DisableVendorParticleCollisionHandlers(TracerVfxCache cache)
        {
            if (cache == null)
                return;

            cache.EnsureCached();
            MonoBehaviour[] behaviours = cache.Behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is ParticleCollisionInstance pci)
                    Object.Destroy(pci);
            }
        }

        private static void FlattenAuthoredDemoOffsets(TracerVfxCache cache, Transform root)
        {
            if (cache == null || root == null || cache.OffsetsFlattened)
                return;

            const float flattenThreshold = 0.35f;
            Transform[] transforms = cache.Transforms;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null || t == root)
                    continue;

                Vector3 local = t.localPosition;
                if (Mathf.Abs(local.y) >= flattenThreshold)
                    local.y = 0f;
                if (Mathf.Abs(local.x) >= flattenThreshold)
                    local.x = 0f;
                if (Mathf.Abs(local.z) >= flattenThreshold)
                    local.z = 0f;

                t.localPosition = local;
            }

            cache.MarkOffsetsFlattened();
        }
    }
}
