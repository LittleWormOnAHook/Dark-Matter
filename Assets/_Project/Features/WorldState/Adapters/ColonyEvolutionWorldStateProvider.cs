using Project.Features.WorldState;
using Project.Pioneers;

namespace Project.Features.WorldState.Adapters
{
    public sealed class ColonyEvolutionWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "colony";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null)
            {
                builder.Colony = ColonyEvolutionSnapshot.Empty;
                return;
            }

            ColonistAggregateState aggregate = roster.GetColonistState();
            int skilled = roster.SkilledPioneers != null ? roster.SkilledPioneers.Count : 0;
            builder.Colony = new ColonyEvolutionSnapshot(
                totalCompanions: skilled + roster.WorkerCount,
                workerCount: roster.WorkerCount,
                injuredCount: aggregate != null ? aggregate.injuredCount : 0,
                shelteredCount: aggregate != null ? aggregate.shelteredCount : 0,
                echoChronicleCount: roster.EchoChronicle != null ? roster.EchoChronicle.Count : 0,
                aetherCredits: roster.AetherCredits);
        }
    }
}
