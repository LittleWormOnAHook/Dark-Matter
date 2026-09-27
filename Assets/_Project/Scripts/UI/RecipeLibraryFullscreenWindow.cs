using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Blueprints tab — pending scrolls to learn + learned blueprint library.
    /// Production crafting (Craft button) is station/campfire popup only (GDD).
    /// </summary>
    public sealed class RecipeLibraryFullscreenWindow : FullscreenUiWindow
    {
        private CraftingUI craftingUi;

        public void Configure(CraftingUI crafting)
        {
            craftingUi = crafting;
        }

        public override void OnShow()
        {
            if (craftingUi == null)
                craftingUi = FindAnyObjectByType<CraftingUI>();

            craftingUi?.EmbedLibraryPanel(contentArea);
            MenuUiBuilder.StretchRectToFill(GetFirstChildRect(contentArea));
        }

        public override void OnHide()
        {
            craftingUi?.RestorePanel();
        }

        public override void Refresh()
        {
            craftingUi?.RefreshRecipeList();
        }

        private static RectTransform GetFirstChildRect(Transform container)
        {
            if (container == null || container.childCount == 0)
                return null;

            return container.GetChild(0) as RectTransform;
        }
    }
}
