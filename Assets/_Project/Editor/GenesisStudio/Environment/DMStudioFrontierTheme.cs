#if UNITY_EDITOR
using System.Collections.Generic;
using GenesisPCG.RockCreation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// DM-side Frontier theme for UI Toolkit studio panels: adds the Frontier variables / cut corners (USS) to every
    /// Genesis PCG studio panel (standalone window and Genesis Studio tab). Picks up any stylesheet in
    /// Assets/_Project/Editor/GenesisTheme first, and the Chakra Petch / Inter fonts when they exist in the project.
    /// </summary>
    [InitializeOnLoad]
    public static class DMStudioFrontierTheme
    {
        public const string ThemeFolder = "Assets/_Project/Editor/GenesisTheme";
        public const string FallbackSheet = "Assets/_Project/Editor/GenesisStudio/Environment/DMStudioFrontierRocks.uss";

        static DMStudioFrontierTheme()
        {
            PcgStudioExtensions.PanelCreated -= Apply;
            PcgStudioExtensions.PanelCreated += Apply;
        }

        public static void Apply(VisualElement ve)
        {
            if (ve == null) return;
            ve.AddToClassList("dm-frontier");
            foreach (StyleSheet s in Sheets())
                if (!ve.styleSheets.Contains(s)) ve.styleSheets.Add(s);
            ApplyFonts(ve);
        }

        private static IEnumerable<StyleSheet> Sheets()
        {
            if (AssetDatabase.IsValidFolder(ThemeFolder))
                foreach (string g in AssetDatabase.FindAssets("t:StyleSheet", new[] { ThemeFolder }))
                {
                    var s = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(g));
                    if (s != null) yield return s;
                }
            var fb = AssetDatabase.LoadAssetAtPath<StyleSheet>(FallbackSheet);
            if (fb != null) yield return fb;
        }

        private static Font s_body, s_head;
        private static bool s_fontsSearched;

        private static Font FindFont(string name)
        {
            foreach (string g in AssetDatabase.FindAssets(name + " t:Font"))
            {
                var f = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>Inter for body text, Chakra Petch for headers, when the fonts are in the project (else the editor font).</summary>
        private static void ApplyFonts(VisualElement ve)
        {
            if (!s_fontsSearched)
            {
                s_fontsSearched = true;
                s_body = FindFont("Inter");
                s_head = FindFont("ChakraPetch") ?? FindFont("Chakra Petch");
            }
            if (s_body != null) ve.style.unityFontDefinition = FontDefinition.FromFont(s_body);
            if (s_head == null) return;
            void Headers() => ve.Query(className: "pcg-header").ForEach(h => h.style.unityFontDefinition = FontDefinition.FromFont(s_head));
            ve.schedule.Execute(Headers).Every(1000); // headers created later (lazy sections) get the font too
        }
    }
}
#endif
