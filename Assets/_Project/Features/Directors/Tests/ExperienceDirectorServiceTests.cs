using NUnit.Framework;
using Project.Features.Directors;
using Project.Features.WorldState;

namespace Project.Features.Directors.Tests
{
    public class ExperienceDirectorServiceTests
    {
        [Test]
        public void Evaluate_IncrementsCount()
        {
            var director = new ExperienceDirectorService();
            director.Evaluate(WorldStateSnapshot.Empty, DirectorTrigger.ManualDebug);
            Assert.AreEqual(1, director.EvaluationCount);
        }
    }
}
