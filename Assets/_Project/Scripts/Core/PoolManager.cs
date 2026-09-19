using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Static prefab-keyed pool registry for projectiles, hit VFX, muzzle flashes, tracers, etc.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<GameObject, GameObjectPool> PoolsByPrefab = new Dictionary<GameObject, GameObjectPool>();
        private static readonly Dictionary<float, WaitForSecondsRealtime> WaitCache =
            new Dictionary<float, WaitForSecondsRealtime>(8);
        private static Transform poolRoot;
        private static CoroutineRunner runner;

        private static Transform PoolRoot
        {
            get
            {
                EnsureRoot();
                return poolRoot;
            }
        }

        private static void EnsureRoot()
        {
            if (poolRoot != null)
                return;

            GameObject rootObject = new GameObject("PooledObjects");
            Object.DontDestroyOnLoad(rootObject);
            poolRoot = rootObject.transform;
            runner = rootObject.AddComponent<CoroutineRunner>();
        }

        private class CoroutineRunner : MonoBehaviour
        {
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
                return null;

            GameObjectPool pool = GetOrCreatePool(prefab);
            GameObject instance = pool.Get(position, rotation, parent);
            if (instance != null && instance.TryGetComponent(out PooledInstanceTag tag))
                tag.BumpLease();
            return instance;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null)
                return;

            if (instance.TryGetComponent(out PooledInstanceTag tag) && tag.SourcePool != null)
            {
                tag.SourcePool.Release(instance);
                return;
            }

            Object.Destroy(instance);
        }

        public static void ReleaseDelayed(GameObject instance, float delay)
        {
            if (instance == null)
                return;

            EnsureRoot();
            int lease = 0;
            if (instance.TryGetComponent(out PooledInstanceTag tag))
                lease = tag.LeaseId;

            runner.StartCoroutine(ReleaseAfterDelay(instance, delay, lease));
        }

        /// <summary>
        /// Runs on the pool runner so one-shot VFX still return after vendor scripts disable the instance.
        /// </summary>
        public static void ScheduleOneShotVfxRelease(
            GameObject instance,
            float maxLifeSeconds,
            ParticleSystem[] cachedParticles = null)
        {
            if (instance == null)
                return;

            EnsureRoot();
            int lease = 0;
            if (instance.TryGetComponent(out PooledInstanceTag tag))
                lease = tag.LeaseId;

            runner.StartCoroutine(OneShotVfxReleaseRoutine(instance, maxLifeSeconds, cachedParticles, lease));
        }

        /// <summary>
        /// Vendor VFX often disables the root without returning to the pool; finish the return off the disabled instance.
        /// </summary>
        public static void TryReturnOrphanedPooledInstance(GameObject instance)
        {
            if (instance == null || !instance.TryGetComponent(out PooledInstanceTag tag) || tag.SourcePool == null)
                return;

            if (IsInactiveUnderPoolRoot(instance))
                return;

            if (!instance.activeSelf)
                ReleaseDelayed(instance, 0f);
        }

        internal static bool IsInactiveUnderPoolRoot(GameObject instance)
        {
            return instance != null
                && poolRoot != null
                && !instance.activeSelf
                && instance.transform.parent == poolRoot;
        }

        public static void ReleaseDelayed(MonoBehaviour host, GameObject instance, float delay)
        {
            if (instance == null)
                return;

            if (host == null || !host.isActiveAndEnabled)
            {
                ReleaseDelayed(instance, delay);
                return;
            }

            int lease = 0;
            if (instance.TryGetComponent(out PooledInstanceTag tag))
                lease = tag.LeaseId;

            host.StartCoroutine(ReleaseAfterDelay(instance, delay, lease));
        }

        private static IEnumerator ReleaseAfterDelay(GameObject instance, float delay, int leaseAtSchedule)
        {
            yield return GetWait(delay);

            if (instance == null)
                yield break;

            // Stale timer: instance was reused for a newer shot.
            if (instance.TryGetComponent(out PooledInstanceTag tag) && tag.LeaseId != leaseAtSchedule)
                yield break;

            // Still return inactive VFX — particle prefabs often disable themselves before the timer fires.
            Release(instance);
        }

        private static WaitForSecondsRealtime GetWait(float delay)
        {
            float key = Mathf.Round(delay * 100f) * 0.01f;
            if (!WaitCache.TryGetValue(key, out WaitForSecondsRealtime wait))
            {
                wait = new WaitForSecondsRealtime(key);
                WaitCache[key] = wait;
            }

            return wait;
        }

        private static IEnumerator OneShotVfxReleaseRoutine(
            GameObject instance,
            float maxLifeSeconds,
            ParticleSystem[] cachedParticles,
            int leaseAtSchedule)
        {
            const float pollInterval = 0.25f;
            var pollWait = new WaitForSecondsRealtime(pollInterval);
            float maxLife = Mathf.Max(0.05f, maxLifeSeconds);
            float elapsed = 0f;
            const float minVisibleSeconds = 0.2f;

            ParticleSystem[] systems = cachedParticles;
            if (systems == null || systems.Length == 0)
            {
                if (instance != null)
                    systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            }

            while (instance != null && elapsed < maxLife)
            {
                if (instance.TryGetComponent(out PooledInstanceTag tag) && tag.LeaseId != leaseAtSchedule)
                    yield break;

                yield return pollWait;
                elapsed += pollInterval;

                if (elapsed < minVisibleSeconds || systems == null || systems.Length == 0)
                    continue;

                bool anyAlive = false;
                for (int i = 0; i < systems.Length; i++)
                {
                    ParticleSystem ps = systems[i];
                    if (ps != null && ps.IsAlive(true))
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                    break;
            }

            if (instance == null)
                yield break;

            if (instance.TryGetComponent(out PooledInstanceTag leaseTag) && leaseTag.LeaseId != leaseAtSchedule)
                yield break;

            Release(instance);
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
                return;

            GameObjectPool pool = GetOrCreatePool(prefab);
            List<GameObject> warm = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
                warm.Add(pool.Get(Vector3.zero, Quaternion.identity));

            for (int i = 0; i < warm.Count; i++)
                pool.Release(warm[i]);
        }

        private static GameObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (PoolsByPrefab.TryGetValue(prefab, out GameObjectPool pool))
                return pool;

            pool = new GameObjectPool(prefab, PoolRoot);
            PoolsByPrefab[prefab] = pool;
            return pool;
        }
    }
}
