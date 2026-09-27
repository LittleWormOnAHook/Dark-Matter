using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class SkillsFullscreenWindow : FullscreenUiWindow
    {
        private SkillsPanelUI skillsPanelUi;

        public void Configure(SkillsPanelUI panel)
        {
            skillsPanelUi = panel;
        }

        public override void OnShow()
        {
            if (skillsPanelUi == null)
                skillsPanelUi = FindAnyObjectByType<SkillsPanelUI>();

            skillsPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            skillsPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            skillsPanelUi?.Refresh();
        }
    }
}
