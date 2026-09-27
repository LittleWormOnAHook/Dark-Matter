using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class PetFullscreenWindow : FullscreenUiWindow
    {
        private PetUI petUi;

        public void Configure(PetUI pet)
        {
            petUi = pet;
        }

        public override void OnShow()
        {
            if (petUi == null)
                petUi = FindAnyObjectByType<PetUI>();

            petUi?.EmbedPanel(contentArea);
            petUi?.RefreshPetList();
        }

        public override void OnHide()
        {
            petUi?.RestorePanel();
        }

        public override void Refresh()
        {
            petUi?.RefreshPetList();
        }
    }
}
