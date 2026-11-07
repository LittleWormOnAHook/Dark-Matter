// force-reimport 2026-09-20-lt-aim
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// RETIRED as binding authority. InputSystem_Actions.inputactions + Project Settings > Input System Package own binds.
    /// This tab only pings the asset; it must not Apply/Save/Listen-overwrite bindings.
    /// stamp: controls-panel-retired-stub 0920h
    /// </summary>
    internal sealed class DMStudioControlsPanel
    {
        private const string ActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        private InputActionAsset asset;
        private string status = string.Empty;

        public void Dispose()
        {
            asset = null;
            status = string.Empty;
        }

        public void Draw()
        {
            EnsureAsset();
            if (asset == null)
            {
                EditorGUILayout.HelpBox(
                    "InputSystem_Actions not found at " + ActionsPath,
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "RETIRED — Genesis Studio is not binding authority.\n\n" +
                "Edit binds only in:\n" +
                "• Assets/_Project/Settings/Input/InputSystem_Actions.inputactions\n" +
                "• Edit > Project Settings > Input System Package\n\n" +
                "Do not use Apply/Save/Listen here — wrong KBM rows were confusing the real Input System asset.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Authority asset", GUILayout.Width(100f));
            EditorGUILayout.ObjectField(asset, typeof(InputActionAsset), false);
            if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select / Open", GUILayout.Width(100f)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();

            DrawGamepadLook();

            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        // 2026-10-25b: gamepad look (PlayerPrefs via GameSettings; same values as in-game Settings > Game Options).
        private static readonly (string name, float value)[] PadPresets = { ("Slow", 0.5f), ("Default", 1f), ("Fast", 2f), ("Very Fast", 3.5f), ("Max", 5f) };

        private static void DrawGamepadLook()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Sidestep / backstep (2026-10-25d)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "KBM: double-tap A / D = sidestep, double-tap S = backstep. Dash = hold WASD (diagonals ok) + Left Alt (no direction = forward). No double-tap dash.\n" +
                "Gamepad dash: hold B with the left stick deflected = dash that way; quick tap B = dodge / roll.\n" +
                "Gamepad: left stick left / right (or back) + HOLD A past the hold time = step. Tap A = jump (fires on release while the stick is deflected); jetpack double-press unchanged.\n" +
                "Tune: Combat Core > Player quick step + Directional dash input (hold time, stick deadzone, presets).", MessageType.None);
            EditorGUILayout.LabelField("Gamepad Look", EditorStyles.boldLabel);
            Project.Core.GameSettings.ReloadGamepadLook();
            float s = EditorGUILayout.Slider(new GUIContent("Right Stick Sensitivity", "Multiplier on right-stick look (0.1-5, default 1). Saved in PlayerPrefs."), Project.Core.GameSettings.GamepadLookSensitivity, Project.Core.GameSettings.GamepadLookSensitivityMin, Project.Core.GameSettings.GamepadLookSensitivityMax);
            if (!Mathf.Approximately(s, Project.Core.GameSettings.GamepadLookSensitivity))
                Project.Core.GameSettings.SetGamepadLookSensitivity(s);
            EditorGUILayout.BeginHorizontal();
            foreach (var p in PadPresets)
                if (GUILayout.Button(p.name + " " + p.value.ToString("0.#") + "x", EditorStyles.miniButton))
                    Project.Core.GameSettings.SetGamepadLookSensitivity(p.value);
            EditorGUILayout.EndHorizontal();
            bool inv = EditorGUILayout.Toggle("Invert Look Y (Gamepad)", Project.Core.GameSettings.GamepadInvertY);
            if (inv != Project.Core.GameSettings.GamepadInvertY)
                Project.Core.GameSettings.SetGamepadInvertY(inv);
        }

        private void EnsureAsset()
        {
            if (asset != null)
                return;
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (asset == null)
                status = "Missing InputSystem_Actions at " + ActionsPath;
        }
    }
}
#endif
