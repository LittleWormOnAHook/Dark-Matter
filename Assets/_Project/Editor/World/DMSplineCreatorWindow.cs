#if UNITY_EDITOR
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public class DMSplineCreatorWindow : EditorWindow
    {
        private enum Tab
        {
            ElectricalLine = 0,
            ObjectPlacer = 1,
            Scatter = 2
        }

        private Tab tab = Tab.ElectricalLine;
        private DMSplineCreator activeSpline;
        private DMSplineScatterPlacer activeScatter;
        private GameObject defaultPrefab;
        private Material defaultLineMaterial;
        private int anchorCount = 4;
        private float anchorSpacing = 8f;

        public static void Open()
        {
            GetWindow<DMSplineCreatorWindow>("Spline Creator");
        }

        private void OnGUI()
        {
            tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "Electrical line", "Object placer", "Scatter" });

            EditorGUILayout.Space(6f);
            switch (tab)
            {
                case Tab.ElectricalLine:
                    DrawSplineTab(DMSplineCreator.CreatorMode.ElectricalLine, true);
                    break;
                case Tab.ObjectPlacer:
                    DrawSplineTab(DMSplineCreator.CreatorMode.ObjectPlacerOnly, false);
                    break;
                case Tab.Scatter:
                    DrawScatterTab();
                    break;
            }
        }

        private void DrawSplineTab(DMSplineCreator.CreatorMode mode, bool showLineMaterial)
        {
            EditorGUILayout.HelpBox(
                "Uses Path Creator (Bézier + Vertex path). Select the spline in the hierarchy and edit anchors in the Scene view.",
                MessageType.Info);

            activeSpline = (DMSplineCreator)EditorGUILayout.ObjectField("Active spline", activeSpline, typeof(DMSplineCreator), true);
            defaultPrefab = (GameObject)EditorGUILayout.ObjectField("Anchor prefab", defaultPrefab, typeof(GameObject), false);
            anchorCount = EditorGUILayout.IntSlider("Anchor count", anchorCount, 2, 64);
            anchorSpacing = EditorGUILayout.FloatField("Anchor spacing", anchorSpacing);

            if (showLineMaterial)
                defaultLineMaterial = (Material)EditorGUILayout.ObjectField("Line material", defaultLineMaterial, typeof(Material), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New"))
            {
                string name = mode == DMSplineCreator.CreatorMode.ElectricalLine ? "ElectricalLineSpline" : "ObjectPlacerSpline";
                activeSpline = DMSplineCreatorMenus.CreateSplineRoot(name);
                activeSpline.SetMode(mode);
                Selection.activeGameObject = activeSpline.gameObject;
            }

            GUI.enabled = activeSpline != null;
            if (GUILayout.Button("Apply Prefab + Rebuild"))
            {
                Undo.RecordObject(activeSpline, "Apply Spline Settings");
                activeSpline.SetMode(mode);
                if (defaultPrefab != null)
                    activeSpline.SetAnchorPrefab(defaultPrefab);
                ApplyLineMaterial(activeSpline, showLineMaterial, defaultLineMaterial);
                ApplyAnchorLayoutFromWindow(activeSpline);
                activeSpline.RebuildAll();
                EditorUtility.SetDirty(activeSpline);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScatterTab()
        {
            activeScatter = (DMSplineScatterPlacer)EditorGUILayout.ObjectField("Active scatter", activeScatter, typeof(DMSplineScatterPlacer), true);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New"))
            {
                DMSplineCreatorMenus.CreateScatter();
                activeScatter = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<DMSplineScatterPlacer>()
                    : null;
            }

            GUI.enabled = activeScatter != null;
            if (GUILayout.Button("Rebuild Scatter"))
            {
                Undo.RecordObject(activeScatter, "Rebuild Scatter");
                activeScatter.RebuildScatter();
                EditorUtility.SetDirty(activeScatter);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyAnchorLayoutFromWindow(DMSplineCreator creator)
        {
            SerializedObject so = new SerializedObject(creator);
            so.FindProperty("anchorPointCount").intValue = anchorCount;
            so.FindProperty("anchorSpacing").floatValue = anchorSpacing;
            so.ApplyModifiedPropertiesWithoutUndo();
            creator.SetDesiredAnchorCount(anchorCount);
            creator.ApplyAnchorCountAndPrefab(true);
        }

        private static void ApplyLineMaterial(DMSplineCreator creator, bool apply, Material material)
        {
            if (!apply || material == null)
                return;

            SerializedObject so = new SerializedObject(creator);
            so.FindProperty("lineMaterial").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
