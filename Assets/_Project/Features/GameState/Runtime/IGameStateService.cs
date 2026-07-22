namespace Project.Features.GameState
{
    public interface IGameStateService
    {
        GameStateSnapshot GetSnapshot();
    }
}
