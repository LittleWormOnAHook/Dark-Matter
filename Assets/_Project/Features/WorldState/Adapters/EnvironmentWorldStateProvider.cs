using Project.Features.Directors;
using Project.Features.WorldState;
using Project.Survival.Exposure;
using Project.UI;

namespace Project.Features.WorldState.Adapters
{
    public sealed class EnvironmentWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "environment";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            ExposureStatusSnapshot exposure = ExposureStatusService.Current;
            bool crisis = EnvironmentalCrisisHudMode.IsCrisisActive;
            StormPhase phase = WeatherCommandServiceAdapter.CurrentPhaseStatic;

            string phaseLabel = phase.ToString();
            if (crisis && phase == StormPhase.Idle)
                phaseLabel = "Active";

            builder.Threat = new ThreatSnapshot(
                environmentThreat01: exposure.CombinedExposureLevel,
                sulfurStormActive: crisis || phase == StormPhase.Active || phase == StormPhase.Warning,
                stormPhaseLabel: phaseLabel,
                dominantHazardLabel: exposure.DominantHazard.DisplayName);

            builder.Planet = new PlanetEvolutionSnapshot(
                worldSeed: 0,
                explorationPercent: 0f,
                biomeUnlockMask: 0);
        }
    }
}
