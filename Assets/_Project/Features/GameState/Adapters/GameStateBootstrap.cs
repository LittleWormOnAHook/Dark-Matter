using Project.Features.GameState;
using Project.Features.PPT.Adapters;
using UnityEngine;

namespace Project.Features.GameState.Adapters
{
    public static class GameStateBootstrap
    {
        public static GameStateService Service { get; private set; }

        public static GameStateService EnsureExists(MonoBehaviour host)
        {
            if (Service != null && GameStateService.Instance != null)
                return Service;

            var service = new GameStateService();
            service.RegisterProvider(new PlayerGameStateProvider());
            service.RegisterProvider(new InventoryGameStateProvider());
            service.RegisterProvider(new MissionGameStateProvider());
            service.RegisterProvider(new WeatherGameStateProvider());
            service.RegisterProvider(new PowerGameStateProvider());
            service.RegisterProvider(new ColonyGameStateProvider());
            service.RegisterProvider(new ResearchGameStateProvider());
            service.RegisterProvider(new CrewGameStateProvider());
            service.RegisterProvider(new BuildingGameStateProvider());
            service.RegisterProvider(new PptGameStateProvider());

            GameStateService.SetInstance(service);
            Service = service;
            Debug.Log("[GameState] Bootstrap complete — 10 providers registered.");
            return service;
        }
    }
}
