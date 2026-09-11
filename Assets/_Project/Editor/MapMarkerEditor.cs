using Project.Map;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(MapMarker))]
    public class MapMarkerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Refresh applies fog reveal + live world position to every MapMarker in the open scenes and _Project prefabs. Enemies also get combat aggro.",
                MessageType.None);

            if (GUILayout.Button("Refresh Map Marker Label", GUILayout.Height(24f)))
                MapMarkerEditorUtility.RefreshAllMapMarkers();
        }
    }
}
