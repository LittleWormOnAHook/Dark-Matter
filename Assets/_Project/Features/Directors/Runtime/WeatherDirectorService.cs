using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>
    /// Stub WeatherDirector — reads threat; writes only via IWeatherCommandService when wired.
    /// SYNC_MARKER: world-engine-782b-v3 — must define ResolveNextPhase (static).
    /// </summary>
    public sealed class WeatherDirectorService : IDirector
    {
        private readonly IWeatherCommandService weatherCommands;

        public string DirectorId => "Weather";
        public int EvaluationCount { get; private set; }

        public WeatherDirectorService(IWeatherCommandService weatherCommands = null)
        {
            this.weatherCommands = weatherCommands;
        }

        /// <summary>Idle → Warning → Active → Clearing → Idle (F11 smoke / scheduler helper).</summary>
        public static StormPhase ResolveNextPhase(StormPhase current)
        {
            if (current == StormPhase.Idle)
                return StormPhase.Warning;
            if (current == StormPhase.Warning)
                return StormPhase.Active;
            if (current == StormPhase.Active)
                return StormPhase.Clearing;
            return StormPhase.Idle;
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
