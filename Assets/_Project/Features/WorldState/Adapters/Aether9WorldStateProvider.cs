using Project.Features.WorldState;

namespace Project.Features.WorldState.Adapters
{
    /// <summary>Stub until Aether-9 quest / Communications advisory flag exists.</summary>
    public sealed class Aether9WorldStateProvider : IWorldStateProvider
    {
        public static bool AdvisoryUnlocked { get; set; }

        public string DomainId => "aether9";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            builder.Aether9 = new Aether9Snapshot(
                advisoryUnlocked: AdvisoryUnlocked,
                awake: AdvisoryUnlocked,
                memoryCoresAttached: 0);
        }
    }
}
