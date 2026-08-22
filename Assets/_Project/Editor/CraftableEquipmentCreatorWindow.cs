using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Obsolete redirect — use <see cref="BlueprintCraftingManagerWindow"/> Equipment Craft tab.
    /// Self-closes if Unity restores a stale layout instance.
    /// </summary>
    public class CraftableEquipmentCreatorWindow : EditorWindow
    {
        [MenuItem(DarkMatterGenesisEditorMenus.Crafting + "Craftable Equipment Recipe Creator", false, 101)]
        public static void Open()
        {
            CloseAllInstances();
            BlueprintCraftingManagerWindow.OpenEquipmentTab();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Craftable Equipment Creator");
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
            CraftableEquipmentCreatorWindow[] windows =
                Resources.FindObjectsOfTypeAll<CraftableEquipmentCreatorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                    windows[i].Close();
            }
        }
    }
}
