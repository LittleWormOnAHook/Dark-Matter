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

        /// <summary>
        /// Circular ring only — Range%/Scan info stacks below the compass, not inside minimap chrome.
        /// </summary>
        public static float MinimapTotalHeight =>
            MinimapSize + MinimapTitleBarHeight;

        // --- Compass strip: top-right, directly below the minimap. ---
        public const float CompassGapBelowMinimap = 10f;
        public const float CompassWidth = 240f;
        public const float CompassStripHeight = 40f;
        public const float CompassPointerHeight = 10f;
        public const float CompassHeadingLabelHeight = 18f;

        public static float CompassTotalHeight =>
            CompassStripHeight + CompassPointerHeight + CompassHeadingLabelHeight;

        public static Vector2 CompassAnchoredPosition =>
            new Vector2(-MinimapEdgeInset, -(MinimapEdgeInset + MinimapTotalHeight + CompassGapBelowMinimap));

        // --- Range%/Scan info panel below the compass. ---
        public const float InfoPanelGapBelowCompass = 5f;

        public static Vector2 InfoPanelAnchoredPosition =>
            new Vector2(
                CompassAnchoredPosition.x,
                CompassAnchoredPosition.y - CompassTotalHeight - InfoPanelGapBelowCompass);

        // --- Center-screen feedback toasts (pickup / XP / messages). ---
        public const float CenterToastY = 36f;
        public const float XpToastGapBelowPickup = 8f;
        public const float AchievementToastGapBelowXp = 10f;

        public static Vector2 PickupToastAnchoredPosition =>
            new Vector2(0f, CenterToastY);

        public static Vector2 XpToastAnchoredPosition =>
            new Vector2(0f, CenterToastY - 48f - XpToastGapBelowPickup);

        public static Vector2 AchievementToastAnchoredPosition =>
            new Vector2(0f, CenterToastY - 48f - XpToastGapBelowPickup - 52f - AchievementToastGapBelowXp);

        public static Vector2 MessageToastAnchoredPosition =>
            PickupToastAnchoredPosition;
    }
}
