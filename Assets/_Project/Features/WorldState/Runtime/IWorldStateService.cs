namespace Project.Features.WorldState
{
    public interface IWorldStateService
    {
        WorldStateSnapshot GetSnapshot();
    }
}
