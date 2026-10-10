#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared validation for Character Creator save / prefab actions (Enemy + Player).
    /// </summary>
    public static class DMCharacterCreatorActionValidation
    {
        public const string EnemyDialogTitle = "Enemy Prefab";
        public const string PlayerDialogTitle = "Player Prefab";

        const string PrepareModelHint =
            "Tip: click Prepare Model Import on the model row, or enable Auto-prepare import on Apply.";

        public static void ShowValidationDialog(string title, string message) =>
            DMStudioActionValidation.ShowValidationDialog(title, message);

        public static void DrawBlockersHelpBox(IReadOnlyList<string> blockers) =>
            DMStudioActionValidation.DrawBlockersHelpBox(blockers, "Save / Create");

        public static string FormatBlockerList(IReadOnlyList<string> blockers) =>
            DMStudioActionValidation.FormatBlockerList(blockers);

        public static bool TryValidateEnemySaveDefinition(
            EnemyDefinition definition,
            string definitionAssetFileName,
            out string message)
        {
            message = null;
            var blockers = CollectEnemySaveBlockers(definition, definitionAssetFileName);
            if (blockers.Count == 0)
                return true;

            message = FormatBlockerList(blockers);
            return false;
        }

        public static bool TryValidatePlayerSaveDefinition(
            PlayerVisualDefinition definition,
            string definitionAssetFileName,
            string prefabFileName,
            string displayName,
            out string message)
        {
            message = null;
            var blockers = CollectPlayerSaveBlockers(definition, definitionAssetFileName, prefabFileName, displayName);
            if (blockers.Count == 0)
                return true;

            message = FormatBlockerList(blockers);
            return false;
        }

        public static bool TryValidateEnemyCreatePrefab(
            EnemyHumanoidPrefabCreatorPanelState state,
            bool requireModel,
            out string message)
        {
            message = null;
            var blockers = CollectEnemyCreateBlockers(state, requireModel);
            if (blockers.Count == 0)
                return true;

            message = FormatBlockerList(blockers);
            return false;
        }

        public static bool TryValidatePlayerCreatePrefab(
            PlayerPrefabCreatorPanelState state,
            bool requireModel,
            out string message)
        {
            message = null;
            var blockers = CollectPlayerCreateBlockers(state, requireModel);
            if (blockers.Count == 0)
                return true;

            message = FormatBlockerList(blockers);
            return false;
        }

        public static List<string> CollectEnemySaveBlockers(
            EnemyDefinition definition,
            string definitionAssetFileName)
        {
            var blockers = new List<string>(4);
            if (definition == null)
            {
                blockers.Add("No enemy definition to save. Use New Custom or pick a library entry.");
                return blockers;
            }

            if (string.IsNullOrWhiteSpace(definition.enemyId))
                blockers.Add("Enemy Id is empty (required for Combat → Enemy Types).");

            if (string.IsNullOrWhiteSpace(definition.displayName))
                blockers.Add("Display Name is empty.");

            if (string.IsNullOrWhiteSpace(definition.prefabFileName))
                blockers.Add("Prefab File Name is empty.");

            TryAddDefinitionAssetNameBlockers(
                blockers,
                definitionAssetFileName,
                definition.enemyId,
                EnemyPrefabBuilder.SanitizeFileName);

            return blockers;
        }

        public static List<string> CollectPlayerSaveBlockers(
            PlayerVisualDefinition definition,
            string definitionAssetFileName,
            string prefabFileName,
            string displayName)
        {
            var blockers = new List<string>(4);
            if (definition == null)
            {
                blockers.Add("No player visual definition to save. Use New Custom or pick a library entry.");
                return blockers;
            }

            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(definition.displayName))
                blockers.Add("Display Name is empty.");

            if (string.IsNullOrWhiteSpace(prefabFileName) && string.IsNullOrWhiteSpace(definition.prefabFileName))
                blockers.Add("Prefab File Name is empty.");

            TryAddDefinitionAssetNameBlockers(
                blockers,
                definitionAssetFileName,
                "Player_Default",
                PlayerPrefabVisualSetupUtility.SanitizeFileName);

            return blockers;
        }

        public static List<string> CollectEnemyCreateBlockers(
            EnemyHumanoidPrefabCreatorPanelState state,
            bool requireModel)
        {
            var blockers = new List<string>(6);
            if (state == null)
            {
                blockers.Add("Panel state is missing.");
                return blockers;
            }

            if (state.definition == null)
                blockers.Add("Enemy definition is missing. Save identity or use New Custom first.");

            TryAddEnemyPrefabOutputBlockers(
                blockers,
                state.prefabFileName,
                state.displayName,
                EnemyPrefabVisualSetupUtility.ResolveTemplatePath(state.templatePrefab));

            if (requireModel)
                TryAddHumanoidModelBlockers(blockers, state.humanoidMeshSource, state.autoPrepareOnApply);

            if (string.IsNullOrWhiteSpace(state.visualChildName))
                blockers.Add("Visual Child Name is empty.");

            return blockers;
        }

        public static List<string> CollectPlayerCreateBlockers(
            PlayerPrefabCreatorPanelState state,
            bool requireModel)
        {
            var blockers = new List<string>(6);
            if (state == null)
            {
                blockers.Add("Panel state is missing.");
                return blockers;
            }

            TryAddPlayerPrefabOutputBlockers(
                blockers,
                state.prefabFileName,
                state.displayName,
                PlayerPrefabVisualSetupUtility.ResolveTemplatePath(state.templatePrefab));

            if (requireModel)
                TryAddHumanoidModelBlockers(blockers, state.humanoidMeshSource, state.autoPrepareOnApply);

            if (string.IsNullOrWhiteSpace(state.visualChildName))
                blockers.Add("Visual Child Name is empty.");

            return blockers;
        }

        static void TryAddDefinitionAssetNameBlockers(
            List<string> blockers,
            string rawName,
            string sanitizeFallback,
            System.Func<string, string, string> sanitize)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                blockers.Add("Definition Asset Name is empty (file name without .asset).");
                return;
            }

            if (!ContainsValidFileNameCharacter(rawName))
            {
                blockers.Add(
                    "Definition Asset Name has no valid file name characters. Use letters, numbers, or underscores.");
                return;
            }

            string sanitized = sanitize(rawName.Trim(), sanitizeFallback);
            if (string.IsNullOrWhiteSpace(sanitized))
                blockers.Add("Definition Asset Name could not be sanitized to a valid file name.");
        }

        static void TryAddEnemyPrefabOutputBlockers(
            List<string> blockers,
            string prefabFileName,
            string displayName,
            string templatePath)
        {
            if (string.IsNullOrWhiteSpace(prefabFileName))
                blockers.Add("Prefab File Name is empty.");

            if (string.IsNullOrWhiteSpace(displayName))
                blockers.Add("Display Name is empty.");

            string outputPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(prefabFileName, displayName);
            if (!EnemyPrefabVisualSetupUtility.TryValidateOutputPath(outputPath, out string pathError))
                blockers.Add(pathError);

            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                blockers.Add(
                    "Template prefab is missing or not found on disk. Assign Template Prefab or use the default template button.");
            }
        }

        static void TryAddPlayerPrefabOutputBlockers(
            List<string> blockers,
            string prefabFileName,
            string displayName,
            string templatePath)
        {
            if (string.IsNullOrWhiteSpace(prefabFileName))
                blockers.Add("Prefab File Name is empty.");

            if (string.IsNullOrWhiteSpace(displayName))
                blockers.Add("Display Name is empty.");

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(prefabFileName, displayName);
            if (!PlayerPrefabVisualSetupUtility.TryValidateOutputPath(outputPath, out string pathError))
                blockers.Add(pathError);

            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                blockers.Add(
                    "Template prefab is missing or not found on disk. Assign Template Prefab or use the default template button.");
            }
        }

        static void TryAddHumanoidModelBlockers(
            List<string> blockers,
            GameObject humanoidMeshSource,
            bool autoPrepareOnApply)
        {
            if (humanoidMeshSource == null)
            {
                blockers.Add("Assign a Model FBX / Prefab before Create Prefab + Apply Visual.");
                return;
            }

            if (autoPrepareOnApply || EnemyModelAvatarUtility.IsReadyForHumanoidPaste(humanoidMeshSource))
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(humanoidMeshSource);
            string rigNote = inspection.Recommendation;
            if (string.IsNullOrWhiteSpace(rigNote))
                rigNote = "Model is not Humanoid-ready for visual paste.";

            blockers.Add($"{rigNote} {PrepareModelHint}");
        }

        static bool ContainsValidFileNameCharacter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == ' ')
                    continue;
                bool invalidChar = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (invalid[j] == c)
                    {
                        invalidChar = true;
                        break;
                    }
                }

                if (!invalidChar)
                    return true;
            }

            return false;
        }
    }
}
#endif
