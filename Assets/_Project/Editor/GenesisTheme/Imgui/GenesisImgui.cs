using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Theme
{
    /// <summary>
    /// Frontier look for IMGUI windows and inspectors that are not converted to UI Toolkit yet.
    /// While a scope is open, the shared editor styles (text/number fields, popups, buttons, sliders, help boxes, labels)
    /// draw with Genesis colours; they are put back when the scope closes, so Unity's own windows are untouched.
    /// Usage, first line of OnGUI:  using var genesisTheme = GenesisImgui.Window(this);
    /// Anywhere else:               using (GenesisImgui.Begin()) { ... }
    /// </summary>
    public static class GenesisImgui
    {
        static int s_Depth;
        static readonly Dictionary<GUIStyle, GUIStyle> s_Original = new Dictionary<GUIStyle, GUIStyle>();
        static readonly Dictionary<GUIStyle, GUIStyle> s_Themed = new Dictionary<GUIStyle, GUIStyle>();
        static readonly List<GUIStyle> s_Applied = new List<GUIStyle>();
        static readonly Dictionary<string, Texture2D> s_Tex = new Dictionary<string, Texture2D>();

        public readonly struct Scope : IDisposable
        {
            readonly bool m_Active;

            internal Scope(bool active) { m_Active = active; }

            public void Dispose()
            {
                if (m_Active) Pop();
            }
        }

        /// <summary>Themes an IMGUI window: fills the background and restyles controls until disposed.</summary>
        public static Scope Window(EditorWindow window)
        {
            Scope s = Begin();
            if (s_Depth > 0 && window != null && Event.current != null && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(0f, 0f, window.position.width, window.position.height), GenesisTheme.Bg1);
            return s;
        }

        /// <summary>Restyles IMGUI controls until disposed (no background fill).</summary>
        public static Scope Begin()
        {
            if (!GenesisTheme.Enabled) return new Scope(false);
            try
            {
                Push();
                return new Scope(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GenesisTheme] IMGUI theme skipped: " + e.Message);
                return new Scope(false);
            }
        }

        static void Push()
        {
            s_Depth++;
            if (s_Depth > 1) return;

            s_Applied.Clear();
            Theme(GUI.skin.button, ButtonLook);
            Theme(EditorStyles.miniButton, ButtonLook);
            Theme(EditorStyles.miniButtonLeft, ButtonLook);
            Theme(EditorStyles.miniButtonMid, ButtonLook);
            Theme(EditorStyles.miniButtonRight, ButtonLook);
            Theme(EditorStyles.textField, FieldLook);
            Theme(EditorStyles.numberField, FieldLook);
            Theme(EditorStyles.textArea, FieldLook);
            Theme(EditorStyles.popup, PopupLook);
            Theme(GUI.skin.horizontalSlider, SliderTrackLook);
            Theme(GUI.skin.horizontalSliderThumb, SliderThumbLook);
            Theme(EditorStyles.helpBox, HelpBoxLook);
            Theme(GUI.skin.box, PanelLook);
            Theme(EditorStyles.label, s => TextOnly(s, GenesisTheme.Text));
            Theme(EditorStyles.miniLabel, s => TextOnly(s, GenesisTheme.Dim));
            Theme(EditorStyles.boldLabel, HeaderLabelLook);
            Theme(EditorStyles.foldout, s => TextOnly(s, GenesisTheme.Text));
        }

        static void Pop()
        {
            s_Depth = Mathf.Max(0, s_Depth - 1);
            if (s_Depth > 0) return;

            for (int i = 0; i < s_Applied.Count; i++)
            {
                GUIStyle s = s_Applied[i];
                if (s != null && s_Original.TryGetValue(s, out GUIStyle o)) CopyLook(o, s);
            }
            s_Applied.Clear();
        }

        static void Theme(GUIStyle style, Action<GUIStyle> look)
        {
            if (style == null) return;
            if (!s_Original.TryGetValue(style, out GUIStyle original) || !s_Themed.TryGetValue(style, out GUIStyle themed) || NeedsRebuild(themed))
            {
                original = new GUIStyle(style);
                s_Original[style] = original;
                themed = new GUIStyle(original);
                look(themed);
                s_Themed[style] = themed;
            }
            CopyLook(themed, style);
            s_Applied.Add(style);
        }

        // Textures are HideAndDontSave; after a skin change or texture loss rebuild the themed copy.
        static bool NeedsRebuild(GUIStyle themed) => themed.normal.background == null && themed.name == "__genesis_tex";

        static void CopyLook(GUIStyle from, GUIStyle to)
        {
            to.normal = from.normal;
            to.hover = from.hover;
            to.active = from.active;
            to.focused = from.focused;
            to.onNormal = from.onNormal;
            to.onHover = from.onHover;
            to.onActive = from.onActive;
            to.onFocused = from.onFocused;
            to.border = new RectOffset(from.border.left, from.border.right, from.border.top, from.border.bottom);
            to.font = from.font;
            to.fontStyle = from.fontStyle;
        }

        // ---------------------------------------------------------------- looks

        static void ButtonLook(GUIStyle s)
        {
            s.border = new RectOffset(2, 2, 2, 2);
            Texture2D normal = Frame("btn", GenesisTheme.Bg3, GenesisTheme.Line);
            Texture2D hover = Frame("btn-h", GenesisTheme.Hex("#332e27"), GenesisTheme.Accent);
            Texture2D down = Frame("btn-d", GenesisTheme.AccentDark, GenesisTheme.Accent);
            State(s.normal, normal, GenesisTheme.Text);
            State(s.hover, hover, GenesisTheme.Text);
            State(s.active, down, Color.white);
            State(s.focused, hover, GenesisTheme.Text);
            // "on" = toggled buttons (toolbars, selected tabs)
            Texture2D on = Frame("btn-on", GenesisTheme.Hex("#3a2a1a"), GenesisTheme.Accent);
            State(s.onNormal, on, GenesisTheme.Accent);
            State(s.onHover, on, GenesisTheme.Accent);
            State(s.onActive, down, Color.white);
            State(s.onFocused, on, GenesisTheme.Accent);
        }

        static void FieldLook(GUIStyle s)
        {
            s.border = new RectOffset(2, 2, 2, 2);
            Texture2D normal = Frame("fld", GenesisTheme.Bg0, GenesisTheme.Line);
            Texture2D hover = Frame("fld-h", GenesisTheme.Bg0, GenesisTheme.Hex("#5a5044"));
            Texture2D focus = Frame("fld-f", GenesisTheme.Bg0, GenesisTheme.Accent);
            State(s.normal, normal, GenesisTheme.Text);
            State(s.hover, hover, GenesisTheme.Text);
            State(s.active, focus, GenesisTheme.Text);
            State(s.focused, focus, GenesisTheme.Text);
            State(s.onNormal, normal, GenesisTheme.Text);
            State(s.onHover, hover, GenesisTheme.Text);
            State(s.onActive, focus, GenesisTheme.Text);
            State(s.onFocused, focus, GenesisTheme.Text);
        }

        static void PopupLook(GUIStyle s)
        {
            // 9-slice with the orange arrow baked into the fixed right border, so the arrow never disappears.
            s.border = new RectOffset(2, 18, 8, 8);
            Texture2D normal = PopupTex("pop", GenesisTheme.Line);
            Texture2D hover = PopupTex("pop-h", GenesisTheme.Accent);
            State(s.normal, normal, GenesisTheme.Text);
            State(s.hover, hover, GenesisTheme.Text);
            State(s.active, hover, GenesisTheme.Text);
            State(s.focused, hover, GenesisTheme.Text);
            State(s.onNormal, hover, GenesisTheme.Text);
            State(s.onHover, hover, GenesisTheme.Text);
            State(s.onActive, hover, GenesisTheme.Text);
            State(s.onFocused, hover, GenesisTheme.Text);
        }

        static void SliderTrackLook(GUIStyle s)
        {
            s.border = new RectOffset(2, 2, 2, 2);
            Texture2D t = Frame("trk", GenesisTheme.Bg0, GenesisTheme.Line);
            State(s.normal, t, GenesisTheme.Text);
            State(s.hover, t, GenesisTheme.Text);
            State(s.active, t, GenesisTheme.Text);
            State(s.focused, t, GenesisTheme.Text);
        }

        static void SliderThumbLook(GUIStyle s)
        {
            s.border = new RectOffset(1, 1, 1, 1);
            Texture2D t = Frame("thm", GenesisTheme.Accent, GenesisTheme.AccentDark);
            Texture2D h = Frame("thm-h", GenesisTheme.Primary, GenesisTheme.Accent);
            State(s.normal, t, GenesisTheme.Text);
            State(s.hover, h, GenesisTheme.Text);
            State(s.active, h, GenesisTheme.Text);
            State(s.focused, h, GenesisTheme.Text);
        }

        static void HelpBoxLook(GUIStyle s)
        {
            s.border = new RectOffset(3, 2, 2, 2);
            Texture2D t = LeftStrip("help", GenesisTheme.Bg2, GenesisTheme.Line, GenesisTheme.Accent);
            State(s.normal, t, GenesisTheme.Text);
        }

        static void PanelLook(GUIStyle s)
        {
            s.border = new RectOffset(2, 2, 2, 2);
            State(s.normal, Frame("box", GenesisTheme.Bg2, GenesisTheme.Line), GenesisTheme.Text);
        }

        static void HeaderLabelLook(GUIStyle s)
        {
            TextOnly(s, GenesisTheme.Text);
            Font f = GenesisTheme.HeaderFont;
            if (f != null)
            {
                s.font = f;
                s.fontStyle = FontStyle.Normal;
            }
        }

        static void TextOnly(GUIStyle s, Color c)
        {
            s.normal.textColor = c;
            s.hover.textColor = c;
            s.focused.textColor = GenesisTheme.Accent;
            s.onNormal.textColor = c;
            s.onHover.textColor = c;
            s.onFocused.textColor = GenesisTheme.Accent;
        }

        static void State(GUIStyleState st, Texture2D bg, Color text)
        {
            st.background = bg;
            st.scaledBackgrounds = new Texture2D[0];
            st.textColor = text;
        }

        // ---------------------------------------------------------------- textures

        static Texture2D Frame(string key, Color fill, Color edge)
        {
            if (s_Tex.TryGetValue(key, out Texture2D t) && t != null) return t;
            const int n = 6;
            t = NewTex(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    t.SetPixel(x, y, x == 0 || y == 0 || x == n - 1 || y == n - 1 ? edge : fill);
            t.Apply();
            s_Tex[key] = t;
            return t;
        }

        static Texture2D LeftStrip(string key, Color fill, Color edge, Color strip)
        {
            if (s_Tex.TryGetValue(key, out Texture2D t) && t != null) return t;
            const int n = 8;
            t = NewTex(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c = fill;
                    if (y == 0 || y == n - 1 || x == n - 1) c = edge;
                    if (x <= 1) c = strip;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            s_Tex[key] = t;
            return t;
        }

        static Texture2D PopupTex(string key, Color edge)
        {
            if (s_Tex.TryGetValue(key, out Texture2D t) && t != null) return t;
            const int w = 24, h = 18;
            t = NewTex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, x == 0 || y == 0 || x == w - 1 || y == h - 1 ? edge : GenesisTheme.Bg0);

            // Down-pointing triangle, 7px wide, centred in the right 18px (texture y=0 is the bottom row).
            int cx = w - 9, top = 11;
            for (int row = 0; row < 4; row++)
                for (int dx = -(3 - row); dx <= 3 - row; dx++)
                    t.SetPixel(cx + dx, top - row, GenesisTheme.Accent);
            t.Apply();
            s_Tex[key] = t;
            return t;
        }

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}
