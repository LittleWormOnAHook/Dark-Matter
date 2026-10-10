#if UNITY_EDITOR
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public sealed class PlayerPrefabCreatorPanelState
    {
        public GameObject templatePrefab;
        public GameObject humanoidMeshSource;
        public string visualChildName = "Visual";
        public string prefabFileName = "Player_Custom";
        public string displayName = "Player Custom";
        public bool autoPrepareOnApply;
    }

    /// <summary>
    /// Shared IMGUI for Player Prefab Creator and Genesis Studio Character Creator.
    /// </summary>
    public static class PlayerPrefabCreatorPanel
    {
        public static void DrawTemplateAndOutput(PlayerPrefabCreatorPanelState state)
        {
            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Template & Output", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            state.templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Template Prefab",
                state.templatePrefab,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && state.templatePrefab == null)
                state.templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Use Player_Invector Template", () =>
                {
                    state.templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
                }, true),
                ("Ping Template", () =>
                {
                    Selection.activeObject = state.templatePrefab;
                    EditorGUIUtility.PingObject(state.templatePrefab);
                }, state.templatePrefab != null));

            string templatePath = PlayerPrefabVisualSetupUtility.ResolveTemplatePath(state.templatePrefab);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Template: ", templatePath);

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Output: ", outputPath);

            if (PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath))
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Prefab File Name resolves to Player_Invector — protected template. Rename output before Create/Rebuild.",
                    MessageType.Error);
            }

            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (outputPrefab != null)
            {
                EditorGUILayout.ObjectField("Existing Output", outputPrefab, typeof(GameObject), false);
                if (GUILayout.Button("Ping Output", GUILayout.Height(22f)))
                {
                    Selection.activeObject = outputPrefab;
                    EditorGUIUtility.PingObject(outputPrefab);
                }
            }
        }

        public static void DrawModelSection(PlayerPrefabCreatorPanelState state)
        {
            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            state.humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                state.humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && state.humanoidMeshSource != null &&
                (string.IsNullOrWhiteSpace(state.visualChildName) || state.visualChildName == "Visual"))
            {
                state.visualChildName = PlayerPrefabVisualSetupUtility.SuggestVisualChildName(state.humanoidMeshSource);
            }

            state.visualChildName = EditorGUILayout.TextField("Visual Child Name", state.visualChildName);
            state.autoPrepareOnApply = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Auto-prepare import on Apply",
                state.autoPrepareOnApply);

            DMCharacterCreatorSharedUi.DrawModelInspectionPanel(
                state.humanoidMeshSource,
                "Assign a Meshy Humanoid FBX to inspect rig, avatar, and scale.",
                playerRecommendations: true);

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Prepare Model Import", () => PrepareModelImport(state), state.humanoidMeshSource != null),
                ("Auto-Detect", () => AutoDetect(state), state.humanoidMeshSource != null),
                ("Force Humanoid Rig", () => ForceHumanoid(state), state.humanoidMeshSource != null));
        }

        public static void DrawDefinitionSection(
            PlayerPrefabCreatorPanelState state,
            PlayerVisualDefinition definition,
            ref string definitionAssetFileName)
        {
            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Visual Definition", EditorStyles.boldLabel);
            if (definition != null)
            {
                definition.notes = EditorGUILayout.TextArea(
                    definition.notes,
                    GUILayout.MinHeight(48f),
                    GUILayout.ExpandWidth(true));
            }

            definitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", definitionAssetFileName);
            SyncToDefinition(state, definition);
        }

        public static void DrawSaveDefinitionRow(
            PlayerPrefabCreatorPanelState state,
            PlayerVisualDefinition definition,
            string definitionAssetFileName,
            System.Action saveDefinition,
            System.Action saveDefinitionAndCreatePrefab = null)
        {
            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectPlayerSaveBlockers(
                    definition,
                    definitionAssetFileName,
                    state.prefabFileName,
                    state.displayName));

            if (saveDefinitionAndCreatePrefab != null)
            {
                if (GUILayout.Button(
                        "Save Definition + Create Prefab + Apply Visual",
                        GUILayout.Height(30f),
                        GUILayout.ExpandWidth(true)))
                {
                    if (!DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                            definition,
                            definitionAssetFileName,
                            state.prefabFileName,
                            state.displayName,
                            out string saveMessage))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.PlayerDialogTitle,
                            saveMessage);
                        return;
                    }

                    bool requireModel = state.humanoidMeshSource != null;
                    if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(
                            state,
                            requireModel,
                            out string createMessage))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.PlayerDialogTitle,
                            "Save checks passed, but Create Prefab cannot run:\n\n" + createMessage);
                        return;
                    }

                    saveDefinitionAndCreatePrefab.Invoke();
                }

                EditorGUILayout.Space(4f);
            }

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                28f,
                ("Save Definition Asset", () =>
                {
                    if (!DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                            definition,
                            definitionAssetFileName,
                            state.prefabFileName,
                            state.displayName,
                            out string message))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.PlayerDialogTitle,
                            message);
                        return;
                    }

                    saveDefinition?.Invoke();
                }, true));
        }

        public static bool TryCreatePrefabFromState(PlayerPrefabCreatorPanelState state, out bool succeeded)
        {
            succeeded = state.humanoidMeshSource != null
                ? ApplyVisualRebuild(state)
                : CreateFromTemplateOnly(state);
            return true;
        }

        public static bool SaveDefinitionAsset(
            PlayerPrefabCreatorPanelState state,
            ref PlayerVisualDefinition workingDefinition,
            string definitionAssetFileName)
        {
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                    workingDefinition,
                    definitionAssetFileName,
                    state.prefabFileName,
                    state.displayName,
                    out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            SyncToDefinition(state, workingDefinition);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PlayersData);

            string fileName = PlayerPrefabVisualSetupUtility.SanitizeFileName(definitionAssetFileName, "Player_Default");
            string path = $"{ProjectAssetPaths.PlayersData}/{fileName}.asset";

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            workingDefinition.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);

            PlayerVisualDefinition existing = AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(path);
            if (existing == null)
            {
                PlayerVisualDefinition asset = Object.Instantiate(workingDefinition);
                asset.name = fileName;
                AssetDatabase.CreateAsset(asset, path);
                workingDefinition = asset;
            }
            else
            {
                EditorUtility.CopySerialized(workingDefinition, existing);
                EditorUtility.SetDirty(existing);
                workingDefinition = existing;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            EditorGUIUtility.PingObject(workingDefinition);
            Debug.Log($"Saved player visual definition to {path} (output={outputPath})");
            return true;
        }

        public static void DrawStatus(PlayerPrefabCreatorPanelState state)
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            DMCharacterCreatorSharedUi.DrawHumanoidPrefabStatus(
                outputPath,
                state.visualChildName,
                showSpawnReady: false,
                spawnReadyCheck: null);
        }

        public static void DrawActions(PlayerPrefabCreatorPanelState state, bool compact, System.Action onPrimarySucceeded = null)
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            bool outputExists = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null;
            bool blocked = PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath);
            bool requireModel = state.humanoidMeshSource != null;
            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectPlayerCreateBlockers(state, requireModel));
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                DMHumanoidVisualRebuildUtility.BoneRenameHelp,
                MessageType.Info);

            if (!compact)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = outputExists && !blocked;
                if (GUILayout.Button("Repair Visual (No Mesh Change)", GUILayout.Height(30f)))
                    RepairWithoutMesh(state);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = !blocked && (state.humanoidMeshSource != null || !outputExists);
            string primaryLabel = state.humanoidMeshSource != null
                ? (outputExists ? "Apply Visual / Rebuild" : "Create Prefab + Apply Visual")
                : (outputExists ? "Rebuild Prefab" : "Create Prefab");
            if (GUILayout.Button(
                    primaryLabel,
                    GUILayout.Height(compact ? 28f : 34f),
                    GUILayout.ExpandWidth(true)))
            {
                if (TryCreatePrefabFromState(state, out bool succeeded) && succeeded)
                    onPrimarySucceeded?.Invoke();
            }

            GUI.enabled = !blocked && state.humanoidMeshSource != null;
            if (GUILayout.Button(
                    "Rebuild From Template + Apply Visual",
                    GUILayout.Height(compact ? 26f : 30f),
                    GUILayout.ExpandWidth(true)))
            {
                if (RebuildFromTemplateApplyVisual(state))
                    onPrimarySucceeded?.Invoke();
            }

            GUI.enabled = true;

            if (outputExists && !blocked && GUILayout.Button("Dedup Weapon Holders", GUILayout.Height(26f)))
                DedupOutput(state);
        }

        public static void PrepareModelImport(PlayerPrefabCreatorPanelState state)
        {
            if (state.humanoidMeshSource == null)
                return;

            string path = EnemyModelAvatarUtility.ResolvePreferredModelAssetPath(state.humanoidMeshSource);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog(
                    "Prepare Model Import",
                    "Could not resolve an FBX/model asset for this prefab. Assign the source FBX or a prefab whose meshes reference an importable model.",
                    "OK");
                return;
            }

            if (!EnemyModelAvatarUtility.TryPrepareModelImport(path, out string message))
            {
                EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
                return;
            }

            state.humanoidMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AutoDetect(state);
            EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
        }

        public static void ForceHumanoid(PlayerPrefabCreatorPanelState state)
        {
            if (state.humanoidMeshSource == null)
                return;

            string path = EnemyModelAvatarUtility.ResolvePreferredModelAssetPath(state.humanoidMeshSource);

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(state.humanoidMeshSource);
            if (inspection.AnimationType == ModelImporterAnimationType.Generic &&
                !EditorUtility.DisplayDialog(
                    "Force Humanoid Rig",
                    "This FBX is Generic (often used for creatures). Force Humanoid import anyway?",
                    "Force Humanoid",
                    "Cancel"))
            {
                return;
            }

            if (!EnemyModelAvatarUtility.TryForceHumanoidImport(path, out string message))
            {
                EditorUtility.DisplayDialog("Force Humanoid Rig", message, "OK");
                return;
            }

            state.humanoidMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.DisplayDialog("Force Humanoid Rig", message, "OK");
        }

        public static void AutoDetect(PlayerPrefabCreatorPanelState state)
        {
            if (state.humanoidMeshSource == null)
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(state.humanoidMeshSource);
            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid &&
                string.IsNullOrWhiteSpace(state.visualChildName))
            {
                state.visualChildName = "Visual";
            }

            if (string.IsNullOrWhiteSpace(state.displayName))
                state.displayName = state.humanoidMeshSource.name.Replace('_', ' ');

            if (string.IsNullOrWhiteSpace(state.prefabFileName) || state.prefabFileName == "Player_Custom")
            {
                state.prefabFileName = PlayerPrefabVisualSetupUtility.SanitizeFileName(
                    "Player_" + state.humanoidMeshSource.name, "Player_Custom");
            }
        }

        public static bool ApplyVisualRebuild(PlayerPrefabCreatorPanelState state)
        {
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(state, requireModel: true, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);

            GameObject model = state.humanoidMeshSource;
            if (!EnemyModelAvatarUtility.EnsureRigReadyForHumanoidPaste(
                    model,
                    state.autoPrepareOnApply,
                    allowForceHumanoid: false,
                    out model,
                    out string rigMessage) &&
                !EnemyModelAvatarUtility.IsReadyForHumanoidPaste(model))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    string.IsNullOrEmpty(rigMessage)
                        ? "Model is not Humanoid-ready for visual paste. Use Prepare Model Import."
                        : rigMessage);
                return false;
            }

            state.humanoidMeshSource = model;
            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                model,
                state.visualChildName,
                state.templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not rebuild at {outputPath}.", "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return true;
        }

        public static bool RebuildFromTemplateApplyVisual(PlayerPrefabCreatorPanelState state)
        {
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(state, requireModel: true, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            if (!DMHumanoidVisualRebuildUtility.ConfirmOverwriteExisting(outputPath))
                return false;

            GameObject model = EnemyModelAvatarUtility.ResolvePreferredVisualModel(state.humanoidMeshSource);
            if (!EnemyModelAvatarUtility.EnsureRigReadyForHumanoidPaste(
                    model,
                    autoPrepareImport: true,
                    allowForceHumanoid: true,
                    out model,
                    out string rigMessage) &&
                !EnemyModelAvatarUtility.IsReadyForHumanoidPaste(model))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    string.IsNullOrEmpty(rigMessage)
                        ? "Model is not Humanoid-ready for visual paste. Use Prepare Model Import or Force Humanoid Rig."
                        : rigMessage);
                return false;
            }

            state.humanoidMeshSource = model;
            GameObject created = DMHumanoidVisualRebuildUtility.RebuildPlayerFromTemplate(
                outputPath,
                model,
                state.visualChildName,
                state.templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    DMHumanoidVisualRebuildUtility.RebuildDialogTitle,
                    $"Could not rebuild at {outputPath}. See Console.",
                    "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return true;
        }

        public static bool CreateFromTemplateOnly(PlayerPrefabCreatorPanelState state)
        {
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerCreatePrefab(state, requireModel: false, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);

            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                null,
                state.visualChildName,
                state.templatePrefab);
            if (created == null)
                return false;

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return true;
        }

        public static bool RepairWithoutMesh(PlayerPrefabCreatorPanelState state)
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(outputPath))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not repair {outputPath}.", "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            GameObject repaired = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            Selection.activeObject = repaired;
            EditorGUIUtility.PingObject(repaired);
            return true;
        }

        public static void DedupOutput(PlayerPrefabCreatorPanelState state)
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            if (!PlayerV7WeaponHolderDedupUtility.DedupAndRepair(outputPath, out _, createFileBackup: false))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", "Dedup failed. See Console.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Player Prefab Creator", "Weapon holder dedup + repair complete.", "OK");
        }

        public static void SyncFromDefinition(PlayerPrefabCreatorPanelState state, PlayerVisualDefinition def)
        {
            if (def == null)
                return;

            state.displayName = def.displayName;
            state.prefabFileName = def.prefabFileName;
            state.templatePrefab = def.templatePrefab != null
                ? def.templatePrefab
                : PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            state.visualChildName = string.IsNullOrWhiteSpace(def.visualChildName) ? "Visual" : def.visualChildName;
            state.humanoidMeshSource = def.lastModelSource;
        }

        public static void SyncToDefinition(PlayerPrefabCreatorPanelState state, PlayerVisualDefinition def)
        {
            if (def == null)
                return;

            def.displayName = state.displayName;
            def.prefabFileName = state.prefabFileName;
            def.templatePrefab = state.templatePrefab;
            def.visualChildName = state.visualChildName;
            def.lastModelSource = state.humanoidMeshSource;
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(state.prefabFileName, state.displayName);
            def.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        }
    }
}
#endif
