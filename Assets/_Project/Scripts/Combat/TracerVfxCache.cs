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
        public Light[] Lights { get; private set; }
        public bool DriversDisabled { get; private set; }
        public bool VendorAutoReleaseDisabled { get; private set; }
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
            Lights = GetComponentsInChildren<Light>(true);
            SuppressLightShadows(Lights);
        }

        /// <summary>
        /// Combat VFX lights are short flashes. Vendor packs (FORGE3D, Magic Spells) ship them with
        /// shadows on; each shadowed point light costs 6 HDRP shadow requests, so sustained fire overflows
        /// the HDRP asset's Max Shadows on Screen ("Max shadow requests count reached" spam) and wastes
        /// shadow-atlas time. Runs once per pooled scene instance; never touches prefab assets.
        /// </summary>
        public static void SuppressLightShadows(Light[] lights)
        {
            if (lights == null)
                return;

            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || light.shadows == LightShadows.None)
                    continue;
                if (!light.gameObject.scene.IsValid())
                    continue;

                light.shadows = LightShadows.None;
            }
        }

        public void MarkDriversDisabled() => DriversDisabled = true;
        public void MarkVendorAutoReleaseDisabled() => VendorAutoReleaseDisabled = true;
        public void MarkOffsetsFlattened() => OffsetsFlattened = true;
    }
}
