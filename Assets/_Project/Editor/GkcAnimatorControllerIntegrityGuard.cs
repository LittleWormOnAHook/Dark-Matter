using System.IO;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Player
{
    /// <summary>
    /// Prevents and repairs corruption of the editable GKC animator copy.
    /// The graph-repair utility previously deleted hundreds of valid GKC sub-assets.
    /// </summary>
    [InitializeOnLoad]
    public static class GkcAnimatorControllerIntegrityGuard
    {
        private const string GuardSessionKey = "GkcAnimatorControllerIntegrityGuard.v1";

        static GkcAnimatorControllerIntegrityGuard()
        {
            EditorApplication.delayCall += TryValidateProjectGkcControllerOnce;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryValidateProjectGkcControllerOnce;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Restore GKC Animator Controller From Source", false, 19)]
        public static void RestoreGkcControllerMenu()
        {
            EditorLayoutGuard.ClearSelectionOnly();
            if (RestoreProjectGkcControllerFromSource(force: true))
            {
                EditorUtility.DisplayDialog(
                    "GKC Animator",
                    "Restored ProjectGKCCharacterController from the pristine GKC source.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "GKC Animator",
                "Could not restore the GKC controller. Check the console for details.",
                "OK");
        }

        private static void TryValidateProjectGkcControllerOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetBool(GuardSessionKey, false))
                return;

            int subAssetCount = CountUnitySubAssets(PlayerAnimatorControllerPaths.GkcControllerPath);
            if (subAssetCount >= PlayerAnimatorControllerPaths.GkcControllerMinSubAssetCount)
            {
                SessionState.SetBool(GuardSessionKey, true);
                return;
            }

            Debug.LogWarning(
                $"GKC animator integrity guard: {PlayerAnimatorControllerPaths.GkcControllerPath} " +
                $"has {subAssetCount} sub-assets (expected >= {PlayerAnimatorControllerPaths.GkcControllerMinSubAssetCount}). " +
                "Restoring from source.");

            if (RestoreProjectGkcControllerFromSource(force: true))
                SessionState.SetBool(GuardSessionKey, true);
        }

        public static bool RestoreProjectGkcControllerFromSource(bool force)
        {
            string sourcePath = PlayerAnimatorControllerPaths.GkcControllerSourcePath;
            string projectPath = PlayerAnimatorControllerPaths.GkcControllerPath;

            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"GkcAnimatorControllerIntegrityGuard: missing source at {sourcePath}");
                return false;
            }

            if (!force)
            {
                int subAssetCount = CountUnitySubAssets(projectPath);
                if (subAssetCount >= PlayerAnimatorControllerPaths.GkcControllerMinSubAssetCount)
                    return false;
            }

            File.Copy(sourcePath, projectPath, overwrite: true);
            AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"GkcAnimatorControllerIntegrityGuard: restored {projectPath} from {sourcePath}.");
            return true;
        }

        private static int CountUnitySubAssets(string assetPath)
        {
            if (!File.Exists(assetPath))
                return 0;

            int count = 0;
            foreach (string line in File.ReadLines(assetPath))
            {
                if (line.StartsWith("--- !u!", System.StringComparison.Ordinal))
                    count++;
            }

            return count;
        }
    }
}
