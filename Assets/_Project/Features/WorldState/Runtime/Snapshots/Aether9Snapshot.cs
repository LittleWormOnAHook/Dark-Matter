namespace Project.Features.WorldState
{
    public sealed class Aether9Snapshot
    {
        public static readonly Aether9Snapshot Empty = new Aether9Snapshot();

        public bool AdvisoryUnlocked { get; }
        public bool Awake { get; }
        public int MemoryCoresAttached { get; }

        public Aether9Snapshot(bool advisoryUnlocked = false, bool awake = false, int memoryCoresAttached = 0)
        {
            AdvisoryUnlocked = advisoryUnlocked;
            Awake = awake;
            MemoryCoresAttached = memoryCoresAttached;
        }
    }
}
