using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Static prefab-keyed pool registry. Central entry point for pooling any Instantiate/Destroy-heavy
    /// prefab (projectiles, hit VFX, muzzle flashes, status-effect VFX, etc) without every call site
    /// needing to own/track its own GameObjectPool. Lazily creates one pool per distinct prefab under
    /// a shared DontDestroyOnLoad root so pooled instances survive between scenes/menu transitions
    /// instead of getting destroyed with the level and forcing every pool to rebuild from scratch.
    ///
    /// Usage:
    ///   GameObject instance = PoolManager.Spawn(prefab, position, rotation);
    ///   ...
    ///   PoolManager.Release(instance);                        // instance remembers its own pool
    ///   PoolManager.ReleaseDelayed(host, instance, 2f);        // release after N seconds (replaces Destroy(obj, delay))
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<GameObject, GameObjectPool> PoolsByPrefab = new Dictionary<GameObject, GameObjectPool>();
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

        /// <summary>Bare MonoBehaviour so static call sites (CombatHitResolver, etc.) can schedule a
        /// delayed pool release without needing to own/pass a MonoBehaviour of their own.</summary>
        private class CoroutineRunner : MonoBehaviour
        {
        }

        /// <summary>
        /// Fetches (or spawns) an active instance of prefab at the given position/rotation. Parent
        /// defaults to the shared pool root's scene — pass parent explicitly (e.g. a muzzle socket)
        /// if the instance needs to ride along with a moving transform; it will be reparented back
        /// to the pool root automatically on Release.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
                return null;

            GameObjectPool pool = GetOrCreatePool(prefab);
            return pool.Get(position, rotation, parent);
        }

        /// <summary>Returns an instance to its originating pool. No-op (safe) if instance wasn't pooled.</summary>
        public static void Release(GameObject instance)
        {
            if (instance == null)
                return;

            if (instance.TryGetComponent(out PooledInstanceTag tag) && tag.SourcePool != null)
            {
                tag.SourcePool.Release(instance);
                return;
            }

            // Not a pooled instance (e.g. pooling disabled/prefab missing at spawn time) — fall back
            // to plain destroy so callers can migrate to pooling without special-casing failures.
            Object.Destroy(instance);
        }

        /// <summary>
        /// Coroutine-based replacement for <c>Destroy(instance, delay)</c> that releases to the pool
        /// instead of destroying. Runs on an internal, always-alive DontDestroyOnLoad runner, so
        /// static utility call sites (CombatHitResolver, etc.) don't need to own/pass a MonoBehaviour.
        /// </summary>
        public static void ReleaseDelayed(GameObject instance, float delay)
        {
            if (instance == null)
                return;

            EnsureRoot();
            runner.StartCoroutine(ReleaseAfterDelay(instance, delay));
        }

        /// <summary>Overload for call sites that already have a MonoBehaviour handy and would rather
        /// tie the delayed release's lifetime to it instead of the always-alive runner.</summary>
        public static void ReleaseDelayed(MonoBehaviour host, GameObject instance, float delay)
        {
            if (instance == null)
                return;

            if (host == null || !host.isActiveAndEnabled)
            {
                ReleaseDelayed(instance, delay);
                return;
            }

            host.StartCoroutine(ReleaseAfterDelay(instance, delay));
        }

        private static IEnumerator ReleaseAfterDelay(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            Release(instance);
        }

        /// <summary>Optional prewarm to avoid first-use instantiate hitches (e.g. call from a bootstrap script).</summary>
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
