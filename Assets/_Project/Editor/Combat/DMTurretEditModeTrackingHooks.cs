#if UNITY_EDITOR
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// After Play Mode, Unity restores scene objects from the enter-play snapshot. F3DTurret limit fields
    /// often revert while <see cref="DMTurretRuntime"/> tracking authority stays authoritative — re-push
    /// authority onto FORGE without capturing from F3D.
    /// </summary>
    [InitializeOnLoad]
    public static class DMTurretEditModeTrackingHooks
    {
        static DMTurretEditModeTrackingHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                // Push before teardown; never CaptureTrackingFromForge here.
                ReapplyTrackingAuthorityToForge();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.delayCall += () =>
            {
                ReapplyTrackingAuthorityToForge();
                // Play Mode Saver applies snapshots on the same frame — push authority again after restore.
                EditorApplication.delayCall += ReapplyTrackingAuthorityToForge;
            };
        }

        /// <summary>Called after <see cref="PlayModeEditPersistence"/> applies play-mode scene snapshots.</summary>
        public static void ReapplyAfterPlayModePersistence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ReapplyTrackingAuthorityToForge();
        }

        static void ReapplyTrackingAuthorityToForge()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            DMTurretRuntime[] runtimes = Object.FindObjectsByType<DMTurretRuntime>(FindObjectsInactive.Include);
            if (runtimes == null || runtimes.Length == 0)
                return;

            for (int i = 0; i < runtimes.Length; i++)
            {
                DMTurretRuntime runtime = runtimes[i];
                if (runtime == null)
                    continue;

                runtime.ApplyTrackingToForge();
                if (runtime.ForgeTurret != null)
                    EditorUtility.SetDirty(runtime.ForgeTurret);
            }
        }
    }
}
#endif
