#if UNITY_EDITOR
using Project.AI;
using Project.EditorTools.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public sealed class EnemyHumanoidPrefabCreatorPanelState
    {
        public GameObject templatePrefab;
        public GameObject humanoidMeshSource;
        public string visualChildName = "Visual";
        public string prefabFileName = "NewEnemy";
        public string displayName = "New Enemy";
        public bool autoPrepareOnApply;
        public EnemyDefinition definition;
    }

    public static class EnemyHumanoidPrefabCreatorPanel
    {
        public static void DrawTemplateAndOutput(EnemyHumanoidPrefabCreatorPanelState state)
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
                state.templatePrefab = EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Use HumanoidEnemy Template", () =>
                {
                    state.templatePrefab = EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();
                }, true),
                ("Ping Template", () =>
                {
                    Selection.activeObject = state.templatePrefab;
                    EditorGUIUtility.PingObject(state.templatePrefab);
                }, state.templatePrefab != null));

            string templatePath = EnemyPrefabVisualSetupUtility.ResolveTemplatePath(state.templatePrefab);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Template: ", templatePath);

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            DMCharacterCreatorSharedUi.DrawAssetPathLabel("Output: ", outputPath);

            if (EnemyPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath))
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Prefab File Name resolves to HumanoidEnemy_Invector — protected template. Rename output.",
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

        public static void DrawModelSection(EnemyHumanoidPrefabCreatorPanelState state)
        {
            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            state.humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                state.humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && state.humanoidMeshSource != null)
                AutoDetect(state);

            if (state.humanoidMeshSource != null && string.IsNullOrWhiteSpace(state.visualChildName))
                state.visualChildName = EnemyInvectorSetupUtility.SuggestVisualChildName(state.humanoidMeshSource);

            state.visualChildName = EditorGUILayout.TextField("Visual Child Name", state.visualChildName);
            state.autoPrepareOnApply = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Auto-prepare import on Apply",
                state.autoPrepareOnApply);

            DMCharacterCreatorSharedUi.DrawModelInspectionPanel(
                state.humanoidMeshSource,
                "Assign a Meshy/character FBX to inspect rig, avatar, and scale.",
                playerRecommendations: false);

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                22f,
                ("Prepare Model Import", () => PrepareModelImport(state), state.humanoidMeshSource != null),
                ("Auto-Detect Archetype", () => AutoDetect(state), state.humanoidMeshSource != null),
                ("Force Humanoid Rig", () => ForceHumanoid(state), state.humanoidMeshSource != null));
        }

        public static void DrawStatus(EnemyHumanoidPrefabCreatorPanelState state)
        {
            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            DMCharacterCreatorSharedUi.DrawHumanoidPrefabStatus(
                outputPath,
                state.visualChildName,
                showSpawnReady: true,
                spawnReadyCheck: EnemyPrefabResolver.IsSpawnReady);
        }

        public static void DrawActions(EnemyHumanoidPrefabCreatorPanelState state, bool compact, System.Action onPrimarySucceeded = null)
        {
            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            bool outputExists = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null;
            bool blocked = EnemyPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath);
            bool requireModel = state.humanoidMeshSource != null;
            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectEnemyCreateBlockers(state, requireModel));
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

        public static void PrepareModelImport(EnemyHumanoidPrefabCreatorPanelState state)
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

        public static void ForceHumanoid(EnemyHumanoidPrefabCreatorPanelState state)
        {
            if (state.humanoidMeshSource == null)
                return;

            string path = EnemyModelAvatarUtility.ResolvePreferredModelAssetPath(state.humanoidMeshSource);

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(state.humanoidMeshSource);
            if (inspection.AnimationType == ModelImporterAnimationType.Generic &&
                !EditorUtility.DisplayDialog(
                    "Force Humanoid Rig",
                    "This FBX is Generic (creature/quads). Force Humanoid import anyway?",
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
            AutoDetect(state);
            EditorUtility.DisplayDialog("Force Humanoid Rig", message, "OK");
        }

        public static void AutoDetect(EnemyHumanoidPrefabCreatorPanelState state)
        {
            if (state.humanoidMeshSource == null || state.definition == null)
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(state.humanoidMeshSource);
            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid)
            {
                state.definition.archetype = EnemyArchetype.HumanoidInvector;
                if (string.IsNullOrWhiteSpace(state.visualChildName))
                    state.visualChildName = "Visual";
            }
            else if (inspection.AnimationType == ModelImporterAnimationType.Generic || inspection.HasModel)
            {
                state.definition.archetype = EnemyArchetype.LegacyCreature;
            }

            if (string.IsNullOrWhiteSpace(state.displayName) || state.displayName == "New Enemy")
                state.displayName = state.humanoidMeshSource.name.Replace('_', ' ');

            if (string.IsNullOrWhiteSpace(state.prefabFileName) || state.prefabFileName == "NewEnemy")
            {
                state.prefabFileName = EnemyPrefabBuilder.SanitizeFileName(
                    state.humanoidMeshSource.name, "Enemy");
            }
        }

        public static bool ApplyVisualRebuild(EnemyHumanoidPrefabCreatorPanelState state)
        {
            EnemyPrefabCreatorPanel.EnsureEnemyIdentityDefaults(state.definition);

            if (!DMCharacterCreatorActionValidation.TryValidateEnemyCreatePrefab(state, requireModel: true, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);

            SyncToDefinition(state);
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
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    string.IsNullOrEmpty(rigMessage)
                        ? "Model is not Humanoid-ready for visual paste. Use Prepare Model Import."
                        : rigMessage);
                return false;
            }

            state.humanoidMeshSource = model;
            state.definition.lastModelSource = model;

            GameObject created = EnemyPrefabVisualSetupUtility.CreateOrRebuildEnemyPrefab(
                outputPath,
                model,
                state.visualChildName,
                state.templatePrefab,
                state.definition,
                state.autoPrepareOnApply,
                allowForceHumanoid: false,
                forceFreshTemplateClone: false);

            if (created == null)
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", $"Could not rebuild at {outputPath}.", "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return true;
        }

        public static bool RebuildFromTemplateApplyVisual(EnemyHumanoidPrefabCreatorPanelState state)
        {
            EnemyPrefabCreatorPanel.EnsureEnemyIdentityDefaults(state.definition);

            if (!DMCharacterCreatorActionValidation.TryValidateEnemyCreatePrefab(state, requireModel: true, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            if (!DMHumanoidVisualRebuildUtility.ConfirmOverwriteExisting(outputPath))
                return false;

            SyncToDefinition(state);
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
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    string.IsNullOrEmpty(rigMessage)
                        ? "Model is not Humanoid-ready for visual paste. Use Prepare Model Import or Force Humanoid Rig."
                        : rigMessage);
                return false;
            }

            state.humanoidMeshSource = model;
            if (state.definition != null)
                state.definition.lastModelSource = model;

            GameObject created = DMHumanoidVisualRebuildUtility.RebuildEnemyFromTemplate(
                outputPath,
                model,
                state.visualChildName,
                state.templatePrefab,
                state.definition);

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

        public static bool TryCreatePrefabFromState(EnemyHumanoidPrefabCreatorPanelState state, out bool succeeded)
        {
            succeeded = state.humanoidMeshSource != null
                ? ApplyVisualRebuild(state)
                : CreateFromTemplateOnly(state);
            return true;
        }

        public static bool CreateFromTemplateOnly(EnemyHumanoidPrefabCreatorPanelState state)
        {
            if (!DMCharacterCreatorActionValidation.TryValidateEnemyCreatePrefab(state, requireModel: false, out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    validationMessage);
                return false;
            }

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);

            SyncToDefinition(state);
            GameObject created = EnemyPrefabVisualSetupUtility.CreateOrRebuildEnemyPrefab(
                outputPath,
                null,
                state.visualChildName,
                state.templatePrefab,
                state.definition);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    $"Could not create enemy prefab at {outputPath}. See Console for details.",
                    "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return true;
        }

        public static bool RepairWithoutMesh(EnemyHumanoidPrefabCreatorPanelState state)
        {
            if (state.definition == null)
                return false;

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            SyncToDefinition(state);
            if (!EnemyPrefabVisualSetupUtility.RepairVisualAtPath(outputPath, state.definition))
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", $"Could not repair {outputPath}.", "OK");
                return false;
            }

            AssetDatabase.SaveAssets();
            GameObject repaired = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            Selection.activeObject = repaired;
            EditorGUIUtility.PingObject(repaired);
            return true;
        }

        public static void DedupOutput(EnemyHumanoidPrefabCreatorPanelState state)
        {
            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                state.prefabFileName, state.displayName);
            if (!PlayerV7WeaponHolderDedupUtility.DedupAndRepair(outputPath, out _, createFileBackup: false))
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", "Dedup failed. See Console.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Enemy Prefab Creator", "Weapon holder dedup + repair complete.", "OK");
        }

        public static void SyncFromDefinition(EnemyHumanoidPrefabCreatorPanelState state, EnemyDefinition def)
        {
            if (def == null)
                return;

            state.definition = def;
            state.displayName = def.displayName;
            state.prefabFileName = def.prefabFileName;
            state.visualChildName = string.IsNullOrWhiteSpace(def.visualChildName) ? "Visual" : def.visualChildName;
            state.templatePrefab = def.templatePrefab != null
                ? def.templatePrefab
                : EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();
            state.humanoidMeshSource = def.lastModelSource;
        }

        public static void SyncToDefinition(EnemyHumanoidPrefabCreatorPanelState state)
        {
            if (state.definition == null)
                return;

            state.definition.displayName = state.displayName;
            state.definition.prefabFileName = state.prefabFileName;
            state.definition.visualChildName = state.visualChildName;
            state.definition.templatePrefab = state.templatePrefab;
            state.definition.lastModelSource = state.humanoidMeshSource;
            state.definition.archetype = EnemyArchetype.HumanoidInvector;
        }
    }
}
#endif
