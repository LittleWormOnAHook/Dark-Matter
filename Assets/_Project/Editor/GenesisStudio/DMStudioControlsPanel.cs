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

            EditorGUILayout.HelpBox(
                "Finisher (2026-10-26l) - Player/Finisher action (rebind in the Input Actions asset above).\n" +
                "KBM: hold Use (E) + Attack (LMB).\n" +
                "Gamepad: hold Use (West/X) + Attack (RT).\n" +
                "Plain Attack on a low-HP enemy is a normal sped-up swing; Use alone still interacts when not in the combo.",
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "Weapon swap / DM Ranged forms (2026-10-28d)\n" +
                "KBM: Tab (or Shift+Tab) tap = next hotbar weapon (melee, mining tool, DM Ranged...), hold 0.5 s = arm focused. 1 = DM Ranged form step.\n" +
                "Gamepad: D-pad Down tap = next hotbar weapon, hold 0.5 s = arm (not in Building mode, where D-pad Down cancels move). Y tap = DM Ranged form step while DM Ranged is drawn (no swap). Hold Y 0.5 s = arm focused weapon.",
                MessageType.Info);

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

            DrawHolsterAll();
            DrawGamepadLook();

            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        // 2026-10-28p: hold Tab / Y = holster all (threshold on DM_CombatCoreProfile).
        private static readonly (string name, float value)[] HolsterPresets = { ("Snappy", 0.3f), ("Default", 0.5f), ("Relaxed", 0.8f), ("Deliberate", 1.2f), ("Very Long", 2f) };

        private static void DrawHolsterAll()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Holster all (2026-10-28p)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "HOLD Tab (KBM) / HOLD Y (gamepad) with any weapon drawn = holster ALL (MorphBlade keeps its blade, DM Ranged morphs to Pistol 1 and remembers its form, tools).\n" +
                "Nothing drawn: hold arms the focused weapon (as before). Tap still cycles focus (Y tap = DM Ranged form step while drawn). D-pad Down hold always arms.\n" +
                "Input action: Player/HolsterAll (Tab, Y) in InputSystem_Actions; threshold below is the authority.", MessageType.None);
            Project.Combat.DM_CombatCoreProfile prof = Project.Combat.DM_CombatCoreProfile.Live;
            if (prof == null)
            {
                EditorGUILayout.HelpBox("DM_CombatCoreProfile not found in Resources.", MessageType.Warning);
                return;
            }
            EditorGUI.BeginChangeCheck();
            bool on = EditorGUILayout.Toggle(new GUIContent("Hold = holster all", "Off = hold Tab/Y always arms (pre-28p)."), prof.holdHolstersAllWhenDrawn);
            float sec = EditorGUILayout.Slider(new GUIContent("Hold threshold (s)", "Tab / Y hold time (0.15-3 s). Also the arm-on-hold time."), prof.weaponHolsterAllHoldSeconds, 0.15f, 3f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(prof, "Holster all");
                prof.holdHolstersAllWhenDrawn = on;
                prof.weaponHolsterAllHoldSeconds = sec;
                EditorUtility.SetDirty(prof);
            }
            EditorGUILayout.BeginHorizontal();
            foreach (var p in HolsterPresets)
                if (GUILayout.Button(p.name + " " + p.value.ToString("0.0#") + "s", EditorStyles.miniButton))
                {
                    Undo.RecordObject(prof, "Holster all preset");
                    prof.weaponHolsterAllHoldSeconds = p.value;
                    EditorUtility.SetDirty(prof);
                }
            EditorGUILayout.EndHorizontal();
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
