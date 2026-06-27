using System.Collections.Generic;
using Project.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Swaps the player Grounded blend tree to Human Animations Melee strafe locomotion clips.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerCombatLocomotionSetupUtility
    {
        private const string ControllerPath =
            "Assets/_Project/Animations/ProjectUnityCharacterController.controller";

        private const string StrafeRoot =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe";

        private const string CombatIdlePath =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx";

        private static readonly (Vector2 position, string clipPath)[] BlendAssignments =
        {
            (new Vector2(0f, 0f), CombatIdlePath),
            (new Vector2(0.5f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Right.fbx"),
            (new Vector2(1f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Right.fbx"),
            (new Vector2(-0.5f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Left.fbx"),
            (new Vector2(-1f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Left.fbx"),
            (new Vector2(0f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx"),
            (new Vector2(1f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardRight.fbx"),
            (new Vector2(0.5f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx"),
            (new Vector2(-1f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardLeft.fbx"),
            (new Vector2(-0.5f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardRight.fbx"),
            (new Vector2(0f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx"),
            (new Vector2(1f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_Right.fbx"),
            (new Vector2(-1f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_Left.fbx"),
            (new Vector2(0.5f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx"),
            (new Vector2(-0.5f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx")
        };

        static PlayerCombatLocomotionSetupUtility()
        {
            EditorApplication.delayCall += TryApplyStrafeLocomotion;
        }

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Setup Player Strafe Locomotion", false, 6)]
        public static void SetupPlayerStrafeLocomotion()
        {
            if (ApplyStrafeLocomotion(showDialog: true))
                return;

            EditorUtility.DisplayDialog(
                "Player Strafe Locomotion",
                "Player strafe locomotion is already configured.",
                "OK");
        }

        private static void TryApplyStrafeLocomotion()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ApplyStrafeLocomotion(showDialog: false);
        }

        private static bool ApplyStrafeLocomotion(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            AnimatorState groundedState = FindState(controller.layers[0].stateMachine, "Grounded");
            if (groundedState == null || groundedState.motion is not BlendTree blendTree)
                return false;

            if (UsesStrafeClips(blendTree))
                return false;

            Dictionary<Vector2, Motion> clipByPosition = BuildClipLookup();
            if (clipByPosition.Count == 0)
                return false;

            AssignBlendTreeMotions(blendTree, clipByPosition);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Player Strafe Locomotion",
                    "Grounded blend tree now uses Human Animations Melee strafe clips.",
                    "OK");
            }
            else
            {
                // Applied silently on editor load.
            }

            return true;
        }

        private static bool UsesStrafeClips(BlendTree blendTree)
        {
            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                Motion motion = children[i].motion;
                if (motion == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(motion);
                if (path.Contains("Human Animations Melee") && path.Contains("Strafe"))
                    return true;
            }

            return false;
        }

        private static Dictionary<Vector2, Motion> BuildClipLookup()
        {
            Dictionary<Vector2, Motion> lookup = new Dictionary<Vector2, Motion>();
            for (int i = 0; i < BlendAssignments.Length; i++)
            {
                (Vector2 position, string clipPath) = BlendAssignments[i];
                AnimationClip clip = LoadFirstClip(clipPath);
                if (clip == null)
                    continue;

                lookup[position] = clip;
            }

            return lookup;
        }

        private static void AssignBlendTreeMotions(BlendTree blendTree, Dictionary<Vector2, Motion> clipByPosition)
        {
            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                Vector2 position = children[i].position;
                foreach (KeyValuePair<Vector2, Motion> pair in clipByPosition)
                {
                    if (Vector2.Distance(pair.Key, position) > 0.01f)
                        continue;

                    children[i].motion = pair.Value;
                    break;
                }
            }

            blendTree.children = children;
        }

        private static AnimationClip LoadFirstClip(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__"))
                    return clip;
            }

            return null;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state != null && child.state.name == stateName)
                    return child.state;
            }

            return null;
        }
    }
}
