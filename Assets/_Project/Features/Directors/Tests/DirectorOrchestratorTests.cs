using NUnit.Framework;
using Project.Features.Directors;
using Project.Features.GameState;
using Project.Features.WorldState;

namespace Project.Features.Directors.Tests
{
    public class DirectorOrchestratorTests
    {
        [Test]
        public void Evaluate_ManualDebug_RunsSevenDirectors()
        {
            var world = new WorldStateService(() => GameStateSnapshot.Empty);
            var orch = new DirectorOrchestrator(world);
            orch.Evaluate(DirectorTrigger.ManualDebug);
            Assert.AreEqual(7, orch.LastEvaluationDirectorCount);
            Assert.AreEqual(DirectorTrigger.ManualDebug, orch.LastTrigger);
        }
    }
}
