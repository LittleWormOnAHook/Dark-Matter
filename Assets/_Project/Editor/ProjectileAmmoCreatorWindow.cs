using UnityEditor;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu redirect only. Ammo types and DMAmmoFxProfiles are authored on the
    /// Blueprint + Crafting Manager Ammo tab.
    /// </summary>
    public static class ProjectileAmmoCreatorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.ProjectileAmmoCreator, false, 20)]
        public static void ShowWindow()
        {
            BlueprintCraftingManagerWindow.OpenAmmoTab();
        }
    }
}
