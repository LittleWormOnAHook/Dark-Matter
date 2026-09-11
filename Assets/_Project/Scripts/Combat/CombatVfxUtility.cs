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
        public static void PreparePooledOneShotVfx(GameObject instance, float maxLifeSeconds)
        {
            if (instance == null)
                return;

            TracerVfxCache cache = instance.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = instance.AddComponent<TracerVfxCache>();
            cache.EnsureCached();
            DisableVendorAutoReleaseBehaviours(cache);
            PlayParticleSystemsRecursive(instance);

            PooledOneShotVfx lifetime = instance.GetComponent<PooledOneShotVfx>();
            if (lifetime == null)
                lifetime = instance.AddComponent<PooledOneShotVfx>();

            lifetime.Begin(maxLifeSeconds, cache.Particles);
        }

        /// <summary>
        /// Vendor VFX packs often ship Destroy-on-finish scripts that break pooling and leave orphans.
        /// Scans once per pooled instance via <see cref="TracerVfxCache"/>.
        /// </summary>
        public static void DisableVendorAutoReleaseBehaviours(TracerVfxCache cache)
        {
            if (cache == null || cache.VendorAutoReleaseDisabled)
                return;

            cache.EnsureCached();
            MonoBehaviour[] behaviours = cache.Behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "CFX_AutoDestructShuriken"
                    || typeName == "CFX_AutoStopLoopedEffect"
                    || typeName == "CFX_Lifetime"
                    || typeName == "AutoDestroy"
                    || typeName == "DestroyAfterTime"
                    || typeName == "DestroyAfterSeconds")
                {
                    behaviour.enabled = false;
                }
            }

            cache.MarkVendorAutoReleaseDisabled();
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
        /// Prepares an ammo tracer that rides a <see cref="CombatProjectile"/> so the visual
        /// begins at the barrel instead of mid-flight.
        /// </summary>
        public static void PrepareAttachedTracer(GameObject tracer, Vector3 muzzleWorldPosition, Vector3 fireDirection)
        {
            if (tracer == null)
                return;

            TracerVfxCache cache = tracer.GetComponent<TracerVfxCache>();
            if (cache == null)
                cache = tracer.AddComponent<TracerVfxCache>();
            cache.EnsureCached();

            DisableVendorProjectileDrivers(cache);

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

            cache.MarkDriversDisabled();
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
