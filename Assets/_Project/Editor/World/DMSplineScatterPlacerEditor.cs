#if UNITY_EDITOR
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(DMSplineScatterPlacer))]
    public class DMSplineScatterPlacerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var placer = (DMSplineScatterPlacer)target;

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Rebuild Scatter", GUILayout.Height(28f)))
            {
                Undo.RecordObject(placer, "Rebuild Scatter");
                placer.RebuildScatter();
                EditorUtility.SetDirty(placer);
            }
        }
    }
}
#endif
