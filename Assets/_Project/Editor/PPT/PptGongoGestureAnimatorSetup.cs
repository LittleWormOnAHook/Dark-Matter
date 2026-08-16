using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Adds a masked upper-body layer with Point / Shrug states to GongoController for seated PPT NPCs.
    /// </summary>
    public static class PptGongoGestureAnimatorSetup
    {
        private const string ControllerPath = ProjectAssetPaths.AnimationsEnemies + "/GongoController.controller";
        private const string UpperBodyMaskPath =
            "Assets/Invector-3rdPersonController/Basic Locomotion/Animator/AvatarMasks/UpperBody.mask";
        private const string PointClipPath = "Assets/Animations/Mixamo Animations/Gestures/Pointing.fbx";
        private const string ShrugClipPath = "Assets/Animations/Mixamo Animations/Dialogue/Dismissing Gesture.fbx";
        private const string UpperBodyLayerName = "Upper Body";
        private const string PointStateName = "Point";
        private const string ShrugStateName = "Shrug";

        [MenuItem(DarkMatterGenesisEditorMenus.Ppt + "Add Upper Body Gesture Layer to Gongo Controller", false, 20)]
        public static void AddUpperBodyGestureLayer()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"PPT gesture setup: missing controller at {ControllerPath}");
                return;
            }

            AvatarMask upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (upperBodyMask == null)
            {
                Debug.LogError($"PPT gesture setup: missing AvatarMask at {UpperBodyMaskPath}");
                return;
            }

            AnimationClip pointClip = EnemyAnimationClipUtility.LoadEmbeddedAnimationClip(PointClipPath);
            AnimationClip shrugClip = EnemyAnimationClipUtility.LoadEmbeddedAnimationClip(ShrugClipPath);
            if (pointClip == null || shrugClip == null)
            {
                Debug.LogError("PPT gesture setup: failed to load Mixamo Point / Shrug clips.");
                return;
            }

            AnimatorControllerLayer upperBodyLayer = GetOrCreateUpperBodyLayer(controller, upperBodyMask);
            AnimatorStateMachine stateMachine = upperBodyLayer.stateMachine;
            AddOrUpdateState(stateMachine, PointStateName, pointClip, new Vector3(240f, 0f, 0f));
            AddOrUpdateState(stateMachine, ShrugStateName, shrugClip, new Vector3(240f, 72f, 0f));

            if (stateMachine.defaultState == null)
            {
                foreach (ChildAnimatorState child in stateMachine.states)
                {
                    if (child.state != null && child.state.name == PointStateName)
                    {
                        stateMachine.defaultState = child.state;
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"PPT gesture setup: '{UpperBodyLayerName}' layer on GongoController now has {PointStateName} + {ShrugStateName}. " +
                "Set PptNpcProfile pointGestureMode to UpperBodyOnly for seated NPCs.");
            EditorGUIUtility.PingObject(controller);
        }

        private static AnimatorControllerLayer GetOrCreateUpperBodyLayer(AnimatorController controller, AvatarMask mask)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == UpperBodyLayerName)
                {
                    controller.layers[i].avatarMask = mask;
                    controller.layers[i].blendingMode = AnimatorLayerBlendingMode.Override;
                    controller.layers[i].defaultWeight = 0f;
                    return controller.layers[i];
                }
            }

            AnimatorStateMachine stateMachine = new AnimatorStateMachine
            {
                name = UpperBodyLayerName,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = UpperBodyLayerName,
                defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = mask,
                stateMachine = stateMachine
            };

            controller.AddLayer(layer);
            return layer;
        }

        private static void AddOrUpdateState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Motion motion,
            Vector3 position)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state == null || child.state.name != stateName)
                    continue;

                child.state.motion = motion;
                return;
            }

            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = motion;
        }
    }
}
