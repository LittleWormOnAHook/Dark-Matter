using Project.Core;
using Project.Features.WorldState;

namespace Project.Features.WorldState.Adapters
{
    public sealed class SessionWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "session";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            builder.Session = new SessionSnapshot(
                phaseLabel: GameSession.Phase.ToString(),
                saveSlotIndex: -1,
                hasStarted: GameSession.HasStarted);
        }
    }
}
