using System.Collections.Generic;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Live seeded generators (any IPcgSeeded component). Registered on enable, removed on destroy, so
    /// objects that were disabled later still count. Lets the editor find "twins" without scanning per type.
    /// </summary>
    public static class PcgSeededRegistry
    {
        private static readonly HashSet<IPcgSeeded> Items = new HashSet<IPcgSeeded>();

        public static IEnumerable<IPcgSeeded> All
        {
            get
            {
                Items.RemoveWhere(i => i == null || (i is UnityEngine.Object o && o == null));
                return Items;
            }
        }

        public static void Register(IPcgSeeded item) { if (item != null) Items.Add(item); }
        public static void Unregister(IPcgSeeded item) { if (item != null) Items.Remove(item); }
    }
}
