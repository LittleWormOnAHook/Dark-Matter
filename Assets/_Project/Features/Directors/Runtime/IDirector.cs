using Project.Features.WorldState;

namespace Project.Features.Directors
{
    public interface IDirector
    {
        string DirectorId { get; }
        void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
    }
}
