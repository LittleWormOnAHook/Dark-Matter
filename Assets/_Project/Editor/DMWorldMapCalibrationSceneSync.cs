#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Project.Map;

namespace Project.EditorTools
{
    /// <summary>
    /// Keeps DMWorldMapCalibrationProfile Play tweaks when you stop Play.
    /// Unity reverts ScriptableObjects before ExitingPlayMode, so this snapshots
    /// the live asset whenever it changes in Play, then writes it back in edit mode
    /// and pushes copies onto scene MAP components.
    /// </summary>
    [InitializeOnLoad]
    public static class DMWorldMapCalibrationSceneSync
    {
        private const string Stamp = "DMMapCalibSave";
        private const string ProfileAssetPath = "Assets/_Project/Resources/Map/DMWorldMapCalibrationProfile.asset";

        private static string lastCapturedJson;
        private static bool restoreQueued;

        private static string SnapshotPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/DMWorldMapCalibrationPlay.json"));

        static DMWorldMapCalibrationSceneSync()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                return;

            CaptureLive(writeEvenIfUnchanged: false);
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                lastCapturedJson = null;
                restoreQueued = false;
                return;
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CaptureLive(writeEvenIfUnchanged: true);
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode || restoreQueued)
                return;

            restoreQueued = true;
            EditorApplication.delayCall += RestoreThenSyncScene;
        }

        private static void CaptureLive(bool writeEvenIfUnchanged)
        {
            DMWorldMapCalibrationProfile profile = FindLiveProfile();
            if (profile == null)
                return;

            string json = EditorJsonUtility.ToJson(profile);
            if (string.IsNullOrEmpty(json))
                return;

            if (!writeEvenIfUnchanged && json == lastCapturedJson)
                return;

            lastCapturedJson = json;
            try
            {
                File.WriteAllText(SnapshotPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{Stamp}] could not write Play snapshot: {ex.Message}");
            }
        }

        private static DMWorldMapCalibrationProfile FindLiveProfile()
        {
            WorldMapProvider[] providers = UnityEngine.Object.FindObjectsByType<WorldMapProvider>(FindObjectsInactive.Include);
            for (int i = 0; i < providers.Length; i++)
            {
                DMWorldMapCalibrationProfile assigned = providers[i] != null ? providers[i].CalibrationProfile : null;
                if (assigned != null)
                    return assigned;
            }

            DMWorldMapCalibrationProfile[] loaded = Resources.FindObjectsOfTypeAll<DMWorldMapCalibrationProfile>();
            for (int i = 0; i < loaded.Length; i++)
            {
                DMWorldMapCalibrationProfile candidate = loaded[i];
                if (candidate == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(candidate);
                if (string.Equals(path, ProfileAssetPath, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return AssetDatabase.LoadAssetAtPath<DMWorldMapCalibrationProfile>(ProfileAssetPath);
        }

        private static void RestoreThenSyncScene()
        {
            restoreQueued = false;
            RestoreSnapshotOntoAsset();
            SyncOpenSceneFromProfile();
        }

        private static void RestoreSnapshotOntoAsset()
        {
            string file = SnapshotPath;
            if (!File.Exists(file))
                return;

            string json;
            try
            {
                json = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{Stamp}] could not read Play snapshot: {ex.Message}");
                return;
            }

            if (string.IsNullOrEmpty(json))
                return;

            DMWorldMapCalibrationProfile asset = AssetDatabase.LoadAssetAtPath<DMWorldMapCalibrationProfile>(ProfileAssetPath);
            if (asset == null)
                return;

            string current = EditorJsonUtility.ToJson(asset);
            if (current != json)
            {
                EditorJsonUtility.FromJsonOverwrite(json, asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[{Stamp}] kept world map calibration from Play.");
            }

            try
            {
                File.Delete(file);
            }
            catch
            {
                // ignore
            }
        }

        private static void SyncOpenSceneFromProfile()
        {
            DMWorldMapCalibrationProfile profile = AssetDatabase.LoadAssetAtPath<DMWorldMapCalibrationProfile>(ProfileAssetPath);
            if (profile == null)
                profile = Resources.Load<DMWorldMapCalibrationProfile>(DMWorldMapCalibrationProfile.ResourcesPath);
            if (profile == null)
                return;

            WorldMapProvider[] providers = UnityEngine.Object.FindObjectsByType<WorldMapProvider>(FindObjectsInactive.Include);
            for (int i = 0; i < providers.Length; i++)
                providers[i].ApplyCalibrationToScene(profile);

            WorldMapArtReference[] artRefs = UnityEngine.Object.FindObjectsByType<WorldMapArtReference>(FindObjectsInactive.Include);
            for (int i = 0; i < artRefs.Length; i++)
                artRefs[i].ApplyCalibrationFromProfile(profile);
        }
    }
}
#endif
