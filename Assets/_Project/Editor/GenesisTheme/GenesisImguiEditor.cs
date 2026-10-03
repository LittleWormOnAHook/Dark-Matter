using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>
    /// Base class for Genesis custom inspectors still written with OnInspectorGUI. With the Genesis Theme on, the IMGUI
    /// inspector is hosted in a themed UI Toolkit root and drawn inside a <see cref="GenesisImgui"/> scope; with it off,
    /// Unity draws OnInspectorGUI exactly as before. Switch an editor over by changing ": Editor" to ": GenesisImguiEditor".
    /// </summary>
    public abstract class GenesisImguiEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (!GenesisTheme.Enabled) return null;

            var root = new VisualElement();
            if (!GenesisTheme.Apply(root)) return null;
            root.AddToClassList("g-inspector");

            var imgui = new IMGUIContainer(DrawThemed);
            imgui.AddToClassList("g-inspector__imgui");
            root.Add(imgui);
            return root;
        }

        void DrawThemed()
        {
            if (target == null || serializedObject == null) return;

            // Match the regular inspector's label width and indentation rules.
            EditorGUIUtility.hierarchyMode = true;
            EditorGUIUtility.wideMode = EditorGUIUtility.currentViewWidth > 330f;

            using (GenesisImgui.Begin())
            {
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
