using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class CharacterFullscreenWindow : FullscreenUiWindow
    {
        private CharacterPanelUI characterPanelUi;

        public void Configure(CharacterPanelUI panel)
        {
            characterPanelUi = panel;
        }

        public override void OnShow()
        {
            if (characterPanelUi == null)
                characterPanelUi = FindAnyObjectByType<CharacterPanelUI>();

            characterPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            characterPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            characterPanelUi?.Refresh();
        }
    }
}
