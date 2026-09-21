using UnityEngine;

namespace Project.Vendor
{
    [CreateAssetMenu(
        fileName = "DM_VendorProfile",
        menuName = "Dark Matter/Vendor/Vendor Profile")]
    public sealed class DMVendorProfile : ScriptableObject
    {
        public string vendorId = "vendor_commissary";
        public string displayName = "Commissary";
        public DMVendorKind kind = DMVendorKind.Commissary;
        [Tooltip("Daily shop stock only. Not the buy log.")]
        public DMVendorCatalog catalog;
        [Tooltip("Items this vendor will buy from the player. Player slots use this, not the catalog.")]
        public DMVendorBuyLog buyLog;
        [Min(0.1f)] public float buyMarkup = DMVendorPricing.DefaultBuyMarkup;
        [Range(0.05f, 1f)] public float sellRate = DMVendorPricing.DefaultSellRate;
        [Min(0)] public int purseMin = 500;
        [Min(0)] public int purseMax = 800;
        [Min(1f)] public float interactRange = 3.5f;
        [Min(1)] public int shopSlots = 42;
        public string promptText = "Press E — Commissary";
    }
}
