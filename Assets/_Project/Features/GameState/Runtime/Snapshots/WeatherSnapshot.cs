namespace Project.Features.GameState
{
    public sealed class WeatherSnapshot
    {
        public static readonly WeatherSnapshot Empty = new WeatherSnapshot();

        public float DisplayTemperatureF { get; }
        public string ThermalStatusLabel { get; }
        public string DominantHazardLabel { get; }
        public float CombinedExposureLevel { get; }
        public float RadiationLevel { get; }
        public float SulfurLevel { get; }
        public float VolcanoLevel { get; }
        public float ColdLevel { get; }
        public float HeatLevel { get; }
        public bool IsInShelter { get; }
        public bool CrisisActive { get; }
        public string[] ActiveZoneNames { get; }

        public WeatherSnapshot(
            float displayTemperatureF = 70f,
            string thermalStatusLabel = "EVA NOMINAL",
            string dominantHazardLabel = "CLEAR",
            float combinedExposureLevel = 0f,
            float radiationLevel = 0f,
            float sulfurLevel = 0f,
            float volcanoLevel = 0f,
            float coldLevel = 0f,
            float heatLevel = 0f,
            bool isInShelter = false,
            bool crisisActive = false,
            string[] activeZoneNames = null)
        {
            DisplayTemperatureF = displayTemperatureF;
            ThermalStatusLabel = thermalStatusLabel ?? string.Empty;
            DominantHazardLabel = dominantHazardLabel ?? string.Empty;
            CombinedExposureLevel = combinedExposureLevel;
            RadiationLevel = radiationLevel;
            SulfurLevel = sulfurLevel;
            VolcanoLevel = volcanoLevel;
            ColdLevel = coldLevel;
            HeatLevel = heatLevel;
            IsInShelter = isInShelter;
            CrisisActive = crisisActive;
            ActiveZoneNames = activeZoneNames ?? System.Array.Empty<string>();
        }
    }
}
