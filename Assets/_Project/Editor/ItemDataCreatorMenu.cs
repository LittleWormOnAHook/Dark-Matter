using UnityEditor;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu redirect only. The obsolete EditorWindow type was removed so Play Mode
    /// layout restore no longer tries to host "Item Data Creator".
    /// </summary>
    public static class ItemDataCreatorMenu
    {
        [MenuItem(DarkMatterGenesisEditorMenus.ItemDataCreator, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Item_Data_Creator)]
        public static void ShowWindow()
        {
            BlueprintCraftingManagerWindow.OpenItemDataTab();
        }
    }
}
