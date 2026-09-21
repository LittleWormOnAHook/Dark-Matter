using UnityEngine;

namespace Project.Vendor
{
    [CreateAssetMenu(
        fileName = "DM_VendorCatalog",
        menuName = "Dark Matter/Vendor/Vendor Catalog")]
    public sealed class DMVendorCatalog : ScriptableObject
    {
        public DMVendorListing[] listings;
    }
}
