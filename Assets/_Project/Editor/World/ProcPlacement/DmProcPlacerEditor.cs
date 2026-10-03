#if UNITY_EDITOR
using Project.World.ProcPlacement;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World.ProcPlacement
{
    [CustomEditor(typeof(DmProcPlacer))]
    [CanEditMultipleObjects]
    internal sealed class DmProcPlacerEditor : Project.EditorTools.Theme.GenesisImguiEditor
    {
        public const string DefaultRecipePath = "Assets/_Project/Art/ProcPlacement/Recipes/DM_ProcRecipe_IoBasaltRock.asset";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recipe"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockSeed"),
                new GUIContent("Lock Seed", "When on, copy/paste keeps this exact shape."));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Variation"))
                {
                    foreach (UnityEngine.Object t in targets)
                    {
                        var p = (DmProcPlacer)t;
                        Undo.RecordObject(p, "Proc Placer New Variation");
                        p.SetSeed(DmProcPasteWatcher.NewSeed());
                        EditorUtility.SetDirty(p);
                    }
                }
                if (GUILayout.Button("Rebuild"))
                {
                    foreach (UnityEngine.Object t in targets)
                        ((DmProcPlacer)t).Rebuild();
                }
            }

            var single = (DmProcPlacer)target;
            if (single.Recipe == null)
            {
                EditorGUILayout.HelpBox("Assign a Proc Recipe to generate.", MessageType.Info);
            }
            else if (single.GeneratedMesh != null && targets.Length == 1)
            {
                EditorGUILayout.HelpBox(
                    $"Generated: {single.GeneratedMesh.vertexCount:N0} verts, {single.GeneratedMesh.triangles.Length / 3:N0} tris.\n" +
                    "Copy/paste or Ctrl+D rolls a new shape with the same material unless Lock Seed is on.",
                    MessageType.None);
            }

            if (single.Recipe != null)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Select Recipe"))
                {
                    Selection.activeObject = single.Recipe;
                    EditorGUIUtility.PingObject(single.Recipe);
                }
            }
        }

        [MenuItem("GameObject/Dark Matter Genesis/Proc Rock", false, 10)]
        private static void CreateProcRock(MenuCommand command)
        {
            var go = new GameObject("ProcRock");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            if (command.context == null && SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;

            var placer = go.AddComponent<DmProcPlacer>();
            var recipe = AssetDatabase.LoadAssetAtPath<DmProcRecipe>(DefaultRecipePath);
            placer.SetSeed(DmProcPasteWatcher.NewSeed());
            if (recipe != null)
                placer.SetRecipe(recipe);

            Undo.RegisterCreatedObjectUndo(go, "Create Proc Rock");
            Selection.activeGameObject = go;
        }
    }
}
#endif
