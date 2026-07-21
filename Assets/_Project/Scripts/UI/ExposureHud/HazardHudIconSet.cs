using System;
using UnityEngine;

namespace Project.UI
{
    [Serializable]
    public struct HazardHudIconEntry
    {
        [Tooltip("Optional sprite shown in the hazard cell. When empty, Glyph is used.")]
        public Sprite Icon;

        [Tooltip("Fallback text when Icon is not assigned.")]
        public string Glyph;
    }

    /// <summary>
    /// Assignable hazard icons for hotbar / journal exposure gauges.
    /// Create via Assets → Create → Dark Matter Genesis → UI → Hazard HUD Icon Set.
    /// Optional runtime default: Resources/HazardHudIconSet.asset
    /// </summary>
    [CreateAssetMenu(fileName = "HazardHudIconSet", menuName = "Dark Matter Genesis/UI/Hazard HUD Icon Set")]
    public class HazardHudIconSet : ScriptableObject
    {
        public HazardHudIconEntry Cold;
        public HazardHudIconEntry Heat;
        public HazardHudIconEntry Radiation;
        public HazardHudIconEntry Bio;
        public HazardHudIconEntry Volcano;
        public HazardHudIconEntry Shelter;

        public HazardHudIconEntry GetCold() => Resolve(Cold, "CL");
        public HazardHudIconEntry GetHeat() => Resolve(Heat, "HT");
        public HazardHudIconEntry GetRadiation() => Resolve(Radiation, "RD");
        public HazardHudIconEntry GetBio() => Resolve(Bio, "BZ");
        public HazardHudIconEntry GetVolcano() => Resolve(Volcano, "VC");
        public HazardHudIconEntry GetShelter() => Resolve(Shelter, "SH");

        public static HazardHudIconSet LoadDefault()
        {
            HazardHudIconSet loaded = Resources.Load<HazardHudIconSet>("HazardHudIconSet");
            return loaded != null ? loaded : CreateRuntimeFallback();
        }

        private static HazardHudIconSet CreateRuntimeFallback()
        {
            HazardHudIconSet fallback = CreateInstance<HazardHudIconSet>();
            fallback.Cold = new HazardHudIconEntry { Glyph = "CL" };
            fallback.Heat = new HazardHudIconEntry { Glyph = "HT" };
            fallback.Radiation = new HazardHudIconEntry { Glyph = "RD" };
            fallback.Bio = new HazardHudIconEntry { Glyph = "BZ" };
            fallback.Volcano = new HazardHudIconEntry { Glyph = "VC" };
            fallback.Shelter = new HazardHudIconEntry { Glyph = "SH" };
            return fallback;
        }

        private static HazardHudIconEntry Resolve(HazardHudIconEntry entry, string defaultGlyph)
        {
            if (string.IsNullOrWhiteSpace(entry.Glyph) && entry.Icon == null)
                entry.Glyph = defaultGlyph;

            return entry;
        }
    }
}
