namespace Project.Features.GameState
{
    public sealed class GameStateSnapshotBuilder
    {
        public PlayerSnapshot Player { get; set; } = PlayerSnapshot.Empty;
        public InventorySnapshot Inventory { get; set; } = InventorySnapshot.Empty;
        public MissionSnapshot Mission { get; set; } = MissionSnapshot.Empty;
        public WeatherSnapshot Weather { get; set; } = WeatherSnapshot.Empty;
        public PowerSnapshot Power { get; set; } = PowerSnapshot.Empty;
        public ColonySnapshot Colony { get; set; } = ColonySnapshot.Empty;
        public ResearchSnapshot Research { get; set; } = ResearchSnapshot.Empty;
        public CrewSnapshot Crew { get; set; } = CrewSnapshot.Empty;
        public BuildingSnapshot Buildings { get; set; } = BuildingSnapshot.Empty;
        public PptKnowledgeSnapshot PptKnowledge { get; set; } = PptKnowledgeSnapshot.Empty;

        public GameStateSnapshot Build(long capturedAtUtcTicks)
        {
            return new GameStateSnapshot(
                capturedAtUtcTicks,
                Player,
                Inventory,
                Mission,
                Weather,
                Power,
                Colony,
                Research,
                Crew,
                Buildings,
                PptKnowledge);
        }
    }
}
