#if UNITY_EDITOR
using System.Collections.Generic;
using Project.EditorTools.Theme;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Editor chrome for Genesis Studio and the other Genesis tool windows.
    /// When the Genesis Theme is on (Tools > Dark Matter Genesis > Theme) it uses the "Frontier" palette and
    /// Chakra Petch headers; when it is off it falls back to the original Dark Matter palette.
    /// </summary>
    public static class DMStudioStyles
    {
        private static GUIStyle headerPanel;
        private static GUIStyle sidebarPanel;
        private static GUIStyle contentPanel;
        private static GUIStyle footerPanel;
        private static GUIStyle categoryTabActive;
        private static GUIStyle categoryTabInactive;
        private static GUIStyle subTabActive;
        private static GUIStyle subTabInactive;
        private static GUIStyle heroTitle;
        private static GUIStyle heroSubtitle;
        private static GUIStyle sectionTitle;
        private static GUIStyle listButton;
        private static GUIStyle listButtonSelected;
        private static GUIStyle badge;
        private static Texture2D headerTex;
        private static Texture2D sidebarTex;
        private static Texture2D contentTex;
        private static Texture2D footerTex;
        private static Texture2D tabActiveTex;
        private static Texture2D tabInactiveTex;
        private static Texture2D subTabActiveTex;
        private static Texture2D subTabInactiveTex;
        private static Texture2D listBtnTex;
        private static Texture2D listBtnSelectedTex;
        private static Texture2D accentLineTex;
        private static bool? builtThemed;
        private static readonly Dictionary<Color, Texture2D> themedSolids = new Dictionary<Color, Texture2D>();

        /// <summary>True when the Genesis "Frontier" theme is active.</summary>
        public static bool Themed => GenesisTheme.Enabled;

        public static GUIStyle HeaderPanel { get { Sync(); return headerPanel ??= Themed ? ThemedPanel(GenesisTheme.Bg2, 10, 10) : CreatePanel(ref headerTex, DarkMatterGenesisUiPalette.DarkNavy, 10, 12); } }
        public static GUIStyle SidebarPanel { get { Sync(); return sidebarPanel ??= Themed ? ThemedPanel(GenesisTheme.Bg1, 8, 8) : CreatePanel(ref sidebarTex, DarkMatterGenesisUiPalette.CharcoalGray, 8, 8); } }
        public static GUIStyle ContentPanel { get { Sync(); return contentPanel ??= Themed ? ThemedPanel(GenesisTheme.Bg2, 10, 10) : CreatePanel(ref contentTex, DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.97f), 10, 10); } }
        public static GUIStyle FooterPanel { get { Sync(); return footerPanel ??= Themed ? ThemedPanel(GenesisTheme.Bg1, 6, 6) : CreatePanel(ref footerTex, DarkMatterGenesisUiPalette.CharcoalGray, 6, 6); } }

        public static GUIStyle HeroTitle
        {
            get
            {
                Sync();
                if (heroTitle != null) return heroTitle;
                heroTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = Themed ? 14 : 15,
                    normal = { textColor = Themed ? GenesisTheme.Text : DarkMatterGenesisUiPalette.WarmOffWhite },
                    margin = new RectOffset(0, 0, 0, 2)
                };
                ApplyHeaderFont(heroTitle);
                return heroTitle;
            }
        }

        public static GUIStyle HeroSubtitle
        {
            get
            {
                Sync();
                return heroSubtitle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Themed ? GenesisTheme.Dim : DarkMatterGenesisUiPalette.SoftBeigeGray },
                    wordWrap = true
                };
            }
        }

        public static GUIStyle SectionTitle
        {
            get
            {
                Sync();
                if (sectionTitle != null) return sectionTitle;
                sectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Themed ? GenesisTheme.Text : DarkMatterGenesisUiPalette.WarmOffWhite }
                };
                if (Themed)
                {
                    sectionTitle.fontSize = 11;
                    sectionTitle.fontStyle = FontStyle.Normal;
                    sectionTitle.alignment = TextAnchor.MiddleLeft;
                }
                ApplyHeaderFont(sectionTitle);
                return sectionTitle;
            }
        }

        public static GUIStyle CategoryTabActive { get { Sync(); return categoryTabActive ??= Themed ? ThemedTab(true, 12) : CreateTab(ref tabActiveTex, DarkMatterGenesisUiPalette.RichFuchsia, true, 13); } }
        public static GUIStyle CategoryTabInactive { get { Sync(); return categoryTabInactive ??= Themed ? ThemedTab(false, 12) : CreateTab(ref tabInactiveTex, DarkMatterGenesisUiPalette.SlateGray, false, 13); } }
        public static GUIStyle SubTabActive { get { Sync(); return subTabActive ??= Themed ? ThemedTab(true, 11) : CreateTab(ref subTabActiveTex, DarkMatterGenesisUiPalette.DeepMagenta, true, 11); } }
        public static GUIStyle SubTabInactive { get { Sync(); return subTabInactive ??= Themed ? ThemedTab(false, 11) : CreateTab(ref subTabInactiveTex, DarkMatterGenesisUiPalette.CharcoalGray, false, 11); } }

        public static GUIStyle ListButton { get { Sync(); return listButton ??= Themed ? ThemedListButton(false) : CreateListButton(ref listBtnTex, false); } }
        public static GUIStyle ListButtonSelected { get { Sync(); return listButtonSelected ??= Themed ? ThemedListButton(true) : CreateListButton(ref listBtnSelectedTex, true); } }

        public static GUIStyle Badge
        {
            get
            {
                Sync();
                if (badge != null) return badge;
                badge = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(6, 6, 2, 2),
                    normal = { textColor = Themed ? GenesisTheme.Text : DarkMatterGenesisUiPalette.WarmOffWhite }
                };
                if (Themed) { badge.fontSize = 9; ApplyHeaderFont(badge); }
                return badge;
            }
        }

        public static void DrawAccentLine(Rect rect, Color color, float height = 2f)
        {
            if (Themed)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - height, rect.width, height), GenesisTheme.Accent);
                return;
            }

            if (accentLineTex == null)
            {
                accentLineTex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            accentLineTex.SetPixel(0, 0, color);
            accentLineTex.Apply();
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - height, rect.width, height), accentLineTex);
        }

        public static void DrawSection(string title, GUIStyle panelStyle, System.Action drawContent, Color? accent = null)
        {
            EditorGUILayout.BeginVertical(panelStyle);
            if (!string.IsNullOrEmpty(title))
            {
                Rect r = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
                if (Themed)
                {
                    EditorGUI.LabelField(r, title.ToUpperInvariant(), SectionTitle);
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), GenesisTheme.Line);
                        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2f, 36f, 2f), GenesisTheme.Accent);
                    }
                    GUILayout.Space(4f);
                }
                else
                {
                    EditorGUI.LabelField(r, title, SectionTitle);
                    DrawAccentLine(r, accent ?? DarkMatterGenesisUiPalette.RichFuchsia, 1.5f);
                }
            }

            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static bool DrawCategoryTab(string label, bool selected, Color accent, string icon = null)
        {
            string text = string.IsNullOrEmpty(icon) || Themed ? label : icon + "  " + label;
            if (Themed) text = text.ToUpperInvariant();
            GUIStyle style = selected ? CategoryTabActive : CategoryTabInactive;
            bool next = GUILayout.Toggle(selected, text, style, GUILayout.MinHeight(30f), GUILayout.MinWidth(88f));
            if (selected)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                DrawAccentLine(r, accent, 2f);
            }

            return next;
        }

        public static bool DrawSubTab(string label, bool selected)
        {
            GUIStyle style = selected ? SubTabActive : SubTabInactive;
            bool next = GUILayout.Toggle(selected, Themed ? label.ToUpperInvariant() : label, style, GUILayout.MinHeight(24f), GUILayout.MinWidth(64f));
            if (selected && Themed)
                DrawAccentLine(GUILayoutUtility.GetLastRect(), GenesisTheme.Accent, 2f);
            return next;
        }

        public static void DrawBadge(string text, Color background, float minWidth = 0f)
        {
            Rect r = minWidth > 0f
                ? GUILayoutUtility.GetRect(minWidth, 18f, GUILayout.Width(minWidth))
                : GUILayoutUtility.GetRect(GUIContent.none, Badge, GUILayout.MinWidth(text.Length * 7f + 12f));

            if (Themed)
            {
                // Outline pill: dark well, 1px edge and text in the badge colour.
                Color c = background;
                c.a = 1f;
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(r, GenesisTheme.Bg0);
                    EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
                    EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
                    EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
                    EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
                }
                Color oldText = Badge.normal.textColor;
                Badge.normal.textColor = c;
                EditorGUI.LabelField(r, text.ToUpperInvariant(), Badge);
                Badge.normal.textColor = oldText;
                return;
            }

            Color old = GUI.color;
            GUI.color = background;
            GUI.DrawTexture(r, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = old;
            EditorGUI.LabelField(r, text, Badge);
        }

        public static void DrawHeroHeader(string title, string subtitle, System.Action drawRight = null)
        {
            EditorGUILayout.BeginVertical(HeaderPanel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(Themed ? title.ToUpperInvariant() : title, HeroTitle);
            if (!string.IsNullOrEmpty(subtitle))
                EditorGUILayout.LabelField(subtitle, HeroSubtitle);
            EditorGUILayout.EndVertical();

            if (drawRight != null)
            {
                GUILayout.FlexibleSpace();
                drawRight();
            }

            EditorGUILayout.EndHorizontal();

            Rect headerRect = GUILayoutUtility.GetLastRect();
            DrawAccentLine(new Rect(headerRect.x - 10f, headerRect.y - 4f, headerRect.width + 20f, headerRect.height + 8f),
                DarkMatterGenesisUiPalette.Gold, 1f);
            EditorGUILayout.EndVertical();
        }

        // ---------- Theme switching ----------

        private static void Sync()
        {
            bool themed = Themed;
            if (builtThemed == themed)
                return;

            builtThemed = themed;
            headerPanel = sidebarPanel = contentPanel = footerPanel = null;
            categoryTabActive = categoryTabInactive = subTabActive = subTabInactive = null;
            heroTitle = heroSubtitle = sectionTitle = listButton = listButtonSelected = badge = null;
        }

        private static void ApplyHeaderFont(GUIStyle style)
        {
            if (!Themed) return;
            Font f = GenesisTheme.HeaderFont;
            if (f != null) style.font = f;
        }

        private static GUIStyle ThemedPanel(Color color, int padH, int padV)
        {
            GUIStyle style = new GUIStyle
            {
                padding = new RectOffset(padH, padH, padV, padV),
                margin = new RectOffset(4, 4, 4, 4)
            };
            style.normal.background = Solid(color);
            return style;
        }

        private static GUIStyle ThemedTab(bool active, int fontSize)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 4, 4),
                margin = new RectOffset(0, 2, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };
            ApplyHeaderFont(style);
            SetState(style.normal, Solid(active ? GenesisTheme.Bg3 : GenesisTheme.Bg2), active ? GenesisTheme.Text : GenesisTheme.Dim);
            SetState(style.hover, Solid(GenesisTheme.Bg3), active ? GenesisTheme.Text : GenesisTheme.Accent);
            SetState(style.active, Solid(GenesisTheme.AccentDark), Color.white);
            CopyState(style.normal, style.onNormal);
            CopyState(style.hover, style.onHover);
            CopyState(style.active, style.onActive);
            return style;
        }

        private static GUIStyle ThemedListButton(bool selected)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 6, 4, 4),
                margin = new RectOffset(0, 0, 1, 1),
                border = new RectOffset(0, 0, 0, 0),
                fontSize = 11,
                fontStyle = selected ? FontStyle.Bold : FontStyle.Normal
            };
            Color selectedFill = Color.Lerp(GenesisTheme.Bg2, GenesisTheme.Accent, 0.2f);
            SetState(style.normal, Solid(selected ? selectedFill : GenesisTheme.Bg2), selected ? GenesisTheme.Text : GenesisTheme.Dim);
            SetState(style.hover, Solid(selected ? selectedFill : GenesisTheme.Bg3), GenesisTheme.Text);
            SetState(style.active, Solid(GenesisTheme.AccentDark), Color.white);
            CopyState(style.normal, style.onNormal);
            CopyState(style.hover, style.onHover);
            CopyState(style.active, style.onActive);
            return style;
        }

        private static void SetState(GUIStyleState state, Texture2D bg, Color text)
        {
            state.background = bg;
            state.scaledBackgrounds = new Texture2D[0];
            state.textColor = text;
        }

        private static void CopyState(GUIStyleState from, GUIStyleState to)
        {
            to.background = from.background;
            to.scaledBackgrounds = new Texture2D[0];
            to.textColor = from.textColor;
        }

        private static Texture2D Solid(Color color)
        {
            if (themedSolids.TryGetValue(color, out Texture2D tex) && tex != null)
                return tex;

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            themedSolids[color] = tex;
            return tex;
        }

        // ---------- Original Dark Matter palette (theme off) ----------

        private static GUIStyle CreatePanel(ref Texture2D tex, Color color, int padH, int padV)
        {
            GUIStyle style = new GUIStyle
            {
                padding = new RectOffset(padH, padH, padV, padV),
                margin = new RectOffset(4, 4, 4, 4)
            };
            style.normal.background = GetSolid(ref tex, color);
            return style;
        }

        private static GUIStyle CreateTab(ref Texture2D tex, Color fill, bool active, int fontSize)
        {
            Color bg = active
                ? DarkMatterGenesisUiPalette.WithAlpha(fill, 0.92f)
                : DarkMatterGenesisUiPalette.WithAlpha(fill, 0.55f);

            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 4, 4),
                margin = new RectOffset(2, 2, 0, 0),
                border = new RectOffset(4, 4, 4, 4)
            };
            style.normal.textColor = active
                ? DarkMatterGenesisUiPalette.WarmOffWhite
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SoftBeigeGray, 0.95f);
            style.normal.background = GetSolid(ref tex, bg);
            style.onNormal = style.normal;
            style.hover.background = GetSolid(ref tex, DarkMatterGenesisUiPalette.WithAlpha(fill, active ? 1f : 0.72f));
            style.onHover = style.hover;
            style.active.background = style.hover.background;
            style.onActive = style.active;
            return style;
        }

        private static GUIStyle CreateListButton(ref Texture2D tex, bool selected)
        {
            Color bg = selected
                ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.35f)
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.45f);

            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 6, 4, 4),
                margin = new RectOffset(0, 0, 1, 1),
                fontSize = 11
            };
            style.normal.textColor = selected
                ? DarkMatterGenesisUiPalette.WarmOffWhite
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SoftBeigeGray, 0.98f);
            style.normal.background = GetSolid(ref tex, bg);
            style.onNormal = style.normal;
            style.hover.background = GetSolid(ref tex, DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DeepMagenta, 0.42f));
            style.onHover = style.hover;
            style.active.background = style.hover.background;
            style.onActive = style.active;
            style.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            return style;
        }

        public static float MeasureInspectorLabelWidth(SerializedObject serialized, System.Func<string, bool> include)
        {
            float widest = 180f;
            if (serialized == null || serialized.targetObject == null)
                return widest;

            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                    continue;
                if (include != null && !include(iterator.propertyPath))
                    continue;

                string label = ObjectNames.NicifyVariableName(iterator.displayName);
                float width = EditorStyles.label.CalcSize(new GUIContent(label)).x;
                if (width > widest)
                    widest = width;
            }

            return Mathf.Clamp(widest + 24f, 200f, 420f);
        }

        public static System.IDisposable PushLabelWidth(float width)
        {
            return new LabelWidthScope(width);
        }

        private sealed class LabelWidthScope : System.IDisposable
        {
            private readonly float previous;

            public LabelWidthScope(float width)
            {
                previous = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = previous;
            }
        }

        private static Texture2D GetSolid(ref Texture2D tex, Color color)
        {
            if (tex == null)
            {
                tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
#endif
