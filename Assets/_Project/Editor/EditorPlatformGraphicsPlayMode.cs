#if UNITY_EDITOR
using Project.Core;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Editor play mode uses the PC quality tier; restore the edit-mode tier on exit.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlatformGraphicsPlayMode
    {
        private static int _savedEditModeQuality = -1;

        static EditorPlatformGraphicsPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    _savedEditModeQuality = QualitySettings.GetQualityLevel();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    PlatformGraphicsBootstrap.ForceEditorPlayModePcProfile();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    if (_savedEditModeQuality < 0)
                        break;

                    int restoreLevel = Mathf.Clamp(
                        _savedEditModeQuality,
                        0,
                        Mathf.Max(0, QualitySettings.names.Length - 1));
                    QualitySettings.SetQualityLevel(restoreLevel, applyExpensiveChanges: true);
                    _savedEditModeQuality = -1;
                    break;
            }
        }
    }
}
#endif
