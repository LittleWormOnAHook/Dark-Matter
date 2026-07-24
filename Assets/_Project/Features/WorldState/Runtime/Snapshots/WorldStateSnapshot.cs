using Project.Features.GameState;

namespace Project.Features.WorldState
{
    public sealed class WorldStateSnapshot
    {
        public static readonly WorldStateSnapshot Empty = new WorldStateSnapshot(0L, GameStateSnapshot.Empty);

        public long CapturedAtUtcTicks { get; }
        public GameStateSnapshot Game { get; }
        public StoryProgressSnapshot Story { get; }
        public PlanetEvolutionSnapshot Planet { get; }
        public ColonyEvolutionSnapshot Colony { get; }
        public Aether9Snapshot Aether9 { get; }
        public SimulationSnapshot Simulation { get; }
        public ThreatSnapshot Threat { get; }
        public ExperienceSnapshot Experience { get; }
        public SessionSnapshot Session { get; }

        public WorldStateSnapshot(
            long capturedAtUtcTicks,
            GameStateSnapshot game,
            StoryProgressSnapshot story = null,
            PlanetEvolutionSnapshot planet = null,
            ColonyEvolutionSnapshot colony = null,
            Aether9Snapshot aether9 = null,
            SimulationSnapshot simulation = null,
            ThreatSnapshot threat = null,
            ExperienceSnapshot experience = null,
            SessionSnapshot session = null)
        {
            CapturedAtUtcTicks = capturedAtUtcTicks;
            Game = game ?? GameStateSnapshot.Empty;
            Story = story ?? StoryProgressSnapshot.Empty;
            Planet = planet ?? PlanetEvolutionSnapshot.Empty;
            Colony = colony ?? ColonyEvolutionSnapshot.Empty;
            Aether9 = aether9 ?? Aether9Snapshot.Empty;
            Simulation = simulation ?? SimulationSnapshot.Empty;
            Threat = threat ?? ThreatSnapshot.Empty;
            Experience = experience ?? ExperienceSnapshot.Empty;
            Session = session ?? SessionSnapshot.Empty;
        }

        public string ToOneLineSummary()
        {
            return string.Format(
                "[WorldState] chapter={0} colony={1}/{2} storm={3} threat={4:0.00} a9={5} session={6}",
                Story.ChapterId,
                Colony.TotalCompanions,
                Colony.WorkerCount,
                Threat.SulfurStormActive ? Threat.StormPhaseLabel : "off",
                Threat.EnvironmentThreat01,
                Aether9.AdvisoryUnlocked ? "advisory" : "silent",
                Session.PhaseLabel);
        }
    }
}
