namespace Project.Features.GameState
{
    public sealed class ColonySnapshot
    {
        public static readonly ColonySnapshot Empty = new ColonySnapshot();

        public float AetherCredits { get; }
        public int WorkerCount { get; }
        public int SkilledCount { get; }
        public int InjuredCount { get; }
        public int ShelteredCount { get; }
        public int AssignedToFacilityCount { get; }
        public int ExpeditionTrioCount { get; }
        public bool StarterPioneerSelected { get; }

        public ColonySnapshot(
            float aetherCredits = 0f,
            int workerCount = 0,
            int skilledCount = 0,
            int injuredCount = 0,
            int shelteredCount = 0,
            int assignedToFacilityCount = 0,
            int expeditionTrioCount = 0,
            bool starterPioneerSelected = false)
        {
            AetherCredits = aetherCredits;
            WorkerCount = workerCount;
            SkilledCount = skilledCount;
            InjuredCount = injuredCount;
            ShelteredCount = shelteredCount;
            AssignedToFacilityCount = assignedToFacilityCount;
            ExpeditionTrioCount = expeditionTrioCount;
            StarterPioneerSelected = starterPioneerSelected;
        }
    }
}
