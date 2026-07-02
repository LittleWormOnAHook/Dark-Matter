#if UNITY_EDITOR
using System.IO;
using Project.AI;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    public static class EnemyRegistrySetupUtility
    {
        private const string RegistryPath = "Assets/_Project/Resources/EnemyRegistry.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Enemy Registry", false, 43)]
        public static void CreateEnemyRegistry()
        {
            EnsureFolder("Assets/_Project/Resources");

            EnemyDefinition[] definitions = LoadDefinitionsFromDataFolder();
            EnemyRegistry registry = AssetDatabase.LoadAssetAtPath<EnemyRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<EnemyRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty array = serialized.FindProperty("enemies");
            array.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Selection.activeObject = registry;
            Debug.Log($"Enemy registry updated with {definitions.Length} definition(s) at {RegistryPath}");
        }

        private static EnemyDefinition[] LoadDefinitionsFromDataFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
            EnemyDefinition[] definitions = new EnemyDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                definitions[i] = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            }

            return definitions;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
