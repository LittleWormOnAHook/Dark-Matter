using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.AI.Editor
{
    /// <summary>
    /// Clears orphaned EnemyDissolveLiftAnchor / detached weapon clones when leaving Play Mode.
    /// </summary>
    internal static class EnemyDeathPlayModeCleanup
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                EnemyDeathRuntimeCleanup.SweepOrphans(destroyImmediately: true);
            }
        }
    }
}
