using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Shared HUD metrics for minimap-adjacent UI and compact gameplay modals.
    /// </summary>
    public static class GameplayHudLayout
    {
        public const float MinimapEdgeInset = 16f;
        public const float MinimapSize = 147f;
        public const float MinimapTitleBarHeight = 0f;
        public const float MinimapInfoPanelHeight = 24f;
        public const float ToastGapBelowMinimap = 10f;
        public const float ToastWidth = 300f;
        public const float ModalHeaderHeight = 44f;
        public static readonly Vector2 GameplayModalSize = new Vector2(480f, 360f);
        public static readonly Vector2 QuestGiverModalSize = new Vector2(900f, 520f);

        public static float MinimapTotalHeight =>
            MinimapSize + MinimapTitleBarHeight + MinimapInfoPanelHeight;

        public static Vector2 PickupToastAnchoredPosition =>
            new Vector2(-MinimapEdgeInset, -(MinimapEdgeInset + MinimapTotalHeight + ToastGapBelowMinimap));

        public const float XpToastGapBelowPickup = 6f;

        public static Vector2 XpToastAnchoredPosition =>
            new Vector2(
                PickupToastAnchoredPosition.x,
                PickupToastAnchoredPosition.y - 48f - XpToastGapBelowPickup);

        public const float AchievementToastGapBelowXp = 6f;

        public static Vector2 AchievementToastAnchoredPosition =>
            new Vector2(
                XpToastAnchoredPosition.x,
                XpToastAnchoredPosition.y - 42f - AchievementToastGapBelowXp);
    }
}
