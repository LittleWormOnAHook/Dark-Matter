using Project.UI;
using UnityEngine;

namespace Project.Progression
{
    public static class ProgressionRewardGranter
    {
        public static bool GrantXp(int amount, XpSource source, string oneTimeKey = null, string toastLabel = null, bool showToast = true)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null || amount <= 0)
                return false;

            if (!progression.TryGrantXp(amount, source, oneTimeKey))
                return false;

            if (showToast)
            {
                string label = toastLabel ?? FormatSourceLabel(source);
                XpToastUI.Show(amount, label);
            }

            return true;
        }

        private static string FormatSourceLabel(XpSource source)
        {
            return source switch
            {
                XpSource.Quest => "Quest",
                XpSource.Craft => "Craft",
                XpSource.Combat => "Combat",
                XpSource.Exploration => "Discovery",
                XpSource.Building => "Building",
                XpSource.SpecialItem => "Special",
                XpSource.Achievement => "Achievement",
                _ => "XP"
            };
        }
    }
}
