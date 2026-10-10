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
        private GameObject templatePrefab;
        private bool placeInSceneAfterCreate = true;
        private string definitionAssetFileName = "new_enemy";
        private PathCreator patrolPathCreator;
        private readonly EnemyHumanoidPrefabCreatorPanelState humanoidPanelState = new EnemyHumanoidPrefabCreatorPanelState();

        private Vector2 listScroll;
        private Vector2 editorScroll;

        private enum VisualSourceMode
        {
            SelectedHierarchyObject,
            PlaceholderCapsule,
            ExistingPrefab
        }

        [MenuItem(DarkMatterGenesisEditorMenus.EnemyPrefabCreator, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Enemy_Prefab_Creator)]
        public static void Open()
        {
            EnemyPrefabCreatorWindow window = GetWindow<EnemyPrefabCreatorWindow>("Enemy Prefab Creator");
            window.minSize = new Vector2(860f, 620f);
        }

        [MenuItem(
            DarkMatterGenesisEditorMenus.EnemyPrefabCreator + "Dedup Weapon Holders On Selected Prefab",
            false,
            Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Enemy_Prefab_Creator_Dedup_Weapon_Holders_On_Selected_Prefab)]
        public static void DedupSelectedHumanoidPrefab()
        {
            if (!PlayerV7WeaponHolderDedupUtility.TryResolveHumanoidPrefabPathFromSelection(out string prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    "Select a humanoid combat prefab asset or scene instance.",
                    "OK");
                return;
            }

            if (!PlayerV7WeaponHolderDedupUtility.DedupAndRepair(prefabPath, out var report, createFileBackup: false))
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", "Dedup failed. See Console.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Enemy Prefab Creator", "Dedup complete.\n\n" + report, "OK");
        }

        [MenuItem(
            DarkMatterGenesisEditorMenus.EnemyPrefabCreator + "Repair Selected Humanoid Enemy Prefab",
            false,
            Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Enemy_Prefab_Creator_Repair_Selected_Humanoid_Enemy_Prefab)]
        public static void RepairSelectedHumanoidPrefab()
        {
            if (!PlayerV7WeaponHolderDedupUtility.TryResolveHumanoidPrefabPathFromSelection(out string prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    "Select a humanoid enemy prefab asset or scene instance.",
                    "OK");
                return;
            }

            EnemyDefinition definition = EnemyInvectorSetupUtility.ResolveDefinitionForPrefab(prefabPath);
            if (!EnemyPrefabVisualSetupUtility.RepairVisualAtPath(prefabPath, definition))
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", $"Could not repair {prefabPath}.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Enemy Prefab Creator] Repaired visual + gameplay at {prefabPath}");
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
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
            EnsureWorkingDefinition();
            RefreshDefinitionList();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Enemy Prefab Creator", EditorStyles.boldLabel);
            EnemyPrefabCreatorPanel.DrawIntroHelpBox(compact: false);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawDefinitionSidebar();
            DrawEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionSidebar()
        {
            int unusedPlayerSelection = -1;
            DMCharacterCreatorDefinitionSidebar.Draw(
                DMCharacterCreatorDefinitionSidebarSections.Enemy,
                ref listScroll,
                ref unusedPlayerSelection,
                ref selectedDefinitionIndex,
                System.Array.Empty<PlayerVisualDefinition>(),
                definitionAssets,
                customPlayerHint: null,
                customEnemyHint: workingDefinition != null ? workingDefinition.displayName : null,
                onSelectCustomPlayer: null,
                onSelectPlayer: null,
                onSelectCustomEnemy: StartNewDefinition,
                onSelectEnemy: LoadDefinition,
                refreshList: RefreshDefinitionList);
        }

        private void DrawEditorPanel()
        {
            DMCharacterCreatorSharedUi.PreferMeasuredColumnWidth = false;
            EditorGUILayout.BeginVertical(DMCharacterCreatorSharedUi.ContentColumnLayoutOptions());
            editorScroll = EditorGUILayout.BeginScrollView(
                editorScroll,
                DMCharacterCreatorSharedUi.ContentColumnScrollOptions());
            DMCharacterCreatorSharedUi.BeginCreatorContentArea();
            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.Space(2f);
            EnemyPrefabCreatorPanelContext panelCtx = BuildPanelContext();

            EnemyPrefabCreatorPanel.DrawIdentity(panelCtx);
            definitionAssetFileName = panelCtx.DefinitionAssetFileName;
            EditorGUILayout.Space(8f);
            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
            {
                SyncHumanoidPanelFromWindow();
                EnemyHumanoidPrefabCreatorPanel.DrawTemplateAndOutput(humanoidPanelState);
                EditorGUILayout.Space(8f);
                EnemyHumanoidPrefabCreatorPanel.DrawModelSection(humanoidPanelState);
                EditorGUILayout.Space(8f);
                EnemyHumanoidPrefabCreatorPanel.DrawStatus(humanoidPanelState);
                EditorGUILayout.Space(8f);
                EnemyHumanoidPrefabCreatorPanel.DrawActions(
                    humanoidPanelState,
                    compact: false,
                    OnHumanoidPrefabCreatedOrRebuilt);
                SyncHumanoidPanelToWindow();
            }
            else
            {
                DrawVisualSourceSection();
            }

            EditorGUILayout.Space(8f);
            EnemyPrefabCreatorPanel.DrawBehaviorPreset(workingDefinition);
            EditorGUILayout.Space(8f);
            EnemyPrefabCreatorPanel.DrawMovementAndBehavior(panelCtx);
            patrolPathCreator = panelCtx.PatrolPathCreator;
            EditorGUILayout.Space(8f);
            if (workingDefinition.archetype != EnemyArchetype.HumanoidInvector)
                DrawAnimationSection();
            else
                EnemyPrefabCreatorPanel.DrawHumanoidAnimatorNote(show: true);
            EditorGUILayout.Space(8f);
            EnemyPrefabCreatorPanel.DrawLoot(panelCtx);
            EditorGUILayout.Space(8f);
            EnemyPrefabCreatorPanel.DrawHealth(workingDefinition);
            EditorGUILayout.Space(4f);
            EnemyPrefabCreatorPanel.DrawHealthBar(workingDefinition);
            EditorGUILayout.Space(4f);
            EnemyPrefabCreatorPanel.DrawSenses(workingDefinition);
            EditorGUILayout.Space(4f);
            EnemyPrefabCreatorPanel.DrawCombatStats(workingDefinition);
            EditorGUILayout.Space(12f);
            if (workingDefinition.archetype != EnemyArchetype.HumanoidInvector)
                DrawSpawnReadyStatus();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            DMCharacterCreatorSharedUi.EndCreatorContentArea();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private EnemyPrefabCreatorPanelContext BuildPanelContext()
        {
            return new EnemyPrefabCreatorPanelContext
            {
                Definition = workingDefinition,
                DefinitionAssetFileName = definitionAssetFileName,
                PatrolPathCreator = patrolPathCreator,
                ApplyPatrolPath = () => ApplyPatrolPathToEnemyTargets(),
                ApplyLoot = () => EnemyPrefabCreatorPanel.ApplyLootToExistingPrefab(workingDefinition)
            };
        }

        private void SyncHumanoidPanelFromWindow()
        {
            humanoidPanelState.definition = workingDefinition;
            humanoidPanelState.displayName = workingDefinition.displayName;
            humanoidPanelState.prefabFileName = workingDefinition.prefabFileName;
            humanoidPanelState.visualChildName = workingDefinition.visualChildName;
            humanoidPanelState.templatePrefab = templatePrefab != null
                ? templatePrefab
                : workingDefinition.templatePrefab;
            humanoidPanelState.humanoidMeshSource = humanoidMeshSource != null
                ? humanoidMeshSource
                : workingDefinition.lastModelSource;
        }

        private void SyncHumanoidPanelToWindow()
        {
            if (workingDefinition == null)
                return;

            humanoidMeshSource = humanoidPanelState.humanoidMeshSource;
            templatePrefab = humanoidPanelState.templatePrefab;
            workingDefinition.displayName = humanoidPanelState.displayName;
            workingDefinition.prefabFileName = humanoidPanelState.prefabFileName;
            workingDefinition.visualChildName = humanoidPanelState.visualChildName;
            workingDefinition.templatePrefab = humanoidPanelState.templatePrefab;
            workingDefinition.lastModelSource = humanoidPanelState.humanoidMeshSource;
        }

        private void DrawVisualSourceSection()
        {
            EditorGUILayout.LabelField("Visual Source (Legacy creature)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generic/creature rigs: set Archetype = LegacyCreature (Auto-Detect on assign).",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && humanoidMeshSource != null)
                ApplyModelAutoDetect(humanoidMeshSource);

            DMCharacterCreatorSharedUi.DrawModelInspectionPanel(
                humanoidMeshSource,
                "Assign a Meshy/character FBX to inspect rig, avatar, and scale.",
                playerRecommendations: false);

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Prepare Model Import", PrepareAssignedModelImport, humanoidMeshSource != null),
                ("Auto-Detect Archetype", () => ApplyModelAutoDetect(humanoidMeshSource), humanoidMeshSource != null));

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

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Use Current Selection", () =>
                {
                    if (Selection.activeGameObject != null)
                    {
                        selectedVisualSource = Selection.activeGameObject;
                        visualSourceMode = VisualSourceMode.SelectedHierarchyObject;
                    }
                }, Selection.activeGameObject != null),
                ("Use Model FBX As Source", () =>
                {
                    if (humanoidMeshSource != null)
                    {
                        existingPrefabSource = humanoidMeshSource;
                        visualSourceMode = VisualSourceMode.ExistingPrefab;
                    }
                }, humanoidMeshSource != null));

            placeInSceneAfterCreate = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Place In Open Scene After Create",
                placeInSceneAfterCreate);
        }

        private void PrepareAssignedModelImport()
        {
            if (humanoidMeshSource == null)
                return;

            string path = EnemyModelAvatarUtility.ResolvePreferredModelAssetPath(humanoidMeshSource);

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
            workingDefinition.buildAnimatorFromClips = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Build Animator From Clips",
                workingDefinition.buildAnimatorFromClips);
            workingDefinition.addEnemyAnimationController = DMCharacterCreatorSharedUi.DrawPropertyToggle(
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
            workingDefinition.lockVisualRootPosition = DMCharacterCreatorSharedUi.DrawPropertyToggle(
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
            string buildAndPlaceLabel = prefabExists ? "Rebuild + Place In Scene" : "Create Prefab + Place In Scene";

            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectEnemySaveBlockers(
                    workingDefinition,
                    definitionAssetFileName));

            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector &&
                GUILayout.Button("Save Definition + Create Prefab + Apply Visual", GUILayout.Height(32f)))
            {
                SaveHumanoidDefinitionAndCreate();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition Asset", GUILayout.Height(30f)))
            {
                if (DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                        workingDefinition,
                        definitionAssetFileName,
                        out string message))
                    SaveDefinitionAsset();
                else
                    DMCharacterCreatorActionValidation.ShowValidationDialog(
                        DMCharacterCreatorActionValidation.EnemyDialogTitle,
                        message);
            }

            if (workingDefinition.archetype != EnemyArchetype.HumanoidInvector)
            {
                string buildLabel = prefabExists ? "Rebuild Prefab" : "Create Prefab";
                if (GUILayout.Button(buildLabel, GUILayout.Height(30f)))
                    CreateEnemyPrefab(false);
            }

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
            templatePrefab = EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();
            humanoidMeshSource = null;
            EnemyHumanoidPrefabCreatorPanel.SyncFromDefinition(humanoidPanelState, workingDefinition);
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
            templatePrefab = asset.templatePrefab != null
                ? asset.templatePrefab
                : EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();
            humanoidMeshSource = asset.lastModelSource;
            EnemyHumanoidPrefabCreatorPanel.SyncFromDefinition(humanoidPanelState, workingDefinition);
        }

        private void SaveDefinitionAsset()
        {
            EnsureWorkingDefinition();
            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
                SyncHumanoidPanelToWindow();

            SyncDefinitionAssetFileNameFromIdentity();
            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingDefinition, definitionAssetFileName))
                RefreshDefinitionListAndSelectCurrent();
        }

        private void SaveHumanoidDefinitionAndCreate()
        {
            EnsureWorkingDefinition();
            SyncHumanoidPanelFromWindow();
            SyncHumanoidPanelToWindow();
            SyncDefinitionAssetFileNameFromIdentity();
            if (!EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingDefinition, definitionAssetFileName))
                return;

            RefreshDefinitionListAndSelectCurrent();
            humanoidPanelState.definition = workingDefinition;
            SyncHumanoidPanelFromWindow();
            if (!EnemyHumanoidPrefabCreatorPanel.TryCreatePrefabFromState(humanoidPanelState, out bool created) || !created)
                return;

            SyncHumanoidPanelToWindow();
            SyncDefinitionAssetFileNameFromIdentity();
            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingDefinition, definitionAssetFileName))
                RefreshDefinitionListAndSelectCurrent();
        }

        private void OnHumanoidPrefabCreatedOrRebuilt()
        {
            SyncHumanoidPanelToWindow();
            SyncDefinitionAssetFileNameFromIdentity();
            if (!DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                    workingDefinition,
                    definitionAssetFileName,
                    out string message))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    "Prefab was created or rebuilt, but Save Definition could not run:\n\n" + message);
                return;
            }

            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingDefinition, definitionAssetFileName))
                RefreshDefinitionListAndSelectCurrent();
        }

        private void SyncDefinitionAssetFileNameFromIdentity()
        {
            if (workingDefinition == null)
                return;

            definitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestEnemyDefinitionAssetFileName(
                workingDefinition.prefabFileName,
                workingDefinition.enemyId,
                workingDefinition.displayName);
        }

        private void RefreshDefinitionListAndSelectCurrent()
        {
            string assetName = definitionAssetFileName;
            string displayName = workingDefinition != null ? workingDefinition.displayName : null;
            string prefabFileName = workingDefinition != null ? workingDefinition.prefabFileName : null;
            string enemyId = workingDefinition != null ? workingDefinition.enemyId : null;

            RefreshDefinitionList();
            selectedDefinitionIndex = DMCharacterCreatorDefinitionSidebar.FindEnemyDefinitionIndex(
                definitionAssets,
                assetName,
                displayName,
                prefabFileName,
                enemyId);
        }

        private void CreateEnemyPrefab(bool forcePlaceInScene)
        {
            EnsureWorkingDefinition();
            if (workingDefinition.archetype == EnemyArchetype.HumanoidInvector)
                SyncHumanoidPanelToWindow();

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
            SyncDefinitionAssetFileNameFromIdentity();
            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingDefinition, definitionAssetFileName))
                RefreshDefinitionListAndSelectCurrent();
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
