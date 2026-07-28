#if UNITY_EDITOR
using System.IO;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Data;
using Project.EditorTools;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    public static class EnemyInvectorSetupUtility
    {
        private const string SourcePlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";
        private const string HumanoidBasePrefabPath = ProjectAssetPaths.HumanoidEnemyPrefab;
        private const string HumanoidEnemyDefinitionPath = "Assets/_Project/Data/Enemies/Humanoid_Enemy.asset";
        private const string DefaultMeleeItemPath = ProjectAssetPaths.ItemsMelee + "/weap2_sword.asset";
        private const string DefaultRangedItemPath = ProjectAssetPaths.ItemsRanged + "/sci_fi_pistol.asset";

        [MenuItem(SurvivalPioneerEditorMenus.RepairAllHumanoidCombatPrefabs, false, 130)]
        public static void RepairAllHumanoidCombatPrefabs()
        {
            EnsureHumanoidEnemyDefinitionAsset();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            int repaired = 0;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null || prefabAsset.GetComponent<EnemyInvectorBootstrap>() == null)
                    continue;

                if (RepairGameplayAtPath(prefabPath))
                    repaired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Repaired spawn-ready gameplay on {repaired} humanoid combat prefab(s).");
        }

        public static GameObject BuildHumanoidEnemyPrefab(
            EnemyDefinition definition,
            GameObject visualSource,
            string outputPrefabPath)
        {
            if (definition == null)
                return null;

            EnsureHumanoidBaseExists();
            GameObject root = PrefabUtility.LoadPrefabContents(HumanoidBasePrefabPath);
            if (root == null)
                return null;

            try
            {
                root.name = Path.GetFileNameWithoutExtension(outputPrefabPath);
                if (visualSource != null)
                    AttachVisualModel(root, visualSource, definition.visualChildName);

                RepairHumanoidRoot(root, definition);

                CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
                return PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static bool RebuildHumanoidEnemyAtPath(
            string prefabPath,
            EnemyDefinition definition,
            GameObject visualSource = null)
        {
            if (string.IsNullOrEmpty(prefabPath) || definition == null)
                return false;

            if (!File.Exists(prefabPath))
                return BuildHumanoidEnemyPrefab(definition, visualSource, prefabPath) != null;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                if (visualSource != null)
                    AttachVisualModel(root, visualSource, definition.visualChildName);

                RepairHumanoidRoot(root, definition);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void RepairHumanoidRoot(GameObject root, EnemyDefinition definition)
        {
            if (root == null || definition == null)
                return;

            EnemyInvectorComponentStripper.StripEditor(root);
            EnemyPrefabBuilder.ApplyGameplayComponents(root, definition);

            EnemyAnimationController legacyAnim = root.GetComponent<EnemyAnimationController>();
            if (legacyAnim != null)
                Object.DestroyImmediate(legacyAnim, true);

            EnsureHumanoidEnemyComponents(root);
            ConfigureHumanoidLoadout(root, definition);
            WireBootstrapDefinition(root, definition);
            EnemyInvectorBodySnapSetupEditor.EnsurePresentEditor(root);
            EnemyInvectorBodySnapSetupEditor.ConfigureEditor(root);
            EnemyInvectorTargetLayers.Apply(root);
            EnemyInvectorRagdollAudit.Repair(root);
            ApplyHumanoidPhysics(root, definition);
            AddDamageReceiversToRoot(root);
            EnemyNavMeshStripUtility.StripNavMeshFromRoot(root);
        }

        public static void ApplyHumanoidToExistingRoot(GameObject root, EnemyDefinition definition, GameObject visualSource)
        {
            if (root == null || definition == null)
                return;

            if (visualSource != null)
                AttachVisualModel(root, visualSource, definition.visualChildName);

            RepairHumanoidRoot(root, definition);
        }

        public static bool SwapVisualOnPrefab(string prefabPath, GameObject newVisualSource, EnemyDefinition definition)
        {
            return RebuildHumanoidEnemyAtPath(prefabPath, definition, newVisualSource);
        }

        private static bool RepairGameplayAtPath(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                EnemyDefinition definition = ResolveDefinitionForPrefab(prefabPath);
                RepairHumanoidRoot(root, definition);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static EnemyDefinition ResolveDefinitionForPrefab(string prefabPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            if (fileName == "HumanoidEnemy_Invector")
                return EnsureHumanoidEnemyDefinitionAsset();

            if (fileName == "The_Evil_One")
            {
                EnemyDefinition evilOne = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                    $"{ProjectAssetPaths.EnemiesData}/The_Evil_One.asset");
                if (evilOne != null)
                    return evilOne;
            }

            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (definition == null)
                    continue;

                if (string.Equals(definition.prefabFileName, fileName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(definition.displayName.Replace(' ', '_'), fileName, System.StringComparison.OrdinalIgnoreCase))
                    return definition;
            }

            return EnsureHumanoidEnemyDefinitionAsset();
        }

        public static EnemyDefinition EnsureHumanoidEnemyDefinitionAsset()
        {
            EnemyDefinition existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(HumanoidEnemyDefinitionPath);
            if (existing != null)
                return existing;

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.enemyId = "humanoid_enemy";
            definition.displayName = "Humanoid Enemy";
            definition.prefabFileName = "HumanoidEnemy_Invector";
            definition.archetype = EnemyArchetype.HumanoidInvector;
            definition.movementMode = EnemyMovementMode.Wander;
            definition.visualChildName = "Visual";
            definition.meleeWeaponItem = AssetDatabase.LoadAssetAtPath<ItemData>(DefaultMeleeItemPath);
            definition.rangedWeaponItem = AssetDatabase.LoadAssetAtPath<ItemData>(DefaultRangedItemPath);
            definition.preferRangedWeapon = false;

            AssetDatabase.CreateAsset(definition, HumanoidEnemyDefinitionPath);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static void WireBootstrapDefinition(GameObject root, EnemyDefinition definition)
        {
            EnemyInvectorBootstrap bootstrap = root.GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap == null)
                return;

            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty definitionProperty = serialized.FindProperty("enemyDefinition");
            if (definitionProperty != null)
                definitionProperty.objectReferenceValue = definition;

            SerializedProperty infiniteAmmo = serialized.FindProperty("infiniteAmmo");
            if (infiniteAmmo != null)
                infiniteAmmo.boolValue = true;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildHumanoidBaseRoot(string rootName)
        {
            if (!File.Exists(SourcePlayerPrefabPath))
            {
                Debug.LogError($"Missing {SourcePlayerPrefabPath}. Run Build Player_Invector Prefab first.");
                return null;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePlayerPrefabPath);
            try
            {
                root.name = rootName;
                PioneerInvectorPlayerSetupUtility.RefreshPreloadedWeaponSlotsOn(root);

                EnemyDefinition defaultDefinition = EnsureHumanoidEnemyDefinitionAsset();
                RepairHumanoidRoot(root, defaultDefinition);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HumanoidBasePrefabPath);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureHumanoidBaseExists()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidBasePrefabPath) == null)
                BuildHumanoidBaseRoot("HumanoidEnemy_Invector");
        }

        private static void EnsureHumanoidEnemyComponents(GameObject root)
        {
            if (root.GetComponent<EnemyInvectorBootstrap>() == null)
                root.AddComponent<EnemyInvectorBootstrap>();
            if (root.GetComponent<EnemyInvectorLoadoutBridge>() == null)
                root.AddComponent<EnemyInvectorLoadoutBridge>();
            if (root.GetComponent<EnemyInvectorMotorBridge>() == null)
                root.AddComponent<EnemyInvectorMotorBridge>();
            if (root.GetComponent<EnemyInvectorCombatBridge>() == null)
                root.AddComponent<EnemyInvectorCombatBridge>();
            if (root.GetComponent<EnemyInvectorOutgoingDamageBridge>() == null)
                root.AddComponent<EnemyInvectorOutgoingDamageBridge>();
            if (root.GetComponent<EnemyTerrainRescue>() == null)
                root.AddComponent<EnemyTerrainRescue>();

            EnemyInvectorRagdollSetup.EnsurePresent(root);
            if (root.GetComponent<EnemyInvectorRagdollBridge>() == null)
                root.AddComponent<EnemyInvectorRagdollBridge>();
            if (root.GetComponent<EnemyInvectorDeathPresenter>() == null)
                root.AddComponent<EnemyInvectorDeathPresenter>();
            if (root.GetComponent<EnemyInvectorPhysicsCache>() == null)
                root.AddComponent<EnemyInvectorPhysicsCache>();
            if (root.GetComponent<HumanoidPerformanceController>() == null)
                root.AddComponent<HumanoidPerformanceController>();
            EnemyInvectorBodySnapSetup.ApplyRuntime(root);

            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                root.layer = enemyLayer;
        }

        private static void ConfigureHumanoidLoadout(GameObject root, EnemyDefinition definition)
        {
            EnemyInvectorLoadoutBridge loadout = root.GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null || definition == null)
                return;

            SerializedObject serialized = new SerializedObject(loadout);
            SerializedProperty melee = serialized.FindProperty("meleeWeaponItem");
            SerializedProperty ranged = serialized.FindProperty("rangedWeaponItem");
            SerializedProperty prefer = serialized.FindProperty("preferRangedAtRange");
            SerializedProperty startMelee = serialized.FindProperty("startWithMeleeWeapon");

            if (melee != null)
                melee.objectReferenceValue = definition.meleeWeaponItem;
            if (ranged != null)
                ranged.objectReferenceValue = definition.rangedWeaponItem;
            if (prefer != null)
                prefer.boolValue = definition.preferRangedWeapon;
            if (startMelee != null)
                startMelee.boolValue = !definition.preferRangedWeapon;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AddDamageReceiversToRoot(GameObject root)
        {
            if (root == null)
                return;

            Collider rootCollider = root.GetComponent<Collider>();
            if (rootCollider == null || rootCollider.isTrigger)
                return;

            if (root.GetComponent<PioneerInvectorDamageReceiver>() == null)
                root.AddComponent<PioneerInvectorDamageReceiver>();
        }

        private static void ApplyHumanoidPhysics(GameObject root, EnemyDefinition definition)
        {
            if (root == null)
                return;

            float radius = definition != null ? definition.colliderRadius : 0.45f;
            float height = definition != null ? definition.colliderHeight : 2f;
            Vector3 center = definition != null ? definition.colliderCenter : new Vector3(0f, 1f, 0f);
            bool fit = definition == null || definition.fitColliderToRenderers;
            EnemyInvectorHitSetup.Apply(root, radius, height, center, fit);
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(root);
        }

        public static void AttachVisualModel(GameObject root, GameObject visualSource, string visualChildName)
        {
            if (root == null || visualSource == null)
                return;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform existing = root.transform.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance == null)
                visualInstance = Object.Instantiate(visualSource);

            visualInstance.name = childName;
            visualInstance.transform.SetParent(root.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;
        }

        public static GameObject ExtractVisualSource(GameObject enemyPrefab, string visualChildName)
        {
            if (enemyPrefab == null)
                return null;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform visual = enemyPrefab.transform.Find(childName);
            if (visual != null)
                return visual.gameObject;

            if (enemyPrefab.transform.childCount > 0)
                return enemyPrefab.transform.GetChild(0).gameObject;

            return enemyPrefab;
        }

        public static string SuggestVisualChildName(GameObject model)
        {
            if (model == null)
                return "Visual";

            string modelName = model.name;
            if (modelName == "scene" || modelName == "Visual")
                return modelName;

            return "Visual";
        }
    }
}
#endif
