using Project.Features.GameState;

namespace Project.Features.GameState.Adapters
{
    public sealed class ResearchGameStateProvider : IGameStateProvider
    {
        public string DomainId => "research";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            builder.Research = ResearchSnapshot.Empty;
        }
    }
}
