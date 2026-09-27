using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class JournalQuestFullscreenWindow : FullscreenUiWindow
    {
        private JournalPanelUI host;

        public void Configure(JournalPanelUI journalHost)
        {
            host = journalHost;
        }

        protected override void OnBuild()
        {
            if (contentArea == null || host == null)
                return;

            host.BuildQuestWindowContent(contentArea);
        }

        public override void Refresh()
        {
            host?.RefreshQuestList();
        }
    }
}
