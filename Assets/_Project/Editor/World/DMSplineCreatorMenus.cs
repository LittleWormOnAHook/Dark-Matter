#if UNITY_EDITOR
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu and GameObject entries for spline / scatter authoring (kept in one file so items always register together).
    /// </summary>
    public static class DMSplineCreatorMenus
    {
        private const int MenuPriority = 12;

        [MenuItem(DarkMatterGenesisEditorMenus.SplineCreatorWindowPrimary, false, MenuPriority)]
        [MenuItem(DarkMatterGenesisEditorMenus.SplineCreatorWindow, false, MenuPriority)]
        public static void OpenWindow()
        {
            DMSplineCreatorWindow.Open();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CreateElectricalLineSpline, false, MenuPriority + 1)]
        public static void CreateElectricalLine()
        {
            DMSplineCreator creator = CreateSplineRoot("ElectricalLineSpline");
            creator.SetMode(DMSplineCreator.CreatorMode.ElectricalLine);
            Selection.activeGameObject = creator.gameObject;
            EditorGUIUtility.PingObject(creator.gameObject);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CreateObjectPlacerSpline, false, MenuPriority + 2)]
        public static void CreateObjectPlacerLine()
        {
            DMSplineCreator creator = CreateSplineRoot("ObjectPlacerSpline");
            creator.SetMode(DMSplineCreator.CreatorMode.ObjectPlacerOnly);
            Selection.activeGameObject = creator.gameObject;
            EditorGUIUtility.PingObject(creator.gameObject);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.CreateScatterPlacer, false, MenuPriority + 3)]
        public static void CreateScatter()
        {
            GameObject root = new GameObject("ScatterPlacer");
            Undo.RegisterCreatedObjectUndo(root, "Create Scatter Placer");
            if (Selection.activeTransform != null)
                root.transform.SetParent(Selection.activeTransform, false);

            root.AddComponent<DMSplineScatterPlacer>();
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        [MenuItem("GameObject/Dark Matter Genesis/Spline/Electrical Line Spline", false, 10)]
        public static void GameObjectCreateElectricalLine()
        {
            CreateElectricalLine();
        }

        [MenuItem("GameObject/Dark Matter Genesis/Spline/Object Placer Spline", false, 11)]
        public static void GameObjectCreateObjectPlacer()
        {
            CreateObjectPlacerLine();
        }

        [MenuItem("GameObject/Dark Matter Genesis/Spline/Scatter Placer", false, 12)]
        public static void GameObjectCreateScatter()
        {
            CreateScatter();
        }

        internal static DMSplineCreator CreateSplineRoot(string name)
        {
            GameObject root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create Spline");
            if (Selection.activeTransform != null)
            {
                root.transform.SetPositionAndRotation(
                    Selection.activeTransform.position,
                    Selection.activeTransform.rotation);
            }

            return root.AddComponent<DMSplineCreator>();
        }
    }
}
#endif
