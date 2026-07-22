using System.Collections.Generic;
using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>Locked eval order (HLA §8.2): Story → Simulation → Mission → Weather → Economy → Experience → Event.</summary>
    public sealed class DirectorOrchestrator : IDirectorOrchestrator
    {
        private static DirectorOrchestrator instance;
        private readonly IWorldStateService worldState;
        private readonly List<IDirector> directors = new List<IDirector>(8);

        public static DirectorOrchestrator Instance => instance;

        public static void SetInstance(DirectorOrchestrator orchestrator)
        {
            instance = orchestrator;
        }

        public int LastEvaluationDirectorCount { get; private set; }
        public DirectorTrigger LastTrigger { get; private set; }

        public DirectorOrchestrator(IWorldStateService worldState)
        {
            this.worldState = worldState;
            // Locked order — stubs until domain logic lands
            directors.Add(new StubDirector("Story"));
            directors.Add(new SimulationDirectorService());
            directors.Add(new StubDirector("Mission"));
            directors.Add(new WeatherDirectorService());
            directors.Add(new StubDirector("Economy"));
            directors.Add(new ExperienceDirectorService());
            directors.Add(new StubDirector("Event"));
        }

        public void ReplaceDirector(string directorId, IDirector replacement)
        {
            if (replacement == null || string.IsNullOrEmpty(directorId))
                return;
            for (int i = 0; i < directors.Count; i++)
            {
                if (directors[i].DirectorId == directorId)
                {
                    directors[i] = replacement;
                    return;
                }
            }
        }

        public void EvaluateAll()
        {
            Evaluate(DirectorTrigger.SimulationTick);
        }

        public void Evaluate(DirectorTrigger trigger)
        {
            LastTrigger = trigger;
            WorldStateSnapshot world = worldState != null ? worldState.GetSnapshot() : WorldStateSnapshot.Empty;
            LastEvaluationDirectorCount = directors.Count;

            for (int i = 0; i < directors.Count; i++)
            {
                try
                {
                    directors[i].Evaluate(world, trigger);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[Directors] " + directors[i].DirectorId + " failed: " + ex.Message);
                }
            }

            if (trigger == DirectorTrigger.ManualDebug)
            {
                Debug.Log(string.Format(
                    "[Directors] trigger={0} directors={1}",
                    trigger,
                    directors.Count));
            }
        }
    }
}
