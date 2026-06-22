using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyModelAvatarUtility
    {
        public struct AvatarStatus
        {
            public bool HasAvatar;
            public Avatar Avatar;
            public string ModelAssetPath;
            public string Message;
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
                $"Model found ({status.ModelAssetPath}) but no avatar. Configure humanoid rig on the FBX import settings manually.";
            return status;
        }

        public static string FindPrimaryModelAssetPath(GameObject root)
        {
            if (root == null)
                return null;

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

        private static bool IsModelAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".fbx") || lower.EndsWith(".obj") || lower.EndsWith(".dae") || lower.EndsWith(".blend");
        }
    }
}
