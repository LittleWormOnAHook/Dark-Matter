using System;
using System.Collections.Generic;
using Project.Features.GameState;

namespace Project.Features.WorldState
{
    public sealed class WorldStateService : IWorldStateService
    {
        private static WorldStateService instance;
        private readonly List<IWorldStateProvider> providers = new List<IWorldStateProvider>(16);
        private readonly Func<GameStateSnapshot> gameSnapshotSource;

        public static WorldStateService Instance => instance;

        public static void SetInstance(WorldStateService service)
        {
            instance = service;
        }

        public WorldStateService(Func<GameStateSnapshot> gameSnapshotSource)
        {
            this.gameSnapshotSource = gameSnapshotSource ?? (() => GameStateSnapshot.Empty);
        }

        public void RegisterProvider(IWorldStateProvider provider)
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

        public void UnregisterProvider(IWorldStateProvider provider)
        {
            if (provider == null)
                return;
            providers.Remove(provider);
        }

        public WorldStateSnapshot GetSnapshot()
        {
            var builder = new WorldStateSnapshotBuilder
            {
                Game = gameSnapshotSource() ?? GameStateSnapshot.Empty
            };

            for (int i = 0; i < providers.Count; i++)
            {
                try
                {
                    providers[i].Contribute(builder);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[WorldState] Provider '" + providers[i].DomainId + "' failed: " + ex.Message);
                }
            }

            return builder.Build(DateTime.UtcNow.Ticks);
        }
    }
}
