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

        public static string ResolveTemplatePath(GameObject templateOverride)
        {
            if (templateOverride != null)
            {
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
            GameObject templateOverride = null)
        {
            if (!TryValidateOutputPath(outputPath, out string error))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] {error}");
                return null;
            }

            string templatePath = ResolveTemplatePath(templateOverride);
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[PlayerPrefabVisualSetup] Template prefab not found: {templatePath}");
                return null;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsPlayers);

            bool outputExists = File.Exists(outputPath);
            if (!outputExists)
            {
                // Copy the template asset so we never open/save Player_Invector itself.
                if (!AssetDatabase.CopyAsset(templatePath, outputPath))
                {
                    Debug.LogError(
                        $"[PlayerPrefabVisualSetup] Failed to copy template '{templatePath}' → '{outputPath}'.");
                    return null;
                }

                AssetDatabase.ImportAsset(outputPath);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(outputPath);
            if (root == null)
                return null;

            try
            {
                root.name = Path.GetFileNameWithoutExtension(outputPath);

                if (modelSource != null)
                    AttachVisualModel(root, modelSource, visualChildName);

                // Always finalize so template-only creates also save edit-mode bind/T-pose
                // (Animator disabled) instead of freezing a mid-clip template pose.
                FinalizePlayerVisualRoot(root);

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
                    AttachVisualModel(root, modelSource, visualChildName);

                FinalizePlayerVisualRoot(root);
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

            EditorLayoutGuard.BeforeDestroySceneObject(root);

            EnemyInvectorBodySnapSetupEditor.ConfigureEditor(root);
            EnemyInvectorWeaponHolderRebind.RebindToAnimatorBones(root);

            int remounted = PlayerInvectorRagdollSetup.RepairSeparatedRagdoll(root);
            if (remounted > 0)
            {
                Debug.Log(
                    $"[PlayerPrefabVisualSetup] Remounted orphan ragdoll onto avatar ({remounted} bone rigidbodies).",
                    root);
            }

            PlayerInvectorRuntimeSetupEditor.WireRuntimeReferences(root);
            EnemyInvectorSetupUtility.RepairWeaponSlotVisuals(root);
            RepairEditModeAnimator(root);

            EditorLayoutGuard.ScheduleInspectorRecovery();
        }

        public static void AttachVisualModel(GameObject root, GameObject visualSource, string visualChildName)
        {
            if (root == null || visualSource == null)
                return;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            ClearPreviousCustomVisual(root, childName);

            GameObject visualInstance = InstantiateVisualSource(visualSource);
            if (visualInstance == null)
                return;

            visualInstance.name = childName;
            visualInstance.transform.SetParent(root.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            EnemyModelAvatarUtility.PrepareModelInstance(visualInstance, preferHumanoidAvatar: true);
            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(visualInstance);

            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid && inspection.Avatar != null)
            {
                IntegrateHumanoidVisual(root, visualInstance, inspection.Avatar);
                Debug.Log(
                    $"[PlayerPrefabVisualSetup] Humanoid visual '{visualSource.name}' bound to player Animator " +
                    $"(avatar={inspection.Avatar.name}, {inspection.Summary}).",
                    root);
            }
            else
            {
                HideStockBodyMeshes(root);
                Debug.LogWarning(
                    $"[PlayerPrefabVisualSetup] Visual '{visualSource.name}' is not a valid Humanoid avatar " +
                    $"({inspection.Summary}). Nested under '{childName}' and stock body meshes were hidden. " +
                    inspection.Recommendation,
                    root);
            }
        }

        private static void IntegrateHumanoidVisual(GameObject root, GameObject visualInstance, Avatar avatar)
        {
            HideStockBodyMeshes(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null)
                rootAnimator = root.AddComponent<Animator>();

            RuntimeAnimatorController keepController = rootAnimator.runtimeAnimatorController;

            Animator[] nestedAnimators = visualInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < nestedAnimators.Length; i++)
            {
                if (nestedAnimators[i] != null && nestedAnimators[i].gameObject != root)
                    Object.DestroyImmediate(nestedAnimators[i]);
            }

            rootAnimator.avatar = avatar;
            if (keepController != null)
                rootAnimator.runtimeAnimatorController = keepController;
            rootAnimator.applyRootMotion = false;
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rootAnimator.Rebind();
            rootAnimator.Update(0f);

            EnableVisualUpdateWhenOffscreen(visualInstance);
            ClearPlayerDeadFlag(root);

            if (rootAnimator.GetBoneTransform(HumanBodyBones.Hips) == null)
            {
                Debug.LogWarning(
                    "[PlayerPrefabVisualSetup] Root Animator did not bind Hips after avatar swap. " +
                    "Check FBX Humanoid mapping. Visual remains nested; stock body stays hidden.",
                    root);
            }
            EnemyInvectorSetupUtility.RepairWeaponSlotVisuals(root);
        }

        /// <summary>
        /// Edit-mode: Animator off + avatar bind/rest pose. Play bootstrap re-enables.
        /// Does not sample Idle/attack clips (that froze mid-pose on droids).
        /// When humanoid bones were left mid-clip / muscle-baked, copies locals from the
        /// Avatar's source FBX under Visual so Scene view matches Meshy rest.
        /// Note: some Meshy "Walking" FBXs use a walk frame as skin bind — that rest is correct.
        /// Does not set health immortal (enemy repair path does).
        /// </summary>
        public static void RepairEditModeAnimator(GameObject root)
        {
            if (root == null)
                return;

            ClearPlayerDeadFlag(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null)
                rootAnimator = root.AddComponent<Animator>();

            Animator[] nested = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < nested.Length; i++)
            {
                if (nested[i] != null && nested[i] != rootAnimator)
                    Object.DestroyImmediate(nested[i], true);
            }

            rootAnimator.applyRootMotion = false;
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RuntimeAnimatorController savedController = rootAnimator.runtimeAnimatorController;
            rootAnimator.runtimeAnimatorController = null;
            rootAnimator.enabled = true;
            if (rootAnimator.avatar != null && rootAnimator.avatar.isValid)
            {
                rootAnimator.Rebind();
                rootAnimator.Update(0f);
            }

            // Authoritative Meshy/FBX rest — overwrites stuck mid-clip or bad muscle bakes.
            bool copiedFromFbx = TryCopyBindPoseFromAvatarSource(rootAnimator);

            rootAnimator.runtimeAnimatorController = savedController;
            // If we baked FBX locals, don't let disable wipe them with animator defaults.
            rootAnimator.writeDefaultValuesOnDisable = !copiedFromFbx;
            if (!Application.isPlaying)
                rootAnimator.enabled = false;

            if (copiedFromFbx)
                TryCopyBindPoseFromAvatarSource(rootAnimator);

            EnableVisualUpdateWhenOffscreen(root);

            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty disableAnim = so.FindProperty("disableAnimations");
                SerializedProperty useRoot = so.FindProperty("useRootMotion");
                if (disableAnim != null)
                    disableAnim.boolValue = false;
                if (useRoot != null)
                    useRoot.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Copies local TRS from the Avatar FBX onto matching bone names under Visual.
        /// </summary>
        private static bool TryCopyBindPoseFromAvatarSource(Animator rootAnimator)
        {
            if (rootAnimator == null || rootAnimator.avatar == null)
                return false;

            string avatarPath = AssetDatabase.GetAssetPath(rootAnimator.avatar);
            if (string.IsNullOrEmpty(avatarPath))
                return false;

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(avatarPath);
            if (fbx == null)
                return false;

            Transform visual = rootAnimator.transform.Find("Visual");
            if (visual == null)
            {
                Transform hips = rootAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null && hips.parent.parent != null)
                    visual = hips.parent.parent;
            }

            if (visual == null)
                return false;

            GameObject temp = Object.Instantiate(fbx);
            try
            {
                var srcRot = new System.Collections.Generic.Dictionary<string, Quaternion>(64);
                var srcPos = new System.Collections.Generic.Dictionary<string, Vector3>(64);
                var srcScl = new System.Collections.Generic.Dictionary<string, Vector3>(64);
                Transform[] srcTransforms = temp.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < srcTransforms.Length; i++)
                {
                    Transform t = srcTransforms[i];
                    if (t == null || srcRot.ContainsKey(t.name))
                        continue;
                    srcRot[t.name] = t.localRotation;
                    srcPos[t.name] = t.localPosition;
                    srcScl[t.name] = t.localScale;
                }

                int applied = 0;
                Transform[] dstTransforms = visual.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < dstTransforms.Length; i++)
                {
                    Transform d = dstTransforms[i];
                    if (d == null || d == visual || !srcRot.ContainsKey(d.name))
                        continue;
                    d.localPosition = srcPos[d.name];
                    d.localRotation = srcRot[d.name];
                    d.localScale = srcScl[d.name];
                    EditorUtility.SetDirty(d);
                    applied++;
                }

                return applied > 0;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        private static GameObject InstantiateVisualSource(GameObject visualSource)
        {
            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(visualInstance))
                    PrefabUtility.UnpackPrefabInstance(
                        visualInstance,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                return visualInstance;
            }

            return Object.Instantiate(visualSource);
        }

        private static void ClearPreviousCustomVisual(GameObject root, string childName)
        {
            Transform existing = root.transform.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel == null)
                return;

            Transform leftoverArmature = root.transform.Find("Armature");
            if (leftoverArmature != null)
                Object.DestroyImmediate(leftoverArmature.gameObject);

            Transform leftoverMesh = root.transform.Find("char1");
            if (leftoverMesh != null)
                Object.DestroyImmediate(leftoverMesh.gameObject);
        }

        private static void HideStockBodyMeshes(GameObject root)
        {
            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel == null)
                return;

            SkinnedMeshRenderer[] skinned = stockModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] == null || IsWeaponVisualNode(skinned[i].transform))
                    continue;
                skinned[i].enabled = false;
                skinned[i].gameObject.SetActive(false);
            }

            MeshRenderer[] meshes = stockModel.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || IsWeaponVisualNode(meshes[i].transform))
                    continue;

                string path = AnimationUtility.CalculateTransformPath(meshes[i].transform, stockModel);
                if (path.IndexOf("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("VBOT_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    meshes[i].enabled = false;
                    meshes[i].gameObject.SetActive(false);
                }
            }
        }

        private static bool IsWeaponVisualNode(Transform node)
        {
            Transform cur = node;
            while (cur != null)
            {
                string name = cur.name;
                if (name.Equals("3D Model", System.StringComparison.Ordinal))
                    return false;

                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    name.StartsWith("Holstered_", System.StringComparison.Ordinal) ||
                    name.StartsWith("PioneerVisual_", System.StringComparison.Ordinal) ||
                    name.Equals("WeaponHolders", System.StringComparison.Ordinal) ||
                    name.Equals("RightHandlers", System.StringComparison.Ordinal) ||
                    name.Equals("LeftHandlers", System.StringComparison.Ordinal) ||
                    name.Equals("HandgunHolder", System.StringComparison.Ordinal) ||
                    name.Equals("RifleHolder", System.StringComparison.Ordinal) ||
                    name.Equals("meleeHandler", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("defaultHandler", System.StringComparison.OrdinalIgnoreCase))
                    return true;

                if (cur.GetComponent<global::Invector.vMelee.vMeleeWeapon>() != null ||
                    cur.GetComponent<global::Invector.vShooter.vShooterWeapon>() != null ||
                    cur.GetComponent<global::Invector.vMelee.vHitBox>() != null)
                    return true;

                cur = cur.parent;
            }

            return false;
        }

        private static void EnableVisualUpdateWhenOffscreen(GameObject rootOrVisual)
        {
            if (rootOrVisual == null)
                return;

            SkinnedMeshRenderer[] renderers = rootOrVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                renderer.updateWhenOffscreen = true;
            }
        }

        private static void ClearPlayerDeadFlag(GameObject root)
        {
            vThirdPersonController controller = root != null
                ? root.GetComponent<vThirdPersonController>()
                : null;
            if (controller == null)
                return;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty isDead = so.FindProperty("_isDead");
            if (isDead != null && isDead.boolValue)
            {
                isDead.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (controller is global::Invector.vHealthController health && health.isDead)
            {
                health.isDead = false;
                health.ResetHealth();
            }
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
