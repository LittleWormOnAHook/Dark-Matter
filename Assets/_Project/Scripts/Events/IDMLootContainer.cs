using System.Collections.Generic;
using Project.Loot;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// Per-item loot container shown by the UITK loot window (<c>DMUiToolkitLoot</c>).
    /// Implemented by <see cref="DmEvents"/> chests and enemy loot bags. Items only (D6).
    /// </summary>
    public interface IDMLootContainer
    {
        /// <summary>Window title.</summary>
        string LootDisplayName { get; }

        /// <summary>Remaining entries in list order. Rows key on <see cref="DMLootEntry.entryId"/>.</summary>
        IReadOnlyList<DMLootEntry> Entries { get; }

        bool IsEmpty { get; }

        /// <summary>A quest or unique entry is still inside (D2).</summary>
        bool HasProtectedEntries { get; }

        /// <summary>World position for the window's walk-away safety net.</summary>
        Vector3 LootWorldPosition { get; }

        /// <summary>Grants one entry by stable id (never a list index).</summary>
        DMLootGrantResult TryLootEntry(int entryId);

        /// <summary>Takes whatever fits in list order and leaves the rest (D4).</summary>
        DMLootGrantResult TryLootAll();

        /// <summary>
        /// Called by the window on every close path (button, Esc/B, emptied, range, death, forced close).
        /// Drives lid close, timers and the full-inventory hold in later phases.
        /// </summary>
        void NotifyWindowClosed();
    }
}
