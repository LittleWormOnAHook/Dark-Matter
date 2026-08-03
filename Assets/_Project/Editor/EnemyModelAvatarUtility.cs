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
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                message = "Asset path is empty.";
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
                message = $"Import OK ({importer.animationType}, fileScale={importer.fileScale}, useFileScale={importer.useFileScale}).";
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
            if (root == null)
                return null;

            string direct = AssetDatabase.GetAssetPath(root);
            if (IsModelAssetPath(direct))
                return direct;

            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[i];
                if (renderer == null || renderer.sharedMesh == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                if (IsModelAssetPath(path))
                    return path;
            }

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter filter = meshFilters[i];
                if (filter == null || filter.sharedMesh == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (IsModelAssetPath(path))
                    return path;
            }

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.avatar != null)
            {
                string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
                if (IsModelAssetPath(avatarPath))
                    return avatarPath;
            }

            return null;
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
    }
}
