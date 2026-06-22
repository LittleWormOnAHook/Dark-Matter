using System.IO;
using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyPrefabBuilder
    {
        public enum VisualSourceMode
        {
            PlaceholderCapsule,
            SelectedHierarchyObject,
            ExistingPrefab
        }

        public static GameObject BuildEnemy(
            EnemyDefinition definition,
            VisualSourceMode sourceMode,
            GameObject sourceObject,
            out string prefabPath)
        {
            definition ??= ScriptableObject.CreateInstance<EnemyDefinition>();
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);

            string fileName = SanitizeFileName(definition.prefabFileName, definition.displayName);
            prefabPath = $"{ProjectAssetPaths.PrefabsCombat}/{fileName}.prefab";

            GameObject root = CreateVisualRoot(definition, sourceMode, sourceObject);
            ApplyGameplayComponents(root, definition);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            EditorLayoutGuard.BeforeDestroySceneObject(root);
            Object.DestroyImmediate(root);
            EditorLayoutGuard.ScheduleInspectorRecovery();

            return prefab;
        }

        public static GameObject PlacePrefabInScene(GameObject prefab, string instanceName, Vector3 position)
        {
            if (prefab == null)
                return null;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;

            instance.name = string.IsNullOrWhiteSpace(instanceName) ? prefab.name : instanceName;
            instance.transform.position = position;
            Undo.RegisterCreatedObjectUndo(instance, "Place Enemy");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return instance;
        }

        public static GameObject CreatePreviewRoot(
            EnemyDefinition definition,
            VisualSourceMode sourceMode,
            GameObject sourceObject)
        {
            return CreateVisualRoot(definition, sourceMode, sourceObject);
        }

        private static GameObject CreateVisualRoot(
            EnemyDefinition definition,
            VisualSourceMode sourceMode,
            GameObject sourceObject)
        {
            switch (sourceMode)
            {
                case VisualSourceMode.SelectedHierarchyObject:
                    if (sourceObject != null)
                        return InstantiateHierarchySource(sourceObject, definition.displayName);
                    break;

                case VisualSourceMode.ExistingPrefab:
                    if (sourceObject != null && PrefabUtility.IsPartOfPrefabAsset(sourceObject))
                    {
                        GameObject instance = PrefabUtility.InstantiatePrefab(sourceObject) as GameObject;
                        if (instance != null)
                        {
                            instance.name = definition.displayName;
                            return instance;
                        }
                    }
                    break;
            }

            return CreatePlaceholderRoot(definition.displayName);
        }

        private static GameObject InstantiateHierarchySource(GameObject source, string displayName)
        {
            GameObject clone = Object.Instantiate(source);
            clone.name = string.IsNullOrWhiteSpace(displayName) ? source.name : displayName;
            clone.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            clone.transform.localScale = source.transform.lossyScale;
            return clone;
        }

        private static GameObject CreatePlaceholderRoot(string displayName)
        {
            GameObject root = new GameObject(string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            return root;
        }

        public static void ApplyGameplayComponents(GameObject root, EnemyDefinition definition)
        {
            EnsureCollider(root, definition);

            EnemyHealth health = GetOrAdd<EnemyHealth>(root);
            EnemySenses senses = GetOrAdd<EnemySenses>(root);
            EnemyCombat combat = GetOrAdd<EnemyCombat>(root);
            EnemyAiController ai = GetOrAdd<EnemyAiController>(root);

            SetSerializedField(health, "maxHealth", definition.maxHealth);
            SetSerializedField(health, "destroyOnDeath", definition.destroyOnDeath);
            SetSerializedField(health, "destroyDelay", definition.destroyDelay);
            SetSerializedField(health, "respawnTime", definition.respawnTime);
            SetSerializedField(health, "healthBarOffset", definition.healthBarOffset);

            SetSerializedField(senses, "visionRange", definition.visionRange);
            SetSerializedField(senses, "visionFov", definition.visionFov);
            SetSerializedField(senses, "eyeHeight", definition.eyeHeight);
            SetSerializedField(senses, "hearingRange", definition.hearingRange);
            SetSerializedField(senses, "proximityRange", definition.proximityRange);

            SetSerializedField(combat, "attackRange", definition.attackRange);
            SetSerializedField(combat, "attackDamage", definition.attackDamage);
            SetSerializedField(combat, "attackCooldown", definition.attackCooldown);
            SetSerializedField(combat, "attackWindup", definition.attackWindup);

            SetSerializedField(ai, "movementMode", (int)definition.movementMode);
            SetSerializedField(ai, "patrolMode", (int)definition.patrolMode);
            SetSerializedField(ai, "investigateNoise", definition.investigateNoise);
            SetSerializedField(ai, "chasePlayer", definition.chasePlayer);
            SetSerializedField(ai, "returnToHomeAfterSearch", definition.returnToHomeAfterSearch);
            SetSerializedField(ai, "chaseRadius", definition.chaseRadius);
            SetSerializedField(ai, "wanderRadius", definition.wanderRadius);
            SetSerializedField(ai, "wanderPauseMin", definition.wanderPauseMin);
            SetSerializedField(ai, "wanderPauseMax", definition.wanderPauseMax);
            SetSerializedField(ai, "walkSpeed", definition.walkSpeed);
            SetSerializedField(ai, "runSpeed", definition.runSpeed);
            SetSerializedField(ai, "turnSpeed", definition.turnSpeed);
            SetSerializedField(ai, "loseTargetDelay", definition.loseTargetDelay);
            SetSerializedField(ai, "searchDuration", definition.searchDuration);
            SetSerializedField(ai, "searchRadius", definition.searchRadius);
            SetSerializedField(ai, "idleDuration", definition.idleDuration);
            SetSerializedField(ai, "patrolWaitDuration", definition.patrolWaitDuration);

            ConfigurePatrolPoints(root, ai, definition);
            ConfigureHealthBar(root, definition);
            ConfigureAnimation(root, definition);
            ConfigureLoot(root, definition);
        }

        public static void ApplyLootToPrefab(GameObject root, EnemyDefinition definition)
        {
            ConfigureLoot(root, definition);
        }

        private static void ConfigureLoot(GameObject root, EnemyDefinition definition)
        {
            EnemyLootable lootable = GetOrAdd<EnemyLootable>(root);
            SetSerializedField(lootable, "enableLoot", definition.enableLoot);
            SetSerializedField(lootable, "lootDisplayName", definition.displayName);
            SetSerializedField(lootable, "piCoinsMin", definition.piCoinsMin);
            SetSerializedField(lootable, "piCoinsMax", definition.piCoinsMax);
            SetSerializedField(lootable, "randomLootCountMin", definition.randomLootCountMin);
            SetSerializedField(lootable, "randomLootCountMax", definition.randomLootCountMax);
            SetSerializedField(lootable, "lootItemPool", definition.lootItemPool);
            SetSerializedField(lootable, "lootRespawnDelay", definition.lootRespawnDelay);
            SetSerializedField(lootable, "lootInteractRange", definition.lootInteractRange);
        }

        private static void ConfigurePatrolPoints(GameObject root, EnemyAiController ai, EnemyDefinition definition)
        {
            if (definition.movementMode != EnemyMovementMode.Patrol || definition.patrolPointCount <= 0)
                return;

            Transform existingRoot = root.transform.Find("PatrolPoints");
            if (existingRoot != null)
                Object.DestroyImmediate(existingRoot.gameObject);

            GameObject patrolRoot = new GameObject("PatrolPoints");
            patrolRoot.transform.SetParent(root.transform, false);

            Transform[] points = new Transform[definition.patrolPointCount];
            for (int i = 0; i < definition.patrolPointCount; i++)
            {
                float angle = (Mathf.PI * 2f / definition.patrolPointCount) * i;
                Vector3 localOffset = new Vector3(
                    Mathf.Cos(angle) * definition.patrolRadius,
                    0f,
                    Mathf.Sin(angle) * definition.patrolRadius);

                GameObject pointObject = new GameObject($"PatrolPoint_{i + 1:00}");
                pointObject.transform.SetParent(patrolRoot.transform, false);
                pointObject.transform.localPosition = localOffset;
                points[i] = pointObject.transform;
            }

            SetSerializedField(ai, "patrolPoints", points);
        }

        private static void ConfigureHealthBar(GameObject root, EnemyDefinition definition)
        {
            EnemyHealthBarPresenter presenter = root.GetComponent<EnemyHealthBarPresenter>();
            if (!definition.showFloatingHealthBar)
            {
                if (presenter != null)
                    Object.DestroyImmediate(presenter);
                return;
            }

            presenter = GetOrAdd<EnemyHealthBarPresenter>(root);
            SetSerializedField(presenter, "showFloatingHealthBar", true);
            SetSerializedField(presenter, "hideUntilDamaged", definition.hideHealthBarUntilDamaged);
            SetSerializedField(presenter, "healthBarOffset", definition.healthBarOffset);
        }

        private static void ConfigureAnimation(GameObject root, EnemyDefinition definition)
        {
            EnemyAnimationBuilder.BuiltAnimationSet builtSet = default;

            if (definition.buildAnimatorFromClips && EnemyAnimationBuilder.HasClipAssignments(definition))
                builtSet = EnemyAnimationSetupUtility.RebuildAnimationTree(definition);

            if (definition.animatorController == null && builtSet.Controller == null)
                return;

            EnemyAnimationSetupUtility.ApplyAnimationToGameObject(root, definition, builtSet);
        }

        private static void EnsureCollider(GameObject root, EnemyDefinition definition)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = root.AddComponent<CapsuleCollider>();

            capsule.isTrigger = false;

            if (definition.fitColliderToRenderers && TryFitCapsuleToRenderers(root, capsule))
                return;

            capsule.center = definition.colliderCenter;
            capsule.radius = definition.colliderRadius;
            capsule.height = definition.colliderHeight;
        }

        private static bool TryFitCapsuleToRenderers(GameObject root, CapsuleCollider capsule)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 lossyScale = root.transform.lossyScale;
            float horizontalScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z), 0.001f);
            float verticalScale = Mathf.Max(Mathf.Abs(lossyScale.y), 0.001f);

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            capsule.center = localCenter;
            capsule.height = Mathf.Max(0.5f / verticalScale, bounds.size.y / verticalScale);
            capsule.radius = Mathf.Max(0.2f / horizontalScale, Mathf.Max(bounds.size.x, bounds.size.z) * 0.2f / horizontalScale);
            return true;
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
                component = root.AddComponent<T>();
            return component;
        }

        private static void SetSerializedField(Object target, string propertyName, object value)
        {
            if (target == null)
                return;

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            if (property.propertyType == SerializedPropertyType.Enum && value is int enumIndex)
            {
                property.enumValueIndex = enumIndex;
            }
            else switch (value)
            {
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
                case int intValue:
                    property.intValue = intValue;
                    break;
                case bool boolValue:
                    property.boolValue = boolValue;
                    break;
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case Vector3 vectorValue:
                    property.vector3Value = vectorValue;
                    break;
                case Object objectValue:
                    property.objectReferenceValue = objectValue;
                    break;
                case Transform[] transforms:
                    property.arraySize = transforms.Length;
                    for (int i = 0; i < transforms.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
                    break;
                case string[] strings:
                    property.arraySize = strings.Length;
                    for (int i = 0; i < strings.Length; i++)
                        property.GetArrayElementAtIndex(i).stringValue = strings[i] ?? string.Empty;
                    break;
                case ItemData[] itemDataArray:
                    property.arraySize = itemDataArray.Length;
                    for (int i = 0; i < itemDataArray.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = itemDataArray[i];
                    break;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static string SanitizeFileName(string preferred, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "Enemy";

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char character in invalid)
                raw = raw.Replace(character, '_');

            return raw.Replace(' ', '_');
        }

        public static EnemyDefinition[] LoadAllDefinitions()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
            EnemyDefinition[] definitions = new EnemyDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                definitions[i] = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            }

            return definitions;
        }

        public static Vector3 ResolveSpawnPosition()
        {
            if (Selection.activeTransform != null)
                return Selection.activeTransform.position;

            GameObject questGiver = GameObject.Find("QuestGiver_PioneerGuide");
            if (questGiver != null)
                return questGiver.transform.position + new Vector3(0f, 0f, 12f);

            if (Camera.main != null)
                return Camera.main.transform.position + Camera.main.transform.forward * 8f;

            return Vector3.zero;
        }
    }
}
