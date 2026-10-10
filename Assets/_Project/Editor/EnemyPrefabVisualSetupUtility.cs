#if UNITY_EDITOR
using System.IO;
using Project.AI;
using Project.EditorTools.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Clones HumanoidEnemy_Invector (template) to output enemy prefabs and applies Meshy/custom visuals.
    /// </summary>
    public static class EnemyPrefabVisualSetupUtility
    {
        public const string ProtectedTemplateFileName = "HumanoidEnemy_Invector";

        public static GameObject LoadDefaultTemplate()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.HumanoidEnemyPrefab);
        }

        public static string ResolveTemplatePath(GameObject templateOverride)
        {
            if (templateOverride != null)
            {
                string path = AssetDatabase.GetAssetPath(templateOverride);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }

            return ProjectAssetPaths.HumanoidEnemyPrefab;
        }

        public static string ResolveOutputPrefabPath(string prefabFileName, string displayNameFallback = null)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombatEnemies);
            string fileName = EnemyPrefabBuilder.SanitizeFileName(prefabFileName, displayNameFallback ?? "Enemy");
            return $"{ProjectAssetPaths.PrefabsCombatEnemies}/{fileName}.prefab";
        }

        public static string ResolveOutputPrefabPath(EnemyDefinition definition)
        {
            if (definition == null)
                return ResolveOutputPrefabPath("NewEnemy");
            return ResolveOutputPrefabPath(definition.prefabFileName, definition.displayName);
        }

        public static bool IsProtectedTemplatePath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return false;

            string normalized = prefabPath.Replace('\\', '/');
            string protectedPath = ProjectAssetPaths.HumanoidEnemyPrefab.Replace('\\', '/');
            if (string.Equals(normalized, protectedPath, System.StringComparison.OrdinalIgnoreCase))
                return true;

            string fileName = Path.GetFileNameWithoutExtension(normalized);
            return string.Equals(fileName, ProtectedTemplateFileName, System.StringComparison.OrdinalIgnoreCase) ||
                   DMCharacterCreatorProtection.IsProtectedPrefabName(fileName);
        }

        public static bool TryValidateOutputPath(string outputPath, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(outputPath))
            {
                errorMessage = "Output prefab path is empty. Set Prefab File Name on the definition.";
                return false;
            }

            if (IsProtectedTemplatePath(outputPath))
            {
                errorMessage =
                    $"Refusing to write over the protected template '{ProtectedTemplateFileName}.prefab'. " +
                    "Choose a different Prefab File Name. HumanoidEnemy_Invector, Player_v7* and Player_Invector* are protected templates.";
                return false;
            }

            return true;
        }

        public static GameObject CreateOrRebuildEnemyPrefab(
            string outputPath,
            GameObject modelSource,
            string visualChildName,
            GameObject templateOverride,
            EnemyDefinition definition,
            bool autoPrepareImport = false,
            bool allowForceHumanoid = false,
            bool forceFreshTemplateClone = false)
        {
            if (!TryValidateOutputPath(outputPath, out string error))
            {
                Debug.LogError($"[EnemyPrefabVisualSetup] {error}");
                return null;
            }

            // The EnemyDefinition is the authority and must be the SAVED asset (never the panel's in-memory copy).
            if (definition != null)
            {
                if (!DMCharacterCreatorDefinitionLink.TryEnsureSaved(definition, out EnemyDefinition savedDefinition, out string linkError))
                {
                    Debug.LogError(
                        $"[EnemyPrefabVisualSetup] Build stopped: the saved definition link cannot be set. {linkError}");
                    return null;
                }

                definition = savedDefinition;
                DMCharacterCreatorPostBuildCheck.LastSavedDefinition = savedDefinition;
            }

            string templatePath = ResolveTemplatePath(templateOverride);
            if (!File.Exists(templatePath))
            {
                EnemyInvectorSetupUtility.EnsureHumanoidTemplatePrefabExists();
                templatePath = ResolveTemplatePath(null);
                if (!File.Exists(templatePath))
                {
                    Debug.LogError($"[EnemyPrefabVisualSetup] Template prefab not found: {templatePath}");
                    return null;
                }
            }

            // Rig check BEFORE any destructive step (a failed check must never cost the user an existing prefab).
            GameObject resolvedModel = modelSource;
            if (modelSource != null)
            {
                resolvedModel = EnemyModelAvatarUtility.ResolvePreferredVisualModel(modelSource);
                if (!EnemyModelAvatarUtility.EnsureRigReadyForHumanoidPaste(
                        resolvedModel,
                        autoPrepareImport,
                        allowForceHumanoid,
                        out resolvedModel,
                        out string rigMessage) &&
                    !EnemyModelAvatarUtility.IsReadyForHumanoidPaste(resolvedModel))
                {
                    Debug.LogWarning($"[EnemyPrefabVisualSetup] Model not ready for humanoid paste: {rigMessage}");
                    return null;
                }
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombatEnemies);

            if (forceFreshTemplateClone || !File.Exists(outputPath))
            {
                if (!DMHumanoidVisualRebuildUtility.TryCloneTemplateOverOutput(
                        templatePath,
                        outputPath,
                        overwriteExisting: forceFreshTemplateClone,
                        isProtected: IsProtectedTemplatePath,
                        out string cloneError))
                {
                    Debug.LogError($"[EnemyPrefabVisualSetup] {cloneError}");
                    return null;
                }
            }

            GameObject root = PrefabUtility.LoadPrefabContents(outputPath);
            if (root == null)
                return null;

            try
            {
                root.name = Path.GetFileNameWithoutExtension(outputPath);
                GameObject templateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);

                if (resolvedModel != null)
                {
                    EnemyInvectorSetupUtility.AttachVisualModel(
                        root,
                        resolvedModel,
                        visualChildName,
                        templateAsset,
                        DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(outputPath));
                    DMHumanoidVisualFinalizeUtility.FinalizeVisualCommon(root);
                }

                if (definition != null)
                    EnemyInvectorSetupUtility.RepairHumanoidRoot(root, definition);

                DMCharacterCreatorBuildReport report =
                    DMCharacterCreatorPostBuildCheck.Run(root, definition, outputPath, templateAsset);
                string reportText = report.Format();
                if (report.Failed)
                {
                    Debug.LogError(
                        $"[EnemyPrefabVisualSetup] Post-build check FAILED for '{outputPath}'. The prefab was NOT saved with this build.\n{reportText}");
                    return null;
                }

                if (report.Warnings.Count > 0)
                    Debug.LogWarning($"[EnemyPrefabVisualSetup] Post-build check for '{outputPath}':\n{reportText}");
                else
                    Debug.Log($"[EnemyPrefabVisualSetup] Post-build check passed for '{outputPath}'.\n{reportText}");

                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(root, visualChildName);
                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                Selection.activeObject = null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        }

        public static bool RepairVisualAtPath(string prefabPath, EnemyDefinition definition)
        {
            if (!TryValidateOutputPath(prefabPath, out string error))
            {
                Debug.LogError($"[EnemyPrefabVisualSetup] {error}");
                return false;
            }

            if (!File.Exists(prefabPath))
            {
                Debug.LogError($"[EnemyPrefabVisualSetup] Prefab not found: {prefabPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                EnemyDefinition target = definition ?? EnemyInvectorSetupUtility.ResolveDefinitionForPrefab(prefabPath);
                if (target != null)
                {
                    if (!DMCharacterCreatorDefinitionLink.TryEnsureSaved(target, out EnemyDefinition saved, out string linkError))
                    {
                        Debug.LogError($"[EnemyPrefabVisualSetup] Repair stopped: the saved definition link cannot be set. {linkError}");
                        return false;
                    }

                    target = saved;
                    DMCharacterCreatorPostBuildCheck.LastSavedDefinition = saved;
                    if (DMCharacterCreatorRepair.RebuildAvatarTPose(root, target, prefabPath, out string avatarMessage))
                        Debug.Log($"[EnemyPrefabVisualSetup] {avatarMessage}");
                    else if (!string.IsNullOrEmpty(avatarMessage))
                        Debug.LogWarning($"[EnemyPrefabVisualSetup] {avatarMessage}");
                }

                DMHumanoidVisualFinalizeUtility.FinalizeVisualCommon(root);
                EnemyInvectorSetupUtility.RepairHumanoidRoot(root, target);

                DMCharacterCreatorBuildReport report = DMCharacterCreatorPostBuildCheck.Run(
                    root, target, prefabPath, AssetDatabase.LoadAssetAtPath<GameObject>(ResolveTemplatePath(target != null ? target.templatePrefab : null)));
                if (report.Failed)
                {
                    Debug.LogError($"[EnemyPrefabVisualSetup] Repair check FAILED for '{prefabPath}'. Not saved.\n{report.Format()}");
                    return false;
                }

                if (report.Warnings.Count > 0)
                    Debug.LogWarning($"[EnemyPrefabVisualSetup] Repair check for '{prefabPath}':\n{report.Format()}");

                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(root, target?.visualChildName);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                Selection.activeObject = null;
            }
        }

        static bool TryCloneTemplateToOutput(string templatePath, string outputPath)
        {
            if (AssetDatabase.CopyAsset(templatePath, outputPath))
                return true;

            GameObject templateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (templateAsset == null)
            {
                Debug.LogError($"[EnemyPrefabVisualSetup] Template not loadable: {templatePath}");
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(templateAsset) as GameObject;
            if (instance == null)
            {
                Debug.LogError(
                    $"[EnemyPrefabVisualSetup] Failed to copy or instantiate template '{templatePath}' → '{outputPath}'.");
                return false;
            }

            try
            {
                instance.name = Path.GetFileNameWithoutExtension(outputPath);
                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(instance);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
                if (saved == null)
                {
                    Debug.LogError(
                        $"[EnemyPrefabVisualSetup] SaveAsPrefabAsset failed for '{outputPath}' from template '{templatePath}'.");
                    return false;
                }

                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        public static bool ApplyVisualAtPath(
            string prefabPath,
            GameObject modelSource,
            string visualChildName,
            EnemyDefinition definition,
            bool autoPrepareImport = false,
            bool allowForceHumanoid = false)
        {
            GameObject result = CreateOrRebuildEnemyPrefab(
                prefabPath,
                modelSource,
                visualChildName,
                definition != null ? definition.templatePrefab : null,
                definition,
                autoPrepareImport,
                allowForceHumanoid);
            return result != null;
        }
    }
}
#endif
