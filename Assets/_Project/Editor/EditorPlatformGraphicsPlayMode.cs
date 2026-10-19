#if UNITY_EDITOR
using Project.Core;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Editor play mode applies the player's saved tier (PC tier when none is saved). Runtime quality changes
    /// (tier switch, lodBias / shadow / LOD overrides) stay runtime-only: the edit-mode QualitySettings are
    /// snapshotted when Play starts and put back (and the asset un-dirtied) when Play ends, so
    /// AssetDatabase.SaveAssets after Play no longer rewrites ProjectSettings/QualitySettings.asset.
    /// perf-1018b. SessionState survives the domain reload on entering Play.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlatformGraphicsPlayMode
    {
        private const string LevelKey = "DM.EditorPlayQuality.Level";
        private const string JsonKey = "DM.EditorPlayQuality.Json";
        private const string QualityAssetPath = "ProjectSettings/QualitySettings.asset";

        static EditorPlatformGraphicsPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    SessionState.SetInt(LevelKey, QualitySettings.GetQualityLevel());
                    UnityEngine.Object qualityAsset = LoadQualityAsset();
                    SessionState.SetString(JsonKey, qualityAsset != null ? EditorJsonUtility.ToJson(qualityAsset) : string.Empty);
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    PlatformGraphicsBootstrap.ForceEditorPlayModePcProfile();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    RestoreEditModeQuality(clearSnapshot: false);
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Runs before delayCall savers (DMProfilePlayModeSaver / map calibration) call SaveAssets.
                    RestoreEditModeQuality(clearSnapshot: true);
                    break;
            }
        }

        private static void RestoreEditModeQuality(bool clearSnapshot)
        {
            int saved = SessionState.GetInt(LevelKey, -1);
            if (saved >= 0)
            {
                int restoreLevel = Mathf.Clamp(saved, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                if (!clearSnapshot || QualitySettings.GetQualityLevel() != restoreLevel)
                    QualitySettings.SetQualityLevel(restoreLevel, applyExpensiveChanges: true);
            }

            string json = SessionState.GetString(JsonKey, string.Empty);
            UnityEngine.Object qualityAsset = LoadQualityAsset();
            if (qualityAsset != null && !string.IsNullOrEmpty(json))
            {
                if (EditorJsonUtility.ToJson(qualityAsset) != json)
                    EditorJsonUtility.FromJsonOverwrite(json, qualityAsset);
                EditorUtility.ClearDirty(qualityAsset);
            }

            if (clearSnapshot)
            {
                SessionState.EraseInt(LevelKey);
                SessionState.EraseString(JsonKey);
            }
        }

        private static UnityEngine.Object LoadQualityAsset()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(QualityAssetPath);
            return assets != null && assets.Length > 0 ? assets[0] : null;
        }
    }
}
#endif
