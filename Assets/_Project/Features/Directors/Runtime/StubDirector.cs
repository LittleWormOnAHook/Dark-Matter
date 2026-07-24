using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>No-op director used until domain logic lands.</summary>
    public sealed class StubDirector : IDirector
    {
        public string DirectorId { get; }
        public int EvaluationCount { get; private set; }

        public StubDirector(string directorId)
        {
            DirectorId = directorId ?? "unnamed";
        }

        public void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger)
        {
            EvaluationCount++;
            if (trigger == DirectorTrigger.ManualDebug)
            {
                Debug.Log(string.Format(
                    "[Directors] {0} trigger={1} storm={2}",
                    DirectorId,
                    trigger,
                    world != null && world.Threat.SulfurStormActive));
            }
        }
    }
}
