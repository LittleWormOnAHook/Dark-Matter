#if UNITY_EDITOR
using System.IO;
using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Fresh-clone Humanoid Invector outputs and attach a resolved FBX visual.
    /// Visual bones are renamed to template names (e.g. ORG-hips → VBOT_:Hips) so
    /// BodySnaps / holders / hitboxes remount by name. Stock VBOT Mesh-LOD stays hidden.
    /// </summary>
    public static class DMHumanoidVisualRebuildUtility
    {
        public const string RebuildDialogTitle = "Rebuild From Template + Apply Visual";
        public const string BoneRenameHelp =
            "Visual bones are renamed to template names (e.g. ORG-hips → VBOT_:Hips). Tune weapon grips after rebuild.";

        public static bool ConfirmOverwriteExisting(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || !File.Exists(outputPath))
                return true;

            return EditorUtility.DisplayDialog(
                RebuildDialogTitle,
                "Rebuild the existing prefab IN PLACE from the template (its GUID and scene instances are kept), then attach the resolved FBX visual.\n\n" +
                "Manual tweaks on the prefab are reset to the template; the old file is backed up under Library/DMCreatorBackups. " +
                "Protected templates (Player_v7*, Player_Invector*, HumanoidEnemy_Invector) are never modified. " +
                BoneRenameHelp,
                "Rebuild",
                "Cancel");
        }

        /// <summary>Copies the existing output to Library/DMCreatorBackups (outside Assets). Returns the backup path or null.</summary>
        public static string BackupExistingOutput(string outputPath)
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string dir = Path.Combine(projectRoot, "Library", "DMCreatorBackups");
                Directory.CreateDirectory(dir);
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dest = Path.Combine(dir, Path.GetFileNameWithoutExtension(outputPath) + "_" + stamp + ".prefab");
                File.Copy(outputPath, dest, true);
                return dest.Replace('\\', '/');
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DMHumanoidVisualRebuild] Could not back up '{outputPath}': {ex.Message}");
                return null;
            }
        }

        public static bool TryCloneTemplateOverOutput(
            string templatePath,
            string outputPath,
            bool overwriteExisting,
            System.Func<string, bool> isProtected,
            out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                error = $"Template prefab not found: {templatePath}";
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                error = "Output prefab path is empty.";
                return false;
            }

            if (isProtected != null && isProtected(outputPath))
            {
                error = "Refusing to write over a protected Invector template.";
                return false;
            }

            string normalizedTemplate = templatePath.Replace('\\', '/');
            string normalizedOutput = outputPath.Replace('\\', '/');
            if (string.Equals(normalizedTemplate, normalizedOutput, System.StringComparison.OrdinalIgnoreCase))
            {
                error = "Output path matches the template. Choose a different Prefab File Name.";
                return false;
            }

            if (overwriteExisting && File.Exists(outputPath))
            {
                // Rebuild IN PLACE: overwrite the prefab file's contents but keep its .meta, so the GUID and every
                // scene / prefab reference to it survive. The previous file is backed up outside Assets first.
                string backup = BackupExistingOutput(outputPath);
                try
                {
                    File.Copy(templatePath, outputPath, true);
                }
                catch (System.Exception ex)
                {
                    error = $"Could not overwrite '{outputPath}' in place: {ex.Message}" +
                            (backup != null ? $" (backup: {backup})" : string.Empty);
                    return false;
                }

                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                Debug.Log(
                    $"[DMHumanoidVisualRebuild] Rebuilt '{outputPath}' in place (GUID kept). " +
                    "Manual tweaks on the old prefab were reset to the template" +
                    (backup != null ? $"; previous file backed up to {backup}." : "."));
                return true;
            }

            if (File.Exists(outputPath))
                return true;

            CraftingEditorUtility.EnsureFolder(Path.GetDirectoryName(outputPath)?.Replace('\\', '/'));
            if (AssetDatabase.CopyAsset(templatePath, outputPath))
            {
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                return true;
            }

            error = $"Failed to copy template '{templatePath}' → '{outputPath}'.";
            return false;
        }

        public static GameObject RebuildEnemyFromTemplate(
            string outputPath,
            GameObject modelSource,
            string visualChildName,
            GameObject templateOverride,
            EnemyDefinition definition,
            bool autoPrepareImport = true,
            bool allowForceHumanoid = true)
        {
            return EnemyPrefabVisualSetupUtility.CreateOrRebuildEnemyPrefab(
                outputPath,
                modelSource,
                visualChildName,
                templateOverride,
                definition,
                autoPrepareImport,
                allowForceHumanoid,
                forceFreshTemplateClone: true);
        }

        public static GameObject RebuildPlayerFromTemplate(
            string outputPath,
            GameObject modelSource,
            string visualChildName,
            GameObject templateOverride,
            bool autoPrepareImport = true,
            bool allowForceHumanoid = true)
        {
            return PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                modelSource,
                visualChildName,
                templateOverride,
                autoPrepareImport,
                allowForceHumanoid,
                forceFreshTemplateClone: true);
        }
    }
}
#endif
