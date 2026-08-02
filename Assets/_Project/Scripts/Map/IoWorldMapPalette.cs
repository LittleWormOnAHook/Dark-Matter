using Project.Survival.World;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Map and biome visualization colors for Io surface regions (B1–B7).
    /// Aligns with life-sheet palettes in Io_Biome_Life_Sheet_Manifest.md.
    /// </summary>
    public static class IoWorldMapPalette
    {
        public const float MaxSurfaceElevationMeters = 1000f;

        public static Color GetBiomeColor(IoSurfaceRegionId region)
        {
            return region switch
            {
                IoSurfaceRegionId.SulfurPlains => SulfurAmber,
                IoSurfaceRegionId.GeyserFields => GeyserAmber,
                IoSurfaceRegionId.AshFlatsAndRidges => AshBronze,
                IoSurfaceRegionId.LavaCalderas => HeatObsidian,
                IoSurfaceRegionId.PolarRadiationFlats => PolarRad,
                IoSurfaceRegionId.BasaltHighlands => BasaltHighland,
                IoSurfaceRegionId.PrecursorRuinBelt => AetherTeal,
                _ => VoidRock
            };
        }

        public static byte GetBiomeMaskId(IoSurfaceRegionId region)
        {
            return (byte)region;
        }

        public static IoSurfaceRegionId GetBiomeFromMaskId(byte maskId)
        {
            if (maskId < 1 || maskId > 7)
                return IoSurfaceRegionId.None;

            return (IoSurfaceRegionId)maskId;
        }

        // sulfur-amber family (B1, B2)
        public static readonly Color SulfurAmber = FromHex("#C9A227");
        public static readonly Color GeyserAmber = FromHex("#D4B04A");

        // ash-bronze (B3)
        public static readonly Color AshBronze = FromHex("#8B7355");

        // heat-obsidian (B4)
        public static readonly Color HeatObsidian = FromHex("#2A1818");

        // polar-rad (B5)
        public static readonly Color PolarRad = FromHex("#6B8CAE");

        // B6 hub — between ash-bronze and basalt
        public static readonly Color BasaltHighland = FromHex("#4A4540");

        // aether-teal (B7)
        public static readonly Color AetherTeal = FromHex("#2A6B6B");

        public static readonly Color VoidRock = FromHex("#1A1A1F");
        public static readonly Color SubJovianHotTint = FromHex("#FF6B35");
        public static readonly Color AntiJovianColdTint = FromHex("#4A6FA5");
        public static readonly Color BreachMarker = FromHex("#D4A017");
        public static readonly Color GraveyardOverlay = FromHex("#5C4A3A");

        private static Color FromHex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
                return Color.magenta;

            return color;
        }
    }
}
