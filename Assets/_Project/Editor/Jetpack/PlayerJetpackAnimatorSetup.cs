using Project.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Jetpack
{
    /// <summary>
    /// Adds Jumps.fbx fly clips and jetpack parameters to a player copy of Invector@ShooterMelee.
    /// </summary>
    public static class PlayerJetpackAnimatorSetup
    {
        public const string ShooterMeleeSourcePath =
            "Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller";

        public const string PlayerJetpackControllerPath =
            ProjectAssetPaths.Animations + "/Player/Invector@ShooterMelee_Jetpack.controller";

        private const string JumpsFlyPath =
            "Assets/Animations/Props Animations/Animations/Jumps.fbx";

        private const string MalbersLandPath =
            "Assets/Malbers Animations/Common/Human Anims/Fly/S_Fly_Land_SuperHero.fbx";

        private const string JetpackFlyBlendTreeName = "Jetpack Fly BT";

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Build Player Jetpack Animator")]
        public static void BuildPlayerJetpackAnimatorMenu()
        {
            RuntimeAnimatorController controller = BuildOrUpdateController(out string message);
            EditorUtility.DisplayDialog(
                controller != null ? "Jetpack Animator" : "Jetpack Animator Failed",
                message,
                "OK");

            if (controller != null)
            {
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
            }
        }

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Repair Player Jetpack Animator Graph")]
        public static void RepairPlayerJetpackAnimatorGraphMenu()
        {
            RuntimeAnimatorController controller = BuildOrUpdateController(out string message);
            EditorUtility.DisplayDialog(
                controller != null ? "Jetpack Animator Repaired" : "Jetpack Animator Repair Failed",
                message,
                "OK");

            if (controller != null)
            {
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
            }
        }

        public static RuntimeAnimatorController BuildOrUpdateController(out string message)
        {
            AnimationClip idle = LoadClip(JumpsFlyPath, "FlyUp") ?? LoadClip(JumpsFlyPath, "IdleFly");
            AnimationClip forward = LoadClip(JumpsFlyPath, "FlyForwardCoast") ?? LoadClip(JumpsFlyPath, "FlyForward");
            AnimationClip back = LoadClip(JumpsFlyPath, "FlyBackward");
            AnimationClip left = LoadClip(JumpsFlyPath, "FlyLeft");
            AnimationClip right = LoadClip(JumpsFlyPath, "FlyRight");
            AnimationClip land = LoadClip(MalbersLandPath, "S_Fly_Land_SuperHero");

            if (idle == null || forward == null || back == null || left == null || right == null)
            {
                message = "Missing one or more Jumps.fbx fly clips at:\n" + JumpsFlyPath;
                return null;
            }

            if (land == null)
            {
                message = "Missing Malbers jetpack land clip at:\n" + MalbersLandPath;
                return null;
            }

            EnsureParentFolder();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerJetpackControllerPath);

            if (controller == null)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(PlayerJetpackControllerPath) != null)
                    AssetDatabase.DeleteAsset(PlayerJetpackControllerPath);

                if (!AssetDatabase.CopyAsset(ShooterMeleeSourcePath, PlayerJetpackControllerPath))
                {
                    message = "Failed to copy ShooterMelee controller to:\n" + PlayerJetpackControllerPath;
                    return null;
                }

                AssetDatabase.ImportAsset(PlayerJetpackControllerPath);
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerJetpackControllerPath);
            }

            if (controller == null || controller.layers.Length == 0)
            {
                message = "Could not load player jetpack animator controller.";
                return null;
            }

            RemoveParameterIfExists(controller, "JetpackStrafe");
            EnsureParameter(controller, "JetpackActive", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "JetpackHorizontal", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "JetpackVertical", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "JetpackLand", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            RemoveStateIfExists(root, "Jetpack Fly Strafe");

            AnimatorState flyState = GetOrCreateState(root, "Jetpack Fly");
            flyState.motion = BuildOrReuseFlyBlendTree(controller, flyState, idle, forward, back, left, right);
            flyState.speed = 1f;
            flyState.writeDefaultValues = true;
            // Airborne keeps Invector ungrounded. CustomAction forces isGrounded and fights thrust.
            flyState.tag = "Airborne";

            AnimatorState landState = GetOrCreateState(root, "Jetpack Land");
            landState.motion = land;
            landState.tag = string.Empty;

            RemoveJetpackTransitions(root, flyState, landState);
            GateAirborneAnyState(root, flyState, landState);

            AddAnyStateTransition(root, flyState, AnimatorConditionMode.If, "JetpackActive", duration: 0.08f);

            AddTransition(flyState, landState, AnimatorConditionMode.If, "JetpackLand", hasExitTime: false, duration: 0.1f);

            AddTransition(flyState, null, AnimatorConditionMode.IfNot, "JetpackActive", hasExitTime: false, duration: 0.12f, isExit: true);
            AddTransition(landState, null, AnimatorConditionMode.IfNot, "JetpackActive", hasExitTime: true, duration: 0.15f, exitTime: 0.9f, isExit: true);

            CleanupOrphanJetpackBlendTrees(controller, flyState.motion as BlendTree);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            message = "Built jetpack fly blend (Jumps.fbx IdleFly center) at:\n" + PlayerJetpackControllerPath;
            return controller;
        }

        private static BlendTree BuildOrReuseFlyBlendTree(
            AnimatorController controller,
            AnimatorState flyState,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip back,
            AnimationClip left,
            AnimationClip right)
        {
            if (flyState.motion is BlendTree existing &&
                (existing.name == JetpackFlyBlendTreeName || existing.name.Contains("Jetpack Fly")))
            {
                ConfigureFlyBlendTree(existing, idle, forward, back, left, right);
                existing.name = JetpackFlyBlendTreeName;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            BlendTree tree = CreateFlyBlendTree(idle, forward, back, left, right);
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        private static BlendTree CreateFlyBlendTree(
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip back,
            AnimationClip left,
            AnimationClip right)
        {
            BlendTree tree = new BlendTree
            {
                name = JetpackFlyBlendTreeName,
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "JetpackHorizontal",
                blendParameterY = "JetpackVertical",
                useAutomaticThresholds = true,
            };

            ConfigureFlyBlendTree(tree, idle, forward, back, left, right);
            return tree;
        }

        private static void ConfigureFlyBlendTree(
            BlendTree tree,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip back,
            AnimationClip left,
            AnimationClip right)
        {
            while (tree.children.Length > 0)
                tree.RemoveChild(0);

            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "JetpackHorizontal";
            tree.blendParameterY = "JetpackVertical";
            tree.useAutomaticThresholds = true;
            tree.AddChild(left, new Vector2(-1f, 0f));
            tree.AddChild(right, new Vector2(1f, 0f));
            tree.AddChild(forward, new Vector2(0f, 1f));
            tree.AddChild(back, new Vector2(0f, -1f));
            tree.AddChild(idle, new Vector2(0f, 0f));
        }

        private static void CleanupOrphanJetpackBlendTrees(AnimatorController controller, BlendTree keepTree)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(PlayerJetpackControllerPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is not BlendTree tree || tree == keepTree)
                    continue;

                if (!tree.name.Contains("Jetpack Fly"))
                    continue;

                Object.DestroyImmediate(tree, true);
            }

            if (controller != null)
                EditorUtility.SetDirty(controller);
        }

        private static void EnsureParentFolder()
        {
            if (!AssetDatabase.IsValidFolder(ProjectAssetPaths.Animations))
                AssetDatabase.CreateFolder(ProjectAssetPaths.Root, "Animations");

            if (!AssetDatabase.IsValidFolder(ProjectAssetPaths.Animations + "/Player"))
                AssetDatabase.CreateFolder(ProjectAssetPaths.Animations, "Player");
        }

        private static AnimationClip LoadClip(string assetPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            return null;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }

        private static void RemoveParameterIfExists(AnimatorController controller, string name)
        {
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                if (controller.parameters[i].name != name)
                    continue;

                controller.RemoveParameter(i);
                return;
            }
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == name)
                    return child.state;
            }

            return sm.AddState(name);
        }

        /// <summary>
        /// Invector's ungrounded AnyState steals Jetpack Fly unless JetpackActive is off.
        /// </summary>
        private static void GateAirborneAnyState(
            AnimatorStateMachine root,
            AnimatorState flyState,
            AnimatorState landState)
        {
            AnimatorStateTransition[] transitions = root.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null)
                    continue;
                if (transition.destinationState == flyState ||
                    transition.destinationState == landState)
                    continue;

                bool requiresUngrounded = false;
                bool alreadyGated = false;
                AnimatorCondition[] conditions = transition.conditions;
                for (int c = 0; c < conditions.Length; c++)
                {
                    if (conditions[c].parameter == "IsGrounded" &&
                        conditions[c].mode == AnimatorConditionMode.IfNot)
                        requiresUngrounded = true;

                    if (conditions[c].parameter == "JetpackActive")
                        alreadyGated = true;
                }

                if (requiresUngrounded && !alreadyGated)
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "JetpackActive");
            }
        }

        private static void RemoveJetpackTransitions(
            AnimatorStateMachine root,
            AnimatorState flyState,
            AnimatorState landState)
        {
            for (int i = root.anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = root.anyStateTransitions[i];
                if (transition.destinationState == flyState ||
                    transition.destinationState == landState)
                    root.RemoveAnyStateTransition(transition);
            }

            RemoveOutgoing(flyState);
            RemoveOutgoing(landState);
        }

        private static void RemoveStateIfExists(AnimatorStateMachine sm, string stateName)
        {
            for (int i = sm.states.Length - 1; i >= 0; i--)
            {
                if (sm.states[i].state.name != stateName)
                    continue;

                Object.DestroyImmediate(sm.states[i].state, true);
            }
        }

        private static void RemoveOutgoing(AnimatorState state)
        {
            if (state == null)
                return;

            for (int i = state.transitions.Length - 1; i >= 0; i--)
                state.RemoveTransition(state.transitions[i]);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            string parameter,
            bool hasExitTime,
            float duration,
            float exitTime = 0.75f,
            bool isExit = false)
        {
            AnimatorStateTransition transition = isExit ? from.AddExitTransition() : from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.AddCondition(mode, 0f, parameter);
        }

        private static void AddAnyStateTransition(
            AnimatorStateMachine root,
            AnimatorState destination,
            AnimatorConditionMode mode,
            string parameter,
            float duration)
        {
            AnimatorStateTransition transition = root.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
            transition.AddCondition(mode, 0f, parameter);
        }
    }
}
