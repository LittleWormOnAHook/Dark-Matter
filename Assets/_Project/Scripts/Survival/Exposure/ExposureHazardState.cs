using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Dominant environmental hazard shown on hotbar / journal / map POIs.
    /// </summary>
    public struct ExposureHazardState
    {
        public ExposureZoneKind Kind;
        public string DisplayName;
        public float Severity;
        public Color DisplayColor;
        public bool IsShelter;
        public bool IsClear;

        public static ExposureHazardState Clear()
        {
            return new ExposureHazardState
            {
                Kind = ExposureZoneKind.Custom,
                DisplayName = "CLEAR",
                Severity = 0f,
                DisplayColor = ExposureHazardPresentation.ClearColor,
                IsShelter = false,
                IsClear = true
            };
        }

        public static ExposureHazardState Shelter(string displayName = "Shelter Safe Zone")
        {
            return new ExposureHazardState
            {
                Kind = ExposureZoneKind.ShelterSafe,
                DisplayName = displayName,
                Severity = 0f,
                DisplayColor = ExposureHazardPresentation.ShelterColor,
                IsShelter = true,
                IsClear = false
            };
        }
    }

    /// <summary>
    /// Presentation colors aligned with DarkMatterGenesisUiPalette (no UI assembly reference).
    /// </summary>
    public static class ExposureHazardPresentation
    {
        public static readonly Color ClearColor = new Color(0.42f, 0.78f, 0.48f, 1f);
        public static readonly Color ShelterColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        public static readonly Color RadiationColor = new Color(0.83f, 0.63f, 0.09f, 1f);
        public static readonly Color ColdColor = new Color(0.35f, 0.72f, 0.95f, 1f);
        public static readonly Color HeatColor = new Color(0.95f, 0.45f, 0.15f, 1f);
        public static readonly Color SulfurColor = new Color(0.56f, 0.12f, 0.37f, 1f);
        public static readonly Color VolcanoColor = new Color(0.92f, 0.38f, 0.32f, 1f);
        public static readonly Color MixedColor = new Color(0.75f, 0.18f, 0.48f, 1f);

        public static Color GetColor(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.RadiationFlat => RadiationColor,
                ExposureZoneKind.ThermalCold => ColdColor,
                ExposureZoneKind.ThermalHeat => HeatColor,
                ExposureZoneKind.SulfurField => SulfurColor,
                ExposureZoneKind.VolcanoCaldera => VolcanoColor,
                ExposureZoneKind.MixedHazard => MixedColor,
                ExposureZoneKind.ShelterSafe => ShelterColor,
                _ => ClearColor
            };
        }

        public static string GetShortLabel(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.RadiationFlat => "RADIATION",
                ExposureZoneKind.ThermalCold => "COLD BASIN",
                ExposureZoneKind.ThermalHeat => "HEAT VENT",
                ExposureZoneKind.SulfurField => "SULFUR FIELD",
                ExposureZoneKind.VolcanoCaldera => "VOLCANO",
                ExposureZoneKind.MixedHazard => "MIXED HAZARD",
                ExposureZoneKind.ShelterSafe => "SHELTER",
                _ => "CLEAR"
            };
        }

        public static string GetHudDisplayName(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.RadiationFlat => "Radiation Flat",
                ExposureZoneKind.ThermalCold => "Cold Basin",
                ExposureZoneKind.ThermalHeat => "Heat Vent",
                ExposureZoneKind.SulfurField => "Sulfur Field",
                ExposureZoneKind.VolcanoCaldera => "Volcano Caldera",
                ExposureZoneKind.MixedHazard => "Mixed Hazard",
                ExposureZoneKind.ShelterSafe => "Shelter Safe Zone",
                _ => GetShortLabel(kind)
            };
        }
    }
}
