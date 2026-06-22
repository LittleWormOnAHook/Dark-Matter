using System.Collections.Generic;
using System.IO;
using Project.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class EnemyLootSetupUtility
    {
        private const string PioneerScenePath = "Assets/Pioneer.unity";

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Update All Enemy Prefabs And Scene", false, 20)]
        public static void UpdateAllEnemyPrefabsAndScene()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            int updatedPrefabs = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!TryUpdatePrefabAtPath(path, out string label))
                    continue;

                updatedPrefabs++;
                Debug.Log($"Updated enemy loot on prefab: {label} ({path})");
            }

            int updatedSceneInstances = ApplyPrefabUpdatesToOpenScenes();
            int pioneerSceneInstances = ApplyPrefabUpdatesToScene(PioneerScenePath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Enemy Loot Update",
                $"Updated {updatedPrefabs} combat prefab(s).\n" +
                $"Refreshed {updatedSceneInstances + pioneerSceneInstances} scene instance(s).",
                "OK");
        }

        public static bool TryUpdatePrefabAtPath(string prefabPath, out string displayName)
        {
            displayName = Path.GetFileNameWithoutExtension(prefabPath);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null || prefabRoot.GetComponent<EnemyHealth>() == null)
                return false;

            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (instance == null)
                return false;

            EnemyDefinition definition = ResolveOrCreateDefinition(instance, prefabPath);
            displayName = definition.displayName;
            EnemyPrefabBuilder.ApplyLootToPrefab(instance, definition);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            return true;
        }

        private static EnemyDefinition ResolveOrCreateDefinition(GameObject instance, string prefabPath)
        {
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string definitionPath =
                $"{ProjectAssetPaths.EnemiesData}/{EnemyPrefabBuilder.SanitizeFileName(prefabName, prefabName)}.asset";

            EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.enemyId = EnemyPrefabBuilder.SanitizeFileName(prefabName, prefabName).ToLowerInvariant();
                definition.displayName = prefabName.Replace('_', ' ');
                definition.prefabFileName = prefabName;
                ApplyPrefabValuesToDefinition(instance, definition);
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            else
            {
                ApplyPrefabValuesToDefinition(instance, definition);
                EditorUtility.SetDirty(definition);
            }

            ApplyDefinitionDefaults(definition, prefabName);
            return definition;
        }

        private static void ApplyPrefabValuesToDefinition(GameObject instance, EnemyDefinition definition)
        {
            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health != null)
            {
                SerializedObject healthObject = new SerializedObject(health);
                definition.maxHealth = healthObject.FindProperty("maxHealth")?.floatValue ?? definition.maxHealth;
                definition.destroyOnDeath = healthObject.FindProperty("destroyOnDeath")?.boolValue ?? definition.destroyOnDeath;
                definition.destroyDelay = healthObject.FindProperty("destroyDelay")?.floatValue ?? definition.destroyDelay;
                definition.respawnTime = health.respawnTime;
            }

            if (string.IsNullOrWhiteSpace(definition.displayName))
                definition.displayName = instance.name;
        }

        private static void ApplyDefinitionDefaults(EnemyDefinition definition, string prefabName)
        {
            definition.enableLoot = true;
            definition.lootRespawnDelay = 20f;
            definition.lootInteractRange = 2.75f;

            if (definition.piCoinsMin <= 0 && definition.piCoinsMax <= 0)
            {
                definition.piCoinsMin = 1;
                definition.piCoinsMax = prefabName.Contains("Evil", System.StringComparison.OrdinalIgnoreCase) ? 8 : 5;
            }

            if (definition.randomLootCountMax <= 0)
            {
                definition.randomLootCountMin = 0;
                definition.randomLootCountMax = prefabName.Contains("Evil", System.StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            }
        }

        private static int ApplyPrefabUpdatesToOpenScenes()
        {
            int updated = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                updated += ApplyPrefabUpdatesInScene(scene);
            }

            return updated;
        }

        private static int ApplyPrefabUpdatesToScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
                return 0;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            int updated = ApplyPrefabUpdatesInScene(scene);
            if (SceneManager.sceneCount > 1)
                EditorSceneManager.CloseScene(scene, true);
            else
                EditorSceneManager.MarkSceneDirty(scene);

            EditorSceneManager.SaveScene(scene);
            return updated;
        }

        private static int ApplyPrefabUpdatesInScene(Scene scene)
        {
            int updated = 0;
            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include);
            HashSet<string> touchedPrefabPaths = new HashSet<string>();

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null)
                    continue;

                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(enemy.gameObject);
                if (root == null)
                {
                    updated += UpdateSceneEmbeddedEnemy(enemy.gameObject) ? 1 : 0;
                    continue;
                }

                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                if (string.IsNullOrEmpty(prefabPath))
                    continue;

                if (touchedPrefabPaths.Add(prefabPath) && TryUpdatePrefabAtPath(prefabPath, out _))
                    updated++;

                PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
            }

            if (updated > 0)
                EditorSceneManager.MarkSceneDirty(scene);

            return updated;
        }

        private static bool UpdateSceneEmbeddedEnemy(GameObject enemyRoot)
        {
            if (enemyRoot.GetComponent<EnemyHealth>() == null)
                return false;

            EnemyDefinition definition = ResolveOrCreateDefinition(
                enemyRoot,
                $"{ProjectAssetPaths.PrefabsCombat}/{EnemyPrefabBuilder.SanitizeFileName(enemyRoot.name, enemyRoot.name)}.asset");

            EnemyPrefabBuilder.ApplyLootToPrefab(enemyRoot, definition);
            return true;
        }
    }
}
