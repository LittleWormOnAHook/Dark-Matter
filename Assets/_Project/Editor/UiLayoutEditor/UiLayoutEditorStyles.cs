using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    internal static class UiLayoutEditorStyles
    {
        private static GUIStyle headerBar;
        private static GUIStyle hierarchyPanel;
        private static GUIStyle inspectorPanel;
        private static GUIStyle gamePanelsPanel;
        private static GUIStyle toolbarPanel;

        public static readonly Color HeaderColor = new Color(0.16f, 0.20f, 0.28f, 1f);
        public static readonly Color HierarchyColor = new Color(0.18f, 0.16f, 0.26f, 1f);
        public static readonly Color InspectorColor = new Color(0.14f, 0.22f, 0.18f, 1f);
        public static readonly Color GamePanelsColor = new Color(0.24f, 0.19f, 0.12f, 1f);
        public static readonly Color ToolbarColor = new Color(0.12f, 0.18f, 0.24f, 1f);

        public static GUIStyle HeaderBar => headerBar ??= CreatePanelStyle(HeaderColor);
        public static GUIStyle HierarchyPanel => hierarchyPanel ??= CreatePanelStyle(HierarchyColor);
        public static GUIStyle InspectorPanel => inspectorPanel ??= CreatePanelStyle(InspectorColor);
        public static GUIStyle GamePanelsPanel => gamePanelsPanel ??= CreatePanelStyle(GamePanelsColor);
        public static GUIStyle ToolbarPanel => toolbarPanel ??= CreatePanelStyle(ToolbarColor);

        public static void DrawSection(string title, GUIStyle panelStyle, Action drawContent)
        {
            EditorGUILayout.BeginVertical(panelStyle);
            if (!string.IsNullOrEmpty(title))
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void DrawMiniBadge(string text, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, EditorStyles.miniLabel, GUILayout.Width(Mathf.Max(42f, text.Length * 7f)));
            GUI.color = old;
        }

        private static GUIStyle CreatePanelStyle(Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            style.normal.background = texture;
            return style;
        }
    }
}
