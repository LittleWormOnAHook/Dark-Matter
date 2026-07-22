using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>Stub WeatherDirector — reads threat; writes only via IWeatherCommandService when wired.</summary>
    public sealed class WeatherDirectorService : IDirector
    {
        private readonly IWeatherCommandService weatherCommands;

        public string DirectorId => "Weather";
        public int EvaluationCount { get; private set; }

        public WeatherDirectorService(IWeatherCommandService weatherCommands = null)
        {
            this.weatherCommands = weatherCommands;
        }

        public void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger)
        {
            EvaluationCount++;
            if (world == null)
                return;

            if (trigger == DirectorTrigger.ManualDebug || trigger == DirectorTrigger.StormPhaseChanged)
            {
                Debug.Log(string.Format(
                    "[Directors] Weather trigger={0} crisis={1} phaseCmd={2}",
                    trigger,
                    world.Threat.SulfurStormActive,
                    weatherCommands != null ? weatherCommands.CurrentPhase.ToString() : "unwired"));
            }
        }
    }
}
