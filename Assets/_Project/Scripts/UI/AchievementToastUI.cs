namespace Project.UI
{
    /// <summary>
    /// Backward-compatible alias for achievement unlock feedback.
    /// </summary>
    public static class AchievementToastUI
    {
        public static void Show(string achievementTitle, int xpReward = 0)
        {
            AchievementUnlockPopupUI.Show(achievementTitle, null, xpReward);
        }
    }
}
