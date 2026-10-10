using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Editor window for creating player prefab variants with Meshy/custom humanoid meshes.
    /// Clones from Player_Invector (template) into a new named prefab — never overwrites the template.
    /// Menu: Tools → Dark Matter Genesis → Prefab Creator → Player Prefab Creator
    /// </summary>
    public class PlayerPrefabCreatorWindow : EditorWindow
    {
        private PlayerVisualDefinition[] definitionAssets = System.Array.Empty<PlayerVisualDefinition>();
        private int selectedDefinitionIndex = -1;
        private PlayerVisualDefinition workingDefinition;

        private GameObject templatePrefab;
        private GameObject humanoidMeshSource;
        private string visualChildName = "Visual";
        private string definitionAssetFileName = "Player_Default";
        private string prefabFileName = "Player_Custom";

        private Vector2 listScroll;
        private Vector2 editorScroll;

        [MenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator + "Repair Player_v7 Prefab", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Player_Prefab_CreatorRepair_Player_v7_Prefab)]
        public static void RepairPlayerV7Prefab()
        {
            const string path = "Assets/_Project/Prefabs/Players/Player_v7.prefab";
            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(path))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not repair {path}.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"[Player Prefab Creator] Repaired {path} (BodySnaps, ragdoll remount, frozen VBOT physics strip).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator + "Dedup Player_v7 Weapon Holders", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Player_Prefab_CreatorDedup_Player_v7_Weapon_Holders)]
        public static void DedupPlayerV7WeaponHolders()
        {
            const string path = PlayerV7WeaponHolderDedupUtility.DefaultPlayerV7Path;
            if (!EditorUtility.DisplayDialog(
                    "Dedup Player_v7 Weapon Holders",
                    "Creates/uses Player_v7_backup.prefab, removes duplicate Visual holder trees, " +
                    "strips hidden VBOT armature, then runs Repair Player_v7.\n\nProceed?",
                    "Dedup + Repair",
                    "Cancel"))
            {
                return;
            }

            if (!PlayerV7WeaponHolderDedupUtility.DedupAndRepair(path, out PlayerV7WeaponHolderDedupUtility.DedupReport report))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Dedup failed for {path}. See Console.", "OK");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            EditorUtility.DisplayDialog(
                "Player Prefab Creator",
                "Player_v7 dedup complete.\n\n" + report,
                "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Prefab_Creator_Player_Prefab_Creator)]
        public static void Open()
        {
            PlayerPrefabCreatorWindow window = GetWindow<PlayerPrefabCreatorWindow>("Player Prefab Creator");
            window.minSize = new Vector2(780f, 560f);
        }

        private void OnEnable()
        {
            RefreshDefinitionList();
            EnsureWorkingDefinition();
            if (templatePrefab == null)
                templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
        }

        private void RefreshDefinitionList()
        {
            definitionAssets = PlayerPrefabVisualSetupUtility.LoadAllDefinitions();
            if (definitionAssets.Length == 0)
            {
                PlayerVisualDefinition created = PlayerPrefabVisualSetupUtility.EnsureDefaultDefinitionAsset();
                definitionAssets = PlayerPrefabVisualSetupUtility.LoadAllDefinitions();
                if (created != null)
                    LoadDefinition(created, 0);
            }
        }

        private void EnsureWorkingDefinition()
        {
            if (workingDefinition != null)
                return;

            if (definitionAssets != null && definitionAssets.Length > 0 && definitionAssets[0] != null)
            {
                LoadDefinition(definitionAssets[0], 0);
                return;
            }

            StartNewDefinition();
        }

        private void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
            EnsureWorkingDefinition();

            EditorGUILayout.LabelField("Player Prefab Creator", EditorStyles.boldLabel);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Creates a NEW player prefab variant — Player_Invector.prefab is the clone TEMPLATE and is never overwritten.\n" +
                "Assign a Meshy Humanoid Model FBX → set Prefab File Name (e.g. Player_MeshyAndroid) → Create Prefab / Rebuild.\n" +
                "Swaps the root Animator avatar on the new prefab, hides the stock VBOT body (weapons stay), rebinds BodySnaps / " +
                "Drawn_/Holstered_ holders onto the Meshy Visual bones (same as Corrupt Patrol), normalizes hand sockets, " +
                "disables ranged support-hand IK (prevents pretzel arms until grips are retargeted), and repairs PioneerVisual slots. " +
                "Player controller, camera, inventory, health, and input stay intact.\n" +
                "Menu: Tools → Dark Matter Genesis → Prefab Creator → Player Prefab Creator",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawDefinitionSidebar();
            DrawEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionSidebar()
        {
            int unusedEnemySelection = -1;
            DMCharacterCreatorDefinitionSidebar.Draw(
                DMCharacterCreatorDefinitionSidebarSections.Player,
                ref listScroll,
                ref selectedDefinitionIndex,
                ref unusedEnemySelection,
                definitionAssets,
                System.Array.Empty<EnemyDefinition>(),
                customPlayerHint: workingDefinition != null ? workingDefinition.displayName : null,
                customEnemyHint: null,
                onSelectCustomPlayer: StartNewDefinition,
                onSelectPlayer: LoadDefinition,
                onSelectCustomEnemy: null,
                onSelectEnemy: null,
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

            DrawIdentitySection();
            EditorGUILayout.Space(8f);
            DrawTemplateAndOutputSection();
            EditorGUILayout.Space(8f);
            DrawModelSection();
            EditorGUILayout.Space(12f);
            DrawStatusSection();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            DMCharacterCreatorSharedUi.EndCreatorContentArea();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            workingDefinition.displayName = EditorGUILayout.TextField("Display Name", workingDefinition.displayName);
            prefabFileName = EditorGUILayout.TextField("Prefab File Name", prefabFileName);
            workingDefinition.prefabFileName = prefabFileName;
            definitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", definitionAssetFileName);
            workingDefinition.notes = EditorGUILayout.TextArea(workingDefinition.notes, GUILayout.MinHeight(40f));

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            if (PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath))
            {
                EditorGUILayout.HelpBox(
                    "Prefab File Name resolves to Player_Invector — that is the protected template. " +
                    "Rename the output (e.g. Player_MeshyAndroid) before Create / Rebuild.",
                    MessageType.Error);
            }
        }

        private void DrawTemplateAndOutputSection()
        {
            EditorGUILayout.LabelField("Template & Output", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Template Prefab",
                templatePrefab,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && workingDefinition != null)
                workingDefinition.templatePrefab = templatePrefab;

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Use Player_Invector Template", () =>
                {
                    templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
                    if (workingDefinition != null)
                        workingDefinition.templatePrefab = templatePrefab;
                }, true),
                ("Ping Template", () =>
                {
                    Selection.activeObject = templatePrefab;
                    EditorGUIUtility.PingObject(templatePrefab);
                }, templatePrefab != null));

            string templatePath = PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Template: ", templatePath);

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Output: ", outputPath);

            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (outputPrefab != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField("Existing Output", outputPrefab, typeof(GameObject), false);
                if (GUILayout.Button("Ping Output", GUILayout.Width(90f)))
                {
                    Selection.activeObject = outputPrefab;
                    EditorGUIUtility.PingObject(outputPrefab);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No output prefab yet. Create Prefab will clone the template to the output path.",
                    MessageType.None);
            }
        }

        private void DrawModelSection()
        {
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && humanoidMeshSource != null)
            {
                if (string.IsNullOrWhiteSpace(visualChildName) || visualChildName == "Visual")
                    visualChildName = PlayerPrefabVisualSetupUtility.SuggestVisualChildName(humanoidMeshSource);
                if (workingDefinition != null)
                    workingDefinition.lastModelSource = humanoidMeshSource;
            }

            visualChildName = EditorGUILayout.TextField("Visual Child Name", visualChildName);
            if (workingDefinition != null)
                workingDefinition.visualChildName = visualChildName;

            DMCharacterCreatorSharedUi.DrawModelInspectionPanel(
                humanoidMeshSource,
                "Assign a Meshy Humanoid FBX to inspect rig, avatar, and scale.",
                playerRecommendations: true);

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Prepare Model Import", PrepareAssignedModelImport, humanoidMeshSource != null),
                ("Auto-Detect", () => ApplyModelAutoDetect(humanoidMeshSource), humanoidMeshSource != null));
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Prefab Status", EditorStyles.boldLabel);
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (prefab == null)
            {
                EditorGUILayout.HelpBox(
                    $"Output prefab not created yet at {outputPath}.",
                    MessageType.None);
                return;
            }

            Animator animator = prefab.GetComponent<Animator>();
            Transform stock = prefab.transform.Find("3D Model");
            Transform visual = prefab.transform.Find(
                string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName);

            string avatarLabel = animator != null && animator.avatar != null
                ? $"{animator.avatar.name} (human={animator.avatar.isHuman}, valid={animator.avatar.isValid})"
                : "none";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Avatar", avatarLabel);
            EditorGUILayout.LabelField("Stock 3D Model", stock != null ? "present" : "missing");
            EditorGUILayout.LabelField("Visual Child", visual != null ? visual.name : "not applied yet");
            EditorGUILayout.LabelField(
                "Edit-mode Animator",
                animator != null ? (animator.enabled ? "enabled" : "disabled (bind pose)") : "n/a");
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            bool outputExists = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null;
            bool blocked = PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath);
            string createLabel = outputExists ? "Rebuild Prefab" : "Create Prefab";
            bool requireModel = humanoidMeshSource != null;
            var panelState = BuildPanelStateForValidation();
            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectPlayerSaveBlockers(
                    workingDefinition,
                    definitionAssetFileName,
                    prefabFileName,
                    workingDefinition != null ? workingDefinition.displayName : null));
            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectPlayerCreateBlockers(panelState, requireModel));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition Asset", GUILayout.Height(30f)))
            {
                if (DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                        workingDefinition,
                        definitionAssetFileName,
                        prefabFileName,
                        workingDefinition != null ? workingDefinition.displayName : null,
                        out string message))
                    SaveDefinitionAsset();
                else
                    DMCharacterCreatorActionValidation.ShowValidationDialog(
                        DMCharacterCreatorActionValidation.PlayerDialogTitle,
                        message);
            }

            GUI.enabled = outputExists && !blocked;
            if (GUILayout.Button("Repair Visual (No Mesh Change)", GUILayout.Height(30f)))
                RepairWithoutMesh();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUI.enabled = !blocked && (humanoidMeshSource != null || !outputExists);
            if (GUILayout.Button(
                    humanoidMeshSource != null
                        ? (outputExists ? "Apply Visual / Rebuild" : "Create Prefab + Apply Visual")
                        : createLabel,
                    GUILayout.Height(34f)))
            {
                bool succeeded = humanoidMeshSource != null
                    ? ApplyVisualRebuild()
                    : CreatePrefabFromTemplateOnly();
                if (succeeded)
                {
                    EnsureWorkingDefinition();
                    SyncWorkingDefinitionFields();
                    definitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestPlayerDefinitionAssetFileName(
                        prefabFileName,
                        workingDefinition.displayName);
                    if (DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                            workingDefinition,
                            definitionAssetFileName,
                            prefabFileName,
                            workingDefinition.displayName,
                            out string saveMessage))
                    {
                        SaveDefinitionAsset();
                    }
                    else
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.PlayerDialogTitle,
                            "Prefab was created or rebuilt, but Save Definition could not run:\n\n" + saveMessage);
                    }
                }
            }
            GUI.enabled = !blocked && humanoidMeshSource != null;
            if (GUILayout.Button("Rebuild From Template + Apply Visual", GUILayout.Height(30f)))
            {
                panelState = BuildPanelStateForValidation();
                if (PlayerPrefabCreatorPanel.RebuildFromTemplateApplyVisual(panelState))
                {
                    humanoidMeshSource = panelState.humanoidMeshSource;
                    EnsureWorkingDefinition();
                    SyncWorkingDefinitionFields();
                }
            }

            GUI.enabled = true;

            if (blocked)
            {
                EditorGUILayout.HelpBox(
                    "Create / Rebuild / Repair are blocked while Prefab File Name is Player_Invector.",
                    MessageType.Warning);
            }
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
            if (model == null)
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid)
            {
                if (string.IsNullOrWhiteSpace(visualChildName))
                    visualChildName = "Visual";
            }

            if (workingDefinition != null)
            {
                workingDefinition.lastModelSource = model;
                if (string.IsNullOrWhiteSpace(workingDefinition.displayName) ||
                    workingDefinition.displayName == "Player" ||
                    workingDefinition.displayName == "New Player Visual")
                {
                    workingDefinition.displayName = model.name.Replace('_', ' ');
                }

                if (string.IsNullOrWhiteSpace(prefabFileName) ||
                    prefabFileName == "Player_Custom" ||
                    prefabFileName == "NewPlayer")
                {
                    prefabFileName = PlayerPrefabVisualSetupUtility.SanitizeFileName(
                        "Player_" + model.name, "Player_Custom");
                    workingDefinition.prefabFileName = prefabFileName;
                }
            }
        }

        private bool TryGetValidatedOutputPath(out string outputPath)
        {
            SyncWorkingDefinitionFields();
            outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            if (!PlayerPrefabVisualSetupUtility.TryValidateOutputPath(outputPath, out string error))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", error, "OK");
                return false;
            }

            return true;
        }

        private void SyncWorkingDefinitionFields()
        {
            if (workingDefinition == null)
                return;

            workingDefinition.displayName = string.IsNullOrWhiteSpace(workingDefinition.displayName)
                ? "Player Custom"
                : workingDefinition.displayName;
            workingDefinition.prefabFileName = prefabFileName;
            workingDefinition.templatePrefab = templatePrefab;
            workingDefinition.visualChildName = visualChildName;
            workingDefinition.lastModelSource = humanoidMeshSource;
        }

        private PlayerPrefabCreatorPanelState BuildPanelStateForValidation()
        {
            return new PlayerPrefabCreatorPanelState
            {
                templatePrefab = templatePrefab,
                humanoidMeshSource = humanoidMeshSource,
                visualChildName = visualChildName,
                prefabFileName = prefabFileName,
                displayName = workingDefinition != null ? workingDefinition.displayName : "Player Custom"
            };
        }

        private bool ApplyVisualRebuild()
        {
            PlayerPrefabCreatorPanelState panelState = BuildPanelStateForValidation();
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(panelState, requireModel: true, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            if (!TryGetValidatedOutputPath(out string outputPath))
                return false;

            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                humanoidMeshSource,
                visualChildName,
                templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Could not create/rebuild player prefab at {outputPath}.",
                    "OK");
                return false;
            }

            if (workingDefinition != null)
                workingDefinition.playerPrefab = created;

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            Debug.Log(
                $"[Player Prefab Creator] Wrote visual from '{humanoidMeshSource.name}' to {outputPath} " +
                $"(template preserved: {PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab)})");
            return true;
        }

        private bool CreatePrefabFromTemplateOnly()
        {
            PlayerPrefabCreatorPanelState panelState = BuildPanelStateForValidation();
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(panelState, requireModel: false, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            if (!TryGetValidatedOutputPath(out string outputPath))
                return false;

            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                null,
                visualChildName,
                templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Could not create player prefab at {outputPath}.",
                    "OK");
                return false;
            }

            if (workingDefinition != null)
                workingDefinition.playerPrefab = created;

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            Debug.Log(
                $"[Player Prefab Creator] Created {outputPath} from template " +
                $"(template preserved: {PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab)})");
            return true;
        }

        private void RepairWithoutMesh()
        {
            if (!TryGetValidatedOutputPath(out string outputPath))
                return;

            if (!System.IO.File.Exists(outputPath))
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Output prefab missing at {outputPath}. Create Prefab first.",
                    "OK");
                return;
            }

            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(outputPath))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not repair {outputPath}.", "OK");
                return;
            }

            GameObject repaired = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (workingDefinition != null)
                workingDefinition.playerPrefab = repaired;

            AssetDatabase.SaveAssets();
            Selection.activeObject = repaired;
            EditorGUIUtility.PingObject(repaired);
            Debug.Log($"[Player Prefab Creator] Repaired holders / weapon visuals / edit-mode animator at {outputPath}");
        }

        private void StartNewDefinition()
        {
            selectedDefinitionIndex = -1;
            workingDefinition = CreateInstance<PlayerVisualDefinition>();
            workingDefinition.displayName = "New Player Visual";
            workingDefinition.prefabFileName = "Player_Custom";
            workingDefinition.visualChildName = "Visual";
            workingDefinition.templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            workingDefinition.playerPrefab = null;
            workingDefinition.notes =
                "Clones from Player_Invector (template). Create Prefab writes a new file — never overwrites the template.";
            templatePrefab = workingDefinition.templatePrefab;
            visualChildName = "Visual";
            humanoidMeshSource = null;
            prefabFileName = "Player_Custom";
            definitionAssetFileName = "player_visual_new";
        }

        private void LoadDefinition(PlayerVisualDefinition asset, int index)
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
            prefabFileName = string.IsNullOrWhiteSpace(asset.prefabFileName)
                ? "Player_Custom"
                : asset.prefabFileName;
            templatePrefab = asset.templatePrefab != null
                ? asset.templatePrefab
                : PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            visualChildName = string.IsNullOrWhiteSpace(asset.visualChildName) ? "Visual" : asset.visualChildName;
            humanoidMeshSource = asset.lastModelSource;

            // Migrate old defs that pointed playerPrefab at the protected template.
            if (asset.playerPrefab != null)
            {
                string linked = AssetDatabase.GetAssetPath(asset.playerPrefab);
                if (PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(linked))
                    workingDefinition.playerPrefab = null;
            }
        }

        private void SaveDefinitionAsset()
        {
            EnsureWorkingDefinition();
            SyncWorkingDefinitionFields();
            definitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestPlayerDefinitionAssetFileName(
                prefabFileName,
                workingDefinition.displayName);

            if (!DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                    workingDefinition,
                    definitionAssetFileName,
                    prefabFileName,
                    workingDefinition.displayName,
                    out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PlayersData);

            string fileName = SanitizeFileName(definitionAssetFileName, "Player_Default");
            string path = $"{ProjectAssetPaths.PlayersData}/{fileName}.asset";

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            workingDefinition.playerPrefab = outputPrefab;

            PlayerVisualDefinition existing = AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(workingDefinition, path);
            }
            else
            {
                EditorUtility.CopySerialized(workingDefinition, existing);
                existing.name = System.IO.Path.GetFileNameWithoutExtension(path); // keep m_Name matching the filename
                EditorUtility.SetDirty(existing);
                workingDefinition = existing;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitionListAndSelectCurrent();
            Debug.Log($"Saved player visual definition to {path} (output={outputPath})");
        }

        private void RefreshDefinitionListAndSelectCurrent()
        {
            string assetName = definitionAssetFileName;
            string displayName = workingDefinition != null ? workingDefinition.displayName : null;
            RefreshDefinitionList();
            selectedDefinitionIndex = DMCharacterCreatorDefinitionSidebar.FindPlayerDefinitionIndex(
                definitionAssets,
                assetName,
                displayName,
                prefabFileName);
        }

        private static string SanitizeFileName(string preferred, string fallback)
        {
            return PlayerPrefabVisualSetupUtility.SanitizeFileName(preferred, fallback);
        }
    }
}
