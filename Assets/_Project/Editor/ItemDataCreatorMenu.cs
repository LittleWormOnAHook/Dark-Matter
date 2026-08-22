using UnityEditor;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu redirect only. The obsolete EditorWindow type was removed so Play Mode
    /// layout restore no longer tries to host "Item Data Creator".
    /// </summary>
    public static class ItemDataCreatorMenu
    {
        [MenuItem(DarkMatterGenesisEditorMenus.ItemDataCreator, false, 0)]
        public static void ShowWindow()
        {
            BlueprintCraftingManagerWindow.OpenItemDataTab();
        }
    }
}
