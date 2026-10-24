#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>Studio: Rendering -> DM Shaders -> Terrain. Edits DM_TerrainParallax.mat only; never assigns it.</summary>
    public class DMStudioTerrainShaderWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Dark Matter Genesis/Rendering/DM Shaders/Terrain";
        public const string MaterialPath = "Assets/_Project/Rendering/DMShaders/DM_TerrainParallax.mat";

        private Material material;
        private MaterialEditor materialEditor;
        private Vector2 scroll;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<DMStudioTerrainShaderWindow>("DM Terrain Shader");
        }

        private void OnDisable()
        {
            if (materialEditor != null)
                DestroyImmediate(materialEditor);
        }

        private void OnGUI()
        {
            if (material == null)
                material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            Material picked = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
            if (picked != material)
            {
                material = picked;
                if (materialEditor != null)
                    DestroyImmediate(materialEditor);
            }

            if (material == null)
            {
                EditorGUILayout.HelpBox("Missing " + MaterialPath, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox("DM_ material only. Nothing is assigned automatically: duplicate a terrain and set its Material to this to test. Height = mask map Blue channel.", MessageType.Info);
            if (GUILayout.Button("Ping material"))
                EditorGUIUtility.PingObject(material);

            if (materialEditor == null || materialEditor.target != material)
            {
                if (materialEditor != null)
                    DestroyImmediate(materialEditor);
                materialEditor = (MaterialEditor)Editor.CreateEditor(material);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            materialEditor.PropertiesGUI();
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
