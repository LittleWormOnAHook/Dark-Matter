using NUnit.Framework;
using Project.Features.GameState;

namespace Project.Features.GameState.Tests
{
    public class GameStateServiceTests
    {
        [Test]
        public void GetSnapshot_WithNoProviders_ReturnsEmptyDomains()
        {
            var service = new GameStateService();
            GameStateSnapshot snap = service.GetSnapshot();
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.Colony.SkilledCount);
            Assert.AreEqual(0, snap.Inventory.OccupiedSlots);
        }

        [Test]
        public void RegisterProvider_ContributesDomain()
        {
            var service = new GameStateService();
            service.RegisterProvider(new StubColonyProvider());
            GameStateSnapshot snap = service.GetSnapshot();
            Assert.AreEqual(42f, snap.Colony.AetherCredits);
            Assert.AreEqual(3, snap.Colony.SkilledCount);
        }

        [Test]
        public void RegisterProvider_SameDomainId_Replaces()
        {
            var service = new GameStateService();
            service.RegisterProvider(new StubColonyProvider());
            service.RegisterProvider(new StubColonyProviderB());
            Assert.AreEqual(99f, service.GetSnapshot().Colony.AetherCredits);
        }

        private sealed class StubColonyProvider : IGameStateProvider
        {
            public string DomainId => "colony";
            public void Contribute(GameStateSnapshotBuilder builder)
            {
                builder.Colony = new ColonySnapshot(aetherCredits: 42f, skilledCount: 3);
            }
        }

        private sealed class StubColonyProviderB : IGameStateProvider
        {
            public string DomainId => "colony";
            public void Contribute(GameStateSnapshotBuilder builder)
            {
                builder.Colony = new ColonySnapshot(aetherCredits: 99f, skilledCount: 1);
            }
        }
    }
}
