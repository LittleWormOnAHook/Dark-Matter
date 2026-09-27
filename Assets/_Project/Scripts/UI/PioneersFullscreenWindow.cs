using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class PioneersFullscreenWindow : FullscreenUiWindow
    {
        private PioneerRosterPanelUI pioneerRosterPanelUi;

        public void Configure(PioneerRosterPanelUI rosterUi)
        {
            pioneerRosterPanelUi = rosterUi;
        }

        public override void OnShow()
        {
            if (pioneerRosterPanelUi == null)
                pioneerRosterPanelUi = FindAnyObjectByType<PioneerRosterPanelUI>();

            pioneerRosterPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            pioneerRosterPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            pioneerRosterPanelUi?.Refresh();
        }
    }
}
