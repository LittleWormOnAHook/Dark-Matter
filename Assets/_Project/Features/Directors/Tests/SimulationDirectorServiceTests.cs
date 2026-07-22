using NUnit.Framework;
using Project.Features.Directors;
using Project.Features.WorldState;

namespace Project.Features.Directors.Tests
{
    public class SimulationDirectorServiceTests
    {
        [Test]
        public void Evaluate_IncrementsCount()
        {
            var director = new SimulationDirectorService();
            director.Evaluate(WorldStateSnapshot.Empty, DirectorTrigger.SimulationTick);
            Assert.AreEqual(1, director.EvaluationCount);
        }
    }
}
