using Project.Data;
using UnityEngine;

namespace Project.Vendor
{
    [CreateAssetMenu(
        fileName = "DM_VendorBuyLog",
        menuName = "Dark Matter/Vendor/Vendor Buy Log")]
    public sealed class DMVendorBuyLog : ScriptableObject
    {
        [Tooltip("Every pickup this vendor will buy. Not shop inventory — that is the catalog.")]
        public ItemData[] items;

        public int Count => items != null ? items.Length : 0;

        public bool Contains(ItemData item)
        {
            if (item == null || items == null)
                return false;

            for (int i = 0; i < items.Length; i++)
            {
                if (DMVendorRuntimeState.SameItem(items[i], item))
                    return true;
            }

            return false;
        }
    }
}
