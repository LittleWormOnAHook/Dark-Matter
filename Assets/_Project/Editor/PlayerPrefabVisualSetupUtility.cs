#if UNITY_EDITOR
using System.IO;
using Invector.vCharacterController;
using Project.AI.Invector;
using Project.Data;
using Project.EditorTools.Invector;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Clones Meshy/custom humanoid visuals onto new player prefab variants.
    /// Template is always <c>Player_Invector.prefab</c> (or an override) — never overwritten by default.
    /// </summary>
    public static class PlayerPrefabVisualSetupUtility
    {
        public const string DefaultPlayerPrefabPath = ProjectAssetPaths.PlayerInvectorPrefab;
        public const string ProtectedTemplateFileName = "Player_Invector";

        public static GameObject LoadDefaultPlayerPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPlayerPrefabPath);
        }

        /// <summary>
        /// Maps a template picked from the scene Hierarchy to its prefab asset. Unapplied scene overrides are
        /// not part of the asset, so <paramref name="warning"/> says so (Apply them to the prefab first).
        /// </summary>
        public static GameObject NormalizeTemplateSelection(GameObject picked, out string warning)
        {
            warning = null;
            if (picked == null || EditorUtility.IsPersistent(picked))
                return picked;

            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(picked);
            GameObject asset = instanceRoot != null
                ? PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot)
                : null;
            if (asset == null)
            {
                warning = $"'{picked.name}' is a scene object that is not a prefab instance. " +
                          "Save it as a prefab asset and assign that asset as the template.";
                return null;
            }

            asset = asset.transform.root.gameObject;
            if (PrefabUtility.HasPrefabInstanceAnyOverrides(instanceRoot, false))
            {
                warning = $"Scene object '{instanceRoot.name}' has unapplied overrides — the creator uses the asset " +
                          $"'{AssetDatabase.GetAssetPath(asset)}' without them. Overrides > Apply first if you need them.";
            }

            return asset;
        }

        public static string ResolveTemplatePath(GameObject templateOverride)
        {
            if (templateOverride != null)
            {
                templateOverride = NormalizeTemplateSelection(templateOverride, out _) ?? templateOverride;
                string path = AssetDatabase.GetAssetPath(templateOverride);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }

            return DefaultPlayerPrefabPath;
        }

        public static string ResolveOutputPrefabPath(string prefabFileName, string displayNameFallback = null)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPlayers);
            string fileName = SanitizeFileName(prefabFileName, displayNameFallback ?? "Player_Custom");
            return $"{ProjectAssetPaths.PrefabsPlayers}/{fileName}.prefab";
        }

        public static string ResolveOutputPrefabPath(PlayerVisualDefinition definition)
        {
            if (definition == null)
                return ResolveOutputPrefabPath("Player_Custom");

            return ResolveOutputPrefabPath(definition.prefabFileName, definition.displayName);
        }

        public static bool IsProtectedTemplatePath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return false;

            string normalized = prefabPath.Replace('\\', '/');
            string protectedPath = DefaultPlayerPrefabPath.Replace('\\', '/');
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
                    "Choose a different Prefab File Name (e.g. Player_MeshyAndroid). " +
                    "Player_Invector is the clone source only.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates or rebuilds a player variant at <paramref name="outputPath"/> by cloning the template,
        /// then optionally applying a Meshy/custom visual.
        /// </summary>
        public static GameObject CreateOrRebuildPlayerPrefab(
            string outputPath,
            GameObject modelSource,
            string visualChildName = "Visual",
            GameObject templateOverride = null,
            bool autoPrepareImport = false,
            bool allowForceHumanoid = false,
            bool forceFreshTemplateClone = false)
        {
            if (!TryValidateOutputPath(outputPath, out string error))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] {error}");
                return null;
            }

            if (templateOverride != null)
            {
                GameObject normalized = NormalizeTemplateSelection(templateOverride, out string templateWarning);
                if (!string.IsNullOrEmpty(templateWarning))
                    Debug.LogWarning($"[PlayerPrefabVisualSetup] {templateWarning}", templateOverride);
                if (normalized == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(normalized)))
                {
                    Debug.LogError(
                        $"[PlayerPrefabVisualSetup] Template '{templateOverride.name}' has no prefab asset; refusing to " +
                        "silently fall back to Player_Invector.", templateOverride);
                    return null;
                }

                templateOverride = normalized;
            }

            string templatePath = ResolveTemplatePath(templateOverride);
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] Template prefab not found: {templatePath}");
                return null;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPlayers);

            if (forceFreshTemplateClone || !File.Exists(outputPath))
            {
                if (!DMHumanoidVisualRebuildUtility.TryCloneTemplateOverOutput(
                        templatePath,
                        outputPath,
                        overwriteExisting: forceFreshTemplateClone,
                        isProtected: IsProtectedTemplatePath,
                        out string cloneError))
                {
                    Debug.LogError($"[PlayerPrefabVisualSetup] {cloneError}");
                    return null;
                }
            }

            GameObject resolvedModel = modelSource;
            bool useSourceAsIs = ShouldUseSourceAsIs(modelSource);
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
                    Debug.LogWarning($"[PlayerPrefabVisualSetup] Model not ready for humanoid paste: {rigMessage}");
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
                    AttachVisualModel(
                        root,
                        useSourceAsIs ? modelSource : resolvedModel,
                        visualChildName,
                        templateAsset,
                        DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(outputPath),
                        useSourceAsIs);
                }

                // Always finalize so template-only creates also save edit-mode bind/T-pose
                // (Animator disabled) instead of freezing a mid-clip template pose.
                FinalizePlayerVisualRoot(root);

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

        public static bool ApplyVisualAtPath(
            string prefabPath,
            GameObject modelSource,
            string visualChildName = "Visual")
        {
            if (!TryValidateOutputPath(prefabPath, out string error))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] {error}");
                return false;
            }

            if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] Prefab not found: {prefabPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                if (modelSource != null)
                {
                    GameObject templateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                        ResolveTemplatePath(null));
                    AttachVisualModel(
                        root,
                        modelSource,
                        visualChildName,
                        templateAsset,
                        DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(prefabPath),
                        ShouldUseSourceAsIs(modelSource));
                }

                FinalizePlayerVisualRoot(root);
                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(root, visualChildName);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                Selection.activeObject = null;
            }
        }

        /// <summary>
        /// Rebinds holders, repairs weapon PioneerVisuals, and restores edit-mode bind pose
        /// without requiring a new Meshy mesh. Refuses to modify the protected template.
        /// </summary>
        public static bool RepairVisualAtPath(string prefabPath)
        {
            if (!TryValidateOutputPath(prefabPath, out string error))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] {error}");
                return false;
            }

            if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] Prefab not found: {prefabPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                FinalizePlayerVisualRoot(root);
                DMHumanoidPrefabSaveUtility.PrepareOutputPrefabForSave(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                Selection.activeObject = null;
            }
        }

        public static void FinalizePlayerVisualRoot(GameObject root)
        {
            if (root == null)
                return;

            DMHumanoidVisualFinalizeUtility.FinalizeVisualCommon(root);
            // Undo template-rig offsets that the placement bake / slot repair re-applied over carried nodes.
            DMHumanoidCombatNodeCarryUtility.ReapplyCarriedPoses(root);
            PlayerInvectorRuntimeSetupEditor.WireRuntimeReferences(root);
        }

        public static void AttachVisualModel(
            GameObject root,
            GameObject visualSource,
            string visualChildName,
            GameObject templateForBoneNames = null,
            string avatarPersistPath = null,
            bool useSourceAsIs = false)
        {
            DMHumanoidVisualAttachUtility.AttachVisualModel(
                root,
                visualSource,
                visualChildName,
                DMHumanoidVisualTarget.Player,
                templateForBoneNames,
                avatarPersistPath,
                carryCombatNodes: true,
                useSourceAsIs: useSourceAsIs);
        }

        /// <summary>
        /// A mesh wrapper prefab (FBX instance + props such as a rebreather, authored root rotation) is used
        /// as-is instead of being swapped for its raw FBX. Invector character prefabs never qualify.
        /// </summary>
        public static bool ShouldUseSourceAsIs(GameObject modelSource)
        {
            if (modelSource == null)
                return false;
            string path = AssetDatabase.GetAssetPath(modelSource);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                return false;
            if (EnemyModelAvatarUtility.LooksLikeInvectorCharacterPrefab(modelSource))
                return false;
            Animator animator = modelSource.GetComponentInChildren<Animator>(true);
            return animator != null && animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid;
        }

        public static void RepairEditModeAnimator(GameObject root)
        {
            DMHumanoidVisualFinalizeUtility.RepairEditModeAnimator(root);
        }

        public static string SuggestVisualChildName(GameObject model)
        {
            return EnemyInvectorSetupUtility.SuggestVisualChildName(model);
        }

        public static string SanitizeFileName(string preferred, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "Player_Custom";

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                raw = raw.Replace(invalid[i], '_');

            return raw.Replace(' ', '_');
        }

        public static PlayerVisualDefinition[] LoadAllDefinitions()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PlayersData);
            string[] guids = AssetDatabase.FindAssets("t:PlayerVisualDefinition", new[] { ProjectAssetPaths.PlayersData });
            var list = new System.Collections.Generic.List<PlayerVisualDefinition>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                PlayerVisualDefinition def =
                    AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (def != null)
                    list.Add(def);
            }

            return list.ToArray();
        }

        public static PlayerVisualDefinition EnsureDefaultDefinitionAsset()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PlayersData);
            string path = ProjectAssetPaths.PlayersData + "/Player_Default.asset";
            PlayerVisualDefinition existing = AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(path);
            if (existing != null)
            {
                bool dirty = false;
                if (string.IsNullOrWhiteSpace(existing.prefabFileName) ||
                    IsProtectedTemplatePath(ResolveOutputPrefabPath(existing.prefabFileName, existing.displayName)))
                {
                    existing.prefabFileName = "Player_Custom";
                    dirty = true;
                }

                // Stock definition: Player_Invector is template source only — clear overwrite destination.
                if (existing.playerPrefab != null)
                {
                    string linked = AssetDatabase.GetAssetPath(existing.playerPrefab);
                    if (IsProtectedTemplatePath(linked))
                    {
                        existing.playerPrefab = null;
                        dirty = true;
                    }
                }

                if (existing.templatePrefab == null)
                {
                    existing.templatePrefab = LoadDefaultPlayerPrefab();
                    dirty = true;
                }

                if (dirty)
                    EditorUtility.SetDirty(existing);

                return existing;
            }

            PlayerVisualDefinition created = ScriptableObject.CreateInstance<PlayerVisualDefinition>();
            created.displayName = "Player Custom";
            created.prefabFileName = "Player_Custom";
            created.visualChildName = "Visual";
            created.templatePrefab = LoadDefaultPlayerPrefab();
            created.playerPrefab = null;
            created.notes =
                "Clones from Player_Invector (template). Create Prefab writes a new file — never overwrites the template.";
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
#endif
