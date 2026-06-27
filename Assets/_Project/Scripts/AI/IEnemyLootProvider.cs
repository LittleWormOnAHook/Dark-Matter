namespace Project.AI
{
    public interface IEnemyLootProvider
    {
        bool HasRemainingLoot { get; }
        bool TryLootNextEntry();
        bool TryLootAll();
        string BuildLootSummary();
    }
}
