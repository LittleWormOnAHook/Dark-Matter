namespace Project.Features.WorldState
{
    public interface IWorldStateProvider
    {
        string DomainId { get; }
        void Contribute(WorldStateSnapshotBuilder builder);
    }
}
