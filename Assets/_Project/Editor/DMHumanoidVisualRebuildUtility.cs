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
                "Replace the existing output with a fresh template clone, then attach the resolved FBX visual.\n\n" +
                "Protected templates (Player_Invector / HumanoidEnemy_Invector) are not modified. " +
                BoneRenameHelp,
                "Rebuild",
                "Cancel");
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
                if (!AssetDatabase.DeleteAsset(outputPath))
                {
                    error = $"Could not delete existing output '{outputPath}' for a fresh template clone.";
                    return false;
                }

                AssetDatabase.Refresh();
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
