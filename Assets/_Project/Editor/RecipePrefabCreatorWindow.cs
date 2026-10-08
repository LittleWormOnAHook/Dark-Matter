using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete redirect — use <see cref="BlueprintCraftingManagerWindow"/> Pickup Prefabs tab.
    /// Self-closes if Unity restores a stale layout instance.
    /// </summary>
    public class RecipePrefabCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.RecipePrefabCreator, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Blueprint_Prefab_Creator)]
        public static void Open()
        {
            CloseAllInstances();
            BlueprintCraftingManagerWindow.OpenPickupsTab();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Recipe Prefab Creator");
            EditorApplication.delayCall += CloseIfAlive;
        }

        private void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
        }

        private void CloseIfAlive()
        {
            if (this != null)
                Close();
        }

        [InitializeOnLoadMethod]
        private static void CleanupOnLoad()
        {
            EditorApplication.delayCall += CloseAllInstances;
        }

        private static void CloseAllInstances()
        {
            RecipePrefabCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<RecipePrefabCreatorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                    windows[i].Close();
            }
        }
    }
}
