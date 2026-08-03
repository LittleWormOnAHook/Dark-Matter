using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Keeps Project Settings → Time → Time Scale at 1.
    /// Pause menus set <see cref="Time.timeScale"/> to 0 at runtime; if Play Mode exits while paused,
    /// Unity can serialize that 0 into TimeManager.asset and freeze Edit Mode / future Play sessions.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorTimeScaleGuard
    {
        private const string TimeManagerPath = "ProjectSettings/TimeManager.asset";
        private const float DesiredTimeScale = 1f;

        static EditorTimeScaleGuard()
        {
            EditorApplication.delayCall += EnsureProjectTimeScale;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Restore before Play Mode tears down so TimeManager is not saved at 0.
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                EnsureProjectTimeScale();
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Reset Time Scale To 1", false, 20)]
        private static void ResetTimeScaleMenu()
        {
            EnsureProjectTimeScale(forceLog: true);
        }

        private static void EnsureProjectTimeScale()
        {
            EnsureProjectTimeScale(forceLog: false);
        }

        private static void EnsureProjectTimeScale(bool forceLog)
        {
            bool runtimeFixed = false;
            if (!Mathf.Approximately(Time.timeScale, DesiredTimeScale))
            {
                Time.timeScale = DesiredTimeScale;
                runtimeFixed = true;
            }

            bool assetFixed = EnsureTimeManagerAsset();

            if (forceLog || runtimeFixed || assetFixed)
            {
                Debug.Log(
                    $"[EditorTimeScaleGuard] Time Scale set to {DesiredTimeScale}" +
                    (assetFixed ? " (TimeManager.asset updated)." : "."));
            }
        }

        private static bool EnsureTimeManagerAsset()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TimeManagerPath);
            if (assets == null || assets.Length == 0)
                return false;

            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty prop = so.FindProperty("m_TimeScale");
            if (prop == null)
                return false;

            if (Mathf.Approximately(prop.floatValue, DesiredTimeScale))
                return false;

            prop.floatValue = DesiredTimeScale;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
