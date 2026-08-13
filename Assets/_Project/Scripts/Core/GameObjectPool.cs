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
                instance.SetActive(false);
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

            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
            inactive.Push(instance);
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Object.Instantiate(prefab, poolRoot);
            PooledInstanceTag tag = instance.AddComponent<PooledInstanceTag>();
            tag.SourcePool = this;
            return instance;
        }
    }
}
