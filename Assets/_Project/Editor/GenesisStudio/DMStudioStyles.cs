#if UNITY_EDITOR
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Shift-theme editor chrome for Genesis Studio (Dark Matter palette).
    /// </summary>
    internal static class DMStudioStyles
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

        public static GUIStyle HeaderPanel => headerPanel ??= CreatePanel(ref headerTex, DarkMatterGenesisUiPalette.DarkNavy, 10, 12);
        public static GUIStyle SidebarPanel => sidebarPanel ??= CreatePanel(ref sidebarTex, DarkMatterGenesisUiPalette.CharcoalGray, 8, 8);
        public static GUIStyle ContentPanel => contentPanel ??= CreatePanel(ref contentTex, DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.97f), 10, 10);
        public static GUIStyle FooterPanel => footerPanel ??= CreatePanel(ref footerTex, DarkMatterGenesisUiPalette.CharcoalGray, 6, 6);

        public static GUIStyle HeroTitle => heroTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            normal = { textColor = DarkMatterGenesisUiPalette.WarmOffWhite },
            margin = new RectOffset(0, 0, 0, 2)
        };

        public static GUIStyle HeroSubtitle => heroSubtitle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = DarkMatterGenesisUiPalette.SoftBeigeGray },
            wordWrap = true
        };

        public static GUIStyle SectionTitle => sectionTitle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = DarkMatterGenesisUiPalette.WarmOffWhite }
        };

        public static GUIStyle CategoryTabActive => categoryTabActive ??= CreateTab(ref tabActiveTex, DarkMatterGenesisUiPalette.RichFuchsia, true, 13);
        public static GUIStyle CategoryTabInactive => categoryTabInactive ??= CreateTab(ref tabInactiveTex, DarkMatterGenesisUiPalette.SlateGray, false, 13);
        public static GUIStyle SubTabActive => subTabActive ??= CreateTab(ref subTabActiveTex, DarkMatterGenesisUiPalette.DeepMagenta, true, 11);
        public static GUIStyle SubTabInactive => subTabInactive ??= CreateTab(ref subTabInactiveTex, DarkMatterGenesisUiPalette.CharcoalGray, false, 11);

        public static GUIStyle ListButton => listButton ??= CreateListButton(ref listBtnTex, false);
        public static GUIStyle ListButtonSelected => listButtonSelected ??= CreateListButton(ref listBtnSelectedTex, true);

        public static GUIStyle Badge => badge ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(6, 6, 2, 2),
            normal = { textColor = DarkMatterGenesisUiPalette.WarmOffWhite }
        };

        public static void DrawAccentLine(Rect rect, Color color, float height = 2f)
        {
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
                EditorGUI.LabelField(r, title, SectionTitle);
                DrawAccentLine(r, accent ?? DarkMatterGenesisUiPalette.RichFuchsia, 1.5f);
            }

            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static bool DrawCategoryTab(string label, bool selected, Color accent, string icon = null)
        {
            string text = string.IsNullOrEmpty(icon) ? label : icon + "  " + label;
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
            return GUILayout.Toggle(selected, label, style, GUILayout.MinHeight(24f), GUILayout.MinWidth(64f));
        }

        public static void DrawBadge(string text, Color background, float minWidth = 0f)
        {
            Rect r = minWidth > 0f
                ? GUILayoutUtility.GetRect(minWidth, 18f, GUILayout.Width(minWidth))
                : GUILayoutUtility.GetRect(GUIContent.none, Badge, GUILayout.MinWidth(text.Length * 7f + 12f));

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
            EditorGUILayout.LabelField(title, HeroTitle);
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
