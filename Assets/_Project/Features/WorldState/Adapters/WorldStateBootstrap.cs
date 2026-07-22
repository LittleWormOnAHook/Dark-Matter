using Project.Features.GameState;
using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.WorldState.Adapters
{
    public static class WorldStateBootstrap
    {
        public static WorldStateService Service { get; private set; }

        public static WorldStateService EnsureExists(MonoBehaviour host)
        {
            if (Service != null && WorldStateService.Instance != null)
                return Service;

            IGameStateService game = GameStateService.Instance;
            var service = new WorldStateService(() =>
                game != null ? game.GetSnapshot() : GameStateSnapshot.Empty);

            service.RegisterProvider(new StoryWorldStateProvider());
            service.RegisterProvider(new ColonyEvolutionWorldStateProvider());
            service.RegisterProvider(new Aether9WorldStateProvider());
            service.RegisterProvider(new EnvironmentWorldStateProvider());
            service.RegisterProvider(new SessionWorldStateProvider());
            service.RegisterProvider(new ExperienceWorldStateProvider());
            service.RegisterProvider(new SimulationWorldStateProvider());

            WorldStateService.SetInstance(service);
            Service = service;
            Debug.Log("[WorldState] Bootstrap complete — 7 providers registered.");
            return service;
        }
    }
}
