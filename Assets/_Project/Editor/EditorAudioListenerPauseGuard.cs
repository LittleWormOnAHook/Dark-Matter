using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Clears <see cref="AudioListener.pause"/> when leaving Play Mode.
    /// Must NOT run on domain reload / SubsystemRegistration — touching AudioListener during
    /// script recompile causes native access violations (Unity.dll c0000005) in this project.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorAudioListenerPauseGuard
    {
        static EditorAudioListenerPauseGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode &&
                state != PlayModeStateChange.EnteredEditMode)
                return;

            // Defer past the play-mode teardown / domain tear-down window.
            EditorApplication.delayCall += ClearPauseDeferred;
        }

        private static void ClearPauseDeferred()
        {
            // Skip during compile / domain-reload windows — AudioListener native calls AV here.
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            if (AudioListener.pause)
                AudioListener.pause = false;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Clear AudioListener Pause", false, 21)]
        private static void ClearPauseMenu()
        {
            AudioListener.pause = false;
            Debug.Log("[EditorAudioListenerPauseGuard] AudioListener.pause cleared.");
        }
    }
}
