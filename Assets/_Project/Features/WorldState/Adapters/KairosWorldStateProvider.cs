using Project.Features.WorldState;

namespace Project.Features.WorldState.Adapters
{
    /// <summary>Stub until Kairos quest / Communications advisory flag exists.</summary>
    public sealed class KairosWorldStateProvider : IWorldStateProvider
    {
        public static bool AdvisoryUnlocked { get; set; }

        public string DomainId => "kairos";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            builder.Kairos = new KairosSnapshot(
                advisoryUnlocked: AdvisoryUnlocked,
                awake: AdvisoryUnlocked,
                memoryCoresAttached: 0);
        }
    }
}
