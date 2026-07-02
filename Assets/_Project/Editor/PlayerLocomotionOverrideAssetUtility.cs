using System.Collections.Generic;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Player
{
    public static class PlayerLocomotionOverrideAssetUtility
    {
        public static AnimatorOverrideController GetOrCreateOverrideAsset()
        {
            AnimatorOverrideController overrideAsset = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
                PlayerAnimatorControllerPaths.LocomotionOverridePath);
            if (overrideAsset != null)
                return overrideAsset;

            RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                PlayerAnimatorControllerPaths.GkcControllerPath);
            if (baseController == null)
            {
                Debug.LogError(
                    $"PlayerLocomotionOverrideAssetUtility: Missing base controller at {PlayerAnimatorControllerPaths.GkcControllerPath}");
                return null;
            }

            overrideAsset = new AnimatorOverrideController(baseController)
            {
                name = "ProjectUnityCharacterLocomotion"
            };
            AssetDatabase.CreateAsset(
                overrideAsset,
                PlayerAnimatorControllerPaths.LocomotionOverridePath);
            AssetDatabase.SaveAssets();
            return overrideAsset;
        }

        public static bool SyncOverrideAsset(PlayerLocomotionAnimationSettings settings)
        {
            if (settings == null || !settings.HasClipOverrides)
                return false;

            AnimatorOverrideController overrideAsset = GetOrCreateOverrideAsset();
            if (overrideAsset == null)
                return false;

            bool changed = ApplyClipOverrides(overrideAsset, settings);
            if (changed)
                EditorUtility.SetDirty(overrideAsset);

            return changed;
        }

        public static RuntimeAnimatorController ResolveControllerForSettings(
            PlayerLocomotionAnimationSettings settings)
        {
            if (settings != null && settings.HasClipOverrides)
            {
                AnimatorOverrideController overrideAsset = GetOrCreateOverrideAsset();
                if (overrideAsset != null)
                {
                    ApplyClipOverrides(overrideAsset, settings);
                    EditorUtility.SetDirty(overrideAsset);
                    return overrideAsset;
                }
            }

            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                PlayerAnimatorControllerPaths.GkcControllerPath);
        }

        public static bool AssignController(Animator animator, PlayerLocomotionAnimationSettings settings)
        {
            if (animator == null)
                return false;

            RuntimeAnimatorController targetController = ResolveControllerForSettings(settings);
            if (targetController == null)
                return false;

            if (animator.runtimeAnimatorController == targetController)
                return false;

            animator.runtimeAnimatorController = targetController;
            animator.applyRootMotion = false;
            return true;
        }

        private static bool ApplyClipOverrides(
            AnimatorOverrideController overrideController,
            PlayerLocomotionAnimationSettings settings)
        {
            if (overrideController == null || settings == null || !settings.HasClipOverrides)
                return false;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            bool changed = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                if (original == null)
                    continue;

                AnimationClip replacement = ResolveReplacementClip(original, settings);
                if (replacement == null || replacement == original)
                    continue;

                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
                changed = true;
            }

            if (!changed)
                return false;

            overrideController.ApplyOverrides(overrides);
            return true;
        }

        private static AnimationClip ResolveReplacementClip(
            AnimationClip original,
            PlayerLocomotionAnimationSettings settings)
        {
            if (IsPrimaryIdleClip(original) && settings.idleAnimation != null)
                return settings.idleAnimation;

            if (IsPrimaryWalkClip(original) && settings.walkAnimation != null)
                return settings.walkAnimation;

            if (IsPrimaryRunClip(original) && settings.runAnimation != null)
                return settings.runAnimation;

            return null;
        }

        private static bool IsPrimaryIdleClip(AnimationClip clip)
        {
            if (clip == null)
                return false;

            string name = clip.name;
            if (name.Contains("Block") || name.Contains("Combat") || name.Contains("Loot"))
                return false;

            return name.Contains("HumanoidIdle") || name.Contains("Idle");
        }

        private static bool IsPrimaryWalkClip(AnimationClip clip)
        {
            if (clip == null)
                return false;

            string name = clip.name;
            if (name.Contains("Run") || name.Contains("Strafe") || name.Contains("Crouch"))
                return false;

            return name.Contains("Walk01_Forward")
                || name.Contains("Walk01 - Forwards")
                || name.Contains("Walk01 - Forward");
        }

        private static bool IsPrimaryRunClip(AnimationClip clip)
        {
            if (clip == null)
                return false;

            string name = clip.name;
            if (name.Contains("Walk") || name.Contains("Strafe"))
                return false;

            return name.Contains("Run01_Forward")
                || name.Contains("Run01 - Forwards")
                || name.Contains("Run01 - Forward");
        }
    }
}
