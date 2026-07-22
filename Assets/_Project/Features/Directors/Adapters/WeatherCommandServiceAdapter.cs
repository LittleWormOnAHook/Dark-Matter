using Project.Features.Directors;
using Project.UI;
using UnityEngine;

namespace Project.Features.Directors.Adapters
{
    /// <summary>Maps WeatherDirector storm phases onto EnvironmentalCrisisHudMode.</summary>
    public sealed class WeatherCommandServiceAdapter : IWeatherCommandService
    {
        public static StormPhase CurrentPhaseStatic { get; private set; } = StormPhase.Idle;

        public StormPhase CurrentPhase => CurrentPhaseStatic;

        public void SetStormPhase(StormPhase phase)
        {
            CurrentPhaseStatic = phase;
            bool crisis = phase == StormPhase.Active || phase == StormPhase.Warning;
            string banner = phase switch
            {
                StormPhase.Warning => "SULFUR STORM WARNING — SEEK SHELTER",
                StormPhase.Active => "SULFUR STORM — BASE OPERATIONS PAUSED",
                StormPhase.Clearing => "STORM CLEARING — RESUME WITH CAUTION",
                _ => null
            };

            EnvironmentalCrisisHudMode mode = EnvironmentalCrisisHudMode.Instance;
            if (mode != null)
                mode.SetCrisisActive(crisis, banner);
            else if (crisis)
                Debug.LogWarning("[Directors] WeatherCommand: crisis requested but HUD mode missing.");

            Debug.Log("[Directors] WeatherCommand SetStormPhase=" + phase);
        }
    }
}
