using NUnit.Framework;
using Project.Features.Directors;
using Project.Features.WorldState;

namespace Project.Features.Directors.Tests
{
    public class WeatherDirectorServiceTests
    {
        [Test]
        public void Evaluate_IncrementsCount()
        {
            var director = new WeatherDirectorService();
            director.Evaluate(WorldStateSnapshot.Empty, DirectorTrigger.ManualDebug);
            Assert.AreEqual(1, director.EvaluationCount);
        }

        [Test]
        public void ResolveNextPhase_CyclesIdleWarningActiveClearing()
        {
            Assert.AreEqual(StormPhase.Warning, WeatherDirectorService.ResolveNextPhase(StormPhase.Idle));
            Assert.AreEqual(StormPhase.Active, WeatherDirectorService.ResolveNextPhase(StormPhase.Warning));
            Assert.AreEqual(StormPhase.Clearing, WeatherDirectorService.ResolveNextPhase(StormPhase.Active));
            Assert.AreEqual(StormPhase.Idle, WeatherDirectorService.ResolveNextPhase(StormPhase.Clearing));
        }
    }
}
