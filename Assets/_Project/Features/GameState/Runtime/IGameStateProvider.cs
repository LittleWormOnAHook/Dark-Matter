namespace Project.Features.GameState
{
    public interface IGameStateProvider
    {
        string DomainId { get; }
        void Contribute(GameStateSnapshotBuilder builder);
    }
}
