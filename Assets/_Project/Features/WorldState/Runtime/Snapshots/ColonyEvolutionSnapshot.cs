namespace Project.Features.WorldState
{
    public sealed class ColonyEvolutionSnapshot
    {
        public static readonly ColonyEvolutionSnapshot Empty = new ColonyEvolutionSnapshot();

        public int TotalCompanions { get; }
        public int WorkerCount { get; }
        public int InjuredCount { get; }
        public int ShelteredCount { get; }
        public int EchoChronicleCount { get; }
        public float AetherCredits { get; }

        public ColonyEvolutionSnapshot(
            int totalCompanions = 0,
            int workerCount = 0,
            int injuredCount = 0,
            int shelteredCount = 0,
            int echoChronicleCount = 0,
            float aetherCredits = 0f)
        {
            TotalCompanions = totalCompanions;
            WorkerCount = workerCount;
            InjuredCount = injuredCount;
            ShelteredCount = shelteredCount;
            EchoChronicleCount = echoChronicleCount;
            AetherCredits = aetherCredits;
        }
    }
}
