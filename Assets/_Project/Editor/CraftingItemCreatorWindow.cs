using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete redirect — use <see cref="BlueprintCraftingManagerWindow"/> Crafting Item tab.
    /// Self-closes if Unity restores a stale layout instance.
    /// </summary>
    public class CraftingItemCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.CraftingItemCreator, false, 1)]
        public static void Open()
        {
            CloseAllInstances();
            BlueprintCraftingManagerWindow.OpenCraftingItemTab();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Crafting Item Creator");
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
            CraftingItemCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<CraftingItemCreatorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                    windows[i].Close();
            }
        }
    }
}
