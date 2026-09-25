using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// HideAndDontSave placement hosts are omitted from the Hierarchy and can survive leaving Play.
    /// </summary>
    [InitializeOnLoad]
    public static class DMBuildingPreviewCleanup
    {
        static DMBuildingPreviewCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += CleanStrayPreviews;
        }

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Clean Stray Building Previews")]
        public static void CleanStrayPreviews()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            int removed = 0;
            bool dirtyScene = false;
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (!IsStray(candidate))
                    continue;

                if ((candidate.hideFlags & HideFlags.DontSave) == 0)
                    dirtyScene = true;

                Object.DestroyImmediate(candidate);
                removed++;
            }

            if (removed > 0 && dirtyScene)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        static bool IsStray(GameObject candidate)
        {
            if (candidate == null || EditorUtility.IsPersistent(candidate) || !candidate.scene.IsValid())
                return false;

            string name = candidate.name;
            if (name == "BuildingPreview" || name == "DMBuildingPlacement" || name.StartsWith("BuildingGhost_"))
                return true;

            return candidate.GetComponent<Project.Building.DMBuildingPlacementController>() != null
                || candidate.GetComponent<Project.Building.DMBuildingGhost>() != null;
        }
    }
}
