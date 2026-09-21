using System;
using Project.Data;
using UnityEngine;

namespace Project.Vendor
{
    [Serializable]
    public class DMVendorListing
    {
        public ItemData item;
        [Min(0)] public int stockMin = 1;
        [Min(0)] public int stockMax = 8;
        [Tooltip("0 = use resolved item price.")]
        [Min(0)] public int buyPriceOverride;
        [Min(0)] public int sellPriceOverride;
        [Min(0)] public int requiredLevel;
        public bool armorPlaceholder;
        public string placeholderLabel = "Coming soon";

        public string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(placeholderLabel))
                return placeholderLabel;
            if (armorPlaceholder)
                return "Suit / Armor — Coming soon";
            if (item != null && !string.IsNullOrWhiteSpace(item.itemName))
                return item.itemName;
            if (item != null && !string.IsNullOrWhiteSpace(item.name))
                return item.name;
            return "Coming soon";
        }
    }
}
