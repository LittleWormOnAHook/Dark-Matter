using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>
    /// Genesis editor theme ("Frontier"). Call <see cref="Apply"/> on any EditorWindow rootVisualElement or
    /// custom-inspector root to give it the shared Genesis look. Colours live in USS/GenesisTheme.uss.
    /// </summary>
    public static class GenesisTheme
    {
        public const string Folder = "Assets/_Project/Editor/GenesisTheme/";
        public const string ThemeSheetPath = Folder + "USS/GenesisTheme.uss";
        public const string ControlsSheetPath = Folder + "USS/GenesisControls.uss";
        public const string HeaderFontPath = Folder + "Fonts/ChakraPetch-SemiBold.ttf";
        public const string HeaderFontBoldPath = Folder + "Fonts/ChakraPetch-Bold.ttf";
        public const string RootClass = "genesis-root";
        public const string HeaderClass = "g-h";
        public const string MenuRoot = DarkMatterGenesisEditorMenus.Root + "Theme/";
        const string MenuEnabled = MenuRoot + "Genesis Theme Enabled";
        const string PrefEnabled = "DMG.GenesisTheme.Enabled";

        /// <summary>Raised when the theme is switched on/off so open windows can rebuild.</summary>
        public static event Action Changed;

        // Palette mirror for IMGUI and painter fallbacks (keep in sync with GenesisTheme.uss).
        public static readonly Color Bg0 = Hex("#0f0e0c");
        public static readonly Color Bg1 = Hex("#171512");
        public static readonly Color Bg2 = Hex("#201d19");
        public static readonly Color Bg3 = Hex("#2a2620");
        public static readonly Color Line = Hex("#3d372e");
        public static readonly Color Text = Hex("#ece4d6");
        public static readonly Color Dim = Hex("#a09582");
        public static readonly Color Accent = Hex("#ff8a2a");
        public static readonly Color AccentDark = Hex("#c25d12");
        public static readonly Color Primary = Hex("#ffc23d");
        public static readonly Color PrimaryText = Hex("#1e1402");
        public static readonly Color Ok = Hex("#8bd450");
        public static readonly Color Warn = Hex("#ffb02e");
        public static readonly Color Bad = Hex("#ff4b3a");

        static Font s_HeaderFont;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefEnabled, true);
            set
            {
                if (Enabled == value) return;
                EditorPrefs.SetBool(PrefEnabled, value);
                Changed?.Invoke();
                foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>()) w.Repaint();
            }
        }

        public static Font HeaderFont
        {
            get
            {
                if (s_HeaderFont == null) s_HeaderFont = AssetDatabase.LoadAssetAtPath<Font>(HeaderFontPath);
                return s_HeaderFont;
            }
        }

        /// <summary>Adds the Genesis stylesheets and root class. Returns false if disabled or assets are missing.</summary>
        public static bool Apply(VisualElement root)
        {
            if (root == null) return false;
            if (!Enabled) { Remove(root); return false; }

            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeSheetPath);
            var controls = AssetDatabase.LoadAssetAtPath<StyleSheet>(ControlsSheetPath);
            if (theme == null || controls == null)
            {
                Debug.LogWarning("[GenesisTheme] Stylesheets not found under " + Folder + "USS. Theme not applied.");
                return false;
            }

            if (!root.styleSheets.Contains(theme)) root.styleSheets.Add(theme);
            if (!root.styleSheets.Contains(controls)) root.styleSheets.Add(controls);
            root.AddToClassList(RootClass);
            return true;
        }

        public static void Remove(VisualElement root)
        {
            if (root == null) return;
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeSheetPath);
            var controls = AssetDatabase.LoadAssetAtPath<StyleSheet>(ControlsSheetPath);
            if (theme != null) root.styleSheets.Remove(theme);
            if (controls != null) root.styleSheets.Remove(controls);
            root.RemoveFromClassList(RootClass);
        }

        /// <summary>Marks an element as a header (Chakra Petch, uppercase spacing). Sets the font inline too so it never depends on USS font parsing.</summary>
        public static T Header<T>(T element) where T : VisualElement
        {
            if (element == null) return null;
            element.AddToClassList(HeaderClass);
            var f = HeaderFont;
            if (f != null && Enabled) element.style.unityFontDefinition = FontDefinition.FromFont(f);
            return element;
        }

        /// <summary>Creates an uppercase header label.</summary>
        public static Label HeaderLabel(string text, float fontSize = 11f)
        {
            var l = Header(new Label((text ?? string.Empty).ToUpperInvariant()));
            l.style.fontSize = fontSize;
            return l;
        }

        public static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var c) ? c : Color.magenta;
        }

        [MenuItem(MenuEnabled, false, 1)]
        static void ToggleEnabled() => Enabled = !Enabled;

        [MenuItem(MenuEnabled, true)]
        static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuEnabled, Enabled);
            return true;
        }
    }
}
