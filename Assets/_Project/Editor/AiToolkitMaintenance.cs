using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Workaround utilities for Unity AI Toolkit temp asset import loops.
    /// </summary>
    public static class AiToolkitMaintenance
    {
        private const string TempFolder = "Assets/AI Toolkit/Temp";

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Fix AI Toolkit Import Loop", false, 20)]
        public static void FixImportLoop()
        {
            CloseUnityAiEditorWindows();
            int removed = ClearTempFolder();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                removed > 0
                    ? $"Cleared {removed} AI Toolkit temp asset(s). If the loop returns, move generated models into Assets/_Project and close Unity AI generator windows."
                    : "No AI Toolkit temp assets found. Closed Unity AI editor windows and refreshed the Asset Database.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Clear AI Toolkit Temp Folder", false, 21)]
        public static void ClearTempFolderMenu()
        {
            int removed = ClearTempFolder();
            AssetDatabase.Refresh();
            Debug.Log(removed > 0
                ? $"Removed {removed} asset(s) from {TempFolder}."
                : $"{TempFolder} is already empty.");
        }

        private static int ClearTempFolder()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
                return 0;

            int removed = 0;
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { TempFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path == TempFolder)
                    continue;

                if (AssetDatabase.DeleteAsset(path))
                    removed++;
            }

            return removed;
        }

        private static void CloseUnityAiEditorWindows()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            int closed = 0;

            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i];
                if (window == null)
                    continue;

                System.Type type = window.GetType();
                if (type == null || type.FullName == null || !type.FullName.StartsWith("Unity.AI."))
                    continue;

                try
                {
                    window.Close();
                    closed++;
                }
                catch
                {
                    // Ignore windows that fail while closing.
                }
            }

            if (closed > 0)
                Debug.Log($"Closed {closed} Unity AI editor window(s).");
        }
    }
}
