using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Ground Fade on cameras WITHOUT TAA (the Scene view, or a game camera with AA off): the screen-space dither only
    /// resolves into a soft fade under TAA; without it a static see-through pattern shows. Global shader float
    /// _GRB_FadeNoTaaMode: 0 = dither (as with TAA), 1 = hard edge at the middle of the band (default in the editor),
    /// 2 = no fade. Cameras with TAA are never affected. Stored per user in EditorPrefs. Builds keep 0 unless a script
    /// sets the global.
    /// </summary>
    [InitializeOnLoad]
    public static class GenesisRockBlendSceneFade
    {
        public const string PrefKey = "GenesisPCG.RockBlend.FadeNoTaaMode";
        public static readonly string[] ModeNames = { "Dither (as with TAA)", "Hard Edge", "Off (full rock)" };
        private const string Menu = "Tools/Genesis PCG Rock Creation/Transparency Fade Without TAA (Scene View)/";
        private static readonly int P_Mode = Shader.PropertyToID("_GRB_FadeNoTaaMode");

        static GenesisRockBlendSceneFade()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static double s_next;

        // Shader globals do not survive every reload / pipeline reset: re-assert about once a second (one float compare).
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < s_next) return;
            s_next = EditorApplication.timeSinceStartup + 1.0;
            if (!Mathf.Approximately(Shader.GetGlobalFloat(P_Mode), Mode)) Apply();
        }

        public static int Mode
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(PrefKey, 1), 0, 2);
            set { EditorPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, 2)); Apply(); }
        }

        public static void Apply()
        {
            Shader.SetGlobalFloat(P_Mode, Mode);
            SceneView.RepaintAll();
        }

        [MenuItem(Menu + "Dither", false, 300)] private static void SetDither() => Mode = 0;
        [MenuItem(Menu + "Hard Edge", false, 301)] private static void SetHard() => Mode = 1;
        [MenuItem(Menu + "Off", false, 302)] private static void SetOff() => Mode = 2;
        [MenuItem(Menu + "Dither", true)] private static bool VDither() { Menu_Check(0); return true; }
        [MenuItem(Menu + "Hard Edge", true)] private static bool VHard() { Menu_Check(1); return true; }
        [MenuItem(Menu + "Off", true)] private static bool VOff() { Menu_Check(2); return true; }

        private static void Menu_Check(int m)
        {
            UnityEditor.Menu.SetChecked(Menu + "Dither", Mode == 0);
            UnityEditor.Menu.SetChecked(Menu + "Hard Edge", Mode == 1);
            UnityEditor.Menu.SetChecked(Menu + "Off", Mode == 2);
        }

        /// <summary>Inline row for the material / rock inspectors (Ground Fade group).</summary>
        public static void DrawInline()
        {
            int m = EditorGUILayout.Popup(new GUIContent("Without TAA (Scene view)",
                "Editor-wide: how the transparency fade draws on cameras without TAA (the Scene view). Cameras with TAA always dither, which TAA resolves into a soft fade."), Mode, ModeNames);
            if (m != Mode) Mode = m;
        }
    }
}
