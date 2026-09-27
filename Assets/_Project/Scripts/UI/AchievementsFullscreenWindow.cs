using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class AchievementsFullscreenWindow : FullscreenUiWindow
    {
        private AchievementsPanelUI achievementsPanelUi;

        public void Configure(AchievementsPanelUI panel)
        {
            achievementsPanelUi = panel;
        }

        public override void OnShow()
        {
            if (achievementsPanelUi == null)
                achievementsPanelUi = FindAnyObjectByType<AchievementsPanelUI>();

            achievementsPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            achievementsPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            achievementsPanelUi?.Refresh();
        }
    }
}
