using Project.Map;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(DMWorldMapCalibrationProfile))]
    public sealed class DMWorldMapCalibrationProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
                if (iterator.propertyPath == "enableMapFogOfWar")
                    DrawResetFowButton();
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(
                "Journal travel: Map UV Scale X (east) and Y (north) are separate.\n" +
                "Full map: Full Map Max Zoom Visible Meters is the closest zoom (e.g. 200m across the viewport).\n" +
                "Minimap: use Flip Horizontal / Vertical ticks and Rotate X / Y / Z sliders. Those do not change the journal.",
                MessageType.Info);
        }

        private static void DrawResetFowButton()
        {
            EditorGUILayout.Space(2f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Reset FOW Coverage"))
                {
                    MapFogOfWar fog = MapFogOfWar.Instance ?? MapFogOfWar.EnsureExists();
                    if (fog != null)
                        fog.ResetCoverage();
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to clear explored fog.", MessageType.None);
        }
    }
}
