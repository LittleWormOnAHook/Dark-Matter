using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Inspects / prepares Meshy and other character FBX imports for the Enemy Prefab Creator.
    /// </summary>
    public static class EnemyModelAvatarUtility
    {
        public struct AvatarStatus
        {
            public bool HasAvatar;
            public Avatar Avatar;
            public string ModelAssetPath;
            public string Message;
        }

        public struct ModelInspection
        {
            public bool HasModel;
            public string AssetPath;
            public ModelImporterAnimationType AnimationType;
            public bool HasAvatar;
            public bool IsHumanoidAvatar;
            public bool IsAvatarValid;
            public Avatar Avatar;
            public float GlobalScale;
            public float FileScale;
            public bool UseFileScale;
            public float EffectiveScale;
            public int SkinnedMeshCount;
            public int TransformCount;
            public Bounds WorldBounds;
            public bool HasAnimator;
            public string Summary;
            public string Recommendation;
            public bool LooksHumanoidSized;
        }

        public static AvatarStatus ResolveAvatar(GameObject root)
        {
            AvatarStatus status = new AvatarStatus
            {
                Message = "No model source found."
            };

            if (root == null)
                return status;

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.GetComponentInChildren<Animator>(true);

            if (animator != null && animator.avatar != null)
            {
                status.HasAvatar = true;
                status.Avatar = animator.avatar;
                status.ModelAssetPath = AssetDatabase.GetAssetPath(animator.avatar);
                status.Message = string.IsNullOrEmpty(status.ModelAssetPath)
                    ? "Avatar assigned on Animator."
                    : $"Avatar from {status.ModelAssetPath}.";
                return status;
            }

            status.ModelAssetPath = FindPrimaryModelAssetPath(root);
            if (string.IsNullOrEmpty(status.ModelAssetPath))
                return status;

            Avatar embeddedAvatar = LoadAvatarFromAssetPath(status.ModelAssetPath);
            if (embeddedAvatar != null)
            {
                status.HasAvatar = true;
                status.Avatar = embeddedAvatar;
                status.Message = $"Avatar found on {status.ModelAssetPath}.";
                return status;
            }

            status.Message =
                $"Model found ({status.ModelAssetPath}) but no avatar. Configure humanoid/generic rig on the FBX import settings.";
            return status;
        }

        public static ModelInspection Inspect(GameObject modelOrInstance)
        {
            ModelInspection inspection = new ModelInspection
            {
                Summary = "No model assigned.",
                Recommendation = "Assign a character FBX or prefab."
            };

            if (modelOrInstance == null)
                return inspection;

            string assetPath = AssetDatabase.GetAssetPath(modelOrInstance);
            if (string.IsNullOrEmpty(assetPath))
                assetPath = FindPrimaryModelAssetPath(modelOrInstance);

            inspection.HasModel = true;
            inspection.AssetPath = assetPath ?? string.Empty;

            ModelImporter importer = string.IsNullOrEmpty(assetPath)
                ? null
                : AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (importer != null)
            {
                inspection.AnimationType = importer.animationType;
                inspection.GlobalScale = importer.globalScale;
                inspection.FileScale = importer.fileScale;
                inspection.UseFileScale = importer.useFileScale;
                inspection.EffectiveScale = importer.globalScale *
                    (importer.useFileScale ? Mathf.Max(importer.fileScale, 0.0001f) : 1f);
            }

            AvatarStatus avatarStatus = ResolveAvatar(modelOrInstance);
            inspection.HasAvatar = avatarStatus.HasAvatar;
            inspection.Avatar = avatarStatus.Avatar;
            if (avatarStatus.Avatar != null)
            {
                inspection.IsHumanoidAvatar = avatarStatus.Avatar.isHuman;
                inspection.IsAvatarValid = avatarStatus.Avatar.isValid;
            }

            Animator animator = modelOrInstance.GetComponent<Animator>();
            if (animator == null)
                animator = modelOrInstance.GetComponentInChildren<Animator>(true);
            inspection.HasAnimator = animator != null;

            SkinnedMeshRenderer[] skinned = modelOrInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            inspection.SkinnedMeshCount = skinned != null ? skinned.Length : 0;
            inspection.TransformCount = modelOrInstance.GetComponentsInChildren<Transform>(true).Length;

            Renderer[] renderers = modelOrInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        bounds.Encapsulate(renderers[i].bounds);
                }

                inspection.WorldBounds = bounds;
                inspection.LooksHumanoidSized = bounds.size.y >= 0.8f && bounds.size.y <= 3.5f;
            }

            inspection.Summary = BuildSummary(inspection);
            inspection.Recommendation = BuildRecommendation(inspection);
            return inspection;
        }

        public static bool TryPrepareModelImport(string assetPath, out string message)
        {
            message = "No changes.";
            assetPath = ResolveModelImporterPath(assetPath, out string resolveError);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                message = string.IsNullOrEmpty(resolveError) ? "Asset path is empty." : resolveError;
                return false;
            }

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                message = $"Not a model asset: {assetPath}";
                return false;
            }

            bool dirty = false;
            System.Text.StringBuilder changes = new System.Text.StringBuilder();

            // Meshy often ships as centimetres (fileScale 0.01). Keep useFileScale so characters are ~2m.
            if (!importer.useFileScale && importer.fileScale > 0f && importer.fileScale < 0.1f)
            {
                importer.useFileScale = true;
                dirty = true;
                changes.Append("Enabled useFileScale for centimetre FBX. ");
            }

            if (importer.animationType == ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
                changes.Append("Set Animation Type to Humanoid. ");
            }
            else if (importer.animationType == ModelImporterAnimationType.Generic)
            {
                // Keep Generic — Meshy creatures/quads often ship as Generic. User can force Humanoid in the Rig tab.
            }

            if (importer.animationType == ModelImporterAnimationType.Human &&
                importer.avatarSetup == ModelImporterAvatarSetup.NoAvatar)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
                changes.Append("Enabled Create From This Model avatar. ");
            }

            if (!dirty)
            {
                message =
                    $"Import OK ({importer.animationType}, fileScale={importer.fileScale}, useFileScale={importer.useFileScale}). {assetPath}";
                return true;
            }

            importer.SaveAndReimport();
            message = changes.ToString().Trim();
            return true;
        }

        /// <summary>
        /// Ensures a scene/prefab instance of a model FBX has Animator + Avatar wired and root motion off.
        /// </summary>
        public static void PrepareModelInstance(GameObject instance, bool preferHumanoidAvatar = true)
        {
            if (instance == null)
                return;

            AvatarStatus avatarStatus = ResolveAvatar(instance);
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = instance.AddComponent<Animator>();

            if (avatarStatus.Avatar != null)
            {
                if (preferHumanoidAvatar || animator.avatar == null)
                    animator.avatar = avatarStatus.Avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public static string FindPrimaryModelAssetPath(GameObject root)
        {
            return ResolvePreferredModelAssetPath(root);
        }

        /// <summary>
        /// Prefers the source FBX over wrapper prefabs (Space Lisa, SpaceSuitGirl, Invector outputs).
        /// Stock VBOT / 3D Model meshes are skipped so a dirty Lisa_Hybrid does not resolve to the template body.
        /// </summary>
        public static string ResolvePreferredModelAssetPath(GameObject root)
        {
            if (root == null)
                return null;

            string direct = AssetDatabase.GetAssetPath(root);
            if (IsModelAssetPath(direct))
                return direct;

            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                string fromVisual = FindModelPathFromRenderers(visual.gameObject, skipStockInvectorBody: false);
                if (IsModelAssetPath(fromVisual))
                    return fromVisual;
            }

            string fromCustom = FindModelPathFromRenderers(root, skipStockInvectorBody: true);
            if (IsModelAssetPath(fromCustom))
                return fromCustom;

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.avatar != null)
            {
                string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
                if (IsModelAssetPath(avatarPath) && !IsStockInvectorModelPath(avatarPath))
                    return avatarPath;
            }

            return null;
        }

        public static GameObject ResolvePreferredVisualModel(GameObject assigned)
        {
            if (assigned == null)
                return null;

            string path = ResolvePreferredModelAssetPath(assigned);
            if (!IsModelAssetPath(path))
                return assigned;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return model != null ? model : assigned;
        }

        public static bool LooksLikeInvectorCharacterPrefab(GameObject root)
        {
            if (root == null)
                return false;

            string name = root.name;
            if (name.Equals("HumanoidEnemy_Invector", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Player_Invector", System.StringComparison.OrdinalIgnoreCase))
                return true;

            bool hasStockModel = root.transform.Find("3D Model") != null;
            bool hasBodySnaps = root.transform.Find("BodySnaps") != null ||
                                root.transform.Find("InvectorComponents/BodySnaps") != null;
            if (hasStockModel && hasBodySnaps)
                return true;

            return root.GetComponent<global::Invector.vCharacterController.vThirdPersonController>() != null;
        }

        static string FindModelPathFromRenderers(GameObject root, bool skipStockInvectorBody)
        {
            if (root == null)
                return null;

            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[i];
                if (renderer == null || renderer.sharedMesh == null)
                    continue;
                if (skipStockInvectorBody && IsStockInvectorBodyRenderer(renderer.transform))
                    continue;

                string path = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                if (IsModelAssetPath(path) && !IsStockInvectorModelPath(path))
                    return path;
            }

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter filter = meshFilters[i];
                if (filter == null || filter.sharedMesh == null)
                    continue;
                if (skipStockInvectorBody && IsStockInvectorBodyRenderer(filter.transform))
                    continue;

                string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (IsModelAssetPath(path) && !IsStockInvectorModelPath(path))
                    return path;
            }

            return null;
        }

        static bool IsStockInvectorBodyRenderer(Transform node)
        {
            Transform cur = node;
            while (cur != null)
            {
                string name = cur.name;
                if (name.Equals("Visual", System.StringComparison.Ordinal))
                    return false;
                if (name.Equals("3D Model", System.StringComparison.Ordinal))
                    return true;
                if (name.StartsWith("VBOT_", System.StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Mesh-LOD", System.StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase))
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        static bool IsStockInvectorModelPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            return normalized.IndexOf("VBOT", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Avatar LoadAvatarFromAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null)
                return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        public static bool IsModelAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".fbx") || lower.EndsWith(".obj") || lower.EndsWith(".dae") || lower.EndsWith(".blend");
        }

        private static string BuildSummary(ModelInspection inspection)
        {
            string rig = inspection.AnimationType.ToString();
            string avatar = !inspection.HasAvatar
                ? "no avatar"
                : (inspection.IsHumanoidAvatar ? "humanoid" : "generic") +
                  (inspection.IsAvatarValid ? " valid" : " INVALID");
            string height = inspection.WorldBounds.size.y > 0.01f
                ? $"height≈{inspection.WorldBounds.size.y:0.00}m"
                : "height unknown";
            string scale = inspection.EffectiveScale > 0f
                ? $"importScale={inspection.EffectiveScale:0.####} (file={inspection.FileScale:0.####})"
                : "importScale n/a";

            return $"{rig} | {avatar} | SMR={inspection.SkinnedMeshCount} | {height} | {scale}";
        }

        private static string BuildRecommendation(ModelInspection inspection)
        {
            if (!inspection.HasModel)
                return "Assign a character FBX or prefab.";

            if (inspection.AnimationType == ModelImporterAnimationType.None)
                return "FBX Animation Type is None — click Prepare Model Import (sets Humanoid).";

            if (inspection.AnimationType == ModelImporterAnimationType.Human &&
                (!inspection.HasAvatar || !inspection.IsAvatarValid))
                return "Humanoid rig has no valid Avatar — open FBX Rig tab and Configure / Apply.";

            if (inspection.AnimationType == ModelImporterAnimationType.Human && inspection.IsAvatarValid)
            {
                if (!inspection.LooksHumanoidSized)
                    return "Humanoid OK but height looks wrong — enable Use File Scale on the FBX (Meshy often uses 0.01).";
                return "Humanoid Meshy/character OK — use Enemy Prefab Creator (Humanoid) or Player Prefab Creator → Apply Visual.";
            }

            if (inspection.AnimationType == ModelImporterAnimationType.Generic)
                return "Generic rig — use Enemy Prefab Creator LegacyCreature + Animation Pipeline, or force Humanoid on the FBX Rig tab for players.";

            return "Model ready for Prefab Creator.";
        }

        public static bool TryForceHumanoidImport(string assetPath, out string message)
        {
            message = "No changes.";
            assetPath = ResolveModelImporterPath(assetPath, out string resolveError);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                message = string.IsNullOrEmpty(resolveError) ? "Asset path is empty." : resolveError;
                return false;
            }

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                message = $"Not a model asset: {assetPath}";
                return false;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            message = $"Set Animation Type to Humanoid and reimported. {assetPath}";
            return true;
        }

        public static bool IsReadyForHumanoidPaste(GameObject model)
        {
            ModelInspection inspection = Inspect(model);
            return inspection.IsHumanoidAvatar && inspection.IsAvatarValid;
        }

        /// <summary>
        /// Prepares FBX import when needed. Returns true when model is valid for humanoid avatar paste.
        /// </summary>
        public static bool EnsureRigReadyForHumanoidPaste(
            GameObject model,
            bool autoPrepareImport,
            bool allowForceHumanoid,
            out GameObject resolvedModel,
            out string message)
        {
            resolvedModel = model;
            message = string.Empty;
            if (model == null)
            {
                message = "No model assigned.";
                return false;
            }

            GameObject preferred = ResolvePreferredVisualModel(model);
            if (preferred != null)
            {
                resolvedModel = preferred;
                model = preferred;
            }

            if (IsReadyForHumanoidPaste(model))
                return true;

            string path = ResolvePreferredModelAssetPath(model);
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(model);

            if (autoPrepareImport && !string.IsNullOrEmpty(path))
            {
                TryPrepareModelImport(path, out message);
                resolvedModel = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? model;
                if (IsReadyForHumanoidPaste(resolvedModel))
                    return true;
            }

            ModelInspection inspection = Inspect(resolvedModel);
            if (inspection.AnimationType == ModelImporterAnimationType.Generic && allowForceHumanoid &&
                !string.IsNullOrEmpty(path))
            {
                TryForceHumanoidImport(path, out message);
                resolvedModel = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? resolvedModel;
                if (IsReadyForHumanoidPaste(resolvedModel))
                    return true;
            }

            message = string.IsNullOrEmpty(message)
                ? BuildRecommendation(Inspect(resolvedModel))
                : message;
            return IsReadyForHumanoidPaste(resolvedModel);
        }

        static string ResolveModelImporterPath(string assetPath, out string error)
        {
            error = null;
            if (IsModelAssetPath(assetPath))
                return assetPath;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "Asset path is empty.";
                return null;
            }

            GameObject assigned = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            string resolved = ResolvePreferredModelAssetPath(assigned);
            if (IsModelAssetPath(resolved))
                return resolved;

            error =
                $"Could not resolve an FBX/model asset from '{assetPath}'. " +
                "Assign Space_suit.fbx (or a prefab whose skinned meshes reference that FBX).";
            return null;
        }
    }
}
