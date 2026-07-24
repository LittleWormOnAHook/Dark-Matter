namespace Project.Features.GameState
{
    public sealed class PowerSnapshot
    {
        public static readonly PowerSnapshot Empty = new PowerSnapshot();

        public int GeneratorCount { get; }
        public int PoweredCount { get; }
        public float AverageFuelPercent { get; }
        public bool AnyCritical { get; }

        public PowerSnapshot(int generatorCount = 0, int poweredCount = 0, float averageFuelPercent = 0f, bool anyCritical = false)
        {
            GeneratorCount = generatorCount;
            PoweredCount = poweredCount;
            AverageFuelPercent = averageFuelPercent;
            AnyCritical = anyCritical;
        }
    }
}
