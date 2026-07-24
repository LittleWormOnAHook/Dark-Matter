using Project.Features.GameState;
using Project.Pioneers;

namespace Project.Features.GameState.Adapters
{
    public sealed class ColonyGameStateProvider : IGameStateProvider
    {
        public string DomainId => "colony";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null)
            {
                builder.Colony = ColonySnapshot.Empty;
                return;
            }

            ColonistAggregateState aggregate = roster.GetColonistState();
            builder.Colony = new ColonySnapshot(
                aetherCredits: roster.AetherCredits,
                workerCount: roster.WorkerCount,
                skilledCount: roster.SkilledPioneers != null ? roster.SkilledPioneers.Count : 0,
                injuredCount: aggregate != null ? aggregate.injuredCount : 0,
                shelteredCount: aggregate != null ? aggregate.shelteredCount : 0,
                assignedToFacilityCount: aggregate != null ? aggregate.assignedToFacilityCount : 0,
                expeditionTrioCount: roster.GetActiveExpeditionTrioCount(),
                starterPioneerSelected: roster.StarterPioneerSelected);
        }
    }
}
