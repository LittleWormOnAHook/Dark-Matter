using System;
using System.Collections.Generic;

namespace Project.Features.GameState
{
    public sealed class GameStateService : IGameStateService
    {
        private static GameStateService instance;
        private readonly List<IGameStateProvider> providers = new List<IGameStateProvider>(16);

        public static GameStateService Instance => instance;

        public static void SetInstance(GameStateService service)
        {
            instance = service;
        }

        public void RegisterProvider(IGameStateProvider provider)
        {
            if (provider == null)
                return;
            for (int i = 0; i < providers.Count; i++)
            {
                if (ReferenceEquals(providers[i], provider) ||
                    string.Equals(providers[i].DomainId, provider.DomainId, StringComparison.Ordinal))
                {
                    providers[i] = provider;
                    return;
                }
            }

            providers.Add(provider);
        }

        public void UnregisterProvider(IGameStateProvider provider)
        {
            if (provider == null)
                return;
            providers.Remove(provider);
        }

        public GameStateSnapshot GetSnapshot()
        {
            var builder = new GameStateSnapshotBuilder();
            for (int i = 0; i < providers.Count; i++)
            {
                try
                {
                    providers[i].Contribute(builder);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[GameState] Provider '" + providers[i].DomainId + "' failed: " + ex.Message);
                }
            }

            return builder.Build(DateTime.UtcNow.Ticks);
        }
    }
}
