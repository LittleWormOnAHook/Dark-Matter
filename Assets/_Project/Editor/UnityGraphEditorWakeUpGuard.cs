#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Prevents UnityEditor.Graphs.Edge.WakeUp NullReferenceExceptions during domain reload
    /// when Animator / Blend Tree graph windows stay open with stale edge references (Unity 6 editor bug).
    /// </summary>
    [InitializeOnLoad]
    public static class UnityGraphEditorWakeUpGuard
    {
        static UnityGraphEditorWakeUpGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnBeforeAssemblyReload;
            EditorApplication.delayCall += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            CloseGraphEditorWindows();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Maintenance + "Close Graph Editor Windows", false, 40)]
        public static void CloseGraphEditorWindowsMenu()
        {
            int closed = CloseGraphEditorWindows();
            Debug.Log($"[GraphGuard] Closed {closed} graph editor window(s).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Maintenance + "Reserialize Animator Controllers", false, 41)]
        public static void ReserializeAnimatorControllersMenu()
        {
            int count = ReserializeAnimatorControllers();
            Debug.Log($"[GraphGuard] Marked {count} AnimatorController asset(s) dirty and saved.");
            EditorUtility.DisplayDialog(
                "Animator Controllers",
                $"Reserialized {count} AnimatorController asset(s).\n\n" +
                "If Graphs.Edge errors persist, close Animator windows before script recompile " +
                "and avoid leaving broken controllers open in the Inspector.",
                "OK");
        }

        public static int CloseGraphEditorWindows()
        {
            int closed = 0;
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i];
                if (window == null)
                    continue;

                if (!ShouldCloseGraphWindow(window))
                    continue;

                try
                {
                    window.Close();
                    closed++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GraphGuard] Could not close {window.GetType().Name}: {ex.Message}");
                }
            }

            return closed;
        }

        public static int ReserializeAnimatorControllers()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
            int count = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                    continue;

                EditorUtility.SetDirty(asset);
                count++;
            }

            if (count > 0)
                AssetDatabase.SaveAssets();

            return count;
        }

        private static bool ShouldCloseGraphWindow(EditorWindow window)
        {
            Type type = window.GetType();
            string fullName = type.FullName ?? string.Empty;
            string name = type.Name ?? string.Empty;

            if (fullName.IndexOf("UnityEditor.Graphs", StringComparison.Ordinal) >= 0)
                return true;

            if (name.IndexOf("Animator", StringComparison.OrdinalIgnoreCase) >= 0
                && name.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (name.IndexOf("BlendTree", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (name.IndexOf("StateMachine", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }
    }
}
#endif
