using Project.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Removes orphaned one-shot "AudioSource(Clone)" roots (Invector AudioSource prefab spawns) after
    /// leaving Play. The existing play-exit cleaner (DMBuildingPreviewCleanup) only matches building
    /// previews, and PoolManager's orphan sweep only runs in Play on inactive VFX names, so these were
    /// never removed.
    /// </summary>
    [InitializeOnLoad]
    public static class DMSpawnedAudioLeftoverCleanup
    {
        private const string MenuPath = DarkMatterGenesisEditorMenus.Maintenance + "Remove Orphaned AudioSource(Clone) Objects";

        static DMSpawnedAudioLeftoverCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode)
                return;

            RemoveLeftovers(logResult: true);
            // Second pass after other EnteredEditMode delayCalls (e.g. live player bind-pose repair).
            EditorApplication.delayCall += () => RemoveLeftovers(logResult: true);
        }

        [MenuItem(MenuPath, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Maintenance_Remove_Orphaned_AudioSource_Clone_Objects)]
        private static void RemoveLeftoversMenu()
        {
            int removed = RemoveLeftovers(logResult: false);
            Debug.Log($"[DMSpawnedAudioLeftoverCleanup] Removed {removed} orphaned '{DMSpawnedAudio.InvectorAudioCloneName}' object(s).");
        }

        public static int RemoveLeftovers(bool logResult)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return 0;

            int removed = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                bool dirtyScene = false;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    if (!DMSpawnedAudio.LooksLikeOrphanedOneShot(root) || PrefabUtility.IsPartOfAnyPrefab(root))
                        continue;

                    // DontSaveInEditor clones were never part of the saved scene; others may have been saved.
                    if ((root.hideFlags & HideFlags.DontSaveInEditor) == 0)
                        dirtyScene = true;

                    Object.DestroyImmediate(root);
                    removed++;
                }

                if (dirtyScene)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            if (logResult && removed > 0)
                Debug.Log($"[DMSpawnedAudioLeftoverCleanup] Removed {removed} orphaned '{DMSpawnedAudio.InvectorAudioCloneName}' object(s) left after Play.");

            return removed;
        }
    }
}
