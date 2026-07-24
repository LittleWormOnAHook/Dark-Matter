using Project.Features.Directors;
using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors.Adapters
{
    public static class DirectorsBootstrap
    {
        public static DirectorOrchestrator Orchestrator { get; private set; }
        public static WeatherCommandServiceAdapter WeatherCommands { get; private set; }
        public static SimulationCommandServiceAdapter SimulationCommands { get; private set; }

        public static DirectorOrchestrator EnsureExists(MonoBehaviour host)
        {
            if (Orchestrator != null && DirectorOrchestrator.Instance != null)
                return Orchestrator;

            IWorldStateService world = WorldStateService.Instance;
            var orchestrator = new DirectorOrchestrator(world);

            WeatherCommands = new WeatherCommandServiceAdapter();
            SimulationCommands = new SimulationCommandServiceAdapter();

            orchestrator.ReplaceDirector("Weather", new WeatherDirectorService(WeatherCommands));
            orchestrator.ReplaceDirector("Simulation", new SimulationDirectorService(SimulationCommands));

            DirectorOrchestrator.SetInstance(orchestrator);
            Orchestrator = orchestrator;

            if (host != null && host.GetComponent<DarkMatterSmokeDriver>() == null)
                host.gameObject.AddComponent<DarkMatterSmokeDriver>();

            Debug.Log("[Directors] Bootstrap complete — orchestrator + weather/simulation command adapters.");
            return orchestrator;
        }
    }
}
