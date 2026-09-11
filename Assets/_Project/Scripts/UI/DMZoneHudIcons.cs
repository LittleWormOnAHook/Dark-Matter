using Project.Survival.Exposure;
using UnityEngine;

namespace Project.UI
{
    /// <summary>Cutout textures for the pilot-cluster zone icons (Resources/UI/ZoneIcons).</summary>
    public static class DMZoneHudIcons
    {
        public const string ResourceFolder = "UI/ZoneIcons";

        public static Texture2D ForKind(ExposureZoneKind kind)
        {
            string file = kind switch
            {
                ExposureZoneKind.RadiationFlat => "DM_Zone_Radiation",
                ExposureZoneKind.ThermalCold => "DM_Zone_ColdBasin",
                ExposureZoneKind.ThermalHeat => "DM_Zone_HeatVent",
                ExposureZoneKind.SulfurField => "DM_Zone_Sulfur",
                ExposureZoneKind.VolcanoCaldera => "DM_Zone_Volcano",
                ExposureZoneKind.ShelterSafe => "DM_Zone_Shelter",
                ExposureZoneKind.MixedHazard => "DM_Zone_Sulfur",
                _ => null
            };

            if (string.IsNullOrEmpty(file))
                return null;

            string path = ResourceFolder + "/" + file;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
                return texture;

            Sprite sprite = Resources.Load<Sprite>(path);
            return sprite != null ? sprite.texture : null;
        }

        public static Color EmissionTint(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.RadiationFlat => new Color(0.92f, 0.12f, 0.14f, 1f),
                ExposureZoneKind.ThermalCold => new Color(0.55f, 0.90f, 1f, 1f),
                ExposureZoneKind.ThermalHeat => new Color(1f, 0.55f, 0.15f, 1f),
                ExposureZoneKind.SulfurField => new Color(1f, 0.92f, 0.25f, 1f),
                ExposureZoneKind.VolcanoCaldera => new Color(1f, 0.28f, 0.22f, 1f),
                ExposureZoneKind.ShelterSafe => new Color(0.45f, 1f, 0.50f, 1f),
                ExposureZoneKind.MixedHazard => new Color(1f, 0.92f, 0.25f, 1f),
                _ => Color.white
            };
        }

        public static ExposureZoneKind ResolveKind(ExposureZoneVolume zone)
        {
            if (zone == null || zone.Profile == null)
                return ExposureZoneKind.Custom;

            ExposureZoneKind kind = zone.Profile.zoneKind;
            if (kind != ExposureZoneKind.Custom)
                return kind;

            ExposureZoneProfile p = zone.Profile;
            if (p.radiationPerSecond > 0.01f)
                return ExposureZoneKind.RadiationFlat;
            if (p.sulfurPerSecond > 0.01f)
                return ExposureZoneKind.SulfurField;
            if (p.volcanoPerSecond > 0.01f)
                return ExposureZoneKind.VolcanoCaldera;
            if (p.thermalColdPerSecond > 0.01f)
                return ExposureZoneKind.ThermalCold;
            if (p.thermalHeatPerSecond > 0.01f)
                return ExposureZoneKind.ThermalHeat;
            return ExposureZoneKind.Custom;
        }
    }
}
