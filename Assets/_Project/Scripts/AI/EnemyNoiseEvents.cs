using System;
using UnityEngine;

namespace Project.AI
{
    public enum EnemyNoiseKind
    {
        /// <summary>Generic noise (melee swings, ally alerts, footsteps-style cues).</summary>
        Generic = 0,
        /// <summary>Ranged projectile/hitscan impact — eligible for hearing aggro.</summary>
        CombatImpact = 1
    }

    public static class EnemyNoiseEvents
    {
        public struct NoiseEvent
        {
            public Vector3 Position;
            public float Radius;
            public GameObject Source;
            public EnemyNoiseKind Kind;
        }

        public static event Action<NoiseEvent> OnNoise;

        public static void RaiseNoise(Vector3 position, float radius, GameObject source)
        {
            RaiseNoise(position, radius, source, EnemyNoiseKind.Generic);
        }

        public static void RaiseNoise(Vector3 position, float radius, GameObject source, EnemyNoiseKind kind)
        {
            if (radius <= 0f)
                return;

            OnNoise?.Invoke(new NoiseEvent
            {
                Position = position,
                Radius = radius,
                Source = source,
                Kind = kind
            });
        }

        /// <summary>Ranged hit impact noise at the world hit point (walls, props, or damageables).</summary>
        public static void RaiseCombatImpactNoise(Vector3 position, float radius, GameObject source)
        {
            RaiseNoise(position, radius, source, EnemyNoiseKind.CombatImpact);
        }
    }
}
