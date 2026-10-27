#if UNITY_EDITOR
using Project.World.Sky;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>2026-10-24ai: Io sky helpers. Nothing here saves scenes.</summary>
    public static class DMIoSkyEditorMenu
    {
        private const string ShaderPath = "Assets/_Project/Scripts/World/Sky/DM_IoSkyBody.shader";

        [MenuItem("Tools/Dark Matter Genesis/World/Io Sky/Ensure Sky Profile", false, 2101)]
        public static void EnsureProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/World"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "World");
            DM_IoSkyProfile p = AssetDatabase.LoadAssetAtPath<DM_IoSkyProfile>(DM_IoSkyProfile.AssetPath);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<DM_IoSkyProfile>();
                AssetDatabase.CreateAsset(p, DM_IoSkyProfile.AssetPath);
            }
            if (p.skyBodyShader == null)
                p.skyBodyShader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssetIfDirty(p);
            Selection.activeObject = p;
            Debug.Log("[DM] Io sky profile ready at " + DM_IoSkyProfile.AssetPath);
        }

        /// <summary>Optional: place the controller in the open scene (marks it dirty; you save).</summary>
        [MenuItem("Tools/Dark Matter Genesis/World/Io Sky/Add Sky Controller To Open Scene", false, 2102)]
        public static void AddControllerToScene()
        {
            if (Object.FindAnyObjectByType<DMIoSkyController>() != null)
            {
                Debug.Log("[DM] Scene already has a DMIoSkyController.");
                return;
            }
            var go = new GameObject("DMIoSkyController");
            go.AddComponent<DMIoSkyController>();
            Undo.RegisterCreatedObjectUndo(go, "Add Io Sky Controller");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeObject = go;
        }
    }
}
#endif
