namespace Project.Features.GameState
{
    public sealed class BuildingSnapshot
    {
        public static readonly BuildingSnapshot Empty = new BuildingSnapshot();

        public int BuildingCount { get; }
        public int AssignedPioneerCount { get; }
        public int QueuedJobs { get; }

        public BuildingSnapshot(int buildingCount = 0, int assignedPioneerCount = 0, int queuedJobs = 0)
        {
            BuildingCount = buildingCount;
            AssignedPioneerCount = assignedPioneerCount;
            QueuedJobs = queuedJobs;
        }
    }
}
