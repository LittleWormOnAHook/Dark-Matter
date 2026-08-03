using Project.AI;
using Project.Map;
using UnityEngine;

namespace Project.Interaction
{
    public static class ScannerHighlightResolver
    {
        public static bool TryResolve(
            GameObject targetObject,
            ScannerHighlightProfile profile,
            out ScannerHighlightRule rule,
            out string label)
        {
            rule = default;
            label = targetObject != null ? targetObject.name : "Unknown";

            if (targetObject == null || profile == null)
                return false;

            ScannableTarget scannable = targetObject.GetComponentInParent<ScannableTarget>();
            if (scannable != null)
            {
                if (scannable.HiddenFromScanner)
                    return false;

                label = scannable.ScanLabel;
                if (scannable.HasCategoryOverride &&
                    profile.TryGetRuleForCategory(scannable.ScanCategory, out rule))
                {
                    rule.outlineColor = scannable.ScanColor;
                    return true;
                }
            }

            ResourceNode resourceNode = targetObject.GetComponentInParent<ResourceNode>();
            if (resourceNode != null)
            {
                label = resourceNode.GetDisplayName();
                if (profile.TryGetRuleForCategory(ScannerTargetCategory.Resource, out rule))
                    return true;
            }

            ItemPickup pickup = targetObject.GetComponentInParent<ItemPickup>();
            if (pickup != null && !pickup.IsPickedUp)
            {
                label = pickup.itemData != null ? pickup.itemData.itemName : "Pickup";
                if (profile.TryGetRuleForCategory(ScannerTargetCategory.Loot, out rule))
                    return true;
            }

            MapMarker mapMarker = targetObject.GetComponentInParent<MapMarker>();
            if (mapMarker != null)
            {
                label = mapMarker.Label;
                if (profile.TryGetRuleForCategory(ScannerTargetCategory.Quest, out rule))
                {
                    rule.outlineColor = mapMarker.Color;
                    return true;
                }
            }

            if (targetObject.GetComponentInParent<EnemyHealth>() != null ||
                targetObject.CompareTag("Enemy") ||
                targetObject.CompareTag("Boss"))
            {
                label = targetObject.name;
                if (profile.TryGetRuleForTag(targetObject.tag, out rule))
                    return true;
            }

            if (profile.TryGetRuleForTag(targetObject.tag, out rule))
                return true;

            rule = profile.fallbackRule;
            if (scannable != null)
                rule.outlineColor = scannable.ScanColor;

            return true;
        }
    }
}
