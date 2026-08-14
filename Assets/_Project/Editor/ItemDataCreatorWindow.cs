using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete — use <see cref="BlueprintCraftingManagerWindow"/> Item Data tab instead.
    /// </summary>
    public class ItemDataCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.ItemDataCreator, false, 0)]
        public static void ShowWindow()
        {
            BlueprintCraftingManagerWindow.OpenItemDataTab();
        }
    }
}
