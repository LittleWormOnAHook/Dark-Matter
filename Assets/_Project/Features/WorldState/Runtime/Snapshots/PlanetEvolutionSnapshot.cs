namespace Project.Features.WorldState
{
    public sealed class PlanetEvolutionSnapshot
    {
        public static readonly PlanetEvolutionSnapshot Empty = new PlanetEvolutionSnapshot();

        public int WorldSeed { get; }
        public float ExplorationPercent { get; }
        public int BiomeUnlockMask { get; }

        public PlanetEvolutionSnapshot(int worldSeed = 0, float explorationPercent = 0f, int biomeUnlockMask = 0)
        {
            WorldSeed = worldSeed;
            ExplorationPercent = explorationPercent;
            BiomeUnlockMask = biomeUnlockMask;
        }
    }
}
