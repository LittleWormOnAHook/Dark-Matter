using Project.AI;
using Project.Data;
using UnityEditor;
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
        private bool placeInSceneAfterCreate = true;
        private string definitionAssetFileName = "new_enemy";

        private Vector2 listScroll;
        private Vector2 editorScroll;

        private enum VisualSourceMode
        {
            SelectedHierarchyObject,
            PlaceholderCapsule,
            ExistingPrefab
        }

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Enemy Prefab Creator", false, 0)]
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
                "Build enemy prefabs from any model: select a Hierarchy object, drag in a prefab, or use a placeholder capsule. " +
                "Configure senses, combat, health bar, movement (stationary, wander, patrol), and optional animation.",
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
            DrawAnimationSection();
            EditorGUILayout.Space(8f);
            DrawLootSection();
            EditorGUILayout.Space(8f);
            DrawDefinitionFields();
            EditorGUILayout.Space(12f);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            workingDefinition.enemyId = EditorGUILayout.TextField("Enemy Id", workingDefinition.enemyId);
            workingDefinition.displayName = EditorGUILayout.TextField("Display Name", workingDefinition.displayName);
            workingDefinition.prefabFileName = EditorGUILayout.TextField("Prefab File Name", workingDefinition.prefabFileName);
            definitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", definitionAssetFileName);
        }

        private void DrawVisualSourceSection()
        {
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);
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
                        existingPrefabSource,
                        typeof(GameObject),
                        false);
                    break;

                case VisualSourceMode.PlaceholderCapsule:
                    EditorGUILayout.HelpBox("Creates a simple capsule placeholder mesh.", MessageType.None);
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
            EditorGUILayout.EndHorizontal();

            placeInSceneAfterCreate = EditorGUILayout.Toggle("Place In Open Scene After Create", placeInSceneAfterCreate);
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
                workingDefinition.patrolPointCount = EditorGUILayout.IntField("Patrol Point Count", workingDefinition.patrolPointCount);
                workingDefinition.patrolRadius = EditorGUILayout.FloatField("Patrol Radius", workingDefinition.patrolRadius);
                workingDefinition.patrolWaitDuration = EditorGUILayout.FloatField(
                    "Patrol Wait Duration",
                    workingDefinition.patrolWaitDuration);
                EditorGUILayout.HelpBox(
                    "Patrol points are generated in a circle around the spawn position when the prefab is created. " +
                    "Move the PatrolPoints children in the prefab to customize the route.",
                    MessageType.None);
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
            workingDefinition.piCoinsMin = EditorGUILayout.IntField("Pi Coins Min", workingDefinition.piCoinsMin);
            workingDefinition.piCoinsMax = EditorGUILayout.IntField("Pi Coins Max", workingDefinition.piCoinsMax);
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
                $"{ProjectAssetPaths.PrefabsCombat}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";

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
            workingDefinition.hearingRange = EditorGUILayout.FloatField("Hearing Range", workingDefinition.hearingRange);
            workingDefinition.proximityRange = EditorGUILayout.FloatField("Proximity Range", workingDefinition.proximityRange);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
            workingDefinition.attackRange = EditorGUILayout.FloatField("Attack Range", workingDefinition.attackRange);
            workingDefinition.attackDamage = EditorGUILayout.FloatField("Attack Damage", workingDefinition.attackDamage);
            workingDefinition.attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", workingDefinition.attackCooldown);
            workingDefinition.attackWindup = EditorGUILayout.FloatField("Attack Windup", workingDefinition.attackWindup);

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
                $"{ProjectAssetPaths.PrefabsCombat}/{EnemyPrefabBuilder.SanitizeFileName(workingDefinition.prefabFileName, workingDefinition.displayName)}.prefab";

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

            switch (visualSourceMode)
            {
                case VisualSourceMode.PlaceholderCapsule:
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.PlaceholderCapsule;
                    return true;

                case VisualSourceMode.ExistingPrefab:
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.ExistingPrefab;
                    source = existingPrefabSource;
                    if (source == null)
                    {
                        EditorUtility.DisplayDialog("Enemy Prefab Creator", "Assign a prefab asset as the visual source.", "OK");
                        return false;
                    }
                    return true;

                default:
                    sourceMode = EnemyPrefabBuilder.VisualSourceMode.SelectedHierarchyObject;
                    source = ResolveVisualSource(out bool missing);
                    if (missing)
                    {
                        EditorUtility.DisplayDialog(
                            "Enemy Prefab Creator",
                            "Select a model in the Hierarchy or switch visual source mode.",
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

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition Asset", GUILayout.Height(30f)))
                SaveDefinitionAsset();

            if (GUILayout.Button("Create Prefab", GUILayout.Height(30f)))
                CreateEnemyPrefab(false);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Prefab + Place In Scene", GUILayout.Height(32f)))
                CreateEnemyPrefab(true);
        }

        private void StartNewDefinition()
        {
            selectedDefinitionIndex = -1;
            workingDefinition = CreateInstance<EnemyDefinition>();
            workingDefinition.enemyId = "new_enemy";
            workingDefinition.displayName = "New Enemy";
            workingDefinition.prefabFileName = "NewEnemy";
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
            GameObject prefab = EnemyPrefabBuilder.BuildEnemy(definitionCopy, builderSourceMode, source, out string prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Enemy Prefab Creator: failed to build prefab.");
                return;
            }

            if (placeInSceneAfterCreate || forcePlaceInScene)
            {
                GameObject instance = EnemyPrefabBuilder.PlacePrefabInScene(
                    prefab,
                    workingDefinition.displayName,
                    EnemyPrefabBuilder.ResolveSpawnPosition());

                if (instance != null)
                    Selection.activeGameObject = instance;
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            AssetDatabase.SaveAssets();
            RefreshDefinitionList();
            Debug.Log($"Created enemy prefab at {prefabPath}");
        }
    }
}
