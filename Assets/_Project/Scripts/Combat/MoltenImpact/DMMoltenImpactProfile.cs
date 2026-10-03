using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Look of a hot ammo impact on one kind of surface: a glowing spot that cools and fades,
    /// plus molten drips that fall with gravity and stop where they land.
    /// Surfaces pick a profile through <see cref="DMMoltenImpactMap"/> (Resources/Combat/DMMoltenImpactMap).
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Molten Impact Profile", fileName = "Molten_Surface")]
    public class DMMoltenImpactProfile : ScriptableObject
    {
        [Header("Glow Spot")]
        [Tooltip("Show a glowing spot on the surface at the hit point.")]
        public bool glow = true;
        [Tooltip("Colour while the spot is hottest.")]
        [ColorUsage(false, true)] public Color hotColor = new Color(1f, 0.42f, 0.08f, 1f);
        [Tooltip("Colour the spot cools to before it fades away.")]
        [ColorUsage(false, true)] public Color coolColor = new Color(0.35f, 0.03f, 0.005f, 1f);
        [Tooltip("Brightness of the hot spot (screen-relative, above 1 blooms).")]
        [Min(0f)] public float glowIntensity = 4f;
        [Tooltip("Spot radius in meters.")]
        [Min(0.005f)] public float glowRadius = 0.07f;
        [Tooltip("Seconds from hot to cool colour.")]
        [Min(0.05f)] public float coolSeconds = 2f;
        [Tooltip("Seconds to fade out after cooling.")]
        [Min(0.05f)] public float fadeSeconds = 1.5f;
        [Tooltip("Ragged / mottled look of the spot. 0 = smooth disc.")]
        [Range(0f, 1f)] public float glowNoise = 0.45f;
        [Tooltip("Meters the spot is pulled toward the camera so it is not hidden when the collider sits slightly inside the visible mesh (baked rocks collide on a lower LOD).")]
        [Min(0f)] public float glowDepthBias = 0.08f;

        [Header("Molten Drips")]
        [Tooltip("Drips thrown at the moment of impact.")]
        [Range(0, 32)] public int dripBurst = 4;
        [Tooltip("Drips per second that keep forming after the hit (running metal). 0 = burst only.")]
        [Min(0f)] public float dripRate = 0f;
        [Tooltip("How long drips keep forming, in seconds.")]
        [Min(0f)] public float dripRunSeconds = 0f;
        [Tooltip("Drip size range in meters.")]
        public Vector2 dripSize = new Vector2(0.01f, 0.022f);
        [Tooltip("Launch speed range (m/s) away from the surface.")]
        public Vector2 dripSpeed = new Vector2(0.2f, 0.9f);
        [Tooltip("Gravity multiplier on drips.")]
        [Min(0f)] public float dripGravity = 1f;
        [Tooltip("Max drip speed (m/s): low values make thick, slow-running molten metal.")]
        [Min(0.05f)] public float dripMaxSpeed = 3f;
        [Tooltip("Seconds a drip lives (glows, cools, fades), landing included.")]
        [Min(0.1f)] public float dripLifetime = 2.5f;
        [Tooltip("Brightness of the drips.")]
        [Min(0f)] public float dripIntensity = 5f;
        [Tooltip("Stretch along the fall direction while moving.")]
        [Min(0f)] public float dripStretch = 0.03f;
        [Tooltip("Drips collide with the world and stop where they land.")]
        public bool dripsCollide = true;

        public bool HasDrips => dripBurst > 0 || (dripRate > 0f && dripRunSeconds > 0f);

        public float TotalLifetime
        {
            get
            {
                float glowLife = glow ? coolSeconds + fadeSeconds : 0f;
                float dripLife = HasDrips ? dripRunSeconds + dripLifetime : 0f;
                return Mathf.Max(glowLife, dripLife);
            }
        }

        /// <summary>Built-in starting points used by the default map (and when no map asset exists).</summary>
        public static DMMoltenImpactProfile CreatePreset(string surface)
        {
            var p = CreateInstance<DMMoltenImpactProfile>();
            p.name = "Molten_" + surface;
            switch (surface)
            {
                case "Metal":
                    p.hotColor = new Color(1f, 0.55f, 0.16f, 1f);
                    p.coolColor = new Color(0.45f, 0.06f, 0.01f, 1f);
                    p.glowIntensity = 9f;
                    p.glowRadius = 0.06f;
                    p.coolSeconds = 2.8f;
                    p.fadeSeconds = 2f;
                    p.glowNoise = 0.3f;
                    p.dripBurst = 4;
                    p.dripRate = 5f;
                    p.dripRunSeconds = 1.4f;
                    p.dripSize = new Vector2(0.008f, 0.018f);
                    p.dripSpeed = new Vector2(0.05f, 0.35f);
                    p.dripGravity = 0.6f;
                    p.dripMaxSpeed = 0.9f;
                    p.dripLifetime = 3f;
                    p.dripIntensity = 9f;
                    p.dripStretch = 0.06f;
                    break;
                case "Rock":
                    p.hotColor = new Color(1f, 0.32f, 0.05f, 1f);
                    p.coolColor = new Color(0.3f, 0.025f, 0.005f, 1f);
                    p.glowIntensity = 2.2f;
                    p.glowRadius = 0.075f;
                    p.coolSeconds = 1.4f;
                    p.fadeSeconds = 1.2f;
                    p.glowNoise = 0.6f;
                    p.dripBurst = 2;
                    p.dripSize = new Vector2(0.008f, 0.016f);
                    p.dripSpeed = new Vector2(0.3f, 1.1f);
                    p.dripLifetime = 1.8f;
                    p.dripIntensity = 4f;
                    break;
                case "Concrete":
                    p.hotColor = new Color(1f, 0.36f, 0.07f, 1f);
                    p.glowIntensity = 1.8f;
                    p.glowRadius = 0.06f;
                    p.coolSeconds = 1.1f;
                    p.fadeSeconds = 1f;
                    p.glowNoise = 0.6f;
                    p.dripBurst = 1;
                    p.dripLifetime = 1.4f;
                    p.dripIntensity = 3f;
                    break;
                case "Glass":
                    p.hotColor = new Color(1f, 0.62f, 0.25f, 1f);
                    p.glowIntensity = 5f;
                    p.glowRadius = 0.04f;
                    p.coolSeconds = 1.6f;
                    p.fadeSeconds = 1f;
                    p.glowNoise = 0.15f;
                    p.dripBurst = 2;
                    p.dripSpeed = new Vector2(0.05f, 0.3f);
                    p.dripMaxSpeed = 1.2f;
                    p.dripLifetime = 2.4f;
                    p.dripIntensity = 6f;
                    p.dripStretch = 0.05f;
                    break;
                case "Wood":
                    p.hotColor = new Color(1f, 0.3f, 0.04f, 1f);
                    p.coolColor = new Color(0.2f, 0.015f, 0f, 1f);
                    p.glowIntensity = 2.5f;
                    p.glowRadius = 0.05f;
                    p.coolSeconds = 2.5f;
                    p.fadeSeconds = 1.5f;
                    p.glowNoise = 0.8f;
                    p.dripBurst = 0;
                    break;
                case "Dirt":
                case "Terrain":
                    p.hotColor = new Color(1f, 0.3f, 0.05f, 1f);
                    p.glowIntensity = 1.2f;
                    p.glowRadius = 0.06f;
                    p.coolSeconds = 0.9f;
                    p.fadeSeconds = 0.8f;
                    p.glowNoise = 0.75f;
                    p.dripBurst = 0;
                    break;
                default:
                    p.glowIntensity = 2f;
                    p.glowRadius = 0.05f;
                    p.coolSeconds = 1.2f;
                    p.fadeSeconds = 1f;
                    p.dripBurst = 1;
                    p.dripLifetime = 1.5f;
                    p.dripIntensity = 3f;
                    break;
            }

            return p;
        }
    }
}
