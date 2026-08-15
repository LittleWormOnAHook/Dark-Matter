using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete — use <see cref="BlueprintCraftingManagerWindow"/> Pickup Prefabs tab instead.
    /// </summary>
    public class RecipePrefabCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.RecipePrefabCreator, false, 14)]
        public static void Open()
        {
            BlueprintCraftingManagerWindow.OpenPickupsTab();
        }
    }
}
