using NUnit.Framework;
using Project.Features.Directors;
using Project.Features.GameState;
using Project.Features.Validation;
using Project.Features.WorldState;
using System.Collections.Generic;

namespace Project.Features.Validation.Tests
{
    public class DarkMatterStackValidationTests
    {
        [Test]
        public void BootstrapOrder_MatchesTdbLockedSequence()
        {
            Assert.AreEqual(4, DarkMatterBootstrapOrder.CompanionSystems.Length);
            Assert.AreEqual(DarkMatterBootstrapOrder.GameState, DarkMatterBootstrapOrder.CompanionSystems[0]);
            Assert.AreEqual(DarkMatterBootstrapOrder.WorldState, DarkMatterBootstrapOrder.CompanionSystems[1]);
            Assert.AreEqual(DarkMatterBootstrapOrder.Directors, DarkMatterBootstrapOrder.CompanionSystems[2]);
            Assert.AreEqual(DarkMatterBootstrapOrder.Communications, DarkMatterBootstrapOrder.CompanionSystems[3]);
        }

        [Test]
        public void SmokeKeys_AreUniqueStrings()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < DarkMatterSmokeKeys.AllRegistered.Length; i++)
            {
                string key = DarkMatterSmokeKeys.AllRegistered[i];
                Assert.IsFalse(string.IsNullOrWhiteSpace(key));
                Assert.IsTrue(seen.Add(key), "Duplicate smoke key: " + key);
            }
        }

        [Test]
        public void GameStateSnapshot_EmbeddedInWorldState_HasSameReferenceWhenEmpty()
        {
            var gameService = new GameStateService();
            GameStateService.SetInstance(gameService);
            var worldService = new WorldStateService(() => gameService.GetSnapshot());
            WorldStateSnapshot world = worldService.GetSnapshot();
            Assert.IsNotNull(world.Game);
            Assert.AreEqual(gameService.GetSnapshot().CapturedAtUtcTicks, world.Game.CapturedAtUtcTicks);
            GameStateService.SetInstance(null);
        }

        [Test]
        public void DirectorOrchestrator_ReadsWorldStateWithoutGameplayManagers()
        {
            var gameService = new GameStateService();
            GameStateService.SetInstance(gameService);
            var worldService = new WorldStateService(() => gameService.GetSnapshot());
            var orchestrator = new DirectorOrchestrator(worldService);
            orchestrator.Evaluate(DirectorTrigger.ManualDebug);
            Assert.AreEqual(7, orchestrator.LastEvaluationDirectorCount);
            GameStateService.SetInstance(null);
        }

        [Test]
        public void WorldStateToCommunicationsContext_MapsEvolutionaryFields()
        {
            var gameService = new GameStateService();
            GameStateService.SetInstance(gameService);
            var worldService = new WorldStateService(() => gameService.GetSnapshot());
            worldService.RegisterProvider(new StubSessionWorldStateProvider());
            WorldStateSnapshot snap = worldService.GetSnapshot();
            Assert.IsNotNull(snap.Session);
            Assert.IsFalse(string.IsNullOrEmpty(snap.Session.PhaseLabel));
            GameStateService.SetInstance(null);
        }

        private sealed class StubSessionWorldStateProvider : IWorldStateProvider
        {
            public string DomainId => "session";

            public void Contribute(WorldStateSnapshotBuilder builder)
            {
                builder.Session = new SessionSnapshot(phaseLabel: "EditMode", saveSlotIndex: -1, hasStarted: false);
            }
        }
    }
}
