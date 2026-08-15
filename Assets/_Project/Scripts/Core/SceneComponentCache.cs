using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Short-TTL cache for FindObjectsByType results. World interaction dots / Use scanning
    /// were calling FindObjectsByType every frame; with large scenes that dominates Scripts CPU.
    /// </summary>
    public static class SceneComponentCache
    {
        private const float DefaultRefreshInterval = 0.4f;

        private struct Entry
        {
            public float ExpiresAtUnscaled;
            public Array Objects;
        }

        private static readonly Dictionary<Type, Entry> Cache = new Dictionary<Type, Entry>(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Cache.Clear();
        }

        public static void Invalidate()
        {
            Cache.Clear();
        }

        public static void Invalidate<T>() where T : UnityEngine.Object
        {
            Cache.Remove(typeof(T));
        }

        public static T[] GetAll<T>(
            FindObjectsInactive inactive = FindObjectsInactive.Exclude,
            float refreshInterval = DefaultRefreshInterval) where T : UnityEngine.Object
        {
            Type type = typeof(T);
            float now = Time.unscaledTime;

            if (Cache.TryGetValue(type, out Entry entry)
                && entry.Objects is T[] cached
                && now < entry.ExpiresAtUnscaled)
            {
                return cached;
            }

            T[] found = UnityEngine.Object.FindObjectsByType<T>(inactive);
            Cache[type] = new Entry
            {
                ExpiresAtUnscaled = now + Mathf.Max(0.05f, refreshInterval),
                Objects = found
            };
            return found;
        }
    }
}
