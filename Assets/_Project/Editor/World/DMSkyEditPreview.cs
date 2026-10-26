#if UNITY_EDITOR
// 2026-10-25o: live edit-mode preview of the Io sky (DMIoSkyController + DMIoNightSky) driven by the same profile assets
// Studio edits (DM_IoSkyProfile / DMIoClockProfile). Preview objects are HideAndDontSave, removed before scene save
// and before Play, so the scene is never dirtied and Play simulation is never fought. Edited profiles auto-save.
using Project.World.Sky;
using Project.World.Clock;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.World
{
    [InitializeOnLoad]
    public static class DMSkyEditPreview
    {
        private static GameObject host;
        private static double nextCheck;

        static DMSkyEditPreview()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode) Teardown();
            };
            EditorSceneManager.sceneSaving += (scene, path) => Teardown();
            EditorSceneManager.sceneClosing += (scene, removing) => Teardown();
            AssemblyReloadEvents.beforeAssemblyReload += Teardown;
        }

        private static void Teardown()
        {
            if (host != null) Object.DestroyImmediate(host);
            host = null;
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Teardown(); return; }
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.5;

            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            bool want = p != null && p.skyEnabled && p.editPreviewEnabled;
            // A scene-placed controller previews by itself (ExecuteAlways); only host one when the scene has none.
            if (want && host == null && Object.FindAnyObjectByType<DMIoSkyController>() == null)
            {
                host = new GameObject("DM Io Sky Edit Preview") { hideFlags = HideFlags.HideAndDontSave };
                host.AddComponent<DMIoSkyController>();
                host.AddComponent<DMIoNightSky>();
            }
            else if (!want && host != null)
                Teardown();

            bool changed = false;
            changed |= SaveIfDirty(p);
            changed |= SaveIfDirty(DMIoClockProfile.Live);
            if (host != null || changed)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }

        private static bool SaveIfDirty(Object o)
        {
            if (o == null || !EditorUtility.IsDirty(o)) return false;
            AssetDatabase.SaveAssetIfDirty(o); // Studio edits already Undo.RecordObject + SetDirty
            return true;
        }
    }
}
#endif
