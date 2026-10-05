using System.Collections.Generic;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Live <see cref="EnemyHealth"/> list maintained by OnEnable/OnDisable — avoids repeated
    /// FindObjectsByType scans for combat focus, melee facing, and companion targeting.
    /// </summary>
    public static class EnemyHealthSceneRegistry
    {
        private static readonly List<EnemyHealth> Live = new List<EnemyHealth>(64);

        public static int Count => Live.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Live.Clear();

        internal static void Register(EnemyHealth health)
        {
            if (health == null || Live.Contains(health))
                return;

            Live.Add(health);
        }

        internal static void Unregister(EnemyHealth health)
        {
            if (health == null)
                return;

            Live.Remove(health);
        }

        /// <summary>Copies the live list into <paramref name="buffer"/> (clears first).</summary>
        public static void CopyTo(List<EnemyHealth> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < Live.Count; i++)
            {
                EnemyHealth health = Live[i];
                if (health != null)
                    buffer.Add(health);
            }
        }

        public static int FindNearestLiving(
            Vector3 playerPosition,
            float maxDistance,
            out EnemyHealth nearest)
        {
            nearest = null;
            if (maxDistance <= 0f)
                return 0;

            float bestSqr = maxDistance * maxDistance;
            int scanned = 0;
            for (int i = 0; i < Live.Count; i++)
            {
                EnemyHealth enemy = Live[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                scanned++;
                Vector3 delta = enemy.transform.position - playerPosition;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                nearest = enemy;
            }

            return scanned;
        }
    }
}
