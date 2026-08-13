using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Caches particle/trail/driver refs on a tracer VFX instance so fire-time setup
    /// does not allocate GetComponentsInChildren arrays every shot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TracerVfxCache : MonoBehaviour
    {
        public ParticleSystem[] Particles { get; private set; }
        public TrailRenderer[] Trails { get; private set; }
        public Rigidbody[] Bodies { get; private set; }
        public Collider[] Colliders { get; private set; }
        public MonoBehaviour[] Behaviours { get; private set; }
        public Transform[] Transforms { get; private set; }
        public bool DriversDisabled { get; private set; }
        public bool OffsetsFlattened { get; private set; }

        public void EnsureCached()
        {
            if (Particles != null)
                return;

            Particles = GetComponentsInChildren<ParticleSystem>(true);
            Trails = GetComponentsInChildren<TrailRenderer>(true);
            Bodies = GetComponentsInChildren<Rigidbody>(true);
            Colliders = GetComponentsInChildren<Collider>(true);
            Behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            Transforms = GetComponentsInChildren<Transform>(true);
        }

        public void MarkDriversDisabled() => DriversDisabled = true;
        public void MarkOffsetsFlattened() => OffsetsFlattened = true;
    }
}
