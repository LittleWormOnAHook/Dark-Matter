using System.Collections.Generic;
using UnityEngine;

namespace Project.Data
{
    [CreateAssetMenu(menuName = "Project/Survival/Item Registry")]
    public class ItemRegistry : ScriptableObject
    {
        private static ItemRegistry cached;
        private static readonly Dictionary<string, ItemData> RuntimeLookup = new Dictionary<string, ItemData>();

        [SerializeField] private ItemData[] items;

        private Dictionary<string, ItemData> lookup;

        public static ItemRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<ItemRegistry>("ItemRegistry");

                return cached;
            }
        }

        public static ItemData Resolve(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            ItemRegistry registry = Instance;
            if (registry != null)
            {
                registry.EnsureLookup();
                if (registry.lookup.TryGetValue(itemId, out ItemData registryItem))
                    return registryItem;
            }

            RuntimeLookup.TryGetValue(itemId, out ItemData runtimeItem);
            return runtimeItem;
        }

        public static void RegisterRuntimeItems(IEnumerable<ItemData> runtimeItems)
        {
            if (runtimeItems == null)
                return;

            foreach (ItemData item in runtimeItems)
                RegisterRuntimeItem(item);
        }

        public static void RegisterRuntimeItem(ItemData item)
        {
            if (item == null)
                return;

            RuntimeLookup[item.name] = item;

            if (!string.IsNullOrEmpty(item.itemName))
                RuntimeLookup[item.itemName] = item;
        }

        public static ItemData[] GetAllItems()
        {
            ItemRegistry registry = Instance;
            if (registry == null || registry.items == null)
                return System.Array.Empty<ItemData>();

            return registry.items;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
                return;

            lookup = new Dictionary<string, ItemData>();
            if (items == null)
                return;

            foreach (ItemData item in items)
                RegisterItem(item);
        }

        private void RegisterItem(ItemData item)
        {
            if (item == null)
                return;

            lookup[item.name] = item;

            if (!string.IsNullOrEmpty(item.itemName))
                lookup[item.itemName] = item;
        }
    }
}
