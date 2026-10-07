using System;

namespace Project.Events
{
    /// <summary>One remaining loot entry (loot plan 9.1). Items only, keyed by ItemData asset name (D6).</summary>
    [Serializable]
    public class LootChestEntrySave
    {
        public string itemId;
        public int amount;
    }

    /// <summary>
    /// Per-chest save state (save v24, loot plan 9.1). Remaining times are seconds, never timestamps.
    /// Phase holds only Closed (0) or Gone (5): open / closing chests save as Closed, dissolving or doomed as Gone.
    /// </summary>
    [Serializable]
    public class LootChestSave
    {
        public string chestId;
        public int phase;
        public bool opened;
        public bool exitedWithLeftovers;
        public bool singleLootLocked;
        public bool timerStarted;
        public float remainingSeconds;
        public float holdRemainingSeconds;
        public LootChestEntrySave[] entries;
    }
}
