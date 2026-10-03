using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Theme
{
    /// <summary>
    /// IMGUI bridge: Genesis colours and fonts for windows still drawn with OnGUI, until they are converted to UI Toolkit.
    /// Usage: GenesisStyles.WindowBackground(position); GenesisStyles.Section("Dodge"); if (GUILayout.Button("Apply", GenesisStyles.PrimaryButton)) ...
    /// </summary>
    public static class GenesisStyles
    {
        static GUIStyle s_Header, s_Section, s_Button, s_Primary, s_Danger, s_Panel, s_Dim;
        static Texture2D s_TexBg2, s_TexBg3, s_TexPrimary, s_TexPrimaryHover, s_TexAccentDark, s_TexClear, s_TexPanel;

        public static bool Active => GenesisTheme.Enabled;

        public static GUIStyle Header { get { Ensure(); return s_Header; } }
        public static GUIStyle SectionStyle { get { Ensure(); return s_Section; } }
        public static GUIStyle Button { get { Ensure(); return s_Button; } }
        public static GUIStyle PrimaryButton { get { Ensure(); return s_Primary; } }
        public static GUIStyle DangerButton { get { Ensure(); return s_Danger; } }
        public static GUIStyle Panel { get { Ensure(); return s_Panel; } }
        public static GUIStyle Dim { get { Ensure(); return s_Dim; } }

        /// <summary>Fills the window with the Genesis background. Call first in OnGUI.</summary>
        public static void WindowBackground(Rect windowPosition)
        {
            if (!Active || Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(0, 0, windowPosition.width, windowPosition.height), GenesisTheme.Bg1);
        }

        /// <summary>Uppercase section header with an accent strip and a 1px rule.</summary>
        public static void Section(string title)
        {
            if (!Active) { EditorGUILayout.LabelField(title, EditorStyles.boldLabel); return; }
            GUILayout.Space(6);
            var r = GUILayoutUtility.GetRect(new GUIContent(title), SectionStyle);
            if (Event.current.type == EventType.Repaint)
            {
                SectionStyle.Draw(r, new GUIContent(title.ToUpperInvariant()), false, false, false, false);
                EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), GenesisTheme.Line);
                EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, 36, 2), GenesisTheme.Accent);
            }
            GUILayout.Space(4);
        }

        public static void BeginPanel() => EditorGUILayout.BeginVertical(Active ? Panel : EditorStyles.helpBox);
        public static void EndPanel() => EditorGUILayout.EndVertical();

        static void Ensure()
        {
            if (s_Header != null && s_TexBg3 != null) return;

            s_TexBg2 = Tex(GenesisTheme.Bg2);
            s_TexBg3 = Tex(GenesisTheme.Bg3);
            s_TexPrimary = Tex(GenesisTheme.Primary);
            s_TexPrimaryHover = Tex(GenesisTheme.Hex("#ffd36e"));
            s_TexAccentDark = Tex(GenesisTheme.AccentDark);
            s_TexClear = Tex(new Color(0, 0, 0, 0));
            s_TexPanel = Tex(GenesisTheme.Bg2);
            var font = GenesisTheme.HeaderFont;

            s_Header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            s_Header.normal.textColor = GenesisTheme.Text;
            if (font != null) s_Header.font = font;

            s_Section = new GUIStyle(EditorStyles.label) { fontSize = 11, fixedHeight = 22, alignment = TextAnchor.MiddleLeft };
            s_Section.normal.textColor = GenesisTheme.Text;
            if (font != null) s_Section.font = font;

            s_Button = new GUIStyle(GUI.skin.button) { fontSize = 11, fixedHeight = 24, padding = new RectOffset(12, 12, 4, 4) };
            if (font != null) s_Button.font = font;
            SetState(s_Button.normal, s_TexBg3, GenesisTheme.Text);
            SetState(s_Button.hover, s_TexBg3, GenesisTheme.Accent);
            SetState(s_Button.active, s_TexAccentDark, Color.white);

            s_Primary = new GUIStyle(s_Button);
            SetState(s_Primary.normal, s_TexPrimary, GenesisTheme.PrimaryText);
            SetState(s_Primary.hover, s_TexPrimaryHover, GenesisTheme.PrimaryText);
            SetState(s_Primary.active, s_TexAccentDark, Color.white);

            s_Danger = new GUIStyle(s_Button);
            SetState(s_Danger.normal, s_TexClear, GenesisTheme.Bad);
            SetState(s_Danger.hover, s_TexBg3, GenesisTheme.Bad);
            SetState(s_Danger.active, s_TexBg3, Color.white);

            s_Panel = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(4, 4, 4, 4) };
            s_Panel.normal.background = s_TexPanel;

            s_Dim = new GUIStyle(EditorStyles.label);
            s_Dim.normal.textColor = GenesisTheme.Dim;
        }

        static void SetState(GUIStyleState st, Texture2D bg, Color text)
        {
            st.background = bg;
            st.scaledBackgrounds = new Texture2D[0];
            st.textColor = text;
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
