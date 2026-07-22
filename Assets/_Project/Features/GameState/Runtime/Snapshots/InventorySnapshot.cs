namespace Project.Features.GameState
{
    public sealed class InventorySnapshot
    {
        public static readonly InventorySnapshot Empty = new InventorySnapshot();

        public int InventorySize { get; }
        public int OccupiedSlots { get; }
        public int DistinctItemCount { get; }
        public int TotalStackCount { get; }
        public string[] TopItemLabels { get; }

        public InventorySnapshot(
            int inventorySize = 0,
            int occupiedSlots = 0,
            int distinctItemCount = 0,
            int totalStackCount = 0,
            string[] topItemLabels = null)
        {
            InventorySize = inventorySize;
            OccupiedSlots = occupiedSlots;
            DistinctItemCount = distinctItemCount;
            TotalStackCount = totalStackCount;
            TopItemLabels = topItemLabels ?? System.Array.Empty<string>();
        }
    }
}
