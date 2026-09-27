using System.Collections.Generic;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Simple stack-backed object pool for one prefab. Deactivates instances instead of destroying
    /// them, and reparents them back under a shared pool root so they don't clutter the hierarchy
    /// or ride along with whatever transform they were last attached to (muzzle sockets, etc).
    /// Not thread-safe; intended for main-thread gameplay/VFX use only, matching everything else
    /// in this project.
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform poolRoot;
        private readonly Stack<GameObject> inactive = new Stack<GameObject>();

        public GameObject Prefab => prefab;

        public GameObjectPool(GameObject prefab, Transform poolRoot, int prewarmCount = 0)
        {
            this.prefab = prefab;
            this.poolRoot = poolRoot;

            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject instance = CreateInstance();
                inactive.Push(instance);
            }
        }

        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = null;

            while (inactive.Count > 0 && instance == null)
                instance = inactive.Pop();

            if (instance == null)
                instance = CreateInstance();

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, false);
            instanceTransform.SetPositionAndRotation(position, rotation);
            // Strip again in case a prior lease re-enabled vendor drivers somehow.
            StripVendorLifecycleComponents(instance);
            instance.SetActive(true);

            if (instance.TryGetComponent(out IPoolable poolable))
                poolable.OnSpawnedFromPool();

            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            // Avoid double-pushing the same instance into the inactive stack.
            if (!instance.activeSelf && instance.transform.parent == poolRoot)
                return;

            if (instance.TryGetComponent(out IPoolable poolable))
                poolable.OnReturnedToPool();

            // Detach from gameplay hosts first — SetParent onto poolRoot while the old parent is
            // activating/deactivating (e.g. TrainingDummy) throws a Unity console error.
            Transform currentParent = instance.transform.parent;
            if (currentParent != null && currentParent != poolRoot)
                instance.transform.SetParent(null, true);

            // Parent under the pool root first so OnDisable orphan-return sees the correct parent
            // and does not schedule a redundant ReleaseDelayed.
            if (poolRoot != null)
                instance.transform.SetParent(poolRoot, false);
            instance.SetActive(false);
            inactive.Push(instance);
        }

        private GameObject CreateInstance()
        {
            // Instantiate while the prefab asset is inactive so Awake/Start on vendor
            // AutoDestroy / ProjectileMover do not run (those schedule Destroy or spawn
            // unpooled muzzle/hit flashes that linger as hierarchy clones).
            bool prefabWasActive = prefab.activeSelf;
            if (prefabWasActive)
                prefab.SetActive(false);

            GameObject instance;
            try
            {
                instance = Object.Instantiate(prefab, poolRoot);
            }
            finally
            {
                if (prefabWasActive)
                    prefab.SetActive(true);
            }

            instance.SetActive(false);
            StripVendorLifecycleComponents(instance);

            PooledInstanceTag tag = instance.GetComponent<PooledInstanceTag>();
            if (tag == null)
                tag = instance.AddComponent<PooledInstanceTag>();
            tag.SourcePool = this;
            return instance;
        }

        /// <summary>
        /// Removes vendor one-shot / projectile drivers before the instance is activated.
        /// Uses type names so Core does not reference Combat/Hovl/WarFX assemblies.
        /// </summary>
        internal static void StripVendorLifecycleComponents(GameObject root)
        {
            if (root == null)
                return;

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                if (!IsVendorLifecycleTypeName(behaviour.GetType().Name))
                    continue;

                behaviour.StopAllCoroutines();
                Object.DestroyImmediate(behaviour);
            }
        }

        private static bool IsVendorLifecycleTypeName(string typeName)
        {
            return typeName == "ProjectileMover"
                || typeName == "ProjectileMover2D"
                || typeName == "AutoDestroyPS"
                || typeName == "AutoDestroy"
                || typeName == "DestroyAfterTime"
                || typeName == "DestroyAfterSeconds"
                || typeName == "CFX_AutoDestructShuriken"
                || typeName == "CFX_AutoStopLoopedEffect"
                || typeName == "CFX_LightIntensityFade"
                || typeName == "CFX_Lifetime"
                || typeName == "ParticleCollisionInstance"
                || typeName == "SFX_SimpleProjectile"
                || typeName == "SFX_PhysicsMotion";
        }
    }
}
