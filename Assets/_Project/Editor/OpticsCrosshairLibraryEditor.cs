#if UNITY_EDITOR
using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Optics
{
    [CustomEditor(typeof(OpticsCrosshairLibrary))]
    public class OpticsCrosshairLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Edit colors, alphas, transforms, sprites, and materials for binocular and scanner overlays. " +
                "Changes apply the next time optics open, or immediately via Apply To Live Overlay while playing.",
                MessageType.Info);

            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Presentation Defaults"))
                {
                    OpticsCrosshairLibrary library = (OpticsCrosshairLibrary)target;
                    Undo.RecordObject(library, "Reset Optics Presentation Defaults");
                    library.ResetPresentationDefaults();
                    EditorUtility.SetDirty(library);
                }

                if (GUILayout.Button("Apply To Live Overlay"))
                {
                    OpticsOverlayUI overlay = Object.FindAnyObjectByType<OpticsOverlayUI>(FindObjectsInactive.Include);
                    if (overlay == null)
                    {
                        overlay = OpticsOverlayUI.EnsureExists();
                    }

                    if (overlay != null)
                    {
                        overlay.ApplyLibraryPresentation(forceRebuildStyles: true);
                        Debug.Log("[Optics] Applied OpticsCrosshairLibrary to live overlay.");
                    }
                    else
                    {
                        Debug.LogWarning("[Optics] No live overlay found.");
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem(SurvivalPioneerEditorMenus.Optics + "Select Crosshair Library")]
        public static void SelectLibraryAsset()
        {
            OpticsCrosshairLibrary library = AssetDatabase.LoadAssetAtPath<OpticsCrosshairLibrary>(
                "Assets/_Project/Resources/Optics/OpticsCrosshairLibrary.asset");
            if (library != null)
                Selection.activeObject = library;
        }
    }
}
#endif
