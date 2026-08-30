using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Saves dirty open scenes every 5 minutes in edit mode, and when leaving Play Mode.
    /// Never writes during compile, play, or a domain/player build.
    /// </summary>
    [InitializeOnLoad]
    public static class DMAutoSceneSave
    {
        private const string PrefsEnabled = "DM.AutoSceneSave.Enabled";
        private const string MenuPath = "Tools/Dark Matter Genesis/Auto Save Scene";
        private const double IntervalSeconds = 5.0 * 60.0;

        private static double _nextSaveAt;
        private static bool _pending;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefsEnabled, true);
            set => EditorPrefs.SetBool(PrefsEnabled, value);
        }

        static DMAutoSceneSave()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += OnUpdate;
            _nextSaveAt = EditorApplication.timeSinceStartup + IntervalSeconds;
        }

        [MenuItem(MenuPath)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!Enabled)
                return;

            // Edit-mode scene only. Saving while still in Play would bake runtime changes.
            if (state == PlayModeStateChange.ExitingEditMode
                || state == PlayModeStateChange.EnteredEditMode)
                RequestSave();
        }

        private static void OnUpdate()
        {
            if (!Enabled)
                return;

            if (EditorApplication.timeSinceStartup >= _nextSaveAt)
            {
                _nextSaveAt = EditorApplication.timeSinceStartup + IntervalSeconds;
                RequestSave();
            }

            if (_pending)
                TrySave();
        }

        private static void RequestSave()
        {
            _pending = true;
            TrySave();
        }

        private static bool IsBlocked()
        {
            return EditorApplication.isCompiling
                || EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isUpdating
                || BuildPipeline.isBuildingPlayer;
        }

        private static void TrySave()
        {
            if (!_pending || IsBlocked())
                return;

            bool dirty = false;
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    dirty = true;
                    break;
                }
            }

            if (!dirty)
            {
                _pending = false;
                return;
            }

            if (!EditorSceneManager.SaveOpenScenes())
                return;

            _pending = false;
            _nextSaveAt = EditorApplication.timeSinceStartup + IntervalSeconds;
        }
    }
}
