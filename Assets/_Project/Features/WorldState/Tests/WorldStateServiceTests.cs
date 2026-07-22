using NUnit.Framework;
using Project.Features.GameState;
using Project.Features.WorldState;

namespace Project.Features.WorldState.Tests
{
    public class WorldStateServiceTests
    {
        [Test]
        public void GetSnapshot_EmbedsGameState()
        {
            var game = new GameStateService();
            game.RegisterProvider(new ColonyProvider());
            var world = new WorldStateService(() => game.GetSnapshot());
            WorldStateSnapshot snap = world.GetSnapshot();
            Assert.AreEqual(7, snap.Game.Colony.SkilledCount);
        }

        [Test]
        public void Provider_SetsThreat()
        {
            var world = new WorldStateService(() => GameStateSnapshot.Empty);
            world.RegisterProvider(new ThreatProvider());
            Assert.IsTrue(world.GetSnapshot().Threat.SulfurStormActive);
        }

        private sealed class ColonyProvider : IGameStateProvider
        {
            public string DomainId => "colony";
            public void Contribute(GameStateSnapshotBuilder builder)
            {
                builder.Colony = new ColonySnapshot(skilledCount: 7);
            }
        }

        private sealed class ThreatProvider : IWorldStateProvider
        {
            public string DomainId => "environment";
            public void Contribute(WorldStateSnapshotBuilder builder)
            {
                builder.Threat = new ThreatSnapshot(sulfurStormActive: true, stormPhaseLabel: "Active");
            }
        }
    }
}
