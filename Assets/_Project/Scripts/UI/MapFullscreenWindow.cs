using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class MapFullscreenWindow : FullscreenUiWindow
    {
        private MapUI mapUi;

        public void Configure(MapUI map)
        {
            mapUi = map;
        }

        public override void OnShow()
        {
            if (mapUi == null)
                mapUi = FindAnyObjectByType<MapUI>();

            if (rootRect != null)
                rootRect.gameObject.SetActive(false);

            mapUi?.OpenMapFullscreen();
            GameplayHudVisibility.SetJournalTabHud(JournalWindowId.Map);
        }

        public override void OnHide()
        {
            mapUi?.CloseFullMapFromNavigator();
        }
    }
}
