using Project.Features.GameState;

namespace Project.Features.WorldState
{
    public sealed class WorldStateSnapshotBuilder
    {
        public GameStateSnapshot Game { get; set; } = GameStateSnapshot.Empty;
        public StoryProgressSnapshot Story { get; set; } = StoryProgressSnapshot.Empty;
        public PlanetEvolutionSnapshot Planet { get; set; } = PlanetEvolutionSnapshot.Empty;
        public ColonyEvolutionSnapshot Colony { get; set; } = ColonyEvolutionSnapshot.Empty;
        public Aether9Snapshot Aether9 { get; set; } = Aether9Snapshot.Empty;
        public SimulationSnapshot Simulation { get; set; } = SimulationSnapshot.Empty;
        public ThreatSnapshot Threat { get; set; } = ThreatSnapshot.Empty;
        public ExperienceSnapshot Experience { get; set; } = ExperienceSnapshot.Empty;
        public SessionSnapshot Session { get; set; } = SessionSnapshot.Empty;

        public WorldStateSnapshot Build(long capturedAtUtcTicks)
        {
            return new WorldStateSnapshot(
                capturedAtUtcTicks,
                Game,
                Story,
                Planet,
                Colony,
                Aether9,
                Simulation,
                Threat,
                Experience,
                Session);
        }
    }
}
