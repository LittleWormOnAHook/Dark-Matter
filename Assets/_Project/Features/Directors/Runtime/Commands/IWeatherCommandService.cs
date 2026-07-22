namespace Project.Features.Directors
{
    public interface IWeatherCommandService
    {
        void SetStormPhase(StormPhase phase);
        StormPhase CurrentPhase { get; }
    }
}
