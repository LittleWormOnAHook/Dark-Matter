using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// One-click Play Mode Saver window. Captures live edits and applies them to scenes/assets on Play Mode exit.
    /// </summary>
    public class PlayModeSaverWindow : EditorWindow
    {
        private PlayModeEditPersistence.PlayModeSaveScope saveScope =
            PlayModeEditPersistence.PlayModeSaveScope.AllOpenScenes;

        [MenuItem(SurvivalPioneerEditorMenus.PlayModeSaverWindow, false, 1)]
        public static void Open()
        {
            PlayModeSaverWindow window = GetWindow<PlayModeSaverWindow>("Play Mode Saver");
            window.minSize = new Vector2(360f, 280f);
            window.Show();
        }

        [MenuItem(SurvivalPioneerEditorMenus.PlayModeSaverSaveNow, false, 2)]
        public static void SaveNowFromMenu()
        {
            PlayModeEditPersistence.SaveNow();
        }

        [MenuItem(SurvivalPioneerEditorMenus.PlayModeSaverSaveNow, true)]
        public static bool SaveNowFromMenuValidate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(SurvivalPioneerEditorMenus.PlayModeSaverSaveAndExit, false, 3)]
        public static void SaveAndExitFromMenu()
        {
            PlayModeEditPersistence.SaveAndExitPlayMode();
        }

        [MenuItem(SurvivalPioneerEditorMenus.PlayModeSaverSaveAndExit, true)]
        public static bool SaveAndExitFromMenuValidate()
        {
            return EditorApplication.isPlaying;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Play Mode Saver", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Save while playing: scenes, tags, runtime UI, and changed Assets/_Project data. " +
                "Player skeleton, body snaps, and weapon slot transforms are never saved.",
                MessageType.Info);

            EditorGUILayout.Space(6f);

            if (!PlayModeEditPersistence.Enabled)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode Saver is disabled. Auto-capture on exit is off and pending snapshots were cleared. " +
                    "Enable below or use Tools → Dark Matter Genesis → Maintenance → Persist Play Mode Edits.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                PlayModeEditPersistence.Enabled = EditorGUILayout.ToggleLeft(
                    "Auto-capture on Play Mode exit",
                    PlayModeEditPersistence.Enabled);
            }

            EditorGUILayout.Space(4f);
            saveScope = (PlayModeEditPersistence.PlayModeSaveScope)EditorGUILayout.EnumPopup("Save Scope", saveScope);

            if (saveScope != PlayModeEditPersistence.PlayModeSaveScope.AllOpenScenes)
            {
                EditorGUILayout.HelpBox(
                    "Selection scope uses the current Hierarchy or Project selection.",
                    MessageType.None);
            }

            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Save Now", GUILayout.Height(36f)))
                    PlayModeEditPersistence.SaveNow(saveScope);

                EditorGUILayout.Space(4f);

                if (GUILayout.Button("Save And Exit Play Mode", GUILayout.Height(28f)))
                    PlayModeEditPersistence.SaveAndExitPlayMode(saveScope);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to capture edits.", MessageType.Warning);
            }

            EditorGUILayout.Space(8f);
            DrawStatusBlock();
        }

        private void DrawStatusBlock()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            if (PlayModeEditPersistence.HasPendingSnapshot)
            {
                EditorGUILayout.HelpBox(
                    "Pending snapshot waiting to apply when Play Mode exits.",
                    MessageType.Warning);
            }

            PlayModeEditPersistence.PlayModeSaveSummary summary = PlayModeEditPersistence.LastSaveSummary;
            if (summary.objectCount > 0 || summary.scriptableObjectCount > 0)
            {
                EditorGUILayout.LabelField("Last Capture", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Objects: {summary.objectCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Scenes: {summary.sceneCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Data/Material/Text Assets: {PlayModeEditPersistence.LastProjectAssetCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Prefabs: {PlayModeEditPersistence.LastPrefabAssetCount}", EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(summary.capturedUtc))
                    EditorGUILayout.LabelField($"UTC: {summary.capturedUtc}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No captures yet this session.", EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField("Shortcut: Ctrl+Shift+S (Save Now)", EditorStyles.miniLabel);
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }
    }
}
