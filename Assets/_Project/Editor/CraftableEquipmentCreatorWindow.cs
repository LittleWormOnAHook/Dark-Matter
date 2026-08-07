using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete — use <see cref="BlueprintCraftingManagerWindow"/> Equipment Craft tab instead.
    /// </summary>
    public class CraftableEquipmentCreatorWindow : EditorWindow
    {
        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Craftable Equipment Recipe Creator", false, 101)]
        public static void Open()
        {
            BlueprintCraftingManagerWindow.OpenEquipmentTab();
        }
    }
}
