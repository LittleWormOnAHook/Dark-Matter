#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>2026-10-12: shared Studio search (Genesis Studio, all tabs incl. Character Creator, Combat Studio).
    /// Property loops skip fields whose label / name / tooltip / sub-field does not match; grouped inspectors open matching groups.</summary>
    public interface IDMStudioSearchAware { }

    public static class DMStudioSearch
    {
        public static string Query = string.Empty;

        public static bool Active => !string.IsNullOrWhiteSpace(Query);

        public static bool Matches(string text)
        {
            if (!Active) return true;
            return !string.IsNullOrEmpty(text) && text.IndexOf(Query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool MatchesProperty(SerializedProperty p, string label = null)
        {
            if (!Active || p == null) return true;
            if (p.propertyPath == "m_Script") return false;
            if (Matches(label) || Matches(p.displayName) || Matches(p.name) || Matches(p.tooltip)) return true;
            if (!p.hasVisibleChildren) return false;
            SerializedProperty it = p.Copy(), end = p.GetEndProperty();
            int guard = 0;
            while (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end) && guard++ < 500)
                if (Matches(it.displayName) || Matches(it.name)) return true;
            return false;
        }

        public static bool HandlesSearch(UnityEditor.Editor ed) => ed is IDMStudioSearchAware;

        public static void DrawFilteredDefault(SerializedObject so)
        {
            if (so == null) return;
            so.Update();
            SerializedProperty it = so.GetIterator();
            bool enter = true; int shown = 0;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (!MatchesProperty(it)) continue;
                EditorGUILayout.PropertyField(it, true); shown++;
            }
            if (shown == 0) EditorGUILayout.LabelField("No fields match \"" + Query.Trim() + "\".", EditorStyles.centeredGreyMiniLabel);
            so.ApplyModifiedProperties();
        }

        /// <summary>Toolbar search field bound to the shared query. Returns true when it changed.</summary>
        public static bool DrawField(string controlName = "DMStudioSearch")
        {
            string old = Query ?? string.Empty;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Search", EditorStyles.miniLabel, GUILayout.Width(44f));
                GUI.SetNextControlName(controlName);
                Query = EditorGUILayout.TextField(old, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                {
                    Query = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            return Query != old;
        }
    }
}
#endif
