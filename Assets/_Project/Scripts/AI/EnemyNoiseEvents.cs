using System;
using UnityEngine;

namespace Project.AI
{
    public static class EnemyNoiseEvents
    {
        public struct NoiseEvent
        {
            public Vector3 Position;
            public float Radius;
            public GameObject Source;
        }

        public static event Action<NoiseEvent> OnNoise;

        public static void RaiseNoise(Vector3 position, float radius, GameObject source)
        {
            if (radius <= 0f)
                return;

            OnNoise?.Invoke(new NoiseEvent
            {
                Position = position,
                Radius = radius,
                Source = source
            });
        }
    }
}
