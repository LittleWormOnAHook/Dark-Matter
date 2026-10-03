using System;
using Project.Combat;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>Natural cut shapes: snapped-off chunk, weathered hollow, or blast crater.</summary>
    public enum DMCarveStyle
    {
        Fracture = 0,
        Erosion = 1,
        Blast = 2
    }

    public enum DMSurfaceDamageMode
    {
        Auto = 0,
        Custom = 1,
        Off = 2
    }

    public enum DMSurfaceDamageRowMode
    {
        Inherit = 0,
        Custom = 1,
        Off = 2
    }

    /// <summary>One carve recipe: cutter shape, size, how deep it sinks, and debris.</summary>
    [Serializable]
    public class DMSurfaceDamageSettings
    {
        [Tooltip("Fracture = faceted chunk snapped off. Erosion = smooth pitted hollow (melt / weathering). Blast = wide crater with a jagged rim.")]
        public DMCarveStyle style = DMCarveStyle.Fracture;

        [Tooltip("Cutter radius in meters.")]
        [Min(0.01f)] public float radius = 0.12f;

        [Tooltip("How far the cutter center sinks below the hit surface, in radius units. 0 = centered on the surface, 1 = a full radius deep.")]
        [Range(0f, 1f)] public float depth = 0.45f;

        [Tooltip("Noise strength on the cutter shape. 0 = clean shape, 1 = natural, 2 = very jagged.")]
        [Range(0f, 2f)] public float roughness = 1f;

        [Tooltip("Small broken-off chunks spawned in Play Mode.")]
        [Range(0, 8)] public int debrisCount = 1;

        [Tooltip("Chunk size as a fraction of the cutter radius.")]
        [Range(0.05f, 1f)] public float debrisScale = 0.35f;

        public DMSurfaceDamageSettings Clone()
        {
            return (DMSurfaceDamageSettings)MemberwiseClone();
        }

        public static DMSurfaceDamageSettings Make(DMCarveStyle style, float radius, float depth, float roughness, int debris, float debrisScale)
        {
            return new DMSurfaceDamageSettings
            {
                style = style,
                radius = radius,
                depth = depth,
                roughness = roughness,
                debrisCount = debris,
                debrisScale = debrisScale
            };
        }
    }

    /// <summary>Per-ammo surface damage (carve) block on DMAmmoFxProfile.</summary>
    [Serializable]
    public class DMAmmoSurfaceDamage
    {
        [Tooltip("Auto = preset from the ammo type (Standard chips, Explosive blasts, Plasma/Laser melt). Custom = the values below. Off = this ammo never carves.")]
        public DMSurfaceDamageMode mode = DMSurfaceDamageMode.Auto;

        [Tooltip("Used when Mode is Custom. Tip: switch to Custom after picking Auto to start from the preset values.")]
        public DMSurfaceDamageSettings custom = new DMSurfaceDamageSettings();

        public bool TryResolve(AmmoType type, string ammoName, out DMSurfaceDamageSettings settings)
        {
            switch (mode)
            {
                case DMSurfaceDamageMode.Off:
                    settings = null;
                    return false;
                case DMSurfaceDamageMode.Custom:
                    settings = custom;
                    return settings != null && settings.radius > 0.005f;
                default:
                    settings = DMSurfaceDamagePresets.For(type, ammoName);
                    return settings != null;
            }
        }
    }

    /// <summary>Per-tag override on a DMHitMarkSurface row (e.g. Metal = Off, Concrete = small chips).</summary>
    [Serializable]
    public class DMSurfaceDamageOverride
    {
        [Tooltip("Inherit = use the ammo's Surface Damage. Custom = the values below on this tag only. Off = never carve this tag with this ammo.")]
        public DMSurfaceDamageRowMode mode = DMSurfaceDamageRowMode.Inherit;
        public DMSurfaceDamageSettings custom = new DMSurfaceDamageSettings();
    }

    /// <summary>Default carve per ammo type, used when an ammo's Surface Damage mode is Auto.</summary>
    public static class DMSurfaceDamagePresets
    {
        public static DMSurfaceDamageSettings For(AmmoType type, string ammoName)
        {
            switch (type)
            {
                case AmmoType.Plasma:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.16f, 0.40f, 0.8f, 0, 0.3f);
                case AmmoType.Ice:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Fracture, 0.09f, 0.35f, 1.3f, 2, 0.3f);
                case AmmoType.Electricity:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.10f, 0.30f, 1.4f, 0, 0.3f);
                case AmmoType.ResonanceStabilizer:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.06f, 0.25f, 0.6f, 0, 0.3f);
                case AmmoType.Laser:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.07f, 0.60f, 0.5f, 0, 0.3f);
                case AmmoType.Ion:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.13f, 0.40f, 1.0f, 0, 0.3f);
                case AmmoType.Fire:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Erosion, 0.10f, 0.25f, 0.7f, 0, 0.3f);
                case AmmoType.Explosive:
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Blast, 0.55f, 0.35f, 1.2f, 5, 0.4f);
                default:
                    // Gunpowder / Standard: small faceted chip with one chunk.
                    return DMSurfaceDamageSettings.Make(DMCarveStyle.Fracture, 0.11f, 0.45f, 1.0f, 1, 0.35f);
            }
        }
    }
}
