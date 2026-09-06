using System;
using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Tracks item types identified by mining F-scan and scanner sweep on pickups.
    /// TODO: Later some items may require Gerald or a designated NPC to identify (not scanner).
    /// Identification is per item type (asset name), not per world node.
    /// </summary>
    public static class ResourceIdentificationRegistry
    {
        private static readonly HashSet<string> IdentifiedIds = new HashSet<string>(StringComparer.Ordinal);

        public static event Action Changed;

        /// <summary>
        /// Returns the stable save key for <paramref name="item"/>: StableItemId if filled,
        /// falling back to the Unity asset name for backwards compatibility.
        /// </summary>
        public static string ResolveItemId(ItemData item)
        {
            if (item == null)
                return null;

            return !string.IsNullOrEmpty(item.StableItemId) ? item.StableItemId : item.name;
        }

        /// <summary>
        /// Returns true when the item is identified, checking by StableItemId and then by asset name
        /// so old saves that stored the asset name still load correctly.
        /// </summary>
        public static bool IsIdentified(ItemData item)
        {
            if (item == null)
                return false;

            if (!string.IsNullOrEmpty(item.StableItemId) && IdentifiedIds.Contains(item.StableItemId))
                return true;

            return IdentifiedIds.Contains(item.name);
        }

        public static bool IsIdentified(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && IdentifiedIds.Contains(itemId);
        }

        /// <returns>True when this id was newly identified.</returns>
        public static bool Identify(ItemData item) =>
            Identify(ResolveItemId(item));

        /// <returns>True when this id was newly identified.</returns>
        public static bool Identify(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!IdentifiedIds.Add(itemId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public static string[] BuildSave()
        {
            if (IdentifiedIds.Count == 0)
                return Array.Empty<string>();

            var ids = new string[IdentifiedIds.Count];
            IdentifiedIds.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            return ids;
        }

        public static void ApplySave(string[] itemIds)
        {
            IdentifiedIds.Clear();
            if (itemIds != null)
            {
                for (int i = 0; i < itemIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(itemIds[i]))
                        IdentifiedIds.Add(itemIds[i]);
                }
            }

            Changed?.Invoke();
        }

        public static void Clear()
        {
            if (IdentifiedIds.Count == 0)
                return;

            IdentifiedIds.Clear();
            Changed?.Invoke();
        }

        internal static void ResetStaticState()
        {
            IdentifiedIds.Clear();
            Changed = null;
        }
    }
}
