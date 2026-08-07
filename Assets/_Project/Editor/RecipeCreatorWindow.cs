using Project.Crafting;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete — use <see cref="BlueprintCraftingManagerWindow"/> instead.
    /// </summary>
    public class RecipeCreatorWindow : EditorWindow
    {
        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Recipe Creator", false, 100)]
        public static void Open()
        {
            BlueprintCraftingManagerWindow.OpenBlueprintsTab();
        }
    }
}
