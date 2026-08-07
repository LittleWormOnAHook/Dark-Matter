using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete — use <see cref="BlueprintCraftingManagerWindow"/> Crafting Item tab instead.
    /// </summary>
    public class CraftingItemCreatorWindow : EditorWindow
    {
        [MenuItem(SurvivalPioneerEditorMenus.CraftingItemCreator, false, 1)]
        public static void Open()
        {
            BlueprintCraftingManagerWindow.OpenCraftingItemTab();
        }
    }
}
