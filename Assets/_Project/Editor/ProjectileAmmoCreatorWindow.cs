using UnityEditor;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu redirect only. Ammo types and DMAmmoFxProfiles are authored on the
    /// Blueprint + Crafting Manager Ammo tab.
    /// </summary>
    public static class ProjectileAmmoCreatorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.ProjectileAmmoCreator, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Projectile_Ammo_Creator)]
        public static void ShowWindow()
        {
            BlueprintCraftingManagerWindow.OpenAmmoTab();
        }
    }
}
