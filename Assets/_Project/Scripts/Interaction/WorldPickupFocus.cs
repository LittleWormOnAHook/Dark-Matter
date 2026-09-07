using Project.Crafting;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Authoritative exclusive world-pickup focus for stem chrome and hold-E gating.
    /// Set each frame by DMUiToolkitWorldChrome from the exclusive nearest-in-cone target.
    /// Proximity alone is never enough — only this focused target may accept hold-E pickup.
    /// </summary>
    public static class WorldPickupFocus
    {
        /// <summary>
        /// Planar (XZ) distance at which the tip swaps from yellow DOT to the Hold-E / Take prompt
        /// and hold-E pickup becomes eligible.
        /// </summary>
        public const float ClosePromptPlanarMeters = 1.25f;

        /// <summary>Hold duration for focused item/blueprint pickup (seconds).</summary>
        public const float PickupHoldSeconds = 1.5f;

        public static ItemPickup Item { get; private set; }
        public static RecipePickup Recipe { get; private set; }
        public static ResourceNode Harvest { get; private set; }

        public static void Clear()
        {
            Item = null;
            Recipe = null;
            Harvest = null;
        }

        public static void SetItem(ItemPickup pickup)
        {
            Item = pickup;
            Recipe = null;
            Harvest = null;
        }

        public static void SetRecipe(RecipePickup recipe)
        {
            Item = null;
            Recipe = recipe;
            Harvest = null;
        }

        public static void SetHarvest(ResourceNode node)
        {
            Item = null;
            Recipe = null;
            Harvest = node;
        }

        public static bool IsFocused(ItemPickup pickup) =>
            pickup != null && Item == pickup;

        public static bool IsFocused(RecipePickup recipe) =>
            recipe != null && Recipe == recipe;

        public static bool IsWithinClosePromptRange(Vector3 playerPosition, Vector3 worldAnchor)
        {
            Vector3 delta = playerPosition - worldAnchor;
            delta.y = 0f;
            return delta.magnitude <= ClosePromptPlanarMeters;
        }

        /// <summary>
        /// Focused hold-capable pickup (item or blueprint) when the exclusive stem is painted
        /// and the player is inside close-prompt planar range.
        /// </summary>
        public static bool TryGetFocusedHoldPickup(WorldUseContext context, out IHoldWorldUsable hold)
        {
            hold = null;
            if (Item != null && Item is IHoldWorldUsable itemHold && itemHold.CanBeginHold(context))
            {
                hold = itemHold;
                return true;
            }

            if (Recipe != null && Recipe is IHoldWorldUsable recipeHold && recipeHold.CanBeginHold(context))
            {
                hold = recipeHold;
                return true;
            }

            return false;
        }
    }
}
