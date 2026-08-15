namespace Project.Features.WorldState
{
    public sealed class KairosSnapshot
    {
        public static readonly KairosSnapshot Empty = new KairosSnapshot();

        public bool AdvisoryUnlocked { get; }
        public bool Awake { get; }
        public int MemoryCoresAttached { get; }

        public KairosSnapshot(bool advisoryUnlocked = false, bool awake = false, int memoryCoresAttached = 0)
        {
            AdvisoryUnlocked = advisoryUnlocked;
            Awake = awake;
            MemoryCoresAttached = memoryCoresAttached;
        }
    }
}
