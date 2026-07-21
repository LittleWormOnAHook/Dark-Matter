using System.Collections.Generic;
using Project.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Adds a directional Locomotion blend tree to enemy animator controllers using Mixamo strafe clips.
    /// </summary>
    public static class EnemyStrafeLocomotionSetupUtility
    {
        private const string LocomotionStateName = "Locomotion";
        private const string StrafeRoot =
            "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Rifle";

        private static readonly (Vector2 position, string clipPath)[] BlendAssignments =
        {
            (new Vector2(0f, 0f), $"{StrafeRoot}/Idle/Strafe Idle.fbx"),
            (new Vector2(0.5f, 0f), $"{StrafeRoot}/Walk/Walk Strafe Right.fbx"),
            (new Vector2(1f, 0f), $"{StrafeRoot}/Walk/Walk Strafe Right.fbx"),
            (new Vector2(-0.5f, 0f), $"{StrafeRoot}/New Ones/strafe.fbx"),
            (new Vector2(-1f, 0f), $"{StrafeRoot}/New Ones/strafe.fbx"),
            (new Vector2(0f, 0.5f), $"{StrafeRoot}/Walk/Walk Forward Strafe.fbx"),
            (new Vector2(1f, 0.5f), $"{StrafeRoot}/Walk/Walk Strafe Right.fbx"),
            (new Vector2(0.5f, 0.5f), $"{StrafeRoot}/Walk/Walk Forward Strafe.fbx"),
            (new Vector2(-1f, 0.5f), $"{StrafeRoot}/Walk/Walk Strafe Backward.fbx"),
            (new Vector2(-0.5f, 0.5f), $"{StrafeRoot}/Walk/Walk Strafe Backward.fbx"),
            (new Vector2(0f, 1f), $"{StrafeRoot}/Run/Run Forward.fbx"),
            (new Vector2(1f, 1f), $"{StrafeRoot}/Run/Run Right.fbx"),
            (new Vector2(-1f, 1f), $"{StrafeRoot}/Run/Run Left.fbx"),
            (new Vector2(0.5f, 1f), $"{StrafeRoot}/Run/Run Forward.fbx"),
            (new Vector2(-0.5f, 1f), $"{StrafeRoot}/Run/Run Left.fbx")
        };

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Setup Enemy Strafe Locomotion", false, 8)]
        public static void SetupEnemyStrafeLocomotion()
        {
            int updated = ApplyToAllEnemyControllers(showDialog: true);
            if (updated > 0)
                return;

            EditorUtility.DisplayDialog(
                "Enemy Strafe Locomotion",
                "Enemy strafe locomotion is already configured.",
                "OK");
        }

        private static void TryApplyEnemyStrafeLocomotion()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ApplyToAllEnemyControllers(showDialog: false);
        }

        private static int ApplyToAllEnemyControllers(bool showDialog)
        {
            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { ProjectAssetPaths.AnimationsEnemies });
            int updatedCount = 0;

            for (int i = 0; i < controllerGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(controllerGuids[i]);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null || HasLocomotionBlendTree(controller))
                    continue;

                if (ApplyLocomotionBlendTree(controller))
                    updatedCount++;
            }

            if (updatedCount > 0)
            {
                AssetDatabase.SaveAssets();
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Enemy Strafe Locomotion",
                        $"Added directional Locomotion blend trees to {updatedCount} enemy controller(s).",
                        "OK");
                }
                else
                {
                    // Applied silently on editor load.
                }
            }

            return updatedCount;
        }

        private static bool HasLocomotionBlendTree(AnimatorController controller)
        {
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in root.states)
            {
                if (child.state != null &&
                    child.state.name == LocomotionStateName &&
                    child.state.motion is BlendTree)
                    return true;
            }

            return false;
        }

        private static bool ApplyLocomotionBlendTree(AnimatorController controller)
        {
            Dictionary<Vector2, Motion> clipByPosition = BuildClipLookup();
            if (clipByPosition.Count == 0)
                return false;

            EnsureFloatParameter(controller, "Forward");
            EnsureFloatParameter(controller, "Turn");

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorState locomotionState = root.AddState(LocomotionStateName, new Vector3(360f, 120f, 0f));
            BlendTree blendTree = new BlendTree
            {
                name = "Enemy Locomotion",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "Turn",
                blendParameterY = "Forward",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(blendTree, controller);
            List<ChildMotion> children = new List<ChildMotion>();
            for (int i = 0; i < BlendAssignments.Length; i++)
            {
                (Vector2 position, string clipPath) = BlendAssignments[i];
                if (!clipByPosition.TryGetValue(position, out Motion motion))
                    continue;

                children.Add(new ChildMotion
                {
                    motion = motion,
                    position = position,
                    timeScale = 1f,
                    cycleOffset = 0f,
                    directBlendParameter = string.Empty,
                    mirror = false
                });
            }

            if (children.Count == 0)
                return false;

            blendTree.children = children.ToArray();
            locomotionState.motion = blendTree;
            locomotionState.writeDefaultValues = true;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(blendTree);
            return true;
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

        private static void EnsureFloatParameter(AnimatorController controller, string parameterName)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                    return;
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
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
    }
}
