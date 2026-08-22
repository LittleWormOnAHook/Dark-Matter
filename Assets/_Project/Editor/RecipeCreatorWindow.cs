using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete redirect — use <see cref="BlueprintCraftingManagerWindow"/>.
    /// Self-closes if Unity restores a stale layout instance.
    /// </summary>
    public class RecipeCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.Crafting + "Recipe Creator", false, 100)]
        public static void Open()
        {
            CloseAllInstances();
            BlueprintCraftingManagerWindow.OpenBlueprintsTab();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Recipe Creator");
            EditorApplication.delayCall += CloseIfAlive;
        }

        private void OnGUI()
        {
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
            RecipeCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<RecipeCreatorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                    windows[i].Close();
            }
        }
    }
}
