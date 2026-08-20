namespace Project.Features.GameState
{
    public sealed class GameStateSnapshot
    {
        public static readonly GameStateSnapshot Empty = new GameStateSnapshot(0L);

        public long CapturedAtUtcTicks { get; }
        public PlayerSnapshot Player { get; }
        public InventorySnapshot Inventory { get; }
        public MissionSnapshot Mission { get; }
        public WeatherSnapshot Weather { get; }
        public PowerSnapshot Power { get; }
        public ColonySnapshot Colony { get; }
        public ResearchSnapshot Research { get; }
        public CrewSnapshot Crew { get; }
        public BuildingSnapshot Buildings { get; }
        public PptKnowledgeSnapshot PptKnowledge { get; }

        public GameStateSnapshot(
            long capturedAtUtcTicks,
            PlayerSnapshot player = null,
            InventorySnapshot inventory = null,
            MissionSnapshot mission = null,
            WeatherSnapshot weather = null,
            PowerSnapshot power = null,
            ColonySnapshot colony = null,
            ResearchSnapshot research = null,
            CrewSnapshot crew = null,
            BuildingSnapshot buildings = null,
            PptKnowledgeSnapshot pptKnowledge = default)
        {
            CapturedAtUtcTicks = capturedAtUtcTicks;
            Player = player ?? PlayerSnapshot.Empty;
            Inventory = inventory ?? InventorySnapshot.Empty;
            Mission = mission ?? MissionSnapshot.Empty;
            Weather = weather ?? WeatherSnapshot.Empty;
            Power = power ?? PowerSnapshot.Empty;
            Colony = colony ?? ColonySnapshot.Empty;
            Research = research ?? ResearchSnapshot.Empty;
            Crew = crew ?? CrewSnapshot.Empty;
            Buildings = buildings ?? BuildingSnapshot.Empty;
            PptKnowledge = pptKnowledge.KnownKeywordCount == 0 && (pptKnowledge.RecentKeywordIds == null || pptKnowledge.RecentKeywordIds.Length == 0)
                ? PptKnowledgeSnapshot.Empty
                : pptKnowledge;
        }
    }
}
