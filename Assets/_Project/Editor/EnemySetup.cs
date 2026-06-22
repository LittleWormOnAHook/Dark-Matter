using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemySetup
    {
        private const string PrefabPath = ProjectAssetPaths.EnemyPrefab;
        private const string SceneEnemyName = "Enemy_Test";

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Place Test Enemy", false, 10)]
        public static void SetupEnemy()
        {
            EnsureDefaultPrefab();
            PlaceTestEnemyInOpenScene();
        }

        public static bool EnsurePrefab()
        {
            EnsureDefaultPrefab();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        }

        public static bool EnsureDefaultPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return false;

            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.displayName = "Enemy";
            definition.prefabFileName = "Enemy";
            definition.ApplyBehaviorPreset(EnemyBehaviorPreset.AggressiveHunter);

            EnemyPrefabBuilder.BuildEnemy(
                definition,
                EnemyPrefabBuilder.VisualSourceMode.PlaceholderCapsule,
                null,
                out _);

            Object.DestroyImmediate(definition);
            return true;
        }

        public static void PlaceTestEnemyInOpenScene()
        {
            EnsureDefaultPrefab();

            GameObject existing = GameObject.Find(SceneEnemyName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log($"{SceneEnemyName} already exists in the scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EnemySetup: failed to create prefab at {PrefabPath}");
                return;
            }

            GameObject instance = EnemyPrefabBuilder.PlacePrefabInScene(
                prefab,
                SceneEnemyName,
                EnemyPrefabBuilder.ResolveSpawnPosition());

            if (instance == null)
            {
                Debug.LogError("EnemySetup: failed to instantiate enemy prefab.");
                return;
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"Placed {SceneEnemyName} at {instance.transform.position}.");
        }

        internal static GameObject CreateEnemyRoot(string rootName)
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.displayName = rootName;
            definition.prefabFileName = rootName;
            definition.ApplyBehaviorPreset(EnemyBehaviorPreset.AggressiveHunter);

            GameObject root = new GameObject(rootName);
            EnemyPrefabBuilder.ApplyGameplayComponents(root, definition);
            Object.DestroyImmediate(definition);
            return root;
        }
    }
}
