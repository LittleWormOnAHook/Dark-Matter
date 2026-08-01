using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Data;
using Project.EditorTools.Invector;
using Project.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools
{
    public class EnemyPrefabCreatorWindow : EditorWindow
    {
        private EnemyDefinition[] definitionAssets = System.Array.Empty<EnemyDefinition>();
        private int selectedDefinitionIndex = -1;

        private EnemyDefinition workingDefinition;

        private VisualSourceMode visualSourceMode = VisualSourceMode.SelectedHierarchyObject;
        private GameObject selectedVisualSource;
        private GameObject existingPrefabSource;
        private GameObject humanoidMeshSource;
        private bool placeInSceneAfterCreate = true;
        private string definitionAssetFileName = "new_enemy";
        private PathCreator patrolPathCreator;

        private Vector2 listScroll;
        private Vector2 editorScroll;

        private enum VisualSourceMode
        {
            SelectedHierarchyObject,
            PlaceholderCapsule,
            ExistingPrefab
        }

        [MenuItem(SurvivalPioneerEditorMenus.EnemyPrefabCreator, false, 12)]
        public static void Open()
        {
            EnemyPrefabCreatorWindow window = GetWindow<EnemyPrefabCreatorWindow>("Enemy Prefab Creator");
            window.minSize = new Vector2(860f, 620f);
        }

        private void OnEnable()
        {
            RefreshDefinitionList();
            EnsureWorkingDefinition();
        }

        private void OnDisable()
        {
            EnemyAnimationPreviewSession.Stop();
        }

        private void RefreshDefinitionList()
        {
            definitionAssets = EnemyPrefabBuilder.LoadAllDefinitions();
        }

        private void EnsureWorkingDefinition()
        {
            if (workingDefinition != null)
                return;

            StartNewDefinition();
        }

        private void OnGUI()
        {
            EnsureWorkingDefinition();

            EditorGUILayout.LabelField("Enemy Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Humanoid Meshy/character FBXs (Android, etc.): set Archetype = HumanoidInvector, assign Model FBX, Create Prefab. " +
                "Creator swaps the root Animator avatar, hides the stock VBOT body, and bakes AI/health/ragdoll. " +
                "Generic/creature FBXs: use LegacyCreature + Animation Pipeline clips. " +
                "Menu: Tools → Dark Matter Genesis → Prefab Creator → Enemy Prefab Creator.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawDefinitionListPanel();
            DrawEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField("Enemy Definitions", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < definitionAssets.Length; i++)
            {
                EnemyDefinition asset = definitionAssets[i];
                if (asset == null)
                    continue;

                string label = string.IsNullOrEmpty(asset.displayName) ? asset.name : asset.displayName;
                bool selected = i == selectedDefinitionIndex;
                if (GUILayout.Toggle(selected, label, "Button") && selectedDefinitionIndex != i)
                    LoadDefinition(asset, i);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Enemy", GUILayout.Height(28f)))
                StartNewDefinition();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                RefreshDefinitionList();

            EditorGUILayout.EndVertical();
        }

        private void DrawEditorPanel()
        {
            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            DrawIdentitySection();
            EditorGUILayout.Space(8f);
            DrawVisualSourceSection();
            EditorGUILayout.Space(8f);
            DrawBehaviorPresetSection();
            EditorGUILayout.Space(8f);
            DrawMovementModeSection();
            EditorGUILayout.Space(8f);
            if (workingDefinition.archetype != EnemyArchetype.HumanoidInvector)
                DrawAnimationSection();
            else
                EditorGUILayout.HelpBox(
                    "Humanoid Invector enemies use the Player_Invector animator controller with the assigned model Avatar. " +
                    "Assign Model FBX (Meshy Humanoid), optional melee/ranged ItemData, then Create/Rebuild.",
                    MessageType.Info);
            EditorGUILayout.Space(8f);
            DrawLootSection();
            EditorGUILayout.Space(8f);
            DrawDefinitionFields();
            EditorGUILayout.Space(12f);
            DrawSpawnReadyStatus();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            workingDefinition.enemyId = EditorGUILayout.TextField("Enemy Id", workingDefinition.enemyId);
            workingDefinition.displayName = EditorGUILayout.TextField("Display Name", workingDefinition.displayName);
            workingDefinition.prefabFileName = EditorGUILayout.TextField("Prefab File Name", workingDefinition.prefabFileName);
            workingDefinition.archetype = (EnemyArchetype)EditorGUILayout.EnumPopup("Archetype", workingDefinition.archetype);
            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
            {
                workingDefinition.meleeWeaponItem = (ItemData)EditorGUILayout.ObjectField(
                    "Melee Weapon Item",
                    workingDefinition.meleeWeaponItem,
                    typeof(ItemData),
                    false);
                workingDefinition.rangedWeaponItem = (ItemData)EditorGUILayout.ObjectField(
                    "Ranged Weapon Item",
                    workingDefinition.rangedWeaponItem,
                    typeof(ItemData),
                    false);
                workingDefinition.preferRangedWeapon = EditorGUILayout.Toggle(
                    "Prefer Ranged",
                    workingDefinition.preferRangedWeapon);
            }

            definitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", definitionAssetFileName);
        }

        private void DrawVisualSourceSection()
        {
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && humanoidMeshSource != null)
                ApplyModelAutoDetect(humanoidMeshSource);

            DrawModelInspectionPanel(humanoidMeshSource);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = humanoidMeshSource != null;
            if (GUILayout.Button("Prepare Model Import", GUILayout.Height(22f)))
                PrepareAssignedModelImport();
            if (GUILayout.Button("Auto-Detect Archetype", GUILayout.Height(22f)))
                ApplyModelAutoDetect(humanoidMeshSource);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            visualSourceMode = (VisualSourceMode)EditorGUILayout.EnumPopup("Source Mode", visualSourceMode);

            switch (visualSourceMode)
            {
                case VisualSourceMode.SelectedHierarchyObject:
                    selectedVisualSource = (GameObject)EditorGUILayout.ObjectField(
                        "Hierarchy Model",
                        selectedVisualSource != null ? selectedVisualSource : Selection.activeGameObject,
                        typeof(GameObject),
                        true);
                    if (selectedVisualSource == null && Selection.activeGameObject != null)
                        selectedVisualSource = Selection.activeGameObject;
                    break;

                case VisualSourceMode.ExistingPrefab:
                    existingPrefabSource = (GameObject)EditorGUILayout.ObjectField(
                        "Prefab Asset",
                        existingPrefabSource != null ? existingPrefabSource : humanoidMeshSource,
                        typeof(GameObject),
                        false);
                    break;

                case VisualSourceMode.PlaceholderCapsule:
                    EditorGUILayout.HelpBox(
                        "Creates a simple capsule placeholder. Prefer assigning Model FBX for real characters.",
                        MessageType.None);
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Current Selection", GUILayout.Width(180f)))
            {
                if (Selection.activeGameObject != null)
                {
                    selectedVisualSource = Selection.activeGameObject;
                    visualSourceMode = VisualSourceMode.SelectedHierarchyObject;
                }
            }

            if (GUILayout.Button("Use Model FBX As Source", GUILayout.Width(180f)))
            {
                if (humanoidMeshSource != null)
                {
                    existingPrefabSource = humanoidMeshSource;
                    visualSourceMode = VisualSourceMode.ExistingPrefab;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
            {
                EditorGUILayout.Space(4f);
                if (humanoidMeshSource != null && string.IsNullOrWhiteSpace(workingDefinition.visualChildName))
                    workingDefinition.visualChildName =
                        EnemyInvectorSetupUtility.SuggestVisualChildName(humanoidMeshSource);

                workingDefinition.visualChildName = EditorGUILayout.TextField(
                    "Visual Child Name",
                    workingDefinition.visualChildName);

                if (GUILayout.Button("Rebuild With Model", GUILayout.Height(24f)))
                    RebuildHumanoidPrefabWithModel();
            }

            placeInSceneAfterCreate = EditorGUILayout.Toggle("Place In Open Scene After Create", placeInSceneAfterCreate);
        }

        private void DrawModelInspectionPanel(GameObject model)
        {
            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            MessageType messageType = MessageType.None;
            if (!inspection.HasModel)
                messageType = MessageType.None;
            else if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid && inspection.LooksHumanoidSized)
                messageType = MessageType.Info;
            else if (inspection.HasModel)
                messageType = MessageType.Warning;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Model Inspection", EditorStyles.miniBoldLabel);
            if (!inspection.HasModel)
            {
                EditorGUILayout.LabelField("Assign a Meshy/character FBX to inspect rig, avatar, and scale.");
            }
            else
            {
                EditorGUILayout.LabelField(inspection.Summary);
                if (!string.IsNullOrEmpty(inspection.AssetPath))
                    EditorGUILayout.LabelField(inspection.AssetPath, EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(inspection.Recommendation, messageType);
            }
            EditorGUILayout.EndVertical();
        }

        private void PrepareAssignedModelImport()
        {
            if (humanoidMeshSource == null)
                return;

            string path = AssetDatabase.GetAssetPath(humanoidMeshSource);
            if (string.IsNullOrEmpty(path))
                path = EnemyModelAvatarUtility.FindPrimaryModelAssetPath(humanoidMeshSource);

            if (!EnemyModelAvatarUtility.TryPrepareModelImport(path, out string message))
            {
                EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
                return;
            }

            humanoidMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ApplyModelAutoDetect(humanoidMeshSource);
            EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
        }

        private void ApplyModelAutoDetect(GameObject model)
        {
            if (model == null || workingDefinition == null)
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid)
            {
                workingDefinition.archetype = EnemyArchetype.HumanoidInvector;
                if (string.IsNullOrWhiteSpace(workingDefinition.visualChildName))
                    workingDefinition.visualChildName = "Visual";
            }
            else if (inspection.AnimationType == ModelImporterAnimationType.Generic ||
                     inspection.HasModel)
            {
                workingDefinition.archetype = EnemyArchetype.LegacyCreature;
            }

            existingPrefabSource = model;
            visualSourceMode = VisualSourceMode.ExistingPrefab;

            if (string.IsNullOrWhiteSpace(workingDefinition.displayName) ||
                workingDefinition.displayName == "New Enemy")
            {
                workingDefinition.displayName = model.name.Replace('_', ' ');
            }

            if (string.IsNullOrWhiteSpace(workingDefinition.prefabFileName) ||
                workingDefinition.prefabFileName == "NewEnemy")
            {
                workingDefinition.prefabFileName = EnemyPrefabBuilder.SanitizeFileName(model.name, "Enemy");
            }
        }

        private void RebuildHumanoidPrefabWithModel()
        {
            GameObject mesh = humanoidMeshSource != null ? humanoidMeshSource : ResolveVisualSource(out _);
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", "Assign a model FBX/prefab to rebuild.", "OK");
                return;
            }

            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";

            if (!EnemyInvectorSetupUtility.RebuildHumanoidEnemyAtPath(prefabPath, workingDefinition, mesh))
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", $"Could not rebuild {prefabPath}.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Rebuilt humanoid enemy at {prefabPath}");
        }

        private void DrawBehaviorPresetSection()
        {
            EditorGUILayout.LabelField("AI Preset", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            workingDefinition.behaviorPreset = (EnemyBehaviorPreset)EditorGUILayout.EnumPopup(
                "Behavior Preset",
                workingDefinition.behaviorPreset);
            if (EditorGUI.EndChangeCheck() && workingDefinition.behaviorPreset != EnemyBehaviorPreset.Custom)
                workingDefinition.ApplyBehaviorPreset(workingDefinition.behaviorPreset);

            if (GUILayout.Button("Apply Preset Values", GUILayout.Height(24f)) &&
                workingDefinition.behaviorPreset != EnemyBehaviorPreset.Custom)
            {
                workingDefinition.ApplyBehaviorPreset(workingDefinition.behaviorPreset);
            }
        }

        private void DrawMovementModeSection()
        {
            EditorGUILayout.LabelField("Movement & Behavior", EditorStyles.boldLabel);
            workingDefinition.movementMode = (EnemyMovementMode)EditorGUILayout.EnumPopup(
                "Movement Mode",
                workingDefinition.movementMode);
            workingDefinition.patrolMode = (EnemyPatrolMode)EditorGUILayout.EnumPopup(
                "Patrol Mode",
                workingDefinition.patrolMode);
            workingDefinition.investigateNoise = EditorGUILayout.Toggle("Investigate Noise", workingDefinition.investigateNoise);
            workingDefinition.chasePlayer = EditorGUILayout.Toggle("Chase Player", workingDefinition.chasePlayer);
            workingDefinition.returnToHomeAfterSearch = EditorGUILayout.Toggle(
                "Return Home After Search",
                workingDefinition.returnToHomeAfterSearch);
            workingDefinition.chaseRadius = EditorGUILayout.FloatField(
                "Chase Radius",
                workingDefinition.chaseRadius);
            EditorGUILayout.HelpBox(
                "Max distance from spawn/home to pursue the player. Beyond this, the enemy gives up and returns home. 0 = unlimited.",
                MessageType.None);

            EnemyMovementMode mode = workingDefinition.movementMode;
            if (mode == EnemyMovementMode.Wander)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Wander Area", EditorStyles.miniBoldLabel);
                workingDefinition.wanderRadius = EditorGUILayout.FloatField("Wander Radius", workingDefinition.wanderRadius);
                workingDefinition.wanderPauseMin = EditorGUILayout.FloatField("Wander Pause Min", workingDefinition.wanderPauseMin);
                workingDefinition.wanderPauseMax = EditorGUILayout.FloatField("Wander Pause Max", workingDefinition.wanderPauseMax);
            }

            if (mode == EnemyMovementMode.Patrol)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Patrol Route", EditorStyles.miniBoldLabel);
                patrolPathCreator = (PathCreator)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Path Creator",
                        "Path Creator or Path Creator Variant. Apply writes onto selected scene AI / placed instance; persistent path assets also bake onto selected prefab assets."),
                    patrolPathCreator,
                    typeof(PathCreator),
                    true);
                if (GUILayout.Button("Apply Patrol Path To Selection / Prefab"))
                    ApplyPatrolPathToEnemyTargets();

                workingDefinition.patrolPointCount = EditorGUILayout.IntField("Patrol Point Count", workingDefinition.patrolPointCount);
                workingDefinition.patrolRadius = EditorGUILayout.FloatField("Patrol Radius", workingDefinition.patrolRadius);
                workingDefinition.patrolWaitDuration = EditorGUILayout.FloatField(
                    "Patrol Wait Duration",
                    workingDefinition.patrolWaitDuration);
                EditorGUILayout.HelpBox(
                    "Preferred: place Path Creator Variant, edit anchors with Path Creator's native Scene tools only, " +
                    "then assign here. Create/Rebuild with Place in Scene applies the path to the instance. " +
                    "Fallback: circle PatrolPoints when no path is set.",
                    MessageType.Info);
            }

            if (mode == EnemyMovementMode.Stationary)
            {
                EditorGUILayout.HelpBox(
                    "Stationary enemies hold position but can still chase, investigate noise, and return home when configured.",
                    MessageType.None);
            }
        }

        private void DrawLootSection()
        {
            EditorGUILayout.LabelField("Loot", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Dead enemies pause respawn for the loot delay (or until fully looted). " +
                "Press E on the corpse to open the loot menu. Leave the item pool empty to roll random items from the item registry.",
                MessageType.Info);

            workingDefinition.enableLoot = EditorGUILayout.Toggle("Enable Loot", workingDefinition.enableLoot);
            workingDefinition.acDropMin = EditorGUILayout.IntField("AC Drop Min", workingDefinition.acDropMin);
            workingDefinition.acDropMax = EditorGUILayout.IntField("AC Drop Max", workingDefinition.acDropMax);
            workingDefinition.randomLootCountMin = EditorGUILayout.IntField("Random Items Min", workingDefinition.randomLootCountMin);
            workingDefinition.randomLootCountMax = EditorGUILayout.IntField("Random Items Max", workingDefinition.randomLootCountMax);
            workingDefinition.lootRespawnDelay = EditorGUILayout.FloatField("Loot Respawn Delay", workingDefinition.lootRespawnDelay);
            workingDefinition.lootInteractRange = EditorGUILayout.FloatField("Loot Interact Range", workingDefinition.lootInteractRange);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Loot Item Pool (optional)", EditorStyles.miniBoldLabel);
            DrawItemPoolArray(ref workingDefinition.lootItemPool);

            if (GUILayout.Button("Apply Loot To Existing Prefab", GUILayout.Height(26f)))
                ApplyLootToExistingPrefab();
        }

        private static void DrawItemPoolArray(ref ItemData[] items)
        {
            int count = EditorGUILayout.IntField("Pool Count", items?.Length ?? 0);
            if (count < 0)
                count = 0;

            if (items == null || items.Length != count)
                System.Array.Resize(ref items, count);

            for (int i = 0; i < count; i++)
            {
                items[i] = (ItemData)EditorGUILayout.ObjectField(
                    $"  Item {i + 1}",
                    items[i],
                    typeof(ItemData),
                    false);
            }
        }

        private void ApplyLootToExistingPrefab()
        {
            EnsureWorkingDefinition();
            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    $"Prefab not found at {prefabPath}. Create the prefab first.",
                    "OK");
                return;
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (instance == null)
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", "Could not open prefab for editing.", "OK");
                return;
            }

            EnemyPrefabBuilder.ApplyLootToPrefab(instance, workingDefinition);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            AssetDatabase.SaveAssets();
            Debug.Log($"Applied loot setup to {prefabPath}");
        }

        private void DrawDefinitionFields()
        {
            EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
            workingDefinition.maxHealth = EditorGUILayout.FloatField("Max Health", workingDefinition.maxHealth);
            workingDefinition.destroyOnDeath = EditorGUILayout.Toggle("Destroy On Death", workingDefinition.destroyOnDeath);
            workingDefinition.destroyDelay = EditorGUILayout.FloatField("Destroy Delay", workingDefinition.destroyDelay);
            workingDefinition.respawnTime = EditorGUILayout.FloatField("Respawn Time", workingDefinition.respawnTime);
            EditorGUILayout.HelpBox(
                "Respawn Time > 0 respawns the enemy at its spawn point after death. Destroy On Death is ignored while respawning is enabled.",
                MessageType.None);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Health Bar", EditorStyles.boldLabel);
            workingDefinition.showFloatingHealthBar = EditorGUILayout.Toggle(
                "Show Floating Health Bar",
                workingDefinition.showFloatingHealthBar);
            workingDefinition.hideHealthBarUntilDamaged = EditorGUILayout.Toggle(
                "Hide Until Damaged",
                workingDefinition.hideHealthBarUntilDamaged);
            workingDefinition.healthBarOffset = EditorGUILayout.Vector3Field(
                "Health Bar Offset",
                workingDefinition.healthBarOffset);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Senses", EditorStyles.boldLabel);
            workingDefinition.visionRange = EditorGUILayout.FloatField("Vision Range", workingDefinition.visionRange);
            workingDefinition.visionFov = EditorGUILayout.FloatField("Vision Fov", workingDefinition.visionFov);
            workingDefinition.eyeHeight = EditorGUILayout.FloatField("Eye Height", workingDefinition.eyeHeight);
            workingDefinition.senseHearingEnabled = EditorGUILayout.Toggle(
                "Hearing Enabled",
                workingDefinition.senseHearingEnabled);
            workingDefinition.hearingRange = EditorGUILayout.FloatField("Hearing Range", workingDefinition.hearingRange);
            workingDefinition.hearingAggroChance = EditorGUILayout.Slider(
                "Hearing Aggro Chance",
                workingDefinition.hearingAggroChance,
                0f,
                1f);
            workingDefinition.hearingCooldown = EditorGUILayout.FloatField(
                "Hearing Cooldown",
                workingDefinition.hearingCooldown);
            workingDefinition.aggroOnDamaged = EditorGUILayout.Toggle(
                "Aggro On Damaged",
                workingDefinition.aggroOnDamaged);
            workingDefinition.aggroOnHeardHit = EditorGUILayout.Toggle(
                "Aggro On Heard Hit",
                workingDefinition.aggroOnHeardHit);
            workingDefinition.proximityRange = EditorGUILayout.FloatField("Proximity Range", workingDefinition.proximityRange);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Melee Combat", EditorStyles.boldLabel);
            workingDefinition.attackRange = EditorGUILayout.FloatField("Attack Range", workingDefinition.attackRange);
            workingDefinition.attackDamage = EditorGUILayout.FloatField("Attack Damage", workingDefinition.attackDamage);
            workingDefinition.attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", workingDefinition.attackCooldown);
            workingDefinition.attackWindup = EditorGUILayout.FloatField("Attack Windup", workingDefinition.attackWindup);
            workingDefinition.meleeDuration = EditorGUILayout.FloatField("Melee Duration", workingDefinition.meleeDuration);
            workingDefinition.unarmedDuration = EditorGUILayout.FloatField("Unarmed Duration", workingDefinition.unarmedDuration);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Ranged Combat", EditorStyles.boldLabel);
            workingDefinition.rangedEngageRange = EditorGUILayout.FloatField("Engage Range", workingDefinition.rangedEngageRange);
            workingDefinition.rangedAttackCooldown = EditorGUILayout.FloatField("Shot Cooldown", workingDefinition.rangedAttackCooldown);
            workingDefinition.rangedDuration = EditorGUILayout.FloatField("Shot Duration", workingDefinition.rangedDuration);
            workingDefinition.aimHoldDuration = EditorGUILayout.FloatField("Aim Hold Duration", workingDefinition.aimHoldDuration);
            workingDefinition.missRate = EditorGUILayout.Slider("Miss Rate", workingDefinition.missRate, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("AI Timing", EditorStyles.boldLabel);
            workingDefinition.walkSpeed = EditorGUILayout.FloatField("Walk Speed", workingDefinition.walkSpeed);
            workingDefinition.runSpeed = EditorGUILayout.FloatField("Run Speed", workingDefinition.runSpeed);
            workingDefinition.turnSpeed = EditorGUILayout.FloatField("Turn Speed", workingDefinition.turnSpeed);
            workingDefinition.loseTargetDelay = EditorGUILayout.FloatField("Lose Target Delay", workingDefinition.loseTargetDelay);
            workingDefinition.searchDuration = EditorGUILayout.FloatField("Search Duration", workingDefinition.searchDuration);
            workingDefinition.searchRadius = EditorGUILayout.FloatField("Search Radius", workingDefinition.searchRadius);
            workingDefinition.idleDuration = EditorGUILayout.FloatField("Idle Duration", workingDefinition.idleDuration);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
            workingDefinition.fitColliderToRenderers = EditorGUILayout.Toggle(
                "Fit Collider To Renderers",
                workingDefinition.fitColliderToRenderers);
            workingDefinition.colliderCenter = EditorGUILayout.Vector3Field("Collider Center", workingDefinition.colliderCenter);
            workingDefinition.colliderRadius = EditorGUILayout.FloatField("Collider Radius", workingDefinition.colliderRadius);
            workingDefinition.colliderHeight = EditorGUILayout.FloatField("Collider Height", workingDefinition.colliderHeight);
        }

        private void DrawAnimationSection()
        {
            EditorGUILayout.LabelField("Animation Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign Mixamo FBX files or AnimationClip assets, then rebuild the controller tree. " +
                "If the model has no avatar, configure humanoid rig on the source FBX import settings.",
                MessageType.Info);

            GameObject visualSource = ResolveVisualSource(out _);
            EnemyAnimationSetupUtility.AnimationSetupStatus status =
                EnemyAnimationSetupUtility.Analyze(workingDefinition, visualSource);

            DrawAnimationStatus(status);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Clip Assignments", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drag Mixamo .fbx files or clips. FBX imports resolve to the embedded mixamo.com clip automatically.",
                MessageType.None);

            DrawClipArray("Idle", ref workingDefinition.idleClips);
            EditorGUILayout.Space(4f);
            DrawClipArray("Walk", ref workingDefinition.walkClips);
            EditorGUILayout.Space(4f);
            DrawClipArray("Run", ref workingDefinition.runClips);
            EditorGUILayout.Space(4f);
            DrawClipArray("Combat / Attack", ref workingDefinition.attackClips);
            EditorGUILayout.Space(4f);
            DrawClipArray("Hit Reaction", ref workingDefinition.hitClips);
            EditorGUILayout.Space(4f);
            DrawClipArray("Death", ref workingDefinition.deathClips);

            EditorGUILayout.Space(6f);
            workingDefinition.buildAnimatorFromClips = EditorGUILayout.Toggle(
                "Build Animator From Clips",
                workingDefinition.buildAnimatorFromClips);
            workingDefinition.addEnemyAnimationController = EditorGUILayout.Toggle(
                "Add Generic Animation Controller",
                workingDefinition.addEnemyAnimationController);
            workingDefinition.animatorControllerFileName = EditorGUILayout.TextField(
                "Generated Controller Name",
                workingDefinition.animatorControllerFileName);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Manual Override", EditorStyles.miniBoldLabel);
            workingDefinition.animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                workingDefinition.animatorController,
                typeof(RuntimeAnimatorController),
                false);

            EditorGUILayout.Space(4f);
            workingDefinition.lockVisualRootPosition = EditorGUILayout.Toggle(
                "Lock Visual Root To Ground",
                workingDefinition.lockVisualRootPosition);
            workingDefinition.visualChildName = EditorGUILayout.TextField(
                "Visual Child Name",
                workingDefinition.visualChildName);

            EditorGUILayout.Space(8f);
            DrawAnimationActionButtons(status, visualSource);
        }

        private void DrawAnimationStatus(EnemyAnimationSetupUtility.AnimationSetupStatus status)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Setup Status", EditorStyles.miniBoldLabel);

            string clipSummary =
                $"Idle {status.IdleCount} | Walk {status.WalkCount} | Run {status.RunCount} | " +
                $"Attack {status.AttackCount} | Hit {status.HitCount} | Death {status.DeathCount}";
            EditorGUILayout.LabelField("Clips", clipSummary);

            string controllerLabel = status.HasBuiltController
                ? status.ControllerPath
                : "Not built yet";
            EditorGUILayout.LabelField("Controller", controllerLabel);

            MessageType avatarMessageType = status.HasAvatar ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(
                status.HasAvatar ? $"Avatar: {status.AvatarMessage}" : $"Avatar missing: {status.AvatarMessage}",
                avatarMessageType);
            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationActionButtons(
            EnemyAnimationSetupUtility.AnimationSetupStatus status,
            GameObject visualSource)
        {
            EditorGUILayout.LabelField("Animation Actions", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = status.HasAnyClips && workingDefinition.buildAnimatorFromClips;
            if (GUILayout.Button("Rebuild Animation Tree", GUILayout.Height(28f)))
                RebuildAnimationTree();
            GUI.enabled = true;

            if (GUILayout.Button("Open Controller", GUILayout.Height(28f)))
                EnemyAnimationSetupUtility.OpenControllerAsset(workingDefinition);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = visualSource != null && status.HasAnyClips;

            if (GUILayout.Button(EnemyAnimationPreviewSession.IsActive ? "Stop Preview" : "Start Preview", GUILayout.Height(28f)))
            {
                if (EnemyAnimationPreviewSession.IsActive)
                    EnemyAnimationPreviewSession.Stop();
                else
                    StartAnimationPreview();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (EnemyAnimationPreviewSession.IsActive)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Preview States", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Idle")) EnemyAnimationPreviewSession.PlayIdle(workingDefinition);
                if (GUILayout.Button("Walk")) EnemyAnimationPreviewSession.PlayWalk(workingDefinition);
                if (GUILayout.Button("Run")) EnemyAnimationPreviewSession.PlayRun(workingDefinition);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Attack")) EnemyAnimationPreviewSession.PlayAttack(workingDefinition);
                if (GUILayout.Button("Hit")) EnemyAnimationPreviewSession.PlayHit(workingDefinition);
                if (GUILayout.Button("Death")) EnemyAnimationPreviewSession.PlayDeath(workingDefinition);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply To Existing Prefab", GUILayout.Height(28f)))
                ApplyAnimationToExistingPrefab();
            if (GUILayout.Button("Clear All Clips", GUILayout.Height(28f)))
                ClearAnimationClips();
            EditorGUILayout.EndHorizontal();

            // Rebuild from ShooterMelee base — upgrades any existing controller.
            EditorGUILayout.Space(4f);
            AnimatorController existingCtrl = ResolveExistingController();
            GUI.enabled = existingCtrl != null;
            if (GUILayout.Button("Rebuild Controller from ShooterMelee Base", GUILayout.Height(28f)))
                RebuildFromShooterMeleeBase(existingCtrl);
            GUI.enabled = true;
        }

        private AnimatorController ResolveExistingController()
        {
            if (workingDefinition == null) return null;

            RuntimeAnimatorController rtc = workingDefinition.animatorController;
            if (rtc is AnimatorController ac) return ac;

            string fileName = EnemyPrefabBuilder.SanitizeFileName(
                string.IsNullOrWhiteSpace(workingDefinition.animatorControllerFileName)
                    ? workingDefinition.prefabFileName + "Controller"
                    : workingDefinition.animatorControllerFileName,
                workingDefinition.displayName + "Controller");
            string path = $"{ProjectAssetPaths.AnimationsEnemies}/{fileName}.controller";
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }

        private void RebuildFromShooterMeleeBase(AnimatorController target)
        {
            if (target == null) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Rebuild from ShooterMelee Base",
                $"Replace '{target.name}' with a full copy of Invector@ShooterMelee, " +
                "restoring its existing Base Layer states (Idle, Walk, Run, Attack, Hit, Death).\n\n" +
                "UpperBody, Shot, and OnlyArms layers will be replaced with ShooterMelee versions.\n\nContinue?",
                "Rebuild", "Cancel");

            if (!confirmed) return;

            string path = AssetDatabase.GetAssetPath(target);
            AnimatorController result = EnemyShooterControllerBuilder.RebuildFromShooterMeleeBase(path);
            EditorUtility.DisplayDialog("Rebuild from ShooterMelee Base",
                result != null
                    ? $"Done — '{result.name}' now uses ShooterMelee as its full base."
                    : "Failed — check the Console.",
                "OK");
        }

        private void RebuildAnimationTree()
        {
            EnsureWorkingDefinition();
            EnemyAnimationBuilder.BuiltAnimationSet builtSet =
                EnemyAnimationSetupUtility.RebuildAnimationTree(workingDefinition);
            if (builtSet.Controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    "Could not rebuild the animation tree. Assign at least one clip first.",
                    "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = builtSet.Controller;
            EditorGUIUtility.PingObject(builtSet.Controller);
            Debug.Log($"Rebuilt animation controller at {AssetDatabase.GetAssetPath(builtSet.Controller)}");
        }

        private void StartAnimationPreview()
        {
            if (!TryResolveBuilderSource(out EnemyPrefabBuilder.VisualSourceMode sourceMode, out GameObject source))
                return;

            if (!EnemyAnimationPreviewSession.Start(workingDefinition, source, sourceMode))
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    "Preview failed. Assign clips, ensure a visual source is available, and configure the model avatar on the FBX if needed.",
                    "OK");
            }
        }

        private void ApplyAnimationToExistingPrefab()
        {
            EnsureWorkingDefinition();
            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    $"Prefab not found at {prefabPath}. Create the prefab first.",
                    "OK");
                return;
            }

            if (!EnemyAnimationSetupUtility.ApplyAnimationToPrefabAsset(prefabPath, workingDefinition))
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    "Could not apply animation setup to the prefab. Assign clips or a controller first.",
                    "OK");
                return;
            }

            Debug.Log($"Applied animation setup to {prefabPath}");
        }

        private bool TryResolveBuilderSource(
            out EnemyPrefabBuilder.VisualSourceMode sourceMode,
            out GameObject source)
        {
            sourceMode = EnemyPrefabBuilder.VisualSourceMode.PlaceholderCapsule;
            source = null;

            // Model FBX field is the preferred one-click source for Meshy/character assets.
            if (humanoidMeshSource != null &&
                (visualSourceMode == VisualSourceMode.ExistingPrefab ||
                 visualSourceMode == VisualSourceMode.PlaceholderCapsule ||
                 workingDefinition.archetype == EnemyArchetype.HumanoidInvector))
            {
                // When a model asset is assigned, prefer it over empty hierarchy selection.
                if (visualSourceMode != VisualSourceMode.SelectedHierarchyObject ||
                    (selectedVisualSource == null && Selection.activeGameObject == null))
                {
                    source = humanoidMeshSource;
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.ExistingPrefab;
                    return true;
                }
            }

            switch (visualSourceMode)
            {
                case VisualSourceMode.PlaceholderCapsule:
                    if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
                    {
                        source = humanoidMeshSource;
                        sourceMode = EnemyPrefabBuilder.VisualSourceMode.ExistingPrefab;
                        if (source == null)
                        {
                            EditorUtility.DisplayDialog(
                                "Enemy Prefab Creator",
                                "Humanoid enemies need a model FBX/prefab — assign Model FBX / Prefab.",
                                "OK");
                            return false;
                        }
                        return true;
                    }

                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.PlaceholderCapsule;
                    return true;

                case VisualSourceMode.ExistingPrefab:
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.ExistingPrefab;
                    source = existingPrefabSource != null ? existingPrefabSource : humanoidMeshSource;
                    if (source == null)
                    {
                        EditorUtility.DisplayDialog(
                            "Enemy Prefab Creator",
                            "Assign a Model FBX / Prefab as the visual source.",
                            "OK");
                        return false;
                    }
                    return true;

                default:
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.SelectedHierarchyObject;
                    bool missing = false;
                    if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector && humanoidMeshSource != null)
                        source = humanoidMeshSource;
                    else
                        source = ResolveVisualSource(out missing);

                    if (source == null && humanoidMeshSource != null)
                    {
                        source = humanoidMeshSource;
                        sourceMode = EnemyPrefabBuilder.VisualSourceMode.ExistingPrefab;
                        missing = false;
                    }

                    if (source == null)
                        missing = true;

                    if (missing)
                    {
                        EditorUtility.DisplayDialog(
                            "Enemy Prefab Creator",
                            "Assign a Model FBX / Prefab, select a Hierarchy model, or switch source mode.",
                            "OK");
                        return false;
                    }
                    return true;
            }
        }

        private GameObject ResolveVisualSource(out bool missingHierarchySource)
        {
            missingHierarchySource = false;
            switch (visualSourceMode)
            {
                case VisualSourceMode.ExistingPrefab:
                    return existingPrefabSource;

                case VisualSourceMode.PlaceholderCapsule:
                    return null;

                default:
                    GameObject source = selectedVisualSource != null ? selectedVisualSource : Selection.activeGameObject;
                    missingHierarchySource = source == null;
                    return source;
            }
        }

        private static void DrawClipArray(string label, ref AnimationClip[] clips)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            int count = EditorGUILayout.IntField("Count", clips?.Length ?? 0);
            if (count < 0)
                count = 0;

            if (clips == null || clips.Length != count)
                System.Array.Resize(ref clips, count);

            for (int i = 0; i < count; i++)
            {
                Object reference = clips[i];
                reference = EditorGUILayout.ObjectField(
                    $"  Clip {i + 1}",
                    reference,
                    typeof(Object),
                    false);

                AnimationClip resolved = EnemyAnimationSetupUtility.ResolveClipReference(reference);
                if (resolved != clips[i])
                    clips[i] = resolved;
            }
        }

        private void ClearAnimationClips()
        {
            EnsureWorkingDefinition();
            workingDefinition.idleClips = System.Array.Empty<AnimationClip>();
            workingDefinition.walkClips = System.Array.Empty<AnimationClip>();
            workingDefinition.runClips = System.Array.Empty<AnimationClip>();
            workingDefinition.attackClips = System.Array.Empty<AnimationClip>();
            workingDefinition.hitClips = System.Array.Empty<AnimationClip>();
            workingDefinition.deathClips = System.Array.Empty<AnimationClip>();
        }

        private void DrawSpawnReadyStatus()
        {
            if (workingDefinition.archetype != EnemyArchetype.HumanoidInvector)
                return;

            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                EditorGUILayout.HelpBox("Prefab not created yet.", MessageType.None);
                return;
            }

            bool ready = EnemyPrefabResolver.IsSpawnReady(prefab);
            EditorGUILayout.HelpBox(
                ready
                    ? $"Spawn-ready: {prefab.name} has baked gameplay components."
                    : $"Prefab exists at {prefabPath} but needs a full rebuild.",
                ready ? MessageType.Info : MessageType.Warning);
        }

        private void DrawActionButtons()
        {
            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            string buildLabel = prefabExists ? "Rebuild Prefab" : "Create Prefab";
            string buildAndPlaceLabel = prefabExists ? "Rebuild + Place In Scene" : "Create Prefab + Place In Scene";

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition Asset", GUILayout.Height(30f)))
                SaveDefinitionAsset();

            if (GUILayout.Button(buildLabel, GUILayout.Height(30f)))
                CreateEnemyPrefab(false);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(buildAndPlaceLabel, GUILayout.Height(32f)))
                CreateEnemyPrefab(true);
        }

        private void StartNewDefinition()
        {
            selectedDefinitionIndex = -1;
            workingDefinition = CreateInstance<EnemyDefinition>();
            workingDefinition.enemyId = "new_enemy";
            workingDefinition.displayName = "New Enemy";
            workingDefinition.prefabFileName = "NewEnemy";
            workingDefinition.archetype = EnemyArchetype.HumanoidInvector;
            workingDefinition.visualChildName = "Visual";
            workingDefinition.ApplyBehaviorPreset(EnemyBehaviorPreset.AggressiveHunter);
            definitionAssetFileName = "new_enemy";
        }

        private void LoadDefinition(EnemyDefinition asset, int index)
        {
            if (asset == null)
            {
                StartNewDefinition();
                return;
            }

            selectedDefinitionIndex = index;
            workingDefinition = Instantiate(asset);
            workingDefinition.name = asset.name;
            definitionAssetFileName = asset.name;
        }

        private void SaveDefinitionAsset()
        {
            EnsureWorkingDefinition();

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);
            string fileName = EnemyPrefabBuilder.SanitizeFileName(definitionAssetFileName, workingDefinition.enemyId);
            string path = $"{ProjectAssetPaths.EnemiesData}/{fileName}.asset";

            EnemyDefinition existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(workingDefinition, path);
            }
            else
            {
                EditorUtility.CopySerialized(workingDefinition, existing);
                EditorUtility.SetDirty(existing);
                workingDefinition = existing;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitionList();
            Debug.Log($"Saved enemy definition to {path}");
        }

        private void CreateEnemyPrefab(bool forcePlaceInScene)
        {
            EnsureWorkingDefinition();
            EnemyAnimationPreviewSession.Stop();

            if (!TryResolveBuilderSource(out EnemyPrefabBuilder.VisualSourceMode builderSourceMode, out GameObject source))
                return;

            EnemyDefinition definitionCopy = Instantiate(workingDefinition);
            string expectedPrefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(definitionCopy.prefabFileName, definitionCopy.displayName)}.prefab";
            bool existedBefore = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPrefabPath) != null;

            GameObject prefab = EnemyPrefabBuilder.BuildEnemy(definitionCopy, builderSourceMode, source, out string prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Enemy Prefab Creator: failed to build prefab.");
                return;
            }

            GameObject sceneInstance = null;
            if (placeInSceneAfterCreate || forcePlaceInScene)
            {
                sceneInstance = EnemyPrefabBuilder.PlacePrefabInScene(
                    prefab,
                    workingDefinition.displayName,
                    EnemyPrefabBuilder.ResolveSpawnPosition());
            }

            if (patrolPathCreator != null && workingDefinition.movementMode == EnemyMovementMode.Patrol)
            {
                int applied = DMIPathFollowEditorUtility.ApplyToEnemies(patrolPathCreator, sceneInstance);
                Debug.Log(applied > 0
                    ? $"Enemy Prefab Creator: assigned Path Creator to {applied} enemy AI target(s) after build."
                    : "Enemy Prefab Creator: Path Creator set, but no EnemyAiController targets to apply (place in scene or select an enemy).");
            }

            if (sceneInstance != null)
                Selection.activeGameObject = sceneInstance;
            else
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitionList();
            Debug.Log($"{(existedBefore ? "Rebuilt" : "Created")} enemy prefab at {prefabPath}");
        }

        private void ApplyPatrolPathToEnemyTargets(GameObject extraRoot = null)
        {
            if (patrolPathCreator == null)
            {
                Debug.LogWarning("Enemy Prefab Creator: assign a Path Creator first.");
                return;
            }

            int applied = DMIPathFollowEditorUtility.ApplyToEnemies(patrolPathCreator, extraRoot);
            Debug.Log(applied > 0
                ? $"Enemy Prefab Creator: assigned Path Creator to {applied} enemy AI target(s)."
                : "Enemy Prefab Creator: select a scene enemy (or persistent path + prefab asset), then Apply Patrol Path.");
        }
    }
}
