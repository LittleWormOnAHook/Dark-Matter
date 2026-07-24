using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>Stub ExperienceDirector — silence / density intents later.</summary>
    public sealed class ExperienceDirectorService : IDirector
    {
        public string DirectorId => "Experience";
        public int EvaluationCount { get; private set; }

        public void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger)
        {
            EvaluationCount++;
            if (trigger == DirectorTrigger.ManualDebug)
            {
                Debug.Log(string.Format(
                    "[Directors] Experience trigger={0} density={1:0.00} silence={2}",
                    trigger,
                    world != null ? world.Experience.RadioDensity01 : 0f,
                    world != null && world.Experience.PreferSilence));
            }
        }
    }
}
