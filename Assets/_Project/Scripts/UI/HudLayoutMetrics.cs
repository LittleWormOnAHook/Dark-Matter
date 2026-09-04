using UnityEngine;

namespace Project.UI
{
    internal static class HudLayoutMetrics
    {
        /// <summary>Hotbar / toolbar rendered at 60% of the original art size (40% reduction).</summary>
        public const float HudScale = 0.6f;

        /// <summary>Additional 20% reduction for inventory slot frames.</summary>
        public const float InventorySlotScale = 0.8f;

        /// <summary>User-facing slot size boost applied on top of scaled slot metrics.</summary>
        public const float SlotDisplayBoost = 1.69f;

        /// <summary>Icons render at 95% of slot area — inset matches sliced frame inner grey holder.</summary>
        public const float InventoryIconScale = 0.95f;

        public static float Scaled(float value) => value * HudScale;

        public static int ScaledInt(float value) => Mathf.RoundToInt(value * HudScale);

        public static float InventorySlotSize(float baseSize = 64f) =>
            baseSize * HudScale * InventorySlotScale * SlotDisplayBoost;

        public static float HalfInchPixels
        {
            get
            {
                float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
                return dpi * 0.5f;
            }
        }

        /// <summary>
        /// Bottom margin for hotbar / toolbar cluster (literal pixels; was ScreenEdgeGap).
        /// </summary>
        public const float BottomHudInset = 30f;

        public static float RightHudInset => HalfInchPixels;

        public static float TopHudInset => HalfInchPixels;
    }
}
