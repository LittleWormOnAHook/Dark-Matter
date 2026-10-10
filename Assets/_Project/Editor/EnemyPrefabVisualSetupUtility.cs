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
            return string.Equals(fileName, ProtectedTemplateFileName, System.StringComparison.OrdinalIgnoreCase);
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
                    "Choose a different Prefab File Name. HumanoidEnemy_Invector is the clone source only.";
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

            GameObject root = PrefabUtility.LoadPrefabContents(outputPath);
            if (root == null)
                return null;

            try
            {
                root.name = Path.GetFileNameWithoutExtension(outputPath);

                if (resolvedModel != null)
                {
                    GameObject templateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
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
                DMHumanoidVisualFinalizeUtility.FinalizeVisualCommon(root);
                if (definition != null)
                    EnemyInvectorSetupUtility.RepairHumanoidRoot(root, definition);
                else
                {
                    EnemyDefinition resolved = EnemyInvectorSetupUtility.ResolveDefinitionForPrefab(prefabPath);
                    EnemyInvectorSetupUtility.RepairHumanoidRoot(root, resolved);
                }

                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(root, definition?.visualChildName);
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
