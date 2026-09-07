#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Project.Map;

namespace Project.EditorTools
{
    /// <summary>
    /// After Play, push saved calibration profile values onto scene MAP components for edit-mode inspection.
    /// </summary>
    [InitializeOnLoad]
    public static class DMWorldMapCalibrationSceneSync
    {
        static DMWorldMapCalibrationSceneSync()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.delayCall += SyncOpenSceneFromProfile;
        }

        private static void SyncOpenSceneFromProfile()
        {
            DMWorldMapCalibrationProfile profile = Resources.Load<DMWorldMapCalibrationProfile>(
                DMWorldMapCalibrationProfile.ResourcesPath);
            if (profile == null)
                return;

            WorldMapProvider[] providers = Object.FindObjectsByType<WorldMapProvider>(FindObjectsInactive.Include);
            for (int i = 0; i < providers.Length; i++)
                providers[i].ApplyCalibrationToScene(profile);

            WorldMapArtReference[] artRefs = Object.FindObjectsByType<WorldMapArtReference>(FindObjectsInactive.Include);
            for (int i = 0; i < artRefs.Length; i++)
                artRefs[i].ApplyCalibrationFromProfile(profile);
        }
    }
}
#endif
