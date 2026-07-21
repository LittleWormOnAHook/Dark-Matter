using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Manual recovery tools for broken Inspector windows and stale selection after Play Mode.
    /// Auto hooks are intentionally minimal to avoid editor instability.
    /// </summary>
    public static class EditorLayoutGuard
    {
        private static bool deferredRecoveryScheduled;

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeRecovery()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;

            ScheduleInspectorRecovery();
        }

        private static void RunDeferredInspectorRecovery()
        {
            deferredRecoveryScheduled = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (HasStaleInspectorTargets())
                ClearSelectionOnly();
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Fix Failed Editor Windows", false, 0)]
        public static void CloseFailedEditorWindowsMenu()
        {
            int closed = CloseFailedEditorWindows();
            bool clearedInspector = RecoverStaleInspectorState(silent: true, aggressive: true);
            Debug.Log(closed > 0 || clearedInspector
                ? "Recovered editor windows and/or stale Inspector state."
                : "No failed editor windows or stale Inspector state found.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Clear Stale Selection", false, 1)]
        public static void ClearStaleSelectionMenu()
        {
            RecoverStaleInspectorState(silent: false, aggressive: true);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Reset Editor Layout", false, 2)]
        public static void ResetEditorLayout()
        {
            CloseFailedEditorWindows();
            RecoverStaleInspectorState(silent: true, aggressive: true);
            EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
            Debug.Log("Editor layout reset to Default.");
        }

        public static void ClearStaleSelection()
        {
            ClearSelectionAndRebuildInspectors();
            RecoverStaleInspectorState(silent: true, aggressive: true);
        }

        public static void ClearSelectionOnly()
        {
            Selection.activeObject = null;
            Selection.activeGameObject = null;
            Selection.objects = System.Array.Empty<Object>();
        }

        /// <summary>
        /// Clears Selection and rebuilds inspectors. Use from menu actions only.
        /// </summary>
        public static void ClearSelectionAndRebuildInspectors()
        {
            ClearSelectionOnly();
            ActiveEditorTracker.sharedTracker.isLocked = false;
            DestroyBrokenActiveEditors();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        public static void BeforeDestroySceneObject(GameObject root)
        {
            if (root == null)
                return;

            if (!SelectionReferencesHierarchy(root))
                return;

            ClearSelectionOnly();
        }

        public static void ScheduleInspectorRecovery()
        {
            if (deferredRecoveryScheduled)
                return;

            deferredRecoveryScheduled = true;
            EditorApplication.delayCall += RunDeferredInspectorRecovery;
        }

        public static bool HasStaleInspectorTargets()
        {
            Object[] selected = Selection.objects;
            if (selected == null || selected.Length == 0)
                return false;

            for (int i = 0; i < selected.Length; i++)
            {
                if (!IsValidEditorTarget(selected[i]))
                    return true;
            }

            return false;
        }

        public static bool RecoverStaleInspectorState(bool silent, bool aggressive = false)
        {
            bool changed = false;
            bool staleTargets = HasStaleInspectorTargets();

            if (aggressive && PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                StageUtility.GoToMainStage();
                changed = true;
            }

            Object[] selected = Selection.objects;
            if (selected != null && selected.Length > 0)
            {
                List<Object> valid = new List<Object>(selected.Length);
                for (int i = 0; i < selected.Length; i++)
                {
                    Object obj = selected[i];
                    if (IsValidEditorTarget(obj))
                        valid.Add(obj);
                    else
                        changed = true;
                }

                if (changed)
                    Selection.objects = valid.Count > 0 ? valid.ToArray() : System.Array.Empty<Object>();
            }

            if (Selection.activeObject != null && !IsValidEditorTarget(Selection.activeObject))
            {
                Selection.activeObject = null;
                changed = true;
            }

            if (Selection.activeGameObject != null && !IsValidEditorTarget(Selection.activeGameObject))
            {
                Selection.activeGameObject = null;
                changed = true;
            }

            if (aggressive || staleTargets || changed)
            {
                if (aggressive || staleTargets)
                {
                    ClearSelectionOnly();
                    changed = true;
                }

                DestroyBrokenActiveEditors();
                ActiveEditorTracker.sharedTracker.isLocked = false;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
            }

            if (changed && !silent)
                Debug.Log("Recovered stale Inspector selection and rebuilt active editors.");

            return changed;
        }

        private static bool IsValidEditorTarget(Object obj)
        {
            if (obj == null)
                return false;

            try
            {
                if (obj is GameObject gameObject)
                    return gameObject != null;

                if (obj is Component component)
                {
                    if (component == null)
                        return false;

                    return component.gameObject != null;
                }
            }
            catch (MissingReferenceException)
            {
                return false;
            }

            return true;
        }

        private static void DestroyBrokenActiveEditors()
        {
            Editor[] editors = ActiveEditorTracker.sharedTracker.activeEditors;
            if (editors == null)
                return;

            for (int i = editors.Length - 1; i >= 0; i--)
            {
                Editor editor = editors[i];
                if (editor == null)
                    continue;

                if (EditorHasBrokenTargets(editor))
                    Object.DestroyImmediate(editor);
            }
        }

        private static bool EditorHasBrokenTargets(Editor editor)
        {
            if (editor == null)
                return true;

            try
            {
                Object[] targets = editor.targets;
                if (targets == null || targets.Length == 0)
                    return editor.target == null;

                for (int i = 0; i < targets.Length; i++)
                {
                    if (!IsValidEditorTarget(targets[i]))
                        return true;
                }

                return !IsValidEditorTarget(editor.target);
            }
            catch (MissingReferenceException)
            {
                return true;
            }
        }

        private static bool SelectionReferencesHierarchy(GameObject root)
        {
            if (root == null)
                return false;

            if (Selection.activeGameObject != null &&
                IsSameHierarchy(root, Selection.activeGameObject))
            {
                return true;
            }

            Object[] selected = Selection.objects;
            if (selected == null)
                return false;

            for (int i = 0; i < selected.Length; i++)
            {
                Object obj = selected[i];
                if (obj == null)
                    return true;

                if (obj is GameObject gameObject && IsSameHierarchy(root, gameObject))
                    return true;

                if (obj is Component component && component != null && IsSameHierarchy(root, component.gameObject))
                    return true;
            }

            return false;
        }

        private static bool IsSameHierarchy(GameObject root, GameObject candidate)
        {
            if (root == null || candidate == null)
                return false;

            return candidate == root || candidate.transform.IsChildOf(root.transform);
        }

        private static int CloseFailedEditorWindows()
        {
            int closed = 0;
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i];
                if (window == null)
                    continue;

                bool shouldClose;
                try
                {
                    shouldClose = IsFailedEditorWindow(window);
                }
                catch
                {
                    continue;
                }

                if (!shouldClose)
                    continue;

                try
                {
                    window.Close();
                    closed++;
                }
                catch
                {
                    // Window became invalid between discovery and close.
                }
            }

            return closed;
        }

        private static bool IsFailedEditorWindow(EditorWindow window)
        {
            GUIContent title = window.titleContent;
            if (title != null && title.text == "Failed to load")
                return true;

            System.Type type = window.GetType();
            if (type == null)
                return false;

            if (type.Name == "FallbackEditorWindow")
                return true;

            return IsBrokenUnityAiAuxWindow(window, type);
        }

        private static bool IsBrokenUnityAiAuxWindow(EditorWindow window, System.Type type)
        {
            string fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName) || !fullName.StartsWith("Unity.AI."))
                return false;

            if (fullName == "Unity.AI.Mesh.Windows.MeshSettingsWindow")
                return true;

            GUIContent title = window.titleContent;
            return title != null && title.text == "Export Settings";
        }
    }
}
