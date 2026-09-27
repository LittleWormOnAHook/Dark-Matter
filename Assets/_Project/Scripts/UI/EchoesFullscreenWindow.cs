using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class EchoesFullscreenWindow : FullscreenUiWindow
    {
        private EchoesPanelUI echoesPanelUi;

        public void Configure(EchoesPanelUI panel)
        {
            echoesPanelUi = panel;
        }

        public override void OnShow()
        {
            if (echoesPanelUi == null)
                echoesPanelUi = FindAnyObjectByType<EchoesPanelUI>();

            echoesPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            echoesPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            echoesPanelUi?.Refresh();
        }
    }
}
