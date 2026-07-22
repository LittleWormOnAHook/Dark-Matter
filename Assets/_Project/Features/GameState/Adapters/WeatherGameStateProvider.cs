using Project.Features.GameState;
using Project.Survival.Exposure;
using Project.UI;

namespace Project.Features.GameState.Adapters
{
    public sealed class WeatherGameStateProvider : IGameStateProvider
    {
        public string DomainId => "weather";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            ExposureStatusSnapshot exposure = ExposureStatusService.Current;
            string hazard = exposure.DominantHazard.DisplayName;
            if (string.IsNullOrEmpty(hazard))
                hazard = exposure.HazardSeverityLabel;

            builder.Weather = new WeatherSnapshot(
                displayTemperatureF: exposure.DisplayTemperatureF,
                thermalStatusLabel: exposure.ThermalStatusLabel,
                dominantHazardLabel: hazard,
                combinedExposureLevel: exposure.CombinedExposureLevel,
                radiationLevel: exposure.RadiationHazardLevel,
                sulfurLevel: exposure.SulfurHazardLevel,
                volcanoLevel: exposure.VolcanoHazardLevel,
                coldLevel: exposure.ColdHazardLevel,
                heatLevel: exposure.HeatHazardLevel,
                isInShelter: exposure.IsInShelter,
                crisisActive: EnvironmentalCrisisHudMode.IsCrisisActive,
                activeZoneNames: exposure.ActiveZoneNames);
        }
    }
}
