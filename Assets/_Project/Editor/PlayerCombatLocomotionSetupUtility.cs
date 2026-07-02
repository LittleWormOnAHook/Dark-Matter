using System;
using System.Collections.Generic;
using Project.EditorTools;
using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Rebuilds the Base Layer Grounded 2D blend tree (Turn x Forward).
    /// Soft side corners keep Human Melee strafe clips; walk-tier hard corners (|Turn| = 1, Forward = 0.5) use Basic Motions lean.
    /// Run-tier forward corners keep Human strafe-run diagonals for leg stride during sprint.
    /// </summary>
    public static class PlayerCombatLocomotionSetupUtility
    {
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;

        private const string IdleRoot =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Idles";

        private const string MovementRoot =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement";

        private const string StrafeRoot = MovementRoot + "/Strafe";
        private const string WalkRoot = MovementRoot + "/Walk";
        private const string RunRoot = MovementRoot + "/Run";

        private const string WalkTurnPath =
            "Assets/Animations/Basic Motions/Animations/Movement/BasicMotions@Turn01.fbx";

        private const float LeanHardCornerTimeScale = 1.3f;

        private static readonly (Vector2 position, string clipPath)[] DirectionalAssignments =
        {
            (new Vector2(0f, 0f), $"{IdleRoot}/HumanM@Idle01.fbx"),

            (new Vector2(0f, 0.5f), $"{WalkRoot}/HumanM@Walk01_Forward.fbx"),
            (new Vector2(0f, 1f), $"{RunRoot}/HumanM@Run01_Forward.fbx"),
            (new Vector2(0f, -0.5f), $"{WalkRoot}/HumanM@Walk01_Backward.fbx"),
            (new Vector2(0f, -1f), $"{RunRoot}/HumanM@Run01_Backward.fbx"),

            (new Vector2(0.5f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Right.fbx"),
            (new Vector2(1f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Right.fbx"),
            (new Vector2(-0.5f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Left.fbx"),
            (new Vector2(-1f, 0f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_Left.fbx"),

            (new Vector2(1f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardRight.fbx"),
            (new Vector2(-1f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx"),
            (new Vector2(0.5f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardRight.fbx"),
            (new Vector2(-0.5f, 0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx"),

            (new Vector2(1f, -0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardRight.fbx"),
            (new Vector2(-1f, -0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardLeft.fbx"),
            (new Vector2(0.5f, -0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardRight.fbx"),
            (new Vector2(-0.5f, -0.5f), $"{StrafeRoot}/StrafeWalk/HumanM@StrafeWalk01_BackwardLeft.fbx"),

            (new Vector2(1f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx"),
            (new Vector2(-1f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx"),
            (new Vector2(0.5f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx"),
            (new Vector2(-0.5f, 1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx"),

            (new Vector2(1f, -1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx"),
            (new Vector2(-1f, -1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx"),
            (new Vector2(0.5f, -1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx"),
            (new Vector2(-0.5f, -1f), $"{StrafeRoot}/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx")
        };

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Rebuild Grounded Blend Tree", false, 6)]
        public static void RebuildGroundedBlendTreeFromMenu()
        {
            if (!RebuildGroundedBlendTree(showDialog: true))
            {
                EditorUtility.DisplayDialog(
                    "Grounded Blend Tree",
                    "Could not rebuild the Grounded blend tree.\n\n" +
                    "Ensure ProjectUnityCharacterController exists and Human Animations Melee clips are imported.",
                    "OK");
            }
        }

        public static bool RebuildGroundedBlendTree(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            if (controller.layers == null || controller.layers.Length == 0)
                return false;

            AnimatorState groundedState = FindState(controller.layers[0].stateMachine, "Grounded");
            if (groundedState == null)
                return false;

            Dictionary<Vector2, Motion> clipByPosition = BuildClipLookup();
            if (clipByPosition.Count == 0)
                return false;

            ApplyForwardTurnClips(clipByPosition);

            EnsureLocomotionParameters(controller);
            RemoveTurnLeanLayer(controller);

            if (groundedState.motion is BlendTree oldTree && oldTree != null)
                UnityEngine.Object.DestroyImmediate(oldTree, true);

            BlendTree blendTree = new BlendTree
            {
                name = "Grounded Locomotion",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "Turn",
                blendParameterY = "Forward",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            List<(Vector2 position, Motion motion)> sorted = new List<(Vector2, Motion)>(clipByPosition.Count);
            foreach (KeyValuePair<Vector2, Motion> pair in clipByPosition)
                sorted.Add((pair.Key, pair.Value));

            sorted.Sort((a, b) =>
            {
                int yCompare = a.position.y.CompareTo(b.position.y);
                return yCompare != 0 ? yCompare : a.position.x.CompareTo(b.position.x);
            });

            ChildMotion[] children = new ChildMotion[sorted.Count];
            for (int i = 0; i < sorted.Count; i++)
            {
                Vector2 position = sorted[i].position;
                children[i] = new ChildMotion
                {
                    motion = sorted[i].motion,
                    position = position,
                    timeScale = ResolveChildTimeScale(position),
                    directBlendParameter = UsesLocomotionAnimSpeed(position) ? "LocomotionAnimSpeed" : string.Empty
                };
            }

            blendTree.children = children;
            groundedState.motion = blendTree;
            groundedState.iKOnFeet = true;

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(groundedState);
            EditorUtility.SetDirty(controller);

            AnimatorControllerGraphRepairUtility.RemoveOrphanSubAssets(ControllerPath);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Grounded Blend Tree",
                    $"Rebuilt Grounded Locomotion with {children.Length} clips.\n\n" +
                    "• Turn (X), Forward (Y)\n" +
                    "• Walk lean hard corners (|Turn| = 1, Forward = 0.5): Basic Motions turn lean\n" +
                    "• Run forward corners: Human strafe-run diagonals for sprint leg stride\n" +
                    "• Foot IK enabled on Grounded state",
                    "OK");
            }

            return true;
        }

        public static void RemoveTurnLeanLayer(AnimatorController controller)
        {
            if (controller == null)
                return;

            RemoveFloatParameter(controller, "TurnLean");
            RemoveFloatParameter(controller, "ForwardLean");

            List<AnimatorControllerLayer> layers = new List<AnimatorControllerLayer>(controller.layers);
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].name != "Turn Lean")
                    continue;

                if (layers[i].stateMachine != null)
                    UnityEngine.Object.DestroyImmediate(layers[i].stateMachine, true);

                layers.RemoveAt(i);
            }

            controller.layers = layers.ToArray();
            EditorUtility.SetDirty(controller);
        }

        /// <summary>
        /// Walk-tier hard corners (|Turn| = 1, Forward = 0.5) use Basic Motions for torso lean.
        /// Run-tier forward corners keep Human strafe-run diagonals for natural sprint leg travel.
        /// </summary>
        private static void ApplyForwardTurnClips(Dictionary<Vector2, Motion> clipByPosition)
        {
            AnimationClip walkTurnLeft = LoadClipByNameFilter(WalkTurnPath, "Left");
            AnimationClip walkTurnRight = LoadClipByNameFilter(WalkTurnPath, "Right");

            if (walkTurnLeft == null || walkTurnRight == null)
                return;

            ReplaceTurnSample(clipByPosition, new Vector2(-1f, 0.5f), walkTurnLeft);
            ReplaceTurnSample(clipByPosition, new Vector2(1f, 0.5f), walkTurnRight);
        }

        private static void ReplaceTurnSample(
            Dictionary<Vector2, Motion> clipByPosition,
            Vector2 position,
            AnimationClip clip)
        {
            if (clip == null || !clipByPosition.ContainsKey(position))
                return;

            clipByPosition[position] = clip;
        }

        private static void EnsureLocomotionParameters(AnimatorController controller)
        {
            EnsureFloatParameter(controller, "Forward");
            EnsureFloatParameter(controller, "Turn");
            EnsureFloatParameter(controller, "LocomotionAnimSpeed");
        }

        private static void EnsureFloatParameter(AnimatorController controller, string name)
        {
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == name)
                    return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void RemoveFloatParameter(AnimatorController controller, string name)
        {
            List<AnimatorControllerParameter> parameters =
                new List<AnimatorControllerParameter>(controller.parameters);
            for (int i = parameters.Count - 1; i >= 0; i--)
            {
                if (parameters[i].name == name)
                    parameters.RemoveAt(i);
            }

            controller.parameters = parameters.ToArray();
        }

        private static bool UsesLocomotionAnimSpeed(Vector2 position)
        {
            float y = position.y;
            return Mathf.Approximately(y, 0.5f)
                || Mathf.Approximately(y, 1f)
                || Mathf.Approximately(y, -0.5f)
                || Mathf.Approximately(y, -1f);
        }

        private static float ResolveChildTimeScale(Vector2 position)
        {
            return IsWalkLeanHardCorner(position) ? LeanHardCornerTimeScale : 1f;
        }

        private static bool IsWalkLeanHardCorner(Vector2 position)
        {
            float absX = Mathf.Abs(position.x);
            return Mathf.Approximately(absX, 1f) && Mathf.Approximately(position.y, 0.5f);
        }

        private static bool IsForwardLeanHardCorner(Vector2 position)
        {
            return IsWalkLeanHardCorner(position);
        }

        private static Dictionary<Vector2, Motion> BuildClipLookup()
        {
            Dictionary<Vector2, Motion> lookup = new Dictionary<Vector2, Motion>();
            for (int i = 0; i < DirectionalAssignments.Length; i++)
            {
                (Vector2 position, string clipPath) = DirectionalAssignments[i];
                AnimationClip clip = LoadFirstClip(clipPath);
                if (clip == null)
                    continue;

                lookup[position] = clip;
            }

            return lookup;
        }

        private static AnimationClip LoadFirstClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__"))
                    return clip;
            }

            return null;
        }

        private static AnimationClip LoadClipByNameFilter(string assetPath, string nameFilter)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not AnimationClip clip || clip.name.StartsWith("__"))
                    continue;

                if (clip.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
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
